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

    public class CommandStartFrame : ICommand
    {

        readonly GameObject gObject;
        readonly int offset;
        readonly List<AnimationSet> animations = new List<AnimationSet>();

        public CommandStartFrame(GameObject obj, int startFrame)
        {
            gObject = obj;

            GetAllAnimations(gObject.transform, animations);

            int firstFrame = int.MaxValue;
            animations.ForEach(x =>
            {
                int fframe = x.GetFirstFrame();
                if (fframe < firstFrame) firstFrame = fframe;
            });
            offset = startFrame - firstFrame;
        }

        private void GetAllAnimations(Transform target, List<AnimationSet> animations)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != animationSet) animations.Add(animationSet);
            foreach (Transform child in target)
            {
                GetAllAnimations(child, animations);
            }
        }



        public override void Redo()
        {
            animations.ForEach(x => x.SetStartOffset(offset));
            GlobalState.Animation.onStartOffsetChanged.Invoke(gObject);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            animations.ForEach(x => x.SetStartOffset(-offset));
            GlobalState.Animation.onStartOffsetChanged.Invoke(gObject);
        }
    }
}