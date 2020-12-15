using UnityEngine;
using UnityEngine.Assertions;

namespace VRtist
{
    public class Tooltips
    {
        public enum Anchors { Trigger, Grip, Primary, Secondary, Joystick, JoystickClick, Pointer, System, Info }

        private static GameObject tooltipPrefab = null;

        public static GameObject FindTooltip(GameObject gObject, string name)
        {
            if (gObject.name == name)
                return gObject;

            for (int i = 0; i < gObject.transform.childCount; i++)
            {
                GameObject child = gObject.transform.GetChild(i).gameObject;
                GameObject tooltip = FindTooltip(child, name);
                if (tooltip)
                    return tooltip;
            }

            return null;
        }

        /// <summary>
        /// Create a tooltip as a GameObject attached to a controller's anchor (paint > right_controller > PrimaryButtonAnchor, for example)
        /// </summary>
        /// <param name="controller">"left_controller" or "right_controller" game object</param>
        /// <param name="anchor">Where to put the tooltip</param>
        /// <param name="text">Its text</param>
        /// <returns></returns>
        public static GameObject CreateTooltip(GameObject controller, Anchors anchor, string text)
        {
            Assert.IsTrue(controller.name == "right_controller" || controller.name == "left_controller", "Expected a controller");

            if (tooltipPrefab == null)
            {
                tooltipPrefab = Resources.Load<GameObject>("Prefabs/UI/Tooltip");
            }

            string tooltipName = anchor.ToString();
            GameObject tooltip = FindTooltip(controller, tooltipName);
            if (null == tooltip)
            {
                tooltip = GameObject.Instantiate(tooltipPrefab);
                tooltip.name = tooltipName;

                Transform frame = tooltip.transform.Find("Frame");
                Vector3 framePosition = frame.localPosition;
                Vector3 linePosition = new Vector3(-0.025f, 0f, 0f);  // for line renderer (go to the left of the anchor)
                float yOffset = 0.01f;
                bool hasLine = true;

                // Put the tooltip as a child of the controller's anchor
                // Default position.x is based on the right controller
                switch (anchor)
                {
                    case Anchors.Grip:
                        tooltip.transform.parent = controller.transform.Find("GripButtonAnchor");
                        break;
                    case Anchors.Joystick:
                        tooltip.transform.parent = controller.transform.Find("JoystickTopAnchor");
                        linePosition.x *= -1f;
                        framePosition.x *= -1f;
                        linePosition.y += 0.5f * yOffset;
                        framePosition.y += 0.5f * yOffset;
                        break;
                    case Anchors.JoystickClick:
                        tooltip.transform.parent = controller.transform.Find("JoystickBaseAnchor");
                        linePosition.x *= -1f;
                        framePosition.x *= -1f;
                        linePosition.y -= 1.5f * yOffset;
                        framePosition.y -= 1.5f * yOffset;
                        break;
                    case Anchors.Pointer:
                        tooltip.transform.parent = controller.transform.Find("FrontAnchor");
                        linePosition.x = 0f;
                        framePosition.x = 0f;
                        framePosition.y += yOffset;
                        break;
                    case Anchors.Primary:
                        tooltip.transform.parent = controller.transform.Find("PrimaryButtonAnchor");
                        break;
                    case Anchors.Secondary:
                        tooltip.transform.parent = controller.transform.Find("SecondaryButtonAnchor");
                        linePosition.y += yOffset;
                        framePosition.y += yOffset;
                        break;
                    case Anchors.Trigger:
                        tooltip.transform.parent = controller.transform.Find("TriggerButtonAnchor");
                        linePosition.x *= -1f;
                        framePosition.x *= -1f;
                        linePosition.y += 2.0f * yOffset;
                        framePosition.y += 2.0f * yOffset;
                        break;
                    case Anchors.System:
                        tooltip.transform.parent = controller.transform.Find("SystemButtonAnchor");
                        linePosition.x *= -1f;
                        framePosition.x *= -1f;
                        break;
                    case Anchors.Info:
                        tooltip.transform.parent = controller.transform.Find("DisplayAnchor");
                        framePosition = Vector3.zero;
                        hasLine = false;
                        break;
                }

                // Reset tooltip's transform after parent is set
                tooltip.transform.localPosition = Vector3.zero;
                tooltip.transform.localRotation = Quaternion.identity;
                tooltip.transform.localScale = Vector3.one;

                // Invert positions for left controller
                //bool rightHanded = GlobalState.Settings.rightHanded;
                //if ((rightHanded && controller.name == "left_controller") || (!rightHanded && controller.name == "right_controller"))
                if (controller.name == "left_controller")
                {
                    linePosition.x *= -1f;
                    framePosition.x *= -1f;
                }

                
                FreeDraw freeDraw = new FreeDraw();
                freeDraw.AddControlPoint(Vector3.zero, 0.00025f);
                freeDraw.AddControlPoint(linePosition, 0.00025f);
                MeshFilter meshFilter = tooltip.AddComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;
                mesh.Clear();
                mesh.vertices = freeDraw.vertices;
                mesh.normals = freeDraw.normals;
                mesh.triangles = freeDraw.triangles;

                // Set the frame position
                frame.localPosition = framePosition;
            }

            // Set text
            SetTooltipText(tooltip, text);

            // Show tooltip (if Create is used on an already existing tooltip, which may have be hidden).
            SetTooltipVisibility(tooltip, true);

            return tooltip;
        }

        public static void HideAllTooltips(GameObject controller)
        {
            Assert.IsTrue(controller.name == "right_controller" || controller.name == "left_controller", "Expected a controller");

            // foreach anchor in Anchors???
            SetTooltipVisibility(controller, Anchors.Grip, false);
            SetTooltipVisibility(controller, Anchors.Joystick, false);
            SetTooltipVisibility(controller, Anchors.Pointer, false);
            SetTooltipVisibility(controller, Anchors.Primary, false);
            SetTooltipVisibility(controller, Anchors.Secondary, false);
            SetTooltipVisibility(controller, Anchors.System, false);
            SetTooltipVisibility(controller, Anchors.Trigger, false);
        }

        public static void SetTooltipVisibility(GameObject controller, Anchors anchor, bool visible)
        {
            Assert.IsTrue(controller.name == "right_controller" || controller.name == "left_controller", "Expected a controller");

            string tooltipName = anchor.ToString();
            GameObject tooltip = FindTooltip(controller, tooltipName);
            if (null != tooltip)
            {
                SetTooltipVisibility(tooltip, visible);
            }
        }

        public static void SetTooltipVisibility(GameObject tooltip, bool visible)
        {
            tooltip.SetActive(visible);
        }

        public static void SetTooltipText(GameObject controller, Anchors anchor, string text)
        {
            Assert.IsTrue(controller.name == "right_controller" || controller.name == "left_controller", "Expected a controller");

            string tooltipName = anchor.ToString();
            GameObject tooltip = FindTooltip(controller, tooltipName);
            if (null != tooltip)
            {
                SetTooltipText(tooltip, text);
            }
        }

        public static void SetTooltipText(GameObject tooltip, string text)
        {
            TMPro.TextMeshProUGUI tmpro = tooltip.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = text;
        }

        public static string GetTooltipText(GameObject tooltip)
        {
            TMPro.TextMeshProUGUI tmpro = tooltip.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            return tmpro.text;
        }
    }
}
