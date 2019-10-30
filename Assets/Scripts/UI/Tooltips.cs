using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class Tooltips
    {
        public enum Anchors { Trigger, Grip, Primary, Secondary, Joystick, Pointer, System }

        private static GameObject tooltipPrefab = null;

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

            GameObject tooltip = GameObject.Instantiate(tooltipPrefab);

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

            // Set text
            TMPro.TextMeshProUGUI tmpro = tooltip.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            tmpro.text = text;

            return tooltip;
        }
    }
}