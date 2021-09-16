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
    public class Matrix3x3
    {
        Vector3[] col = new Vector3[3];
        public Matrix3x3()
        {
            col[0] = Vector3.right;
            col[1] = Vector3.up;
            col[2] = Vector3.forward;
        }
        public Matrix3x3(Matrix4x4 m)
        {
            Vector3 c0 = m.GetColumn(0);
            Vector3 c1 = m.GetColumn(1);
            Vector3 c2 = m.GetColumn(2);
            col[0] = new Vector3(c0.x, c0.y, c0.z);
            col[1] = new Vector3(c1.x, c1.y, c1.z);
            col[2] = new Vector3(c2.x, c2.y, c2.z);
        }

        public Vector3 GetColumn(int index)
        {
            return new Vector3(col[index].x, col[index].y, col[index].z);
        }

        public void SetColumn(int index, Vector3 column)
        {
            col[index] = column;
        }

        public void Negate()
        {
            for (int i = 0; i < 3; i++)
            {
                col[i].x *= -1f;
                col[i].y *= -1f;
                col[i].z *= -1f;
            }
        }

        public bool IsNegative()
        {
            Vector3 v = Vector3.Cross(col[0], col[1]);
            float d = Vector3.Dot(v, col[2]);
            return d < 0f;
        }

        public void GetRotationScale(out Matrix3x3 rotation, out Vector3 scale)
        {
            scale.x = GetColumn(0).magnitude;
            scale.y = GetColumn(1).magnitude;
            scale.z = GetColumn(2).magnitude;

            rotation = new Matrix3x3();
            rotation.SetColumn(0, GetColumn(0).normalized);
            rotation.SetColumn(1, GetColumn(1).normalized);
            rotation.SetColumn(2, GetColumn(2).normalized);

            if (rotation.IsNegative())
            {
                rotation.Negate();
                scale *= -1f;
            }
        }
    }

    public class Maths
    {
        // decompose matrix like blender does
        public static void DecomposeMatrix(Matrix4x4 m, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            Matrix3x3 m3 = new Matrix3x3(m);
            Matrix3x3 r;
            m3.GetRotationScale(out r, out scale);

            position = m.GetColumn(3);

            rotation = Quaternion.LookRotation(
                r.GetColumn(2),
                r.GetColumn(1)
                );
        }

        public static Quaternion GetRotationFromMatrix(Matrix4x4 m)
        {
            Vector3 p, s;
            Quaternion q;
            DecomposeMatrix(m, out p, out q, out s);
            return q;
        }

        public static Vector3 ThreeAxisRotation(Quaternion q)
        {
            return ThreeAxisRotation(2 * (q.x * q.y + q.w * q.z),
                             q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z,
                            -2 * (q.x * q.z - q.w * q.y),
                             2 * (q.y * q.z + q.w * q.x),
                             q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
        }

        public static Vector3 ThreeAxisRotation(float r11, float r12, float r21, float r31, float r32)
        {
            return new Vector3(Mathf.Atan2(r31, r32), Mathf.Asin(r21), Mathf.Atan2(r11, r12));
        }

        public static Vector3 GetFirstPerpVector(Vector3 v)
        {
            Vector3 result = new Vector3();
            // That's easy.
            if (v.x == 0.0f || v.y == 0.0f || v.z == 0.0f)
            {
                if (v.x == 0.0f)
                    result.x = 1.0f;
                else if (v.y == 0.0f)
                    result.y = 1.0f;
                else
                    result.z = 1.0f;
            }
            else
            {
                // If xyz is all set, we set the z coordinate as first and second argument .
                // As the scalar product must be zero, we add the negated sum of x and y as third argument
                result.x = v.z;      //scalp = z*x
                result.y = v.z;      //scalp = z*(x+y)
                result.z = -(v.x + v.y); //scalp = z*(x+y)-z*(x+y) = 0
                                         // Normalize vector
                result.Normalize();
            }
            return result;
        }

        public static double[,] Add(double[,] m1, double[,] m2)
        {
            int row1 = m1.GetUpperBound(0) + 1;
            int col1 = m1.GetUpperBound(1) + 1;
            int row2 = m2.GetUpperBound(0) + 1;
            int col2 = m2.GetUpperBound(1) + 1;

            if (row1 != row2 || col1 != col2)
            {
                Debug.Log("Matrix addition with uncompatible matrix sizes");
                return new double[1, 1];
            }

            else
            {
                double[,] response = new double[row1, col1];
                for (int i = 0; i < row1; i++)
                {
                    for (int j = 0; j < col1; j++)
                    {
                        response[i, j] = m1[i, j] + m2[i, j];
                    }
                }
                return response;
            }
        }

        public static double[,] Multiply(double alpha, double[,] m)
        {
            int row = m.GetUpperBound(0) + 1;
            int col = m.GetUpperBound(1) + 1;

            double[,] response = new double[row, col];
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    response[i, j] = alpha * m[i, j];
                }
            }
            return response;
        }

        public static double[,] ColumnArrayToArray(double[] m)
        {
            int row = m.Length;
            double[,] response = new double[row, 1];
            for (int i = 0; i < row; i++)
            {
                response[i, 0] = m[i];
            }
            return response;
        }

        public static double[,] Transpose(double[,] m)
        {
            int row = m.GetUpperBound(0) + 1;
            int col = m.GetUpperBound(1) + 1;
            double[,] response = new double[col, row];
            for (int i = 0; i < col; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    response[i, j] = m[j, i];
                }
            }
            return response;
        }

        public static double[,] Multiply(double[,] m1, double[,] m2)
        {
            int row1 = m1.GetUpperBound(0) + 1;
            int col1 = m1.GetUpperBound(1) + 1;
            int row2 = m2.GetUpperBound(0) + 1;
            int col2 = m2.GetUpperBound(1) + 1;

            if (col1 != row2)
            {
                Debug.Log("Matrix multiplication with uncompatible matrix sizes");
                return new double[1, 1];
            }
            else
            {
                double[,] response = new double[row1, col2];
                for (int i = 0; i < row1; i++)
                {
                    for (int j = 0; j < col2; j++)
                    {
                        double sum = 0d;
                        for (int k = 0; k < col1; k++)
                        {
                            sum += m1[i, k] * m2[k, j];
                        }
                        response[i, j] = sum;
                    }
                }
                return response;
            }
        }

        public static double[,] Identity(int p)
        {
            double[,] response = new double[p, p];
            for (int i = 0; i < p; i++)
            {
                response[i, i] = 1d;
            }
            return response;
        }

        public static double[] ArrayToColumnArray(double[,] m)
        {
            int row = m.GetUpperBound(0) + 1;
            int col = m.GetUpperBound(1) + 1;
            if (col != 1)
            {
                Debug.Log("Impossible to make a column array.");
                return new double[1];
            }
            else
            {
                double[] response = new double[row];
                for (int i = 0; i < row; i++)
                {
                    response[i] = m[i, 0];
                }
                return response;
            }
        }

    }

    public class Bezier
    {
        public static float EvaluateBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, int frame)
        {
            if ((float)frame == A.x)
                return A.y;

            if ((float)frame == D.x)
                return D.y;

            float pmin = 0;
            float pmax = 1;
            Vector2 avg = A;
            float dt = D.x - A.x;
            int safety = 0;
            while (dt > 0.1f)
            {
                float param = (pmin + pmax) * 0.5f;
                avg = CubicBezier(A, B, C, D, param);
                if (avg.x < frame)
                {
                    pmin = param;
                }
                else
                {
                    pmax = param;
                }
                dt = Mathf.Abs(avg.x - (float)frame);
                if (safety > 1000)
                {
                    Debug.LogError("bezier job error");
                    break;
                }
                else safety++;
            }
            return avg.y;
        }

        public static Vector2 CubicBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, float t)
        {
            float invT1 = 1 - t;
            float invT2 = invT1 * invT1;
            float invT3 = invT2 * invT1;

            float t2 = t * t;
            float t3 = t2 * t;

            return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
        }

        public static float CubicBezier(float A, float B, float C, float D, float t)
        {
            float invT1 = 1 - t;
            float invT2 = invT1 * invT1;
            float invT3 = invT2 * invT1;

            float t2 = t * t;
            float t3 = t2 * t;

            return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
        }

    }



}
