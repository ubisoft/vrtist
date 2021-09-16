/* MIT License
 *
 * Université de Rennes 1 / Invictus Project 
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframeTangent : ICommand
    {

        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly List<AnimationKey> oldKeys;
        readonly List<AnimationKey> newKeys;

        public CommandAddKeyframeTangent(GameObject obj, AnimatableProperty property, int frame, int startFrame, int endFrame, List<AnimationKey> keysChanged)
        {
            gObject = obj;
            this.property = property;
            oldKeys = new List<AnimationKey>();
            newKeys = keysChanged;

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet) return;
            Curve curve = animationSet.GetCurve(property);

            curve.GetTangentKeys(startFrame, endFrame, ref oldKeys);
        }

        public override void Redo()
        {
            oldKeys.ForEach(x =>
            {
                if (!newKeys.Exists(y => x.frame == y.frame))
                {
                    SceneManager.RemoveKeyframe(gObject, property, x, false, true);
                }
            });
            newKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x), false, true));
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            newKeys.ForEach(x =>
            {
                if (!oldKeys.Exists(y => x.frame == y.frame))
                {
                    SceneManager.RemoveKeyframe(gObject, property, x, false, true);
                }
            });
            oldKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x), false, true));
        }
    }
}
