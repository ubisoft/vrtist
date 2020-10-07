using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public enum AnchorTypes
    {
        Left,
        Right,
        Top,
        Bottom
    }

    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public class WindowAnchor : MonoBehaviour
    {
        [SerializeField] private AnchorTypes anchorType;

        bool attached = false;
        bool otherAttached = false;
        bool gripped = false;
        bool previouslyGripped = false;
        Transform window;
        Transform target;
        GameObject anchorObject;
        GameObject anchoredObject;

        static HashSet<WindowAnchor> allAnchors = new HashSet<WindowAnchor>();

        void Start()
        {
            allAnchors.Add(this);

            anchorObject = transform.GetChild(0).gameObject;
            anchoredObject = transform.GetChild(1).gameObject;

            window = GetComponentInParent<UIHandle>().transform;
        }

        private void Update()
        {
            gripped = Selection.GetGrippedObject() == window.gameObject;

            // Window hidden
            if (window.localScale == Vector3.zero)
            {
                SetTarget(null); 
            }
            // Window visible
            else
            {
                if (gripped != previouslyGripped)
                {
                    ShowAllAnchors(gripped);
                }                

                // Attach when release grip
                if (null != target && !gripped && !attached)
                {
                    WindowAnchor targetAnchor = target.GetComponent<WindowAnchor>();
                    if (!targetAnchor.attached)
                    {
                        attached = true;
                        targetAnchor.otherAttached = true;
                        anchoredObject.SetActive(true);
                    }
                }

                // Move window with attached one
                if (attached)
                {
                    if (!gripped)
                    {
                        SnapToAnchor();
                    }
                    else
                    {
                        SetTarget(null);
                    }
                }
            }

            previouslyGripped = gripped;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("WindowAnchor"))
            {
                // We may have OnTriggerEnter from the other anchor
                if (!gripped || attached) { return; }

                WindowAnchor otherAnchor = other.gameObject.GetComponent<WindowAnchor>();
                if (otherAnchor.attached)
                    return;

                AnchorTypes otherAnchorType = otherAnchor.anchorType;
                switch (anchorType)
                {
                    case AnchorTypes.Left: if (otherAnchorType != AnchorTypes.Right) { return; } break;
                    case AnchorTypes.Right: if (otherAnchorType != AnchorTypes.Left) { return; } break;
                    case AnchorTypes.Bottom: if (otherAnchorType != AnchorTypes.Top) { return; } break;
                    case AnchorTypes.Top: if (otherAnchorType != AnchorTypes.Bottom) { return; } break;
                }
                SetTarget(other.transform);
                StartCoroutine(AnimAnchors());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("WindowAnchor"))
            {
                // We may have OnTriggerEnter from the other anchor
                if (!gripped) { return; }
                SetTarget(null);
                ShowAllAnchors(true);
            }
        }

        private void SetTarget(Transform target)
        {
            if(null != this.target)
            {
                this.target.GetComponent<WindowAnchor>().otherAttached = false;
            }
            this.target = target;
            attached = false;

            anchorObject.SetActive(null == target);
            anchoredObject.SetActive(null != target);
        }

        private void SnapToAnchor()
        {
            window.rotation = target.GetComponentInParent<UIHandle>().transform.rotation;
            Vector3 offset = target.transform.position - transform.position;
            window.position += offset;
        }

        private void ShowAllAnchors(bool show)
        {            
            foreach (WindowAnchor anchor in allAnchors)
            {
                if (!show)
                {
                    anchor.anchorObject.SetActive(false);
                    anchor.anchoredObject.SetActive(anchor.attached);
                }
                else
                {
                    anchor.anchorObject.SetActive(!anchor.otherAttached && !anchor.attached);
                    anchor.anchoredObject.SetActive(anchor.attached);
                }
                //anchor.anchorObject.SetActive(show && !otherAttached);
            }
        }

        private IEnumerator AnimAnchors()
        {
            float step = 0.01f;
            float factor = 1f;
            float threshold = 0.08f;
            Vector3 initScale = anchorObject.transform.localScale;
            //Vector3 targetInitScale = target.localScale;
            while (null != target && !attached)
            {
                yield return null;
                float offset = factor * step;
                target.localScale += new Vector3(offset, offset, offset);
                transform.localScale += new Vector3(offset, offset, offset);
                if (Mathf.Abs(transform.localScale.x - initScale.x) >= threshold)
                {
                    factor *= -1f;
                }
            }
            //target.localScale = targetInitScale;
            anchorObject.transform.localScale = initScale;
        }
    }
}
