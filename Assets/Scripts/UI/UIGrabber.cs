using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class UIGrabber : UIElement
    {
        //public Deformer deformer = null;
        public GameObject prefab = null;
        public Color hoverColor = new Color(0f, 167f / 255f, 1f);
        //public Color baseColor = new Color(186f / 255f, 186f / 255f, 186 / 255f);

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public GameObjectHashChangedEvent onEnterUI3DObject = null;
        public GameObjectHashChangedEvent onExitUI3DObject = null;
        public UnityEvent onHoverEvent = null;
        public UnityEvent onClickEvent = null;
        public UnityEvent onReleaseEvent = null;

        void Start()
        {
            if (prefab == null)
            {
                Debug.LogWarning("No Prefab set for 3d Object.");
            }
            else
            {
                ToolsUIManager.Instance.RegisterUI3DObject(prefab);
            }

            onClickEvent.AddListener(OnPush3DObject);
            onReleaseEvent.AddListener(OnRelease3DObject);
            onHoverEvent.AddListener(OnStayOn3DObject);
        }

        public override void UpdateLocalPosition()
        {
            // just to avoid grabbing the UIElement shit, for the moment.
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                if (prefab != null)
                {
                    int hash = prefab.GetHashCode();
                    onEnterUI3DObject.Invoke(hash);
                }
            }
        }

        private void OnTriggerExit(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionExit())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onReleaseEvent.Invoke();
                if (prefab != null)
                {
                    int hash = prefab.GetHashCode();
                    onExitUI3DObject.Invoke(hash);
                }
            }
        }

        private void OnTriggerStay(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionStay())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onHoverEvent.Invoke();
            }
        }

        public void OnPush3DObject()
        {
            SetColor(hoverColor);

            transform.localPosition += new Vector3(0f, 0f, -0.02f); // avance vers nous, dnas le repere de la page (local -Z)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        public void OnRelease3DObject()
        {
            SetColor(BaseColor);

            transform.localPosition += new Vector3(0f, 0f, +0.02f); // recule, dnas le repere de la page (local +Z)
            transform.localScale = Vector3.one;
        }

        public void OnStayOn3DObject()
        {
            transform.localRotation *= Quaternion.Euler(0f, -3f, 0f); // rotate autour du Y du repere du parent (penche a 25, -35, 0)
        }

        // Handles multi-mesh and multi-material per mesh.
        public override void SetColor(Color color)
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                Material[] materials = meshRenderer.materials;
                foreach (Material material in materials)
                {
                    material.SetColor("_BaseColor", color);
                }
            }
        }

        public override bool HandlesCursorBehavior() { return true; }
        public override void HandleCursorBehavior(Vector3 worldCursorColliderCenter, ref Transform cursorShapeTransform)
        {
            cursorShapeTransform.position = transform.position;
            cursorShapeTransform.rotation = transform.parent.rotation;
        }
    }
}
