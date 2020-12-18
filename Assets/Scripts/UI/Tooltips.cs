using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class Tooltips
    {
        //public enum Anchors { Trigger, Grip, Primary, Secondary, Joystick, JoystickClick, Pointer, System, Info }

        public enum Location { Trigger, Grip, Primary, Secondary, Joystick }
        public enum Action { Push, HoldPush, Horizontal, HoldHorizontal, Vertical, HoldVertical, Joystick }

        public static void SetText(VRDevice device, Location location, Action action, string text, int index = 0, bool visible = true)
        {
            Transform tooltip = GetTooltipTransform(device, location);
            if (null == tooltip) { return; }

            string textPath = "Canvas/Panel/Text";
            if (index > 0) { textPath += index; }
            Transform textTransform = tooltip.Find(textPath);
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
            string imagePath = "Canvas/Panel/Image";
            if (index > 0) { imagePath += index; }
            Transform imageTransform = tooltip.Find(imagePath);
            if (null != imageTransform)
            {
                Image image = imageTransform.GetComponent<Image>();
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
                case Location.Joystick: tooltip = controller.Find("JoystickButtonAnchor/Tooltip"); break;
            }
            return tooltip;
        }
    }
}
