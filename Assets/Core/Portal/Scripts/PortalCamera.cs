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

        [SerializeField]
        private int iterations = 7;

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
            if (!portals[0].IsPlaced || !portals[1].IsPlaced)
            {
                return;
            }

            if (portals[0].Renderer.isVisible)
            {
                portalCamera.targetTexture = tempTexture1;
                for (int i = iterations - 1; i >= 0; --i)
                {
                    RenderCamera(portals[0], portals[1], i, src);
                }
            }

            if (portals[1].Renderer.isVisible)
            {
                portalCamera.targetTexture = tempTexture2;
                for (int i = iterations - 1; i >= 0; --i)
                {
                    RenderCamera(portals[1], portals[0], i, src);
                }
            }
        }

        private void RenderCamera(Portal inPortal, Portal outPortal, int iterationID, ScriptableRenderContext src)
        {
            Transform inTransform = inPortal.transform;
            Transform outTransform = outPortal.transform;

            Transform cameraTransform = portalCamera.transform;
            cameraTransform.position = transform.position;
            cameraTransform.rotation = transform.rotation;

            for (int i = 0; i <= iterationID; ++i)
            {
                // Position the camera behind the other portal.
                Vector3 relativePos = inTransform.InverseTransformPoint(cameraTransform.position);
                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                cameraTransform.position = outTransform.TransformPoint(relativePos);

                // Rotate the camera to look through the other portal.
                Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * cameraTransform.rotation;
                relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
                cameraTransform.rotation = outTransform.rotation * relativeRot;
            }

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
