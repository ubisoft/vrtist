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
    public class CommandClearRecursiveAnimations : ICommand
    {
        readonly GameObject gObject;
        readonly Dictionary<GameObject, AnimationSet> animationSets;

        public CommandClearRecursiveAnimations(GameObject obj)
        {
            gObject = obj;
            animationSets = new Dictionary<GameObject, AnimationSet>();
            RecursiveClear(obj.transform);
        }

        private void RecursiveClear(Transform target)
        {
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != anim)
                animationSets.Add(target.gameObject, anim);

            foreach (Transform child in target)
            {
                RecursiveClear(child);
            }
        }

        public override void Redo()
        {
            foreach (KeyValuePair<GameObject, AnimationSet> pair in animationSets)
            {
                SceneManager.ClearObjectAnimations(pair.Key, false);
            }
            GlobalState.Animation.onRemoveAnimation.Invoke(gObject);
        }


        public override void Undo()
        {
            foreach (KeyValuePair<GameObject, AnimationSet> pair in animationSets)
            {
                SceneManager.SetObjectAnimations(pair.Key, pair.Value, false);
            }
            GlobalState.Animation.onAddAnimation.Invoke(gObject);
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}