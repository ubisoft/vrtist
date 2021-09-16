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
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
namespace VRtist
{

    public struct DsThetaJob : IJobParallelFor
    {
        [WriteOnly]
        public NativeArray<double> Js0;
        [WriteOnly]
        public NativeArray<double> Js1;
        [WriteOnly]
        public NativeArray<double> Js2;
        [WriteOnly]
        public NativeArray<double> Js3;
        [WriteOnly]
        public NativeArray<double> Js4;
        [WriteOnly]
        public NativeArray<double> Js5;
        [WriteOnly]
        public NativeArray<double> Js6;

        [ReadOnly]
        public NativeArray<KeyFrame> prevFrames;
        [ReadOnly]
        public NativeArray<KeyFrame> postFrames;
        [ReadOnly]
        public float dtheta;

        [ReadOnly]
        public Matrix4x4 ParentMatrix;
        [ReadOnly]
        public int frame;

        public void Execute(int index)
        {
            int objectIndex = index / 24;
            int executionIndex = index % 24;


            Matrix4x4 prevMatrix = GetFrameMatrix();
            Maths.DecomposeMatrix(prevMatrix, out Vector3 currentPosition, out Quaternion currentRotation, out Vector3 prevScale);
            KeyFrame prevFrame = prevFrames[0];
            KeyFrame nextFrame = postFrames[0];
            if (objectIndex < prevFrames.Length)
            {
                prevFrame = prevFrames[objectIndex];
                nextFrame = postFrames[objectIndex];
                switch (executionIndex)
                {
                    case 0: prevFrame.rotX.inTanX += dtheta; break;
                    case 1: prevFrame.rotX.inTanY += dtheta; break;
                    case 2: prevFrame.rotX.outTanX += dtheta; break;
                    case 3: prevFrame.rotX.outTanY += dtheta; break;

                    case 4: nextFrame.rotX.inTanX += dtheta; break;
                    case 5: nextFrame.rotX.inTanY += dtheta; break;
                    case 6: nextFrame.rotX.outTanX += dtheta; break;
                    case 7: nextFrame.rotX.outTanY += dtheta; break;

                    case 8: prevFrame.rotY.inTanX += dtheta; break;
                    case 9: prevFrame.rotY.inTanY += dtheta; break;
                    case 10: prevFrame.rotY.outTanX += dtheta; break;
                    case 11: prevFrame.rotY.outTanY += dtheta; break;

                    case 12: nextFrame.rotY.inTanX += dtheta; break;
                    case 13: nextFrame.rotY.inTanY += dtheta; break;
                    case 14: nextFrame.rotY.outTanX += dtheta; break;
                    case 15: nextFrame.rotY.outTanY += dtheta; break;

                    case 16: prevFrame.rotZ.inTanX += dtheta; break;
                    case 17: prevFrame.rotZ.inTanY += dtheta; break;
                    case 18: prevFrame.rotZ.outTanX += dtheta; break;
                    case 19: prevFrame.rotZ.outTanY += dtheta; break;

                    case 20: nextFrame.rotZ.inTanX += dtheta; break;
                    case 21: nextFrame.rotZ.inTanY += dtheta; break;
                    case 22: nextFrame.rotZ.outTanX += dtheta; break;
                    case 23: nextFrame.rotZ.outTanY += dtheta; break;
                }
            }
            else
            {
                switch (executionIndex)
                {
                    case 0: prevFrame.posX.inTanX += dtheta; break;
                    case 1: prevFrame.posX.inTanY += dtheta; break;
                    case 2: prevFrame.posX.outTanX += dtheta; break;
                    case 3: prevFrame.posX.outTanY += dtheta; break;

                    case 4: nextFrame.posX.inTanX += dtheta; break;
                    case 5: nextFrame.posX.inTanY += dtheta; break;
                    case 6: nextFrame.posX.outTanX += dtheta; break;
                    case 7: nextFrame.posX.outTanY += dtheta; break;

                    case 8: prevFrame.posY.inTanX += dtheta; break;
                    case 9: prevFrame.posY.inTanY += dtheta; break;
                    case 10: prevFrame.posY.outTanX += dtheta; break;
                    case 11: prevFrame.posY.outTanY += dtheta; break;

                    case 12: nextFrame.posY.inTanX += dtheta; break;
                    case 13: nextFrame.posY.inTanY += dtheta; break;
                    case 14: nextFrame.posY.outTanX += dtheta; break;
                    case 15: nextFrame.posY.outTanY += dtheta; break;

                    case 16: prevFrame.posZ.inTanX += dtheta; break;
                    case 17: prevFrame.posZ.inTanY += dtheta; break;
                    case 18: prevFrame.posZ.outTanX += dtheta; break;
                    case 19: prevFrame.posZ.outTanY += dtheta; break;

                    case 20: nextFrame.posZ.inTanX += dtheta; break;
                    case 21: nextFrame.posZ.inTanY += dtheta; break;
                    case 22: nextFrame.posZ.outTanX += dtheta; break;
                    case 23: nextFrame.posZ.outTanY += dtheta; break;
                }
            }

            Matrix4x4 matrix = GetFrameMatrix(objectIndex, prevFrame, nextFrame);
            Maths.DecomposeMatrix(matrix, out Vector3 plusPosition, out Quaternion plusRotation, out Vector3 plusScale);
            Js0[index] = (double)(plusPosition.x - currentPosition.x) * dtheta;
            Js1[index] = (double)(plusPosition.y - currentPosition.y) * dtheta;
            Js2[index] = (double)(plusPosition.z - currentPosition.z) * dtheta;
            Js3[index] = (double)(plusRotation.x - currentRotation.x) * dtheta;
            Js4[index] = (double)(plusRotation.y - currentRotation.y) * dtheta;
            Js5[index] = (double)(plusRotation.z - currentRotation.z) * dtheta;
            Js6[index] = (double)(plusRotation.w - currentRotation.w) * dtheta;
        }

        public Matrix4x4 GetFrameMatrix(int index, KeyFrame prevKey, KeyFrame nextKey)
        {
            Matrix4x4 trs = Matrix4x4.identity;// ParentMatrix;
            for (int i = 0; i < prevFrames.Length; i++)
            {
                if (i == index)
                {
                    trs = trs * FMatrix(prevKey, nextKey);
                }
                else
                {
                    trs = trs * FMatrix(prevFrames[i], postFrames[i]);
                }
            }
            return trs;
        }

        public Matrix4x4 GetFrameMatrix()
        {
            Matrix4x4 trs = Matrix4x4.identity;// ParentMatrix;
            for (int i = 0; i < prevFrames.Length; i++)
            {
                trs = trs * FMatrix(prevFrames[i], postFrames[i]);
            }
            return trs;
        }

        public Matrix4x4 FMatrix(KeyFrame prev, KeyFrame next)
        {
            float posX = CurveValue(prev.posX, next.posX, frame, prev.frame, next.frame);
            float posY = CurveValue(prev.posY, next.posY, frame, prev.frame, next.frame);
            float posZ = CurveValue(prev.posZ, next.posZ, frame, prev.frame, next.frame);
            float rotX = CurveValue(prev.rotX, next.rotX, frame, prev.frame, next.frame);
            float rotY = CurveValue(prev.rotY, next.rotY, frame, prev.frame, next.frame);
            float rotZ = CurveValue(prev.rotZ, next.rotZ, frame, prev.frame, next.frame);

            return Matrix4x4.TRS(new Vector3(posX, posY, posZ), Quaternion.Euler(new Vector3(rotX, rotY, rotZ)), Vector3.one);
        }

        public float CurveValue(KeyProperty prev, KeyProperty next, int frame, int prevFrame, int nextFrame)
        {
            Vector2 A = new Vector2(prevFrame, prev.value);
            Vector2 D = new Vector2(nextFrame, next.value);

            Vector2 B = A + new Vector2(prev.outTanX, prev.outTanY);
            Vector2 C = D - new Vector2(next.inTanX, next.inTanY);

            return Bezier.EvaluateBezier(A, B, C, D, frame);
        }

    }

    public struct KeyFrame
    {
        public KeyProperty posX;
        public KeyProperty posY;
        public KeyProperty posZ;
        public KeyProperty rotX;
        public KeyProperty rotY;
        public KeyProperty rotZ;

        public int frame;

        public static KeyFrame GetKey(AnimationSet set, int frame)
        {
            KeyProperty px = GetProperty(set, AnimatableProperty.PositionX, frame);
            KeyProperty py = GetProperty(set, AnimatableProperty.PositionY, frame);
            KeyProperty pz = GetProperty(set, AnimatableProperty.PositionZ, frame);
            KeyProperty rx = GetProperty(set, AnimatableProperty.RotationX, frame);
            KeyProperty ry = GetProperty(set, AnimatableProperty.RotationY, frame);
            KeyProperty rz = GetProperty(set, AnimatableProperty.RotationZ, frame);

            return new KeyFrame()
            {
                frame = frame,
                posX = px,
                posY = py,
                posZ = pz,
                rotX = rx,
                rotY = ry,
                rotZ = rz
            };
        }

        private static KeyProperty GetProperty(AnimationSet set, AnimatableProperty property, int frame)
        {
            Curve curve = set.GetCurve(property);
            if (curve.GetKeyIndex(frame, out int index))
            {

                KeyProperty key = new KeyProperty()
                {
                    value = curve.keys[index].value,
                    inTanX = curve.keys[index].inTangent.x,
                    inTanY = curve.keys[index].inTangent.y,
                    outTanX = curve.keys[index].outTangent.x,
                    outTanY = curve.keys[index].outTangent.y
                };
                return key;
            }
            else
            {
                KeyProperty key = new KeyProperty();
                return key;
            }
        }
    }

    public struct KeyProperty
    {
        public float value;
        public float inTanX;
        public float inTanY;
        public float outTanX;
        public float outTanY;
    }
}