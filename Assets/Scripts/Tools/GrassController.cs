using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GrassController : ParametersController
    {
        [SerializeField] private Material material = default;
        [SerializeField] private ComputeShader computeShader = default;

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

        // The structure to send to the compute shader
        // This layout kind assures that the data is laid out sequentially
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct SourceVertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
            public Vector3 color;
        }

        private List<SourceVertex> vertices;

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

        public void AddPoint(Vector3 position, Vector3 normal, Vector2 uv, Vector3 color)
        {
            vertices.Add(new SourceVertex() { position = position, normal = normal, uv = uv, color = color });
        }

        public void Clear()
        {
            //vertices.Clear();
            vertices = new List<SourceVertex>();
        }
    }
}
