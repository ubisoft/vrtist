/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
            Transform tooltip = GlobalState.GetTooltipTransform(device, location);
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
            Transform tooltip = GlobalState.GetTooltipTransform(device, location);
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
            Vector3 primaryTarget = -GlobalState.GetPrimaryControllerUp();
            float primaryAngle = Vector3.Angle(primaryTarget, camTransform.forward);
            SetOpacity(VRDevice.PrimaryController, primaryAngle);

            Vector3 secondaryTarget = -GlobalState.GetSecondaryControllerUp();
            float secondaryAngle = Vector3.Angle(secondaryTarget, camTransform.forward);
            SetOpacity(VRDevice.SecondaryController, secondaryAngle);
        }

        private static void SetOpacity(VRDevice device, float angle)
        {
            Transform tooltip = GlobalState.GetTooltipTransform(device, Location.Grip);
            SetOpacity(tooltip, angle);
            tooltip = GlobalState.GetTooltipTransform(device, Location.Trigger);
            SetOpacity(tooltip, angle);
            tooltip = GlobalState.GetTooltipTransform(device, Location.Primary);
            SetOpacity(tooltip, angle);
            tooltip = GlobalState.GetTooltipTransform(device, Location.Secondary);
            SetOpacity(tooltip, angle);
            tooltip = GlobalState.GetTooltipTransform(device, Location.Joystick);
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

        public static void InitSecondaryTooltips()
        {
            SetText(VRDevice.SecondaryController, Tooltips.Location.Trigger, Tooltips.Action.HoldPush, "Open Palette");
            SetText(VRDevice.SecondaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Undo");
            SetText(VRDevice.SecondaryController, Tooltips.Location.Secondary, Tooltips.Action.Push, "Redo");
            SetText(VRDevice.SecondaryController, Tooltips.Location.Joystick, Tooltips.Action.Push, "Reset");
        }
    }
}
