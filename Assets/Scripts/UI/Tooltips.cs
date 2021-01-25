using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class Tooltips
    {
        public enum Location { Trigger, Grip, Primary, Secondary, Joystick }
        public enum Action { Push, HoldPush, Horizontal, HoldHorizontal, Vertical, HoldVertical, Joystick }

        private static float visibilityAngle = 30f;

        public static void SetText(VRDevice device, Location location, Action action, string text, bool visible = true)
        {
            Transform tooltip = GetTooltipTransform(device, location);
            if (null == tooltip) { return; }

            Transform textTransform = tooltip.Find("Canvas/Panel/Text");
            if (null != textTransform)
            {
                TextMeshProUGUI tmpro = textTransform.GetComponent<TextMeshProUGUI>();
                tmpro.text = text;
            }

            string icon = "";
            switch (action)
            {
                case Action.Push: icon = "action-push"; break;
                case Action.HoldPush: icon = "action-hold-push"; break;
                case Action.Horizontal: icon = "action-joystick-horizontal"; break;
                case Action.HoldHorizontal: icon = "action-hold-joystick-horizontal"; break;
                case Action.Vertical: icon = "action-joystick-vertical"; break;
                case Action.HoldVertical: icon = "action-hold-joystick-vertical"; break;
                case Action.Joystick: icon = "action-joystick"; break;
            }
            Transform imageTransform = tooltip.Find("Canvas/Panel/Image");
            if (null != imageTransform)
            {
                Image image = imageTransform.GetComponent<Image>();
                icon = "empty";  // don't use icon value yet, we don't have any image
                image.sprite = UIUtils.LoadIcon(icon);
            }

            tooltip.gameObject.SetActive(visible);
        }

        public static void SetVisible(VRDevice device, Location location, bool visible)
        {
            Transform tooltip = GetTooltipTransform(device, location);
            if (null == tooltip) { return; }

            tooltip.gameObject.SetActive(visible);
        }

        public static void HideAll(VRDevice device)
        {
            SetVisible(device, Location.Grip, false);
            SetVisible(device, Location.Trigger, false);
            SetVisible(device, Location.Primary, false);
            SetVisible(device, Location.Secondary, false);
            SetVisible(device, Location.Joystick, false);
        }

        public static void UpdateOpacity()
        {
            Transform camTransform = Camera.main.transform;
            Vector3 primaryTarget = -GlobalState.GetPrimaryControllerTransform().up;
            float primaryAngle = Vector3.Angle(primaryTarget, camTransform.forward);
            SetOpacity(VRDevice.PrimaryController, primaryAngle);

            Vector3 secondaryTarget = -GlobalState.GetSecondaryControllerTransform().up;
            float secondaryAngle = Vector3.Angle(secondaryTarget, camTransform.forward);
            SetOpacity(VRDevice.SecondaryController, secondaryAngle);
        }

        private static void SetOpacity(VRDevice device, float angle)
        {
            Transform controller = GlobalState.GetControllerTransform(device);
            if (null == controller) { return; }

            Transform tooltip = controller.Find("GripButtonAnchor/Tooltip");
            SetOpacity(tooltip, angle);
            tooltip = controller.Find("TriggerButtonAnchor/Tooltip");
            SetOpacity(tooltip, angle);
            tooltip = controller.Find("PrimaryButtonAnchor/Tooltip");
            SetOpacity(tooltip, angle);
            tooltip = controller.Find("SecondaryButtonAnchor/Tooltip");
            SetOpacity(tooltip, angle);
            tooltip = controller.Find("JoystickBaseAnchor/Tooltip");
            SetOpacity(tooltip, angle);
        }

        private static void SetOpacity(Transform tooltip, float angle)
        {
            if (null == tooltip) { return; }
            Image imagePanel = tooltip.Find("Canvas/Panel").GetComponent<Image>();
            Image image = tooltip.Find("Canvas/Panel/Image").GetComponent<Image>();
            TextMeshProUGUI text = tooltip.Find("Canvas/Panel/Text").GetComponent<TextMeshProUGUI>();

            float factor = 1f - Mathf.Min(visibilityAngle, angle) / visibilityAngle;

            Color color = image.color;
            color.a = 1f * factor;
            image.color = color;

            color = text.color;
            color.a = 1f * factor;
            text.color = color;

            color = imagePanel.color;
            color.a = 0.39f * factor;
            imagePanel.color = color;
        }

        private static Transform GetTooltipTransform(VRDevice device, Location location)
        {
            Transform controller = GlobalState.GetControllerTransform(device);
            if (null == controller) { return null; }

            Transform tooltip = null;
            switch (location)
            {
                case Location.Grip: tooltip = controller.Find("GripButtonAnchor/Tooltip"); break;
                case Location.Trigger: tooltip = controller.Find("TriggerButtonAnchor/Tooltip"); break;
                case Location.Primary: tooltip = controller.Find("PrimaryButtonAnchor/Tooltip"); break;
                case Location.Secondary: tooltip = controller.Find("SecondaryButtonAnchor/Tooltip"); break;
                case Location.Joystick: tooltip = controller.Find("JoystickBaseAnchor/Tooltip"); break;
            }
            return tooltip;
        }
    }
}
