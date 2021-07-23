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

namespace VRtist
{
    public class LobbyTool : ToolBase
    {
        protected override void Init()
        {
            enableToggleTool = false;
            SetTooltips();
        }

        public override void SetTooltips()
        {
            base.SetTooltips();
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Primary, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Secondary, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Joystick, false);
        }

        protected override void DoUpdate()
        {
            // Nothing to do
        }
    }
}
