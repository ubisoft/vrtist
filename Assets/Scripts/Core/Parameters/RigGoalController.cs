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
    public class RigGoalController : MonoBehaviour
    {
        public List<Transform> PathToRoot = new List<Transform>();
        public List<AnimationSet> AnimToRoot = new List<AnimationSet>();
        public AnimationSet Animation;
        public RigController RootController;

        public float stiffness;
        public bool IsGoal;
        public bool ShowCurve;
        public Vector3 LowerAngleBound;
        public Vector3 UpperAngleBound;
        public Renderer MeshRenderer;
        public Collider goalCollider;

        public void SetPathToRoot(RigController controller, List<Transform> path)
        {
            path.ForEach(x =>
            {
                AnimationSet anim = GlobalState.Animation.GetObjectAnimation(x.gameObject);
                PathToRoot.Add(x);

                AnimToRoot.Add(anim);
            });
            if (PathToRoot.Count == 0) PathToRoot.Add(transform);
            Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            RootController = controller;
        }

        public Vector3 FramePosition(int frame)
        {
            if (null == Animation) Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            if (null == Animation) return Vector3.zero;

            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(RootController.gameObject);
            Matrix4x4 trsMatrix = RootController.transform.parent.localToWorldMatrix;
            if (null != rootAnimation) trsMatrix = trsMatrix * rootAnimation.GetTRSMatrix(frame);
            else trsMatrix = trsMatrix * Matrix4x4.TRS(RootController.transform.localPosition, RootController.transform.localRotation, RootController.transform.localScale);

            if (PathToRoot.Count > 1)
            {
                for (int i = 0; i < PathToRoot.Count; i++)
                {
                    if (null != AnimToRoot[i])
                        trsMatrix = trsMatrix * AnimToRoot[i].GetTRSMatrix(frame);
                }
            }
            trsMatrix = trsMatrix * Animation.GetTRSMatrix(frame);

            Maths.DecomposeMatrix(trsMatrix, out Vector3 parentPosition, out Quaternion quaternion, out Vector3 scale);
            return parentPosition;
        }

        public Matrix4x4 MatrixAtFrame(int frame)
        {
            if (null == Animation) Animation = GlobalState.Animation.GetObjectAnimation(this.gameObject);
            if (null == Animation) return Matrix4x4.identity;

            AnimationSet rootAnimation = GlobalState.Animation.GetObjectAnimation(RootController.gameObject);
            Matrix4x4 trsMatrix = RootController.transform.parent.localToWorldMatrix;
            if (null != rootAnimation) trsMatrix = trsMatrix * rootAnimation.GetTRSMatrix(frame);
            else trsMatrix = trsMatrix * Matrix4x4.TRS(RootController.transform.localPosition, RootController.transform.localRotation, RootController.transform.localScale);

            if (PathToRoot.Count > 1)
            {
                for (int i = 0; i < PathToRoot.Count; i++)
                {
                    trsMatrix = trsMatrix * AnimToRoot[i].GetTRSMatrix(frame);
                }
            }
            trsMatrix = trsMatrix * Animation.GetTRSMatrix(frame);
            return trsMatrix;
        }

        public void CheckAnimations()
        {
            AnimToRoot.Clear();
            PathToRoot.ForEach(x =>
            {
                AnimToRoot.Add(GlobalState.Animation.GetOrCreateObjectAnimation(x.gameObject));
            });
            Animation = GlobalState.Animation.GetObjectAnimation(gameObject);
        }

        public void UseGoal(bool state)
        {
            ShowRenderer(state);
            UseCollider(state);
        }

        private void ShowRenderer(bool state)
        {
            if (null != MeshRenderer) MeshRenderer.enabled = state;
        }
        private void UseCollider(bool state)
        {
            if (null != goalCollider) goalCollider.enabled = state;
        }
    }
}
