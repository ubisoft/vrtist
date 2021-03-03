/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
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

        public void  SetColumn(int index, Vector3 column)
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

            if(rotation.IsNegative())
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
    }
}
