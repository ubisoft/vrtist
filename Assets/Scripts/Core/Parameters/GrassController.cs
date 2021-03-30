using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))] // DEBUG
    public class GrassController : ParametersController
    {
        // DEBUG visualization
        public MeshFilter DEBUG_filter;
        public MeshRenderer DEBUG_render;
        public List<Vector3> DEBUG_positions = new List<Vector3>();
        public List<Color> DEBUG_colors = new List<Color>();
        public List<int> DEBUG_indices = new List<int>();
        public List<Vector3> DEBUG_normals = new List<Vector3>();
        public List<Vector2> DEBUG_uvs = new List<Vector2>();

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct SourceVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
            public Vector3 color;
        }

        private List<SourceVertex> vertices = new List<SourceVertex>();
        public int nbSourceVertices = 0;

        public Material material = default;
        public ComputeShader computeShader = default;






        // DEBUG
        public void InitDebugData()
        {
            Material debugMaterial = ResourceManager.GetMaterial(MaterialID.ObjectOpaque);

            DEBUG_filter = GetComponent<MeshFilter>();
            DEBUG_render = GetComponent<MeshRenderer>();
            DEBUG_render.sharedMaterial = debugMaterial;
            DEBUG_render.material.SetColor("_BaseColor", Color.red);
            DEBUG_render.material.SetFloat("_Opacity", 1.0f);
            DEBUG_positions = new List<Vector3>();
            DEBUG_colors = new List<Color>();
            DEBUG_indices = new List<int>();
            DEBUG_normals = new List<Vector3>();
            DEBUG_uvs = new List<Vector2>();
        }


        public void AddPoint(Vector3 position, Vector3 normal, Vector2 uv, Color color)
        {
            Vector3 col = new Vector3(color.r, color.g, color.b);
            vertices.Add(new SourceVertex() { position = position, normal = normal, uv = uv, color = col });

            // Update DEBUG arrays & mesh
            DEBUG_positions.Add(position);
            DEBUG_normals.Add(normal);
            DEBUG_uvs.Add(uv);
            DEBUG_colors.Add(color);
            DEBUG_indices.Add(nbSourceVertices);

            Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.name = "DEBUG Grass Mesh";
            mesh.SetVertices(DEBUG_positions);
            int[] indi = DEBUG_indices.ToArray();
            mesh.SetIndices(indi, MeshTopology.Points, 0);
            mesh.SetUVs(0, DEBUG_uvs);
            mesh.SetColors(DEBUG_colors);
            mesh.SetNormals(DEBUG_normals);
            DEBUG_filter.mesh = mesh;

            nbSourceVertices++;
        }

        public void Clear()
        {
            //vertices.Clear();
            vertices = new List<SourceVertex>();
            nbSourceVertices = 0;
        }




        // Blade
        [Header("Blade")]
        public float grassHeight = 1;
        public float grassWidth = 0.06f;
        public float grassRandomHeight = 0.25f;
        [Range(0, 1)] public float bladeRadius = 0.6f;
        public float bladeForwardAmount = 0.38f;
        [Range(1, 4)] public float bladeCurveAmount = 2;

        // Wind
        [Header("Wind")]
        public float windSpeed = 3;
        public float windStrength = 0.01f;
        public float windMultiplierXX = 1.0f;
        public float windMultiplierXY = 1.0f;
        public float windMultiplierXZ = 1.0f;
        public float windMultiplierZX = 1.0f;
        public float windMultiplierZY = 1.0f;
        public float windMultiplierZZ = 1.0f;


        // Interactor
        [Header("Interactor")]
        public float affectRadius = 0.3f;
        public float affectStrength = 5;
        public Transform interactorXf;


        // LOD
        [Header("LOD")]
        public float minFadeDistance = 40;
        public float maxFadeDistance = 60;
        // Material
        [Header("Material")]
        public bool overrideMaterial;
        public Color topColor = new Color(1, 1, 0);
        public Color bottomColor = new Color(0, 1, 0);


        // Other
        [Header("Other")]
        public bool castShadow;
        public Transform cameraXf;

        private readonly int m_AllowedBladesPerVertex = 4;
        private readonly int m_AllowedSegmentsPerBlade = 5;

        // A state variable to help keep track of whether compute buffers have been set up
        private bool m_Initialized;
        // A compute buffer to hold vertex data of the source mesh
        private ComputeBuffer m_SourceVertBuffer;
        // A compute buffer to hold vertex data of the generated mesh
        private ComputeBuffer m_DrawBuffer;
        // A compute buffer to hold indirect draw arguments
        private ComputeBuffer m_ArgsBuffer;
        // Instantiate the shaders so data belong to their unique compute buffers
        private ComputeShader m_InstantiatedComputeShader;
        private Material m_InstantiatedMaterial;
        // The id of the kernel in the grass compute shader
        private int m_IdGrassKernel;
        // The x dispatch size for the grass compute shader
        private int m_DispatchSize;
        // The local bounds of the generated mesh
        private Bounds m_LocalBounds;





    }
}
