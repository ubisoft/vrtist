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
    /// Command to ad a keyframe to a property of an object.
    /// </summary>
    public class CommandAddKeyframe : ICommand
    {
        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly AnimationKey oldAnimationKey = null;
        readonly AnimationKey newAnimationKey = null;
        readonly bool updateCurve = true;

        public CommandAddKeyframe(GameObject obj, AnimatableProperty property, int frame, float value, Interpolation interpolation, bool updateCurve = true)
        {
            gObject = obj;
            this.property = property;
            newAnimationKey = new AnimationKey(frame, value, interpolation);
            this.updateCurve = updateCurve;

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(obj);
            if (null == animationSet)
                return;

            Curve curve = animationSet.GetCurve(property);
            if (null == curve)
                return;

            curve.TryFindKey(frame, out oldAnimationKey);
        }

        public override void Undo()
        {
            SceneManager.RemoveKeyframe(gObject, property, newAnimationKey, updateCurve);

            if (null != oldAnimationKey)
            {
                SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(oldAnimationKey), updateCurve);
            }
        }

        public override void Redo()
        {
            SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(newAnimationKey), updateCurve);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}