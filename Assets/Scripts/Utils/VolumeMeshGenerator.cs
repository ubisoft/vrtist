using UnityEngine;

// Based on pieces of: https://github.com/SebLague/Marching-Cubes

namespace VRtist
{
    public class VolumeMeshGenerator
    {
        public Vector3[] vertices;
        public Vector3[] normals;
        public int[] triangles;

        public Vector3 origin;
        public Bounds bounds;
        public float[,,] field;
        public Matrix4x4 toLocalMatrix;

        public float stepSize = 0.01f; // 1 cm
        //public float stepSize = 0.1f; // 10 cm
        public Vector3Int resolution = Vector3Int.zero;
        public float strength = 1.0f;
        public float isoLevel = 0.5f;

        ComputeShader computeShader;

        // Buffers
        ComputeBuffer triangleBuffer; // buffer of struct Triangle (3 vertices)
        ComputeBuffer fieldBuffer;
        //ComputeBuffer pointsBuffer;
        ComputeBuffer triCountBuffer;

        Vector3 prevControlPoint;

        struct Triangle
        {
#pragma warning disable 649 // disable unassigned variable warning
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;

            public Vector3 this[int i]
            {
                get
                {
                    switch (i)
                    {
                        case 0:
                            return a;
                        case 1:
                            return b;
                        default:
                            return c;
                    }
                }
            }
        }

        public VolumeMeshGenerator()
        {
            computeShader = Resources.Load<ComputeShader>("Compute/MarchingCubes");

            Reset();

            toLocalMatrix = Matrix4x4.identity;
        }

        public void Reset()
        {
            origin = Vector3.zero;
            resolution = Vector3Int.zero;
            bounds.center = Vector3.zero;
            bounds.extents = Vector3.zero;

            field = null; // force garbage collection?
            field = new float[0, 0, 0];

            System.Array.Resize(ref vertices, 0);
            System.Array.Resize(ref normals, 0);
            System.Array.Resize(ref triangles, 0);

            ReleaseComputeBuffers();
        }

        public void InitFromController(VolumeController controller)
        {
            // NOTE: is it useful to have data in Generator. Couldnt we have a controller reference?

            origin = controller.origin;
            resolution = controller.resolution;
            bounds = controller.bounds;
            field = controller.field;
            stepSize = controller.stepSize;

            ComputeIsosurface();
        }

        // + fallof curve
        // + brush shape
        public void AddPoint(Vector3 nextPoint, float nextPointRadius)
        {
            // nextPoint is in realworld space, which does not change, it is the player space.
            // -> point is in the world/righthanded space, which has a scale
            Vector3 point = toLocalMatrix.MultiplyPoint(nextPoint); // repere RightHanded, subit le scale du monde.

            // nextPointRadius is in realworld space.
            // -> radius is in world/righthanded space, it is SCALED with the RH matrix.
            //    if the world is made really small, the pen radius becomes really big.
            float radius = Mathf.Abs(toLocalMatrix.lossyScale.x) * nextPointRadius;

            if (Vector3.Distance(prevControlPoint, point) < 0.5f * radius) // distance in RH space
                return;

            prevControlPoint = point;

            // 1) expand the volume bounds if necessary
            bool bboxChanged = UpdateBounds(point, radius);

            // 2) Resize field array, copy old content
            if (bboxChanged) UpdateFieldDimensions();

            // 3) Add matter to the field
            AddMatter(point, radius); // TODO: do it using computeShaders with brushes.

            // 4) Compute isosurface
            ComputeIsosurface();
        }

        private bool UpdateBounds(Vector3 point, float radius)
        {
            bool bboxChanged = false;

            // First point
            if (bounds.size == Vector3.zero)
            {
                bounds.center = point; // local = 0,0,0
                bounds.size = new Vector3(radius * 2, radius * 2, radius * 2); // local
                origin = bounds.center - bounds.extents;

                bboxChanged = true;
            }
            else
            {
                Vector3 toPoint = point - bounds.center;

                float offsetx = 0.0f;
                if (toPoint.x + radius > bounds.extents.x)
                {
                    offsetx = toPoint.x + radius - bounds.extents.x;
                }
                else if (toPoint.x - radius < -bounds.extents.x)
                {
                    offsetx = toPoint.x - radius + bounds.extents.x;
                }

                float offsety = 0.0f;
                if (toPoint.y + radius > bounds.extents.y)
                {
                    offsety = toPoint.y + radius - bounds.extents.y;
                }
                else if (toPoint.y - radius < -bounds.extents.y)
                {
                    offsety = toPoint.y - radius + bounds.extents.y;
                }

                float offsetz = 0.0f;
                if (toPoint.z + radius > bounds.extents.z)
                {
                    offsetz = toPoint.z + radius - bounds.extents.z;
                }
                else if (toPoint.z - radius < -bounds.extents.z)
                {
                    offsetz = toPoint.z - radius + bounds.extents.z;
                }

                Vector3 center_offset = new Vector3(offsetx / 2.0f, offsety / 2.0f, offsetz / 2.0f);
                Vector3 size_offset = new Vector3(Mathf.Abs(offsetx), Mathf.Abs(offsety), Mathf.Abs(offsetz));

                if (size_offset.sqrMagnitude > Mathf.Epsilon)
                {
                    bounds.size = bounds.size + size_offset;
                    bounds.center = bounds.center + center_offset;
                    bboxChanged = true;
                }
            }

            return bboxChanged;
        }

        private void UpdateFieldDimensions()
        {
            Vector3 originToBounds = bounds.center - bounds.extents - origin;

            Vector3Int newResPositive = new Vector3Int(
                Mathf.FloorToInt((bounds.size.x-Mathf.Abs(originToBounds.x)) / stepSize),
                Mathf.FloorToInt((bounds.size.y-Mathf.Abs(originToBounds.y)) / stepSize),
                Mathf.FloorToInt((bounds.size.z-Mathf.Abs(originToBounds.z)) / stepSize)
            );

            Vector3Int newResNegative = new Vector3Int(
                Mathf.FloorToInt(Mathf.Abs(originToBounds.x) / stepSize),
                Mathf.FloorToInt(Mathf.Abs(originToBounds.y) / stepSize),
                Mathf.FloorToInt(Mathf.Abs(originToBounds.z) / stepSize)
            );

            Vector3Int newRes = newResPositive + newResNegative + Vector3Int.one;
            origin = origin - new Vector3(newResNegative.x * stepSize, newResNegative.y * stepSize, newResNegative.z * stepSize);

            float[,,] newField = new float[newRes.z, newRes.y, newRes.x];

            // copy old field into new.
            float nz = field.GetLength(0);
            float ny = field.GetLength(1);
            float nx = field.GetLength(2);
            
            int oz = newResNegative.z;
            int oy = newResNegative.y;
            int ox = newResNegative.x;

            for (int z = 0; z < nz; ++z)
            {
                for (int y = 0; y < ny; ++y)
                {
                    for (int x = 0; x < nx; ++x)
                    {
                        newField[z + oz, y + oy, x + ox] = field[z, y, x];
                    }
                }
            }

            field = newField;
            resolution = newRes;
        }

        private void AddMatter(Vector3 point, float radius)
        {
            float nz = field.GetLength(0);
            float ny = field.GetLength(1);
            float nx = field.GetLength(2);

            if (nz < 2 || ny < 2 || nx < 2)
                return;

            for (int z = 0; z < nz; ++z)
            {
                for (int y = 0; y < ny; ++y)
                {
                    for (int x = 0; x < nx; ++x)
                    {
                        Vector3 fieldPoint = origin + new Vector3(x * stepSize, y * stepSize, z * stepSize);

                        float D = Vector3.Magnitude(fieldPoint - point);
                        float influence = Mathf.Clamp01(1.0f - (D / (radius))); // [0..1] big in the middle, fades as the distance grows
                        if (influence > 0)
                        {
                            field[z, y, x] += strength * influence;
                        }
                    }
                }
            }
        }

        private void CreateComputeBuffers()
        {
            int numPoints = resolution.x * resolution.y * resolution.z;
            int numVoxels = (resolution.x - 1) * (resolution.y - 1) * (resolution.z - 1);
            int maxTriangleCount = numVoxels * 5;

            // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
            // Otherwise, only create if null or if size has changed
            if (!Application.isPlaying || (fieldBuffer == null || numPoints != fieldBuffer.count))
            {
                if (Application.isPlaying)
                {
                    ReleaseComputeBuffers();
                }
                triangleBuffer = new ComputeBuffer(maxTriangleCount, sizeof(float) * 3 * 3, ComputeBufferType.Append);
                //pointsBuffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
                fieldBuffer = new ComputeBuffer(numPoints, sizeof(float));
                triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
            }
        }

        private void ReleaseComputeBuffers()
        {
            if (triangleBuffer != null)
            {
                triangleBuffer.Release();
                fieldBuffer.Release();
                triCountBuffer.Release();

                triangleBuffer = null;
                fieldBuffer = null;
                triCountBuffer = null;
            }
        }

        private void ComputeIsosurface()
        {
            float nz = field.GetLength(0);
            float ny = field.GetLength(1);
            float nx = field.GetLength(2);

            if (nz < 2 || ny < 2 || nx < 2)
                return;

            const int threadGroupSize = 8;

            CreateComputeBuffers(); // or re-create if need be.

            Vector3Int numVoxelsPerAxis = new Vector3Int(resolution.x - 1, resolution.y - 1, resolution.z - 1);
            Vector3Int numThreadsPerAxis = new Vector3Int(
                Mathf.CeilToInt(numVoxelsPerAxis.x / (float)threadGroupSize),
                Mathf.CeilToInt(numVoxelsPerAxis.y / (float)threadGroupSize),
                Mathf.CeilToInt(numVoxelsPerAxis.z / (float)threadGroupSize)
                );

            //float pointSpacing = stepSize;
            //Vector3Int coord = chunk.coord;
            //Vector3 centre = CentreFromCoord(coord);
            //Vector3 worldBounds = new Vector3(numChunks.x, numChunks.y, numChunks.z) * boundsSize;
            //densityGenerator.Generate(pointsBuffer, numPointsPerAxis, boundsSize, worldBounds, centre, offset, pointSpacing);

            fieldBuffer.SetData(field); // upload field to a compute buffer

            triangleBuffer.SetCounterValue(0);
            //shader.SetBuffer(0, "points", pointsBuffer);
            computeShader.SetBuffer(0, "field", fieldBuffer);
            computeShader.SetBuffer(0, "triangles", triangleBuffer);
            //shader.SetInt("numPointsPerAxis", numPointsPerAxis);
            computeShader.SetInt("numPointsPerAxisX", resolution.x);
            computeShader.SetInt("numPointsPerAxisY", resolution.y);
            computeShader.SetInt("numPointsPerAxisZ", resolution.z);
            //computeShader.SetVector("numPointsPerAxis", new Vector4(resolution.x, resolution.y, resolution.z, 0.0f));
            computeShader.SetFloat("isoLevel", isoLevel);

            computeShader.SetFloat("stepSize", stepSize);
            computeShader.SetVector("origin", new Vector4(origin.x, origin.y, origin.z, 1.0f));

            computeShader.Dispatch(0, numThreadsPerAxis.x, numThreadsPerAxis.y, numThreadsPerAxis.z);

            // Get number of triangles in the triangle buffer
            ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData(triCountArray);
            int numTris = triCountArray[0];

            // Get triangle data from shader
            Triangle[] tris = new Triangle[numTris];
            triangleBuffer.GetData(tris, 0, 0, numTris);

            vertices = new Vector3[numTris * 3];
            triangles = new int[numTris * 3];

            for (int i = 0; i < numTris; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    triangles[i * 3 + j] = i * 3 + j;
                    vertices[i * 3 + j] = tris[i][j];
                }
            }

            // Release buffers immediately in editor
            if (!Application.isPlaying)
            {
                ReleaseComputeBuffers();
            }
        }
    }
}
