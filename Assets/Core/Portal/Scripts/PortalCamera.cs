using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

namespace TheOneFramework.Portals
{
    // Attach this to the actual rendering Camera the player sees through (the one tagged
    // MainCamera / driven by CinemachineBrain) — not the Cinemachine virtual camera itself.
    public class PortalCamera : MonoBehaviour
    {
        [SerializeField]
        private Portal[] portals = new Portal[2];

        [SerializeField]
        private Camera portalCamera;

        [Tooltip("Maximum depth of the portal-inside-portal effect. Extra levels are only rendered when they're actually visible.")]
        [SerializeField]
        private int iterations = 7;

        [Tooltip("Shadows are one of the most expensive parts of each portal render. Untick to skip them in the portal view.")]
        [SerializeField]
        private bool renderShadowsInPortals = true;

        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        private readonly Plane[] frustumPlanes = new Plane[6];
        private readonly Plane[] recursionPlanes = new Plane[6];

        private RenderTexture tempTexture1;
        private RenderTexture tempTexture2;

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();

            tempTexture1 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            tempTexture2 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        }

        private void Start()
        {
            portals[0].Renderer.material.mainTexture = tempTexture1;
            portals[1].Renderer.material.mainTexture = tempTexture2;

            portalCamera.GetUniversalAdditionalCameraData().renderShadows = renderShadowsInPortals;
        }

        private void OnEnable()
        {
            RenderPipeline.beginCameraRendering += UpdateCamera;
        }

        private void OnDisable()
        {
            RenderPipeline.beginCameraRendering -= UpdateCamera;
        }

        private void UpdateCamera(ScriptableRenderContext src, Camera camera)
        {
            // beginCameraRendering fires for every camera - the Scene view, previews, and possibly
            // the portal camera's own renders. Only the player's camera should trigger portal
            // renders, otherwise each of those pays for the full set of portal renders again.
            if (camera != mainCamera)
            {
                return;
            }

            if (!portals[0].IsPlaced || !portals[1].IsPlaced)
            {
                return;
            }

            // Renderer.isVisible is true if *any* camera sees the portal (Scene view, shadow
            // casting, ...), so test against the player's camera frustum directly instead.
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);

            if (GeometryUtility.TestPlanesAABB(frustumPlanes, portals[0].Renderer.bounds))
            {
                portalCamera.targetTexture = tempTexture1;
                for (int i = GetNeededIterations(portals[0], portals[1]) - 1; i >= 0; --i)
                {
                    RenderCamera(portals[0], portals[1], i, src);
                }
            }

            if (GeometryUtility.TestPlanesAABB(frustumPlanes, portals[1].Renderer.bounds))
            {
                portalCamera.targetTexture = tempTexture2;
                for (int i = GetNeededIterations(portals[1], portals[0]) - 1; i >= 0; --i)
                {
                    RenderCamera(portals[1], portals[0], i, src);
                }
            }
        }

        // Each extra iteration is only needed to fill in the "portal seen inside a portal" image,
        // so stop as soon as the virtual camera can no longer see inPortal at all. In the common
        // case (portals not facing each other) that's a single render instead of `iterations`.
        private int GetNeededIterations(Portal inPortal, Portal outPortal)
        {
            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            for (int count = 1; count < iterations; ++count)
            {
                MoveThroughPortal(inPortal.transform, outPortal.transform, ref pos, ref rot);

                Matrix4x4 worldToCamera = Matrix4x4.TRS(pos, rot, Vector3.one).inverse;
                // Unity cameras look down -Z in view space.
                worldToCamera = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * worldToCamera;
                GeometryUtility.CalculateFrustumPlanes(mainCamera.projectionMatrix * worldToCamera, recursionPlanes);

                // Portal forward points into its wall, so the camera only sees its front when
                // looking along that direction (this rules out e.g. two portals on the same wall).
                bool facesCamera = Vector3.Dot(inPortal.transform.forward, inPortal.transform.position - pos) > 0.0f;

                if (!facesCamera || !GeometryUtility.TestPlanesAABB(recursionPlanes, inPortal.Renderer.bounds))
                {
                    return count;
                }
            }

            return iterations;
        }

        private static void MoveThroughPortal(Transform inTransform, Transform outTransform, ref Vector3 pos, ref Quaternion rot)
        {
            // Position the camera behind the other portal.
            Vector3 relativePos = inTransform.InverseTransformPoint(pos);
            relativePos = halfTurn * relativePos;
            pos = outTransform.TransformPoint(relativePos);

            // Rotate the camera to look through the other portal.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * rot;
            relativeRot = halfTurn * relativeRot;
            rot = outTransform.rotation * relativeRot;
        }

        private void RenderCamera(Portal inPortal, Portal outPortal, int iterationID, ScriptableRenderContext src)
        {
            Transform outTransform = outPortal.transform;

            Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            for (int i = 0; i <= iterationID; ++i)
            {
                MoveThroughPortal(inPortal.transform, outTransform, ref pos, ref rot);
            }

            portalCamera.transform.SetPositionAndRotation(pos, rot);

            // Set the camera's oblique view frustum so it only renders what's beyond the exit portal.
            Plane p = new Plane(-outTransform.forward, outTransform.position);
            Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
            Vector4 clipPlaneCameraSpace =
                Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;

            var newMatrix = mainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
            portalCamera.projectionMatrix = newMatrix;

            // RenderSingleCamera(context, camera) was marked obsolete in URP 17 in favour of
            // RenderPipeline.SubmitRenderRequest<UniversalRenderer.SingleCameraRequest>, but that
            // newer API renders standalone (it submits its own context) and can't be nested inside
            // an in-flight beginCameraRendering callback the way this portal recursion needs.
            // The obsolete call still works correctly in 17.6.0, so it is kept deliberately here.
#pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(src, portalCamera);
#pragma warning restore CS0618
        }
    }
}
