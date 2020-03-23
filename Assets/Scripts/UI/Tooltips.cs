using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class Tooltips
    {
        public enum Anchors { Trigger, Grip, Primary, Secondary, Joystick, Pointer, System }

        private static GameObject tooltipPrefab = null;
        private static GameObject displayPrefab = null;

        public static GameObject FindTooltip(GameObject gObject, string name)
        {
            if (gObject.name == name)
                return gObject;

            for(int i = 0; i < gObject.transform.childCount; i++)
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
            if(controller.name != "right_controller" && controller.name != "left_controller")
            {
                throw new System.Exception("Expected a prefab controller");
            }

            if(tooltipPrefab == null)
            {
                tooltipPrefab = (GameObject) Resources.Load("Prefabs/UI/Tooltip");
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
                        linePosition.y += yOffset;
                        framePosition.y += yOffset;
                        break;
                    case Anchors.System:
                        tooltip.transform.parent = controller.transform.Find("SystemButtonAnchor");
                        linePosition.x *= -1f;
                        framePosition.x *= -1f;
                        break;
                }

                // Reset tooltip's transform after parent is set
                tooltip.transform.localPosition = Vector3.zero;
                tooltip.transform.localRotation = Quaternion.identity;
                tooltip.transform.localScale = Vector3.one;

                // Invert positions for left controller
                if (controller.name == "left_controller")
                {
                    linePosition.x *= -1f;
                    framePosition.x *= -1f;
                }

                // Set the line renderer positions
                LineRenderer line = tooltip.GetComponent<LineRenderer>();
                line.SetPosition(0, Vector3.zero);
                line.SetPosition(1, linePosition);

                // Set the frame position
                frame.localPosition = framePosition;
            }

            // Set text
            SetTooltipText(tooltip, text);

            return tooltip;
        }

        public static GameObject CreateDisplay(GameObject controller, int slot, string text, string icon = "") {
            if(controller.name != "right_controller" && controller.name != "left_controller") {
                throw new System.Exception("Expected a prefab controller");
            }

            if(null == displayPrefab) {
                displayPrefab = (GameObject) Resources.Load("Prefabs/UI/DisplayTooltip");
            }

            string name = "displayInfo";
            GameObject display = FindTooltip(controller, name);
            if(null == display) {
                display = GameObject.Instantiate(displayPrefab);
                display.name = name;
                display.transform.parent = controller.transform.Find("DisplayAnchor");

                // Reset tooltip's transform after parent is set
                display.transform.localPosition = Vector3.zero;
                display.transform.localRotation = Quaternion.identity;
                display.transform.localScale = Vector3.one;
            }

            SetDisplaySlot(display, slot, text, icon);

            return display;
        }

        public static void SetDisplaySlot(GameObject display, int index, string text, string icon = "") {
            string slotName = $"Slot_{index}";
            Transform slotTransform = display.transform.Find($"Frame/Canvas/{slotName}");
            if(null == slotTransform) {
                return;
            }

            GameObject slot = slotTransform.gameObject;
            SetTooltipText(slot, text);
            SetSlotIcon(slot, icon);
        }

        public static void SetDisplaySlotText(GameObject display, int index, string text) {
            string slotName = $"Slot_{index}";
            Transform slotTransform = display.transform.Find($"Frame/Canvas/{slotName}");
            if(null == slotTransform) {
                return;
            }

            GameObject slot = slotTransform.gameObject;
            SetTooltipText(slot, text);
        }

        public static void SetTooltipVisibility(GameObject controller, Anchors anchor, bool visible)
        {
            if(controller.name != "right_controller" && controller.name != "left_controller")
            {
                throw new System.Exception("Expected a prefab controller");
            }

            string tooltipName = anchor.ToString();
            GameObject tooltip = FindTooltip(controller, tooltipName);
            if(null != tooltip)
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
            if(controller.name != "right_controller" && controller.name != "left_controller")
            {
                throw new System.Exception("Expected a prefab controller");
            }

            string tooltipName = anchor.ToString();
            GameObject tooltip = FindTooltip(controller, tooltipName);
            if(null != tooltip)
            {
                SetTooltipText(tooltip, text);
            }
        }

        public static void SetTooltipText(GameObject tooltip, string text)
        {
            TMPro.TextMeshProUGUI tmpro = tooltip.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = text;
        }

        public static void SetSlotIcon(GameObject slot, string icon) {
            GameObject imageObject = slot.transform.GetChild(0).gameObject;
            Image image = imageObject.GetComponent<Image>();
            if(null == image) { return; }

            if(icon.Length > 0) {
                Sprite sprite = Resources.Load<Sprite>(icon);
                if(null == sprite) { return; }
                imageObject.SetActive(true);
                image.sprite = sprite;
            } else {
                imageObject.SetActive(false);
            }
        }
    }
}
