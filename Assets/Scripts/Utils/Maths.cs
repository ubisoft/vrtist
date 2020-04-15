using System.Collections;
using System.Collections.Generic;
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
    }
}
