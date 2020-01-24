using Newtonsoft.Json;
using System.Collections;
using UnityEngine;

// Generates mesh arrays from free drawing

namespace VRtist
{

    public class FreeDraw
    {
        public Vector3[] controlPoints;
        public float[] controlPointsRadius;

        private Vector3[] linePoints;
        private int[] linePointIndices;
        private float[] lineRadius;
        private Vector3[] controlPointNormals;

        public Vector3[] vertices;
        public Vector3[] normals;
        public int[] triangles;

        Vector3 prevControlPoint;
        public Matrix4x4 matrix;

        public FreeDraw()
        {
            Reset();
        }
        public FreeDraw(Vector3[] points, float[] radius)
        {
            Reset();
            matrix = Matrix4x4.identity;

            int count = points.Length;
            for (int i = 0; i < count; i++)
            {
                AddControlPoint(points[i], radius[i]);
            }
        }

        public void AddFlatLineControlPoint(Vector3 nextPoint, Vector3 nextNormal, float length)
        {
            Vector3 next = matrix.MultiplyPoint(nextPoint);
            Vector3 normal = matrix.MultiplyVector(nextNormal);

            length *= matrix.lossyScale.x;

            int size = controlPoints.Length;

            if (size > 1 && Vector3.Distance(prevControlPoint, next) < 0.01)
            {
                size--;
            }
            else
            {
                prevControlPoint = next;
            }

            System.Array.Resize(ref controlPoints, size + 1);
            System.Array.Resize(ref controlPointNormals, size + 1);
            System.Array.Resize(ref controlPointsRadius, size + 1);

            System.Array.Resize(ref vertices, 2 * (size + 1));
            System.Array.Resize(ref normals, 2 * (size + 1));
            System.Array.Resize(ref triangles, 6 * size);

            controlPoints[size] = next;
            controlPointNormals[size] = normal;
            controlPointsRadius[size] = length;

            if (size == 0)
            {
                Vector3 c = Vector3.Cross(normal, Vector3.forward).normalized;
                vertices[0] = next + c * length;
                vertices[1] = next - c * length;
                normals[0] = normal;
                normals[1] = normal;
            }
            else
            {
                Vector3 direction = (next - controlPoints[size - 1]).normalized;
                Vector3 c = Vector3.Cross(normal, direction).normalized;
                float prevLength = controlPointsRadius[size - 1];
                vertices[2 * (size - 1)] = controlPoints[size - 1] + c * prevLength;
                vertices[2 * (size - 1) + 1] = controlPoints[size - 1] - c * prevLength;

                vertices[2 * size] = next + c * length;
                vertices[2 * size + 1] = next - c * length;
                normals[2 * size] = normal;
                normals[2 * size + 1] = normal;

                int triIndex = 6 * (size - 1);
                triangles[triIndex + 0] = 2 * size - 2;
                triangles[triIndex + 1] = 2 * size - 1;
                triangles[triIndex + 2] = 2 * size + 1;

                triangles[triIndex + 3] = 2 * size - 2;
                triangles[triIndex + 4] = 2 * size + 1;
                triangles[triIndex + 5] = 2 * size + 0;
            }

        }

        public void AddControlPoint(Vector3 nextPoint, float nextPointRadius)
        {
            Vector3 next = matrix.MultiplyPoint(nextPoint);
            float radius = matrix.lossyScale.x * nextPointRadius;

            int ANZ = 8;  // number of vertices per circle
            int size = controlPoints.Length;

            // if new control point is too close to previous control point then replace it.
            if (size > 1 && Vector3.Distance(prevControlPoint, next) < Mathf.Abs(radius))
            {
                size--;
            }
            else
            {
                prevControlPoint = next;
            }

            System.Array.Resize(ref controlPoints, size + 1);
            System.Array.Resize(ref controlPointsRadius, size + 1);
            System.Array.Resize(ref linePointIndices, linePointIndices.Length + 1);

            controlPoints[size] = next;
            controlPointsRadius[size] = radius;

            if (size == 0)
                return;

            float prevPrevPrevRadius = 0;
            float prevPrevRadius = 0;
            float prevRadius = controlPointsRadius[size - 1];

            if (size >= 3)
            {
                prevPrevPrevRadius = controlPointsRadius[size - 3];
            }

            if (size >= 2)
            {
                prevPrevRadius = controlPointsRadius[size - 2];
            }

            int prevIndex = linePointIndices[size - 1];
            if (size >= 3)
            {
                prevIndex = linePointIndices[size - 3];
            }
            else if (size >= 2)
            {
                prevIndex = linePointIndices[size - 2];
            }
            if (prevIndex >= 0)
            {
                System.Array.Resize(ref linePoints, prevIndex);
                System.Array.Resize(ref lineRadius, prevIndex);

                System.Array.Resize(ref vertices, prevIndex * ANZ);
                System.Array.Resize(ref normals, prevIndex * ANZ);
                System.Array.Resize(ref triangles, prevIndex * 6 * ANZ);
            }

            if (size == 1)
            {
                Vector3 A = controlPoints[0];
                Vector3 D = controlPoints[1];
                float thirdDist = Vector3.Distance(D, A) / 3f;
                Vector3 B = A + (D - A).normalized * thirdDist;
                Vector3 C = D - (D - A).normalized * thirdDist;

                AddArc(A, B, C, D, radius, radius, ANZ);
            }
            else if (size == 2)
            {
                {
                    Vector3 A = controlPoints[0];
                    Vector3 D = controlPoints[1];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (D - A).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[2] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevPrevRadius, prevRadius, ANZ);
                }

                {
                    Vector3 A = controlPoints[1];
                    Vector3 D = controlPoints[2];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (D - controlPoints[0]).normalized * thirdDist;
                    Vector3 C = D - (D - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevRadius, radius, ANZ);
                }
            }
            else if (size == 3)
            {
                {
                    Vector3 A = controlPoints[size - 3];
                    Vector3 D = controlPoints[size - 2];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (D - A).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[size - 1] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevPrevPrevRadius, prevRadius, ANZ);
                }

                {
                    Vector3 A = controlPoints[size - 2];
                    Vector3 D = controlPoints[size - 1];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (controlPoints[size - 1] - controlPoints[size - 3]).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[size] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevPrevRadius, prevRadius, ANZ);
                }

                {
                    Vector3 A = controlPoints[size - 1];
                    Vector3 D = controlPoints[size - 0];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (controlPoints[size] - controlPoints[size - 2]).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[size] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevRadius, radius, ANZ);
                }
            }
            else
            {
                {
                    Vector3 A = controlPoints[size - 3];
                    Vector3 D = controlPoints[size - 2];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (controlPoints[size - 2] - controlPoints[size - 4]).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[size - 1] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevPrevPrevRadius, prevRadius, ANZ);
                }

                {
                    Vector3 A = controlPoints[size - 2];
                    Vector3 D = controlPoints[size - 1];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (controlPoints[size - 1] - controlPoints[size - 3]).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[size] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevPrevRadius, prevRadius, ANZ);
                }

                {
                    Vector3 A = controlPoints[size - 1];
                    Vector3 D = controlPoints[size - 0];
                    float thirdDist = Vector3.Distance(D, A) / 3f;
                    Vector3 B = A + (controlPoints[size] - controlPoints[size - 2]).normalized * thirdDist;
                    Vector3 C = D - (controlPoints[size] - A).normalized * thirdDist;
                    AddArc(A, B, C, D, prevRadius, radius, ANZ);
                }
            }

            AddPointToLine(next, next, radius, ANZ);
            linePointIndices[size] = linePoints.Length;
        }

        private void Reset()
        {
            System.Array.Resize(ref controlPoints, 0);
            System.Array.Resize(ref controlPointsRadius, 0);
            System.Array.Resize(ref vertices, 0);
            System.Array.Resize(ref normals, 0);
            System.Array.Resize(ref triangles, 0);
            System.Array.Resize(ref linePoints, 0);
            System.Array.Resize(ref linePointIndices, 0);
            System.Array.Resize(ref lineRadius, 0);
        }

        private Vector3 GetFirstPerpVector(Vector3 v)
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

        // reorder vertices to minimize distances
        // the first vertex of the previous circle is the reference
        // get the closest point to this ref and re index circle points according to this
        private void Reorder(ref Vector3[] vertices, ref Vector3[] normals, int pointIndex, int ANZ)
        {
            Vector3[] tmpVertices = new Vector3[ANZ];
            Vector3[] tmpNormals = new Vector3[ANZ];
            for (int i = 0; i < ANZ; i++)
            {
                tmpVertices[i] = vertices[pointIndex * ANZ + i];
                tmpNormals[i] = normals[pointIndex * ANZ + i];
            }

            int minPrevIndex = 0;
            int minIndex = 0;
            float minDist = Mathf.Infinity;
            for (int i = 0; i < ANZ; i++)
            {
                Vector3 refVertex = vertices[(pointIndex - 1) * ANZ + i];
                for (int j = 0; j < ANZ; j++)
                {
                    float dist = Vector3.Distance(tmpVertices[j], refVertex);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        minIndex = j;
                        minPrevIndex = i;
                    }
                }
            }

            for (int j = 0; j < ANZ; j++)
            {
                vertices[pointIndex * ANZ + ((j + minPrevIndex) % ANZ)] = tmpVertices[(j + minIndex) % ANZ];
                normals[pointIndex * ANZ + ((j + minPrevIndex) % ANZ)] = tmpNormals[(j + minIndex) % ANZ];
            }
        }

        private Vector3 CubicBezier(Vector3 A, Vector3 B, Vector3 C, Vector3 D, float t)
        {
            float invT1 = 1 - t;
            float invT2 = invT1 * invT1;
            float invT3 = invT2 * invT1;

            float t2 = t * t;
            float t3 = t2 * t;

            return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
        }

        private int Step(Vector3 A, Vector3 B, float radius)
        {
            return 4;
        }

        private void AddArc(Vector3 A, Vector3 B, Vector3 C, Vector3 D, float radiusA, float radiusD, int ANZ)
        {
            int s = Step(A, D, radiusD);
            int i = 0;
            float ratio = 0;
            Vector3 point = CubicBezier(A, B, C, D, ratio);
            do
            {
                i++;
                float nextRatio = (float)i / (float)s;
                Vector3 nextPoint = CubicBezier(A, B, C, D, nextRatio);
                AddPointToLine(point, nextPoint, radiusA + ratio * (radiusD - radiusA), ANZ);
                point = nextPoint;
                ratio = nextRatio;
            } while (i < s);
        }

        private void AddPointToLine(Vector3 point, Vector3 nextPoint, float R1, int ANZ)
        {
            const float FULL = 2.0f * Mathf.PI;

            int count = linePoints.Length;
            int index = count;
            if (count > 0)
            {                
                if (point == linePoints[count - 1])
                    index = count - 1;
                    
                count = index;
            }
            count++;

            System.Array.Resize(ref linePoints, count);
            System.Array.Resize(ref lineRadius, count);

            System.Array.Resize(ref vertices, count * ANZ);
            System.Array.Resize(ref normals, count * ANZ);

            if(count > 1)
                System.Array.Resize(ref triangles, (count - 1) * 6 * ANZ);

            linePoints[index] = point;
            lineRadius[index] = R1;

            Vector3 prevP = point;
            if (index > 0)
                prevP = linePoints[index - 1];

            Vector3 prevPrevP = prevP;
            if (index > 1)
                prevPrevP = linePoints[index - 2];

            // reorient current circle
            Vector3 dir = new Vector3(1f, 0f, 0f);
            Vector3 firstPerp = new Vector3();
            Vector3 secondPerp = new Vector3();

            Vector3 center;
            float radius;

            if (index > 0)
            {
                dir = (point - prevPrevP).normalized;
                firstPerp = GetFirstPerpVector(dir);
                secondPerp = Vector3.Cross(dir, firstPerp).normalized;

                center = linePoints[index - 1];
                radius = lineRadius[index - 1];
                for (int j = 0; j < ANZ; j++)
                {
                    float angle = FULL * (j / (float)ANZ);
                    Vector3 pos = radius * (Mathf.Cos(angle) * firstPerp + Mathf.Sin(angle) * secondPerp);
                    vertices[(index - 1) * ANZ + j] = center + pos;
                    normals[(index - 1) * ANZ + j] = pos.normalized;
                }

                // reorder circles by distance to avoid twists
                if (index > 1)
                    Reorder(ref vertices, ref normals, index - 1, ANZ);

            }

            dir = (nextPoint - prevP).normalized;
            firstPerp = GetFirstPerpVector(dir);
            secondPerp = Vector3.Cross(dir, firstPerp).normalized;

            center = linePoints[index];
            radius = lineRadius[index];
            for (int j = 0; j < ANZ; j++)
            {
                float angle = FULL * (j / (float)ANZ);
                Vector3 pos = radius * (Mathf.Cos(angle) * firstPerp + Mathf.Sin(angle) * secondPerp);
                vertices[index * ANZ + j] = center + pos;
                normals[index * ANZ + j] = pos.normalized;
            }

            // reorder circles by distance to avoid twists
            if (index > 0)
            {
                Reorder(ref vertices, ref normals, index, ANZ);

                int i = 0, quadIndex;
                int tIndex = 6 * ANZ * (index - 1);
                int vIndex = ANZ * (index - 1);
                for (quadIndex = 0; quadIndex < (ANZ - 1); quadIndex++)
                {
                    triangles[tIndex + i + 0] = vIndex + quadIndex + 0;
                    triangles[tIndex + i + 1] = vIndex + quadIndex + 1;
                    triangles[tIndex + i + 2] = vIndex + quadIndex + ANZ;


                    triangles[tIndex + i + 3] = vIndex + quadIndex + 1;
                    triangles[tIndex + i + 4] = vIndex + quadIndex + ANZ + 1;
                    triangles[tIndex + i + 5] = vIndex + quadIndex + ANZ;

                    // debug
                    i += 6;
                }

                triangles[tIndex + i + 0] = vIndex + ANZ - 1;
                triangles[tIndex + i + 1] = vIndex + 0;
                triangles[tIndex + i + 2] = vIndex + 2 * ANZ - 1;

                triangles[tIndex + i + 3] = vIndex + 0;
                triangles[tIndex + i + 4] = vIndex + ANZ;
                triangles[tIndex + i + 5] = vIndex + 2 * ANZ - 1;
            }

        }
    }
}
