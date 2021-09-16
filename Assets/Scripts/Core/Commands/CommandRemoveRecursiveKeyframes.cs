/* MIT License
 *
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{

    public class CommandRemoveRecursiveKeyframes : CommandGroup
    {
        readonly GameObject gObject;


        public CommandRemoveRecursiveKeyframes(GameObject obj) : base("Remove Keyframes")
        {
            gObject = obj;
            int frame = GlobalState.Animation.CurrentFrame;
            RecursiveRemove(obj.transform, frame);
        }

        public void RecursiveRemove(Transform target, int frame)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != animationSet)
            {
                foreach (Curve curve in animationSet.curves.Values)
                {
                    if (curve.HasKeyAt(frame)) new CommandRemoveKeyframe(target.gameObject, curve.property, frame, false).Submit();
                }
            }
            foreach (Transform child in target)
            {
                RecursiveRemove(child, frame);
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