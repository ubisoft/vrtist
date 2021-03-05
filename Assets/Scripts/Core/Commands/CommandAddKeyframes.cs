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

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to add keyframes to all supported properties of an object.
    /// </summary>
    public class CommandAddKeyframes : CommandGroup
    {
        readonly GameObject gObject;
        public CommandAddKeyframes(GameObject obj) : base("Add Keyframes")
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;
            int frame = GlobalState.Animation.CurrentFrame;

            new CommandAddKeyframe(gObject, AnimatableProperty.PositionX, frame, gObject.transform.localPosition.x, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionY, frame, gObject.transform.localPosition.y, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionZ, frame, gObject.transform.localPosition.z, interpolation).Submit();

            // convert to ZYX euler
            Vector3 angles = gObject.transform.localEulerAngles;
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationX, frame, angles.x, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationY, frame, angles.y, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationZ, frame, angles.z, interpolation).Submit();

            CameraController controller = gObject.GetComponent<CameraController>();
            LightController lcontroller = gObject.GetComponent<LightController>();

            if (null != controller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraFocal, frame, controller.focal, interpolation).Submit();
            }
            else if (null != lcontroller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.Power, frame, lcontroller.Power, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorR, frame, lcontroller.Color.r, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorG, frame, lcontroller.Color.g, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorB, frame, lcontroller.Color.b, interpolation).Submit();
            }
            else
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleX, frame, scale.x, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleY, frame, scale.y, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleZ, frame, scale.z, interpolation).Submit();
            }
        }

        public override void Undo()
        {
            base.Undo();
        }

        public override void Redo()
        {
            base.Redo();
        }
        public override void Submit()
        {
            base.Submit();
        }
    }
}
