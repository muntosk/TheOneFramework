using UnityEngine;

namespace TheOneFramework.Portals
{
    // Attach this to physics props (crates, cubes, ...) that should be able to travel through a Portal.
    // The player uses a separate warp component tied to its own controller instead of a Rigidbody.
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PortalableObject : MonoBehaviour, IPortalTraveller
    {
        public Transform Transform => transform;

        private GameObject cloneObject;

        private int inPortalCount = 0;

        private Portal inPortal;
        private Portal outPortal;
        private Collider wallCollider;

        private new Rigidbody rigidbody;
        protected new Collider collider;

        private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

        protected virtual void Awake()
        {
            cloneObject = new GameObject();
            cloneObject.SetActive(false);
            var meshFilter = cloneObject.AddComponent<MeshFilter>();
            var meshRenderer = cloneObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = GetComponent<MeshFilter>().mesh;
            meshRenderer.materials = GetComponent<MeshRenderer>().materials;
            cloneObject.transform.localScale = transform.localScale;

            rigidbody = GetComponent<Rigidbody>();
            collider = GetComponent<Collider>();
        }

        private void LateUpdate()
        {
            if (inPortal == null || outPortal == null)
            {
                return;
            }

            if (cloneObject.activeSelf && inPortal.IsPlaced && outPortal.IsPlaced)
            {
                var inTransform = inPortal.transform;
                var outTransform = outPortal.transform;

                // Update position of clone.
                Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
                relativePos = halfTurn * relativePos;
                cloneObject.transform.position = outTransform.TransformPoint(relativePos);

                // Update rotation of clone.
                Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
                relativeRot = halfTurn * relativeRot;
                cloneObject.transform.rotation = outTransform.rotation * relativeRot;
            }
            else
            {
                cloneObject.transform.position = new Vector3(-1000.0f, 1000.0f, -1000.0f);
            }
        }

        public void SetIsInPortal(Portal inPortal, Portal outPortal, Collider wallCollider)
        {
            this.inPortal = inPortal;
            this.outPortal = outPortal;
            this.wallCollider = wallCollider;

            Physics.IgnoreCollision(collider, wallCollider);

            cloneObject.SetActive(false);

            ++inPortalCount;
        }

        public void ExitPortal(Collider wallCollider)
        {
            Physics.IgnoreCollision(collider, wallCollider, false);
            --inPortalCount;

            if (inPortalCount == 0)
            {
                cloneObject.SetActive(false);
            }
        }

        // See PlayerPortalTraveller.Update() for why this re-checks every frame instead of just
        // ignoring the wall for as long as the object is anywhere inside the portal trigger volume:
        // a wall is usually one single collider, so collision can only be turned fully on/off for
        // it as a whole - this keeps that "off" state bounded to the portal's own rectangle.
        private void Update()
        {
            if (inPortal == null)
            {
                return;
            }

            bool withinPortalRect = IsWithinPortalRect(inPortal, transform.position);
            Physics.IgnoreCollision(collider, wallCollider, withinPortalRect);
        }

        private static bool IsWithinPortalRect(Portal portal, Vector3 worldPos)
        {
            Transform t = portal.transform;
            Vector3 offset = worldPos - t.position;

            float horiz = Vector3.Dot(offset, t.right);
            float vert = Vector3.Dot(offset, t.up);

            // A unit Quad's local bounds are +-0.5, so lossyScale directly gives world half-size
            // (same assumption Portal.cs itself uses for HalfWidth/HalfHeight).
            float halfWidth = t.lossyScale.x * 0.5f;
            float halfHeight = t.lossyScale.y * 0.5f;

            return Mathf.Abs(horiz) <= halfWidth && Mathf.Abs(vert) <= halfHeight;
        }

        public virtual void Warp()
        {
            var inTransform = inPortal.transform;
            var outTransform = outPortal.transform;

            // Update position of object.
            Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
            relativePos = halfTurn * relativePos;
            transform.position = outTransform.TransformPoint(relativePos);

            // Update rotation of object.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
            relativeRot = halfTurn * relativeRot;
            transform.rotation = outTransform.rotation * relativeRot;

            // Update velocity of rigidbody.
            Vector3 relativeVel = inTransform.InverseTransformDirection(rigidbody.linearVelocity);
            relativeVel = halfTurn * relativeVel;
            rigidbody.linearVelocity = outTransform.TransformDirection(relativeVel);

            // Swap portal references.
            var tmp = inPortal;
            inPortal = outPortal;
            outPortal = tmp;
        }
    }
}
