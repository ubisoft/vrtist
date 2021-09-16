/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
 * Université de Rennes 1 / Invictus Project
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
    /// Command to move a keyframe of an object.
    /// </summary>
    public class CommandMoveKeyframes : CommandGroup
    {
        readonly GameObject gObject;

        public CommandMoveKeyframes(GameObject obj, int frame, int newFrame) : base("Move Keyframes")
        {
            gObject = obj;
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(obj);
            if (null == animationSet)
                return;

            bool isHuman = obj.TryGetComponent(out RigController skinController);

            foreach (Curve curve in animationSet.curves.Values)
            {
                new CommandMoveKeyframe(gObject, curve.property, frame, newFrame).Submit();
            }

            if (isHuman)
            {
                foreach (Transform child in obj.transform)
                {
                    RecursiveMoveKeyframe(child, frame, newFrame);
                }
            }
        }

        private void RecursiveMoveKeyframe(Transform target, int frame, int newFrame)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (animationSet != null)
            {
                foreach (Curve curve in animationSet.curves.Values)
                {
                    new CommandMoveKeyframe(target.gameObject, curve.property, frame, newFrame).Submit();
                }
            }
            foreach (Transform child in target)
            {
                RecursiveMoveKeyframe(child, frame, newFrame);
            }
        }

        public override void Undo()
        {
            base.Undo();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }

        public override void Redo()
        {
            base.Redo();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }

        public override void Submit()
        {
            base.Submit();
            GlobalState.Animation.onChangeCurve.Invoke(gObject, AnimatableProperty.PositionX);
        }
    }
}
