using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    public class UIGrabber : UIElement
    {
        public GameObject prefab = null;
        
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
                // TODO: remove? est-ce qu'on utilise encore des UnityEvent<hash>
                if (ToolsUIManager.Instance != null)
                {
                    ToolsUIManager.Instance.RegisterUI3DObject(prefab);
                }
            }
        }

        private void OnValidate()
        {
            NeedsRebuild = true;
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (NeedsRebuild)
            {
                //RebuildMesh();
                UpdateLocalPosition();
                //UpdateAnchor();
                //UpdateChildren();
                ResetColor();
                NeedsRebuild = false;
            }
#endif
        }

        public override void ResetColor()
        {
            base.ResetColor();

            // Make the canvas pop front if Hovered.
            //Canvas c = GetComponentInChildren<Canvas>();
            //if (c != null)
            //{
            //    RectTransform rt = c.GetComponent<RectTransform>();
            //    if (rt != null)
            //    {
            //        rt.localPosition = Hovered ? new Vector3(0, 0, -0.003f) : Vector3.zero;
            //    }
            //}
        }

        private void OnTriggerEnter(Collider otherCollider)
        {
            if (NeedToIgnoreCollisionEnter())
                return;

            if (otherCollider.gameObject.name == "Cursor")
            {
                onClickEvent.Invoke();
                OnPush3DObject();
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
                OnRelease3DObject();
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
                OnStayOn3DObject();
            }
        }

        public void OnPush3DObject()
        {
            Pushed = true;
            ResetColor();

            transform.localPosition += new Vector3(0f, 0f, -0.02f); // avance vers nous, dnas le repere de la page (local -Z)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        public void OnRelease3DObject()
        {
            Pushed = false;
            ResetColor();

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
                foreach (Material material in meshRenderer.materials)
                {
                    material.SetColor("_BaseColor", color);
                }
            }
        }

        // --- RAY API ----------------------------------------------------

        public override void OnRayEnter()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();

            GoFrontAnimation();

            if (prefab != null)
            {
                int hash = prefab.GetHashCode();
                onEnterUI3DObject.Invoke(hash);
            }
        }

        public override void OnRayEnterClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true;
            Pushed = true;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();

            GoFrontAnimation();

            if (prefab != null)
            {
                int hash = prefab.GetHashCode();
                onEnterUI3DObject.Invoke(hash);
            }
        }

        public override void OnRayHover()
        {
            if (IgnoreRayInteraction())
                return;

            onHoverEvent.Invoke();

            Hovered = true;
            Pushed = false;
            ResetColor();

            RotateAnimation();
        }

        public override void OnRayHoverClicked()
        {
            if (IgnoreRayInteraction())
                return;

            onHoverEvent.Invoke();

            Hovered = true;
            Pushed = true;
            ResetColor();

            RotateAnimation();
        }

        public override void OnRayExit()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();

            GoBackAnimation();
            ResetRotation();

            if (prefab != null)
            {
                int hash = prefab.GetHashCode();
                onExitUI3DObject.Invoke(hash);
            }
        }

        public override void OnRayExitClicked()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = true; // exiting while clicking shows a hovered button.
            Pushed = false;
            VRInput.SendHaptic(VRInput.rightController, 0.005f, 0.005f);
            ResetColor();

            GoBackAnimation();

            if (prefab != null)
            {
                int hash = prefab.GetHashCode();
                onExitUI3DObject.Invoke(hash);
            }
        }

        public override void OnRayClick()
        {
            if (IgnoreRayInteraction())
                return;

            onClickEvent.Invoke();

            Hovered = true;
            Pushed = true;
            ResetColor();
        }

        public override void OnRayReleaseInside()
        {
            if (IgnoreRayInteraction())
                return;

            onReleaseEvent.Invoke();

            Hovered = true;
            Pushed = false;
            ResetColor();
        }

        public override void OnRayReleaseOutside()
        {
            if (IgnoreRayInteraction())
                return;

            Hovered = false;
            Pushed = false;
            ResetColor();

            ResetRotation();
        }

        // --- / RAY API ----------------------------------------------------

        public void GoFrontAnimation()
        {
            transform.localPosition += new Vector3(0f, 0f, -0.02f); // avance vers nous, dnas le repere de la page (local -Z)
            transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        }

        public void GoBackAnimation()
        {
            transform.localPosition += new Vector3(0f, 0f, +0.02f); // recule, dnas le repere de la page (local +Z)
            transform.localScale = Vector3.one;
        }

        public void RotateAnimation()
        {
            transform.localRotation *= Quaternion.Euler(0f, -3f, 0f); // rotate autour du Y du repere du parent (penche a 25, -35, 0)
        }

        public void ResetRotation()
        {
            transform.localRotation = Quaternion.Euler(25f, -35f, 0f);
        }
    }
}
