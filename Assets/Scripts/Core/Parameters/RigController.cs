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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class RigController : ParametersController
    {

        public SkinnedMeshRenderer SkinMesh;
        public Transform RootObject;
        public BoxCollider Collider;

        public void Start()
        {
            GlobalState.Animation.onFrameEvent.AddListener(RefreshCollider);
            RefreshCollider(GlobalState.Animation.CurrentFrame);
        }

        public void RefreshCollider(int frame)
        {
            Collider.center = RootObject.localPosition + SkinMesh.localBounds.center;
            Collider.size = SkinMesh.localBounds.size;
        }

        public void OnDisable()
        {
            GlobalState.Animation.onFrameEvent.RemoveListener(RefreshCollider);
        }

        internal void GetKeyList(SortedList<int, List<Dopesheet.AnimKey>> keys)
        {
            List<Curve> childCurves = new List<Curve>();
            RecursiveAnimation(childCurves, transform);

            childCurves.ForEach(curve =>
            {
                foreach (AnimationKey key in curve.keys)
                {
                    if (!keys.TryGetValue(key.frame, out List<Dopesheet.AnimKey> keylist))
                    {
                        keylist = new List<Dopesheet.AnimKey>();
                        keys[key.frame] = keylist;
                    }
                    keylist.Add(new Dopesheet.AnimKey(key.value, key.interpolation));
                }
            });
        }

        private void RecursiveAnimation(List<Curve> curves, Transform target)
        {
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(target.gameObject);
            if (null != anim) curves.Add(anim.GetCurve(AnimatableProperty.PositionX));
            foreach (Transform child in target)
            {
                RecursiveAnimation(curves, child);
            }
        }
    }
}