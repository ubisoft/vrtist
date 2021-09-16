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
    public class TangentRigSolver
    {
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private List<AnimationSet> animationList;
        private List<RigGoalController> controllers;
        private AnimationSet objectAnimation;
        private int animationCount;
        private IEnumerator Execution;

        private int currentFrame;
        private int firstFrame;
        private int lastFrame;

        private List<Curve> curves;

        struct State
        {
            public Vector3 position;
            public Quaternion rotation;
            public int time;

            public override string ToString()
            {
                return "position : " + position + " rotation : " + rotation + " time : " + time;
            }
        }

        struct JobData
        {
            public List<NativeArray<double>> jsValues;
            public DsThetaJob Job;
            public JobHandle Handle;
            public NativeArray<KeyFrame> prevFrames;
            public NativeArray<KeyFrame> postFrames;
        }

        private JobData jobData;
        private bool isJobActive = false;

        private State currentState;
        private State targetState;

        public List<int> requiredKeyframe;

        Vector2[,] curvesMinMax;

        double tangentContinuity;

        int paramCount,
            keyCount;

        double[,] Q_opt,
            Stiffnes_D,
            Continuity_T,
            Delta_s_prime,
            Theta;
        double[] b_opt,
            delta_theta_0,
            lowerBound,
            upperBound,
            scale,
            delta_theta,
            theta;

        public TangentRigSolver(Vector3 targetPosition, Quaternion targetRotation, AnimationSet objectAnim, List<AnimationSet> animation, int frame, int startFrame, int endFrame, double continuity)
        {
            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
            animationList = new List<AnimationSet>();
            controllers = new List<RigGoalController>();
            curves = new List<Curve>();
            GetCurves(objectAnim, animation);
            objectAnimation = objectAnim;
            animationCount = animationList.Count;
            tangentContinuity = continuity;
            currentFrame = frame;
            firstFrame = startFrame;
            lastFrame = endFrame;
        }

        private void GetCurves(AnimationSet objectAnim, List<AnimationSet> animation)
        {
            //Rotation curves for each animated element in hierarchy
            animation.ForEach(anim =>
            {
                if (null != anim)
                {
                    animationList.Add(anim);
                    controllers.Add(anim.transform.GetComponent<RigGoalController>());

                    curves.Add(anim.GetCurve(AnimatableProperty.RotationX));
                    curves.Add(anim.GetCurve(AnimatableProperty.RotationY));
                    curves.Add(anim.GetCurve(AnimatableProperty.RotationZ));
                }
            });

            animationList.Add(objectAnim);
            controllers.Add(objectAnim.transform.GetComponent<RigGoalController>());
            //Object rotation curves
            curves.Add(objectAnim.GetCurve(AnimatableProperty.RotationX));
            curves.Add(objectAnim.GetCurve(AnimatableProperty.RotationY));
            curves.Add(objectAnim.GetCurve(AnimatableProperty.RotationZ));
            //Position curves for root
            curves.Add(animationList[0].GetCurve(AnimatableProperty.PositionX));
            curves.Add(animationList[0].GetCurve(AnimatableProperty.PositionY));
            curves.Add(animationList[0].GetCurve(AnimatableProperty.PositionZ));
        }

        public bool NextStep()
        {
            if (Execution == null)
            {
                Execution = Step();
                return true;
            }
            return Execution.MoveNext();
        }

        public IEnumerator Step()
        {
            yield return Setup();
            yield return Compute();
            yield return Apply();
            yield return false;
        }

        public bool Setup()
        {
            Curve rotXCurve = objectAnimation.GetCurve(AnimatableProperty.RotationX);
            rotXCurve.GetKeyIndex(firstFrame, out int firstIndex);
            rotXCurve.GetKeyIndex(lastFrame, out int lastIndex);

            if (currentFrame < firstFrame) return false;
            if (currentFrame > lastFrame) return false;

            requiredKeyframe = new List<int>() { firstFrame, lastFrame };
            keyCount = requiredKeyframe.Count;
            int curveCount = curves.Count;
            //number of curve * (in tangent x , in tangent y, out tangent x, out tangent y) * (k- , k+)
            paramCount = curveCount * 4 * 2;

            ds_thetaJob(paramCount, keyCount);

            theta = new double[paramCount];
            Stiffnes_D = new double[paramCount, paramCount];
            Continuity_T = new double[paramCount, paramCount];
            curvesMinMax = new Vector2[curveCount, 2];
            lowerBound = new double[paramCount];
            upperBound = new double[paramCount];
            scale = new double[paramCount];
            delta_theta_0 = new double[paramCount];

            //Hierarchy rotation curves
            for (int animIndex = 0; animIndex < animationCount; animIndex++)
            {
                for (int curve = 0; curve < 3; curve++)
                {
                    int curveIndex = animIndex * 3 + curve;
                    AnimationKey previous1Key = curves[curveIndex].keys[firstIndex];
                    AnimationKey previous2Key = firstIndex > 0 ? curves[curveIndex].keys[firstIndex - 1] : new AnimationKey(previous1Key.frame, previous1Key.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                    AnimationKey next1Key = curves[curveIndex].keys[lastIndex];
                    AnimationKey next2key = lastIndex < curves[curveIndex].keys.Count - 1 ? curves[curveIndex].keys[lastIndex + 1] : new AnimationKey(next1Key.frame, next1Key.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                    GetTangents(curveIndex * 8, previous1Key, next1Key);
                    curvesMinMax[curveIndex, 0] = GetMinMax(previous2Key, previous1Key);
                    curvesMinMax[curveIndex, 1] = GetMinMax(next1Key, next2key);

                    float Min = controllers[animIndex].LowerAngleBound[curve];
                    float Max = controllers[animIndex].UpperAngleBound[curve];
                    FillLowerBounds(curveIndex, previous1Key, next1Key, Min, Max);
                    FillUpperBounds(curveIndex, previous1Key, next1Key, previous2Key, next2key, Min, Max);
                    GetContinuity(curveIndex * 8, Min, Max);
                    //k- in x, k- in y, k- out x, k- out y, k+ in x, k+ in y, k+ out x, k+ out y
                    for (int tan = 0; tan < 8; tan++)
                    {
                        int tanIndice = curveIndex * 8 + tan;
                        Stiffnes_D[tanIndice, tanIndice] = animIndex == animationCount - 1 ? 0 : controllers[animIndex].stiffness;
                        scale[tanIndice] = 1d;
                        delta_theta_0[tanIndice] = 0;
                    }
                }
            }

            //Root position curves
            int aIndex = animationCount;
            for (int curve = 0; curve < 3; curve++)
            {
                int curveIndex = aIndex * 3 + curve;
                AnimationKey previous1Key = curves[curveIndex].keys[firstIndex];
                AnimationKey previous2Key = firstIndex > 0 ? curves[curveIndex].keys[firstIndex - 1] : new AnimationKey(previous1Key.frame, previous1Key.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                AnimationKey next1Key = curves[curveIndex].keys[lastIndex];
                AnimationKey next2Key = lastIndex < curves[curveIndex].keys.Count - 1 ? curves[curveIndex].keys[lastIndex + 1] : new AnimationKey(next1Key.frame, next1Key.value, inTangent: Vector2.zero, outTangent: Vector2.zero);
                GetTangents(curveIndex * 8, previous1Key, next1Key);
                curvesMinMax[curveIndex, 0] = GetMinMax(previous2Key, previous1Key);
                curvesMinMax[curveIndex, 1] = GetMinMax(next1Key, next2Key);
                GetContinuity(curveIndex * 8);
                for (int tan = 0; tan < 8; tan++)
                {
                    int tanIndice = curveIndex * 8 + tan;
                    Stiffnes_D[tanIndice, tanIndice] = controllers[0].stiffness;
                    lowerBound[tanIndice] = -10;
                    upperBound[tanIndice] = 10;
                    scale[tanIndice] = 1d;
                    delta_theta_0[tanIndice] = 0;
                }

            }
            currentState = GetCurrentState(currentFrame);
            targetState = new State()
            {
                position = targetPosition,
                rotation = targetRotation,
                time = currentFrame

            };

            Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = targetState.position[i] - currentState.position[i];
            }
            if ((currentState.rotation * Quaternion.Inverse(targetState.rotation)).w < 0)
                targetState.rotation = new Quaternion(-targetState.rotation.x, -targetState.rotation.y, -targetState.rotation.z, -targetState.rotation.w);
            for (int i = 0; i < 4; i++)
            {
                Delta_s_prime[i + 3, 0] = targetState.rotation[i] - currentState.rotation[i];
            }

            Theta = Maths.ColumnArrayToArray(theta);
            return true;
        }


        public bool Compute()
        {
            double targetW = 20;
            double continuityW = tangentContinuity;
            double stiffnessW = 50;

            double[,] Js = ThetaFromJob(paramCount);

            Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * targetW, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Add(Maths.Multiply(2d * stiffnessW, Stiffnes_D),
                Maths.Multiply(2d * continuityW, Continuity_T))), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(paramCount)));

            double[,] B_opt = Maths.Add(Maths.Multiply(-2d * targetW, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime)), Maths.Multiply(2d * continuityW, Maths.Multiply(Continuity_T, Theta)));
            b_opt = Maths.ArrayToColumnArray(B_opt);

            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(paramCount, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);

            alglib.minqpsetscale(state_opt, scale);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            return true;
        }
        public bool Apply()
        {
            double[] new_theta = new double[paramCount];
            for (int i = 0; i < paramCount; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }
            for (int l = 0; l < animationCount; l++)
            {
                AnimationSet currentAnim = animationList[l];
                for (int i = 0; i < 3; i++)
                {

                    AnimatableProperty property = (AnimatableProperty)i + 3;
                    Curve curve = currentAnim.GetCurve(property);

                    for (int k = 0; k < keyCount; k++)
                    {
                        curve.GetKeyIndex(requiredKeyframe[k], out int index);
                        Vector2 inTangent = new Vector2((float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 0], (float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 1]);
                        Vector2 outTangent = new Vector2((float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 2], (float)new_theta[12 * keyCount * l + 4 * (i * keyCount + k) + 3]);
                        ModifyTangents(curve, index, inTangent, outTangent);
                    }
                }
            }
            for (int i = 3; i < 6; i++)
            {
                Curve curve = animationList[0].GetCurve((AnimatableProperty)i - 3);

                for (int k = 0; k < keyCount; k++)
                {
                    curve.GetKeyIndex(requiredKeyframe[k], out int index);
                    Vector2 inTangent = new Vector2((float)new_theta[12 * keyCount * animationCount + 4 * ((i - 3) * keyCount + k) + 0], (float)new_theta[12 * keyCount * animationCount + 4 * ((i - 3) * keyCount + k) + 1]);
                    Vector2 outTangent = new Vector2((float)new_theta[12 * keyCount * animationCount + 4 * ((i - 3) * keyCount + k) + 2], (float)new_theta[12 * keyCount * animationCount + 4 * ((i - 3) * keyCount + k) + 3]);
                    ModifyTangents(curve, index, inTangent, outTangent);
                }
            }

            return true;
        }

        public void ClearJob()
        {
            Debug.Log("clear job");
            if (isJobActive)
            {
                jobData.Handle.Complete();
                for (int v = 0; v < 7; v++)
                {
                    jobData.jsValues[v].Dispose();
                }
                jobData.postFrames.Dispose();
                jobData.prevFrames.Dispose();
                isJobActive = false;
            }
        }

        /// <summary>
        /// Fill lower bounds array with delta from current values
        /// </summary>
        private void FillLowerBounds(int curveIndex, AnimationKey previous1Key, AnimationKey next1Key, float Min, float Max)
        {
            //k- in.x
            lowerBound[curveIndex * 8 + 0] = 0 - previous1Key.inTangent.x;
            //k- in.y
            lowerBound[curveIndex * 8 + 1] = -Mathf.Max(0, Max - Mathf.Max(curvesMinMax[curveIndex, 0].x, curvesMinMax[curveIndex, 0].y));
            //k- out.x
            lowerBound[curveIndex * 8 + 2] = 0 - previous1Key.outTangent.x;
            //k- out.y
            lowerBound[curveIndex * 8 + 3] = Mathf.Min(0, (4 / 3f) * (Min - (previous1Key.value + (3 / 4f) * previous1Key.outTangent.y)));
            //k+ in.x
            lowerBound[curveIndex * 8 + 4] = 0 - next1Key.inTangent.x;
            //k+ in.y
            lowerBound[curveIndex * 8 + 5] = Mathf.Min(0, -(4 / 3f) * (Max - (next1Key.value - (3 / 4f) * next1Key.inTangent.y)));
            //k+ out.x
            lowerBound[curveIndex * 8 + 6] = 0 - next1Key.outTangent.x;
            //k+ out.y
            lowerBound[curveIndex * 8 + 7] = Mathf.Min(0, Min - Mathf.Min(curvesMinMax[curveIndex, 1].x, curvesMinMax[curveIndex, 1].y));
        }

        /// <summary>
        /// Fill upper bounds array with delta from current values
        /// </summary>
        private void FillUpperBounds(int curveIndex, AnimationKey previous1Key, AnimationKey next1Key, AnimationKey previous2Key, AnimationKey next2Key, float Min, float Max)
        {
            //k- in.x
            upperBound[curveIndex * 8 + 0] = previous1Key.frame - previous2Key.frame - previous1Key.inTangent.x;
            //k- in.y
            upperBound[curveIndex * 8 + 1] = -Mathf.Min(0, Min - Mathf.Min(curvesMinMax[curveIndex, 0].x, curvesMinMax[curveIndex, 0].y));
            //k- out.x
            upperBound[curveIndex * 8 + 2] = currentFrame - previous1Key.frame - previous1Key.outTangent.x;
            //k- out.y
            upperBound[curveIndex * 8 + 3] = Mathf.Max(0, (4 / 3f) * (Max - (previous1Key.value + (3 / 4f) * previous1Key.outTangent.y)));
            //k+ in.x
            upperBound[curveIndex * 8 + 4] = next1Key.frame - currentFrame - next1Key.inTangent.x;
            //k+ in.y
            upperBound[curveIndex * 8 + 5] = Mathf.Max(0, -(4 / 3f) * (Min - (next1Key.value - (3 / 4f) * next1Key.inTangent.y)));
            //k+ out.x
            upperBound[curveIndex * 8 + 6] = next2Key.frame - next1Key.frame - next1Key.outTangent.x;
            //k+ out.y
            upperBound[curveIndex * 8 + 7] = Mathf.Max(0, Max - Mathf.Max(curvesMinMax[curveIndex, 1].x, curvesMinMax[curveIndex, 1].y));
        }

        /// <summary>
        /// To preserv curve bounds befor and after we modify continuity depending on min and max values on each segments
        /// /// </summary>
        private void GetContinuity(int ac, float Min, float Max)
        {
            double continuity;

            //for previous segment
            if (theta[ac + 3] <= 0) continuity = Max == 0 ? 0 : Mathf.Clamp((float)-lowerBound[ac + 1] / (float)Max, 0.001f, 1);
            else continuity = Min == 0 ? 0 : Mathf.Clamp((float)-upperBound[ac + 1] / (float)Min, 0.001f, 1);


            for (int j = 0; j < 4; j++)
            {
                int indice = ac + j;
                Continuity_T[indice, indice] = continuity;
                if (indice % 4 == 0 || j % 4 == 1) Continuity_T[indice + 2, indice] = -continuity;
                else Continuity_T[indice - 2, indice] = -continuity;
            }

            //for next segment
            if (theta[ac + 5] >= 0) continuity = Max == 0 ? 0 : Mathf.Clamp((float)upperBound[ac + 7] / (float)Max, 0.001f, 1);
            else continuity = Min == 0 ? 0 : Mathf.Clamp((float)lowerBound[ac + 7] / (float)Min, 0.001f, 1);

            for (int j = 4; j < 8; j++)
            {
                int indice = ac + j;
                Continuity_T[indice, indice] = continuity;
                if (indice % 4 == 0 || indice % 4 == 1) Continuity_T[indice + 2, indice] = -continuity;
                else Continuity_T[indice - 2, indice] = -continuity;
            }
        }
        private void GetContinuity(int ac)
        {

            for (int i = 0; i < 8; i++)
            {
                int j = (ac) + i;
                Continuity_T[j, j] = 1;
                if (j % 4 == 0 || j % 4 == 1)
                {
                    Continuity_T[j + 2, j] = -1d;
                }
                else
                {
                    Continuity_T[j - 2, j] = -1d;
                }
            }
        }


        void ds_thetaJob(int p, int K)
        {
            float dtheta = 1;
            jobData = new JobData();
            jobData.jsValues = new List<NativeArray<double>>();
            for (int i = 0; i < 7; i++)
            {
                jobData.jsValues.Add(new NativeArray<double>(12 * 2 * animationCount + 24, Allocator.TempJob));
            }
            Matrix4x4 parentMatrix = animationList[0].transform.parent.localToWorldMatrix;

            jobData.prevFrames = new NativeArray<KeyFrame>(animationCount, Allocator.TempJob);
            jobData.postFrames = new NativeArray<KeyFrame>(animationCount, Allocator.TempJob);

            for (int l = 0; l < animationCount; l++)
            {
                jobData.prevFrames[l] = KeyFrame.GetKey(animationList[l], requiredKeyframe[0]);
                jobData.postFrames[l] = KeyFrame.GetKey(animationList[l], requiredKeyframe[1]);
            }

            jobData.Job = new DsThetaJob()
            {
                Js0 = jobData.jsValues[0],
                Js1 = jobData.jsValues[1],
                Js2 = jobData.jsValues[2],
                Js3 = jobData.jsValues[3],
                Js4 = jobData.jsValues[4],
                Js5 = jobData.jsValues[5],
                Js6 = jobData.jsValues[6],
                dtheta = dtheta,
                frame = currentFrame,
                ParentMatrix = parentMatrix,
                postFrames = jobData.postFrames,
                prevFrames = jobData.prevFrames
            };

            jobData.Handle = jobData.Job.Schedule(24 * animationCount, 48);
            isJobActive = true;
        }

        double[,] ThetaFromJob(int p)
        {
            double[,] Js = new double[7, p];

            jobData.Handle.Complete();
            for (int i = 0; i < p; i++)
            {
                for (int v = 0; v < 7; v++)
                {
                    Js[v, i] = jobData.jsValues[v][i];
                }
            }
            jobData.jsValues.ForEach(x => x.Dispose());
            jobData.jsValues.Clear();
            jobData.postFrames.Dispose();
            jobData.prevFrames.Dispose();
            isJobActive = false;

            return Js;
        }

        private void GetTangents(int ac, AnimationKey previous1Key, AnimationKey next1Key)
        {
            theta[ac + 0] = previous1Key.inTangent.x;
            theta[ac + 1] = previous1Key.inTangent.y;
            theta[ac + 2] = previous1Key.outTangent.x;
            theta[ac + 3] = previous1Key.outTangent.y;
            theta[ac + 4] = next1Key.inTangent.x;
            theta[ac + 5] = next1Key.inTangent.y;
            theta[ac + 6] = next1Key.outTangent.x;
            theta[ac + 7] = next1Key.outTangent.y;
        }

        private State GetCurrentState(int currentFrame)
        {
            Matrix4x4 currentMatrix = FrameMatrix(currentFrame, animationList);
            Maths.DecomposeMatrix(currentMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);
            return new State()
            {
                position = pos,
                rotation = rot,
                time = currentFrame
            };
        }

        #region Tools

        private Vector2 GetMinMax(AnimationKey previous1Key, AnimationKey next1Key)
        {
            float A = previous1Key.value;
            float B = A + previous1Key.outTangent.y;
            float D = next1Key.value;
            float C = D - next1Key.inTangent.y;

            float a = -A + (3 * B) - (3 * C) + D;
            float b = (3 * A) - (6 * B) + (3 * C);
            float c = (-3 * A) + (3 * B);

            float tMin = 0;
            float tMax = 1;

            if (a != 0 && ((b * b) - 3 * a * c) > 0)
            {
                tMin = (-b - Mathf.Sqrt((b * b) - 3 * a * c)) / (3 * a);
                tMax = (-b + Mathf.Sqrt((b * b) - 3 * a * c)) / (3 * a);
            }
            float MinValue = Bezier.CubicBezier(A, B, C, D, Mathf.Clamp01(tMin));
            float MaxValue = Bezier.CubicBezier(A, B, C, D, Mathf.Clamp01(tMax));
            return new Vector2(MinValue, MaxValue);
        }


        public Matrix4x4 FrameMatrix(int frame, List<AnimationSet> animations)
        {
            Matrix4x4 trsMatrix = Matrix4x4.identity;

            for (int i = 0; i < animationList.Count; i++)
            {
                trsMatrix = trsMatrix * GetBoneMatrix(animations[i], frame);
            }
            return trsMatrix;
        }

        private Matrix4x4 GetBoneMatrix(AnimationSet anim, int frame)
        {
            if (null == anim) return Matrix4x4.identity;
            Vector3 position = Vector3.zero;
            Curve posx = anim.GetCurve(AnimatableProperty.PositionX);
            Curve posy = anim.GetCurve(AnimatableProperty.PositionY);
            Curve posz = anim.GetCurve(AnimatableProperty.PositionZ);
            if (null != posx && null != posy && null != posz)
            {
                if (posx.Evaluate(frame, out float px) && posy.Evaluate(frame, out float py) && posz.Evaluate(frame, out float pz))
                {
                    position = new Vector3(px, py, pz);
                }
            }
            Quaternion rotation = Quaternion.identity;
            Curve rotx = anim.GetCurve(AnimatableProperty.RotationX);
            Curve roty = anim.GetCurve(AnimatableProperty.RotationY);
            Curve rotz = anim.GetCurve(AnimatableProperty.RotationZ);
            if (null != posx && null != roty && null != rotz)
            {
                if (rotx.Evaluate(frame, out float rx) && roty.Evaluate(frame, out float ry) && rotz.Evaluate(frame, out float rz))
                {
                    rotation = Quaternion.Euler(rx, ry, rz);
                }
            }
            Vector3 scale = Vector3.one;
            Curve scalex = anim.GetCurve(AnimatableProperty.ScaleX);
            Curve scaley = anim.GetCurve(AnimatableProperty.ScaleY);
            Curve scalez = anim.GetCurve(AnimatableProperty.ScaleZ);
            if (null != scalex && null != scaley && null != scalez)
            {
                if (scalex.Evaluate(frame, out float sx) && scaley.Evaluate(frame, out float sy) && scalez.Evaluate(frame, out float sz))
                {
                    scale = new Vector3(sx, sy, sz);
                }
            }
            return Matrix4x4.TRS(position, rotation, scale);
        }

        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
        }

        #endregion
    }
}
