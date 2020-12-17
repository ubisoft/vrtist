using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class ColorizeTrigger : MonoBehaviour
    {
        [SerializeField] private Colorize colorizer;

        public NavigationOptions navigation;

        private HashSet<GameObject> collidedObjects = new HashSet<GameObject>();
        private MeshRenderer paintingRenderer;

        private void Start()
        {
            paintingRenderer = gameObject.transform.Find("bucket/Plane").gameObject.GetComponent<MeshRenderer>();

            GlobalState.colorChangedEvent.AddListener(OnGlobalColorChanged);
            OnGlobalColorChanged(GlobalState.CurrentColor);
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.tag != "PhysicObject") { return; }

            collidedObjects.Add(other.gameObject);
        }

        private void OnTriggerExit(Collider other)
        {
            if(other.tag != "PhysicObject") { return; }

            collidedObjects.Remove(other.gameObject);
        }

        private void Update()
        {
            // Trigger action forwarded to Colorize script
            bool triggerState = VRInput.GetValue(VRInput.primaryController, CommonUsages.triggerButton);
            if(triggerState && collidedObjects.Count > 0) {
                // Process then remove objects from the list of collided objects in order to prevent
                // processing the same objects each frame (until trigger exit)
                colorizer.ProcessObjects(collidedObjects.ToList());
                collidedObjects.Clear();
            }

            // Scaling of collider
            if(navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 val = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if(val != Vector2.zero)
                {
                    float scale = gameObject.transform.localScale.x;
                    if(val.y > 0.3f) { scale += 0.001f; }
                    if(val.y < -0.3f) { scale -= 0.001f; }
                    scale = Mathf.Clamp(scale, 0.001f, 0.5f);
                    gameObject.transform.localScale = new Vector3(scale, scale, scale);
                }
            }
        }

        private void OnGlobalColorChanged(Color color)
        {
            gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", color);
            paintingRenderer.material.SetColor("_BaseColor", color);
        }
    }
}
