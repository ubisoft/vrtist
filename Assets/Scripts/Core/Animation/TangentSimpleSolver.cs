using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Profiling;

namespace VRtist
{

    public class TangentSimpleSolver
    {
        private Vector3 positionTarget;
        private Quaternion rotationTarget;
        public AnimationSet ObjectAnimation;
        private int currentFrame;
        private int startFrame;
        private int endFrame;
        private double tangentEnergy;

        public List<int> RequiredKeyframeIndices;

        struct State
        {
            public Vector3 position;
            public Vector3 euler_orientation;
            public int time;
        }


        public TangentSimpleSolver(Vector3 targetPosition, Quaternion targetRotation, AnimationSet animation, int frame, int start, int end, double tanEnergy)
        {
            positionTarget = targetPosition;
            rotationTarget = targetRotation;
            ObjectAnimation = animation;
            currentFrame = frame;
            startFrame = start;
            endFrame = end;
            tangentEnergy = tanEnergy;
        }



        public bool TrySolver()
        {
            ObjectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(startFrame, out int firstIndex);
            int firstFrame = ObjectAnimation.curves[AnimatableProperty.PositionX].keys[firstIndex].frame;
            ObjectAnimation.curves[AnimatableProperty.PositionX].GetKeyIndex(endFrame, out int lastIndex);
            int lastFrame = ObjectAnimation.curves[AnimatableProperty.PositionX].keys[lastIndex].frame;

            if (currentFrame < firstFrame) return false;
            if (currentFrame > lastFrame) return false;


            RequiredKeyframeIndices = FindRequiredTangents(firstFrame, lastFrame, ObjectAnimation.GetCurve(AnimatableProperty.PositionX));
            int K = RequiredKeyframeIndices.Count;
            int totalKeyframes = ObjectAnimation.GetCurve(AnimatableProperty.PositionX).keys.Count;
            int n = 6;
            int p = 24 * K;
            double[] theta = GetAllTangents(p, K, RequiredKeyframeIndices);
            double[,] Theta = ColumnArrayToArray(theta);
            State currentState = GetCurrentState(currentFrame);
            State desiredState = new State()
            {
                position = positionTarget,
                euler_orientation = rotationTarget.eulerAngles,
                time = currentFrame
            };


            double[,] Js = ds_dtheta(currentFrame, n, p, K, RequiredKeyframeIndices);
            double[,] DT_D = new double[p, p];
            for (int i = 0; i < p; i++)
            {
                DT_D[i, i] = 0d * 0d;
            }

            double[,] Delta_s_prime = new double[6, 1];
            for (int i = 0; i <= 2; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            for (int i = 3; i <= 5; i++)
            {
                Delta_s_prime[i, 0] = -Mathf.DeltaAngle(desiredState.euler_orientation[i - 3], currentState.euler_orientation[i - 3]);
            }

            double[,] TT_T = new double[p, p];
            for (int j = 0; j < p; j++)
            {
                TT_T[j, j] = 1d;
                if (j % 4 == 0 || j % 4 == 1)
                {
                    TT_T[j + 2, j] = -1d;
                }
                else
                {
                    TT_T[j - 2, j] = -1d;
                }
            }
            double wm = 100d;
            double wb = tangentEnergy;
            double wd = 1d;

            double[,] Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * wm, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Add(Maths.Multiply(2d * wd, DT_D), Maths.Multiply(2d * wb, TT_T))), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(p)));

            double[,] B_opt = Maths.Add(Maths.Multiply(-2d * wm, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime)), Maths.Multiply(2d * wb, Maths.Multiply(TT_T, Theta)));
            double[] b_opt = Maths.ArrayToColumnArray(B_opt);

            double[] delta_theta_0 = new double[p];
            double[] delta_theta;
            double[] s = new double[p];
            for (int i = 0; i < p; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }

            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);

            alglib.minqpsetscale(state_opt, s);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);


            double[] new_theta = new double[p];
            for (int i = 0; i < p; i++)
            {
                new_theta[i] = delta_theta[i] + theta[i];
            }

            for (int i = 0; i < p; i++)
            {
                if (System.Double.IsNaN(delta_theta[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < 6; i++)
            {

                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.curves[property];

                for (int k = 0; k < K; k++)
                {
                    Vector2 inTangent = new Vector2((float)new_theta[4 * (i * K + k) + 0], (float)new_theta[4 * (i * K + k) + 1]);
                    Vector2 outTangent = new Vector2((float)new_theta[4 * (i * K + k) + 2], (float)new_theta[4 * (i * K + k) + 3]);
                    ModifyTangents(curve, RequiredKeyframeIndices[k], inTangent, outTangent);
                }
            }
            return true;
        }

        private State GetCurrentState(int currentFrame)
        {
            float[] data = new float[6];
            for (int i = 0; i < data.Length; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                ObjectAnimation.GetCurve(property).Evaluate(currentFrame, out data[i]);
            }
            return new State()
            {
                position = new Vector3(data[0], data[1], data[2]),
                euler_orientation = new Vector3(data[3], data[4], data[5]),
                time = currentFrame
            };

        }

        private double[] GetAllTangents(int p, int K, List<int> requieredKeys)
        {
            double[] theta = new double[p];
            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.GetCurve(property);
                for (int k = 0; k < K; k++)
                {
                    AnimationKey key = curve.keys[requieredKeys[k]];
                    theta[4 * (i * K + k) + 0] = key.inTangent.x;
                    theta[4 * (i * K + k) + 1] = key.inTangent.y;
                    theta[4 * (i * K + k) + 2] = key.outTangent.x;
                    theta[4 * (i * K + k) + 3] = key.outTangent.y;
                }
            }

            return theta;
        }

        public void ModifyTangents(Curve curve, int index, Vector2 inTangent, Vector2 outTangent)
        {
            curve.keys[index].inTangent = inTangent;
            curve.keys[index].outTangent = outTangent;
            curve.ComputeCacheValuesAt(index);
        }

        private List<int> FindRequiredTangents(int firstFrame, int lastFrame, Curve curve)
        {
            curve.GetKeyIndex(firstFrame, out int firstKeyIndex);
            curve.GetKeyIndex(lastFrame, out int lastKeyIndex);
            List<int> keys = new List<int>() { firstKeyIndex, lastKeyIndex };

            return keys;
        }

        double[,] ColumnArrayToArray(double[] m)
        {
            int row = m.Length;
            double[,] response = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                response[i, 0] = m[i];
            }
            return response;
        }

        double[,] ds_dc(int frame, int n)
        {
            double[,] Js1 = new double[6, n];

            ObjectAnimation.curves[AnimatableProperty.PositionX].Evaluate(frame, out float x);
            ObjectAnimation.curves[AnimatableProperty.PositionY].Evaluate(frame, out float y);
            ObjectAnimation.curves[AnimatableProperty.PositionZ].Evaluate(frame, out float z);
            Vector3 sp = new Vector3(x, y, z);

            for (int j = 0; j < 6; j++)
            {
                if (j <= 2)
                {
                    Js1[j, j] = 1d;
                }
                else
                {
                    Vector3 v = new Vector3(0, 0, 0);
                    v[j - 3] = 1f;
                    Vector3 r = new Vector3(x, y, z);
                    Vector3 derive_position = Vector3.Cross(v, sp - r);
                    Js1[0, j] = derive_position[0];
                    Js1[1, j] = derive_position[1];
                    Js1[2, j] = derive_position[2];
                    Js1[3, j] = v[0];
                    Js1[4, j] = v[1];
                    Js1[5, j] = v[2];
                }
            }
            return Js1;
        }

        double[,] ds_dtheta(int frame, int n, int p, int K, List<int> requiredKeyframes)
        {
            double[,] Js1 = ds_dc(frame, n);
            double[,] Js2 = dc_dtheta(frame, n, p, K, requiredKeyframes);
            return Maths.Multiply(Js1, Js2);
        }

        double[,] dc_dtheta(int frame, int n, int p, int K, List<int> requiredKeyframeIndices)
        {

            double[,] Js2 = new double[n, p];
            float dtheta = 1;

            for (int i = 0; i < 6; i++)
            {
                AnimatableProperty property = (AnimatableProperty)i;
                Curve curve = ObjectAnimation.curves[property];
                for (int k = 0; k < K; k++)
                {
                    Vector2 inTangent = curve.keys[requiredKeyframeIndices[k]].inTangent;
                    Vector2 outTangent = curve.keys[requiredKeyframeIndices[k]].outTangent;
                    float c_plus, c_minus;

                    int j1 = 4 * (i * K + k);
                    inTangent.x += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    inTangent.x -= dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j1] = (double)((c_plus - c_minus) / dtheta);

                    int j2 = 4 * (i * K + k) + 1;
                    inTangent.y += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    inTangent.y -= dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j2] = (double)((c_plus - c_minus) / dtheta);

                    int j3 = 4 * (i * K + k) + 2;
                    outTangent.x += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    outTangent.x -= dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j3] = (double)((c_plus - c_minus) / dtheta);

                    int j4 = 4 * (i * K + k) + 3;
                    outTangent.y += dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_plus);
                    outTangent.y -= dtheta;
                    ModifyTangents(curve, requiredKeyframeIndices[k], inTangent, outTangent);
                    curve.Evaluate(frame, out c_minus);
                    Js2[i, j4] = (double)((c_plus - c_minus) / dtheta);
                }
            }
            return Js2;
        }
    }
}
