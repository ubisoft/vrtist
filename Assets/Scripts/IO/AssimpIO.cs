using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace VRtist
{
    public class AssimpIO
    {
        private Assimp.Scene scene;
        private string directoryName;
        private List<Material> materials = new List<Material>();
        private List<SubMeshComponent> meshes = new List<SubMeshComponent>();
        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        private class SubMeshComponent
        {
            public Mesh mesh;
            public string name;
            public int materialIndex;
        }
        private struct MeshStruct
        {
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector2[][] Uv;
            public int[] Triangles;
            public Vector4[] Tangents;
            public Color[] VertexColors;
        }
        private SubMeshComponent ImportMesh(Assimp.Mesh assimpMesh)
        {
            int i;
            MeshStruct meshs = new MeshStruct();

            meshs.Vertices = new Vector3[assimpMesh.VertexCount];
            meshs.Uv = new Vector2[assimpMesh.TextureCoordinateChannelCount][];
            for( i = 0; i < assimpMesh.TextureCoordinateChannelCount; i++ )
            {
                meshs.Uv[i] = new Vector2[assimpMesh.VertexCount];
            }
            meshs.Normals = new Vector3[assimpMesh.VertexCount];
            meshs.Triangles = new int[assimpMesh.FaceCount * 3];
            meshs.Tangents = null;
            meshs.VertexColors = null;

            i = 0;
            foreach (Assimp.Vector3D v in assimpMesh.Vertices)
            {
                meshs.Vertices[i].x = v.X;
                meshs.Vertices[i].y = v.Y;
                meshs.Vertices[i].z = v.Z;
                i++;
            }

            for (int UVlayer = 0; UVlayer < assimpMesh.TextureCoordinateChannelCount; UVlayer++)
            {
                i = 0;
                foreach (Assimp.Vector3D uv in assimpMesh.TextureCoordinateChannels[UVlayer])
                {
                    meshs.Uv[UVlayer][i].x = uv.X;
                    meshs.Uv[UVlayer][i].y = uv.Y;
                    i++;
                }
            }

            i = 0;
            foreach (Assimp.Vector3D n in assimpMesh.Normals)
            {
                meshs.Normals[i].x = n.X;
                meshs.Normals[i].y = n.Y;
                meshs.Normals[i].z = n.Z;
                i++;
            }

            if (assimpMesh.HasTangentBasis)
            {
                i = 0;
                meshs.Tangents = new Vector4[assimpMesh.VertexCount];
                foreach (Assimp.Vector3D t in assimpMesh.Tangents)
                {
                    meshs.Tangents[i].x = t.X;
                    meshs.Tangents[i].y = t.Y;
                    meshs.Tangents[i].z = t.Z;
                    meshs.Tangents[i].w = 1f;
                    i++;
                }
            }

            if (assimpMesh.VertexColorChannelCount >= 1)
            {
                i = 0;
                meshs.VertexColors = new Color[assimpMesh.VertexCount];
                foreach (Assimp.Color4D c in assimpMesh.VertexColorChannels[0])
                {
                    meshs.VertexColors[i].r = c.R;
                    meshs.VertexColors[i].g = c.G;
                    meshs.VertexColors[i].b = c.B;
                    meshs.VertexColors[i].a = c.A;
                    i++;
                }
            }

            i = 0;
            foreach (Assimp.Face face in assimpMesh.Faces)
            {
                meshs.Triangles[i + 0] = face.Indices[0];
                meshs.Triangles[i + 1] = face.Indices[1];
                meshs.Triangles[i + 2] = face.Indices[2];
                i+=3;
            }

            SubMeshComponent subMeshComponent = new SubMeshComponent();
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = meshs.Vertices;
            mesh.uv = meshs.Uv[0];
            if (assimpMesh.TextureCoordinateChannelCount > 1)
                mesh.uv2 = meshs.Uv[1];
            if (assimpMesh.TextureCoordinateChannelCount > 2)
                mesh.uv3 = meshs.Uv[2];
            if (assimpMesh.TextureCoordinateChannelCount > 3)
                mesh.uv4 = meshs.Uv[3];
            if (assimpMesh.TextureCoordinateChannelCount > 4)
                mesh.uv5 = meshs.Uv[4];
            if (assimpMesh.TextureCoordinateChannelCount > 5)
                mesh.uv6 = meshs.Uv[5];
            if (assimpMesh.TextureCoordinateChannelCount > 6)
                mesh.uv7 = meshs.Uv[6];
            if (assimpMesh.TextureCoordinateChannelCount > 7)
                mesh.uv8 = meshs.Uv[7];
            mesh.normals = meshs.Normals;
            if (meshs.Tangents != null)
                mesh.tangents = meshs.Tangents;
            if (meshs.VertexColors != null)
                mesh.colors = meshs.VertexColors;
            mesh.triangles = meshs.Triangles;
            mesh.RecalculateBounds();

            subMeshComponent.mesh = mesh;
            subMeshComponent.name = assimpMesh.Name;
            subMeshComponent.materialIndex = assimpMesh.MaterialIndex;

            return subMeshComponent;
        }

        private void ImportMeshes()
        {
            int i = 0;
            foreach(Assimp.Mesh assimpMesh in scene.Meshes)
            {
                SubMeshComponent subMeshComponent = ImportMesh(assimpMesh);
                meshes.Add(subMeshComponent);
                i++;
            }
        }

        private Texture2D GetOrCreateTextureFromFile(string filename)
        {
            CultureInfo ci = new CultureInfo("en-US");
            if (!filename.EndsWith(".jpg", false, ci) && 
                !filename.EndsWith(".png", false, ci) && 
                !filename.EndsWith(".exr", false, ci) && 
                !filename.EndsWith(".tga", false, ci))
                return null;

            Texture2D texture;
            if (textures.TryGetValue(filename, out texture))
                return texture;

            byte[] bytes = System.IO.File.ReadAllBytes(filename);
            texture = new Texture2D(1,1);
            texture.LoadImage(bytes);
            textures[filename] = texture;
            return texture;
        }

        private void ImportMaterials()
        {            
            int i = 0;
            Shader hdrplit = Shader.Find("HDRP/Lit");
            foreach (Assimp.Material assimpMaterial in scene.Materials)
            {
                materials.Add(new Material(hdrplit));

                var material = materials[i];
                material.SetFloat("_Smoothness", 0.2f);

                if (assimpMaterial.IsTwoSided)
                {
                    // does not work...
                    material.SetInt("_DoubleSidedEnable", 1);
                    material.EnableKeyword("_DOUBLESIDED_ON");
                }

                material.name = assimpMaterial.Name;
                if (assimpMaterial.HasColorDiffuse)
                {
                    Color baseColor = new Color(assimpMaterial.ColorDiffuse.R, assimpMaterial.ColorDiffuse.G, assimpMaterial.ColorDiffuse.B, assimpMaterial.ColorDiffuse.A);
                    material.SetColor("_BaseColor", baseColor);
                }
                if (assimpMaterial.HasTextureDiffuse)
                {
                    Assimp.TextureSlot tslot = assimpMaterial.TextureDiffuse;
                    string fullpath = Path.IsPathRooted(tslot.FilePath) ? tslot.FilePath : directoryName + "\\" + tslot.FilePath;
                    if (File.Exists(fullpath))
                    {
                        Texture2D texture = GetOrCreateTextureFromFile(fullpath);
                        material.SetTexture("_BaseColorMap", texture);
                    }
                    else
                    {
                        Debug.LogError("File not found : " + tslot.FilePath);
                    }
                }
                i++;
            }            
        }

        private void AssignMeshes(Assimp.Node node, GameObject parent)
        {
            if (node.MeshIndices.Count == 0)
                return;

            MeshFilter meshFilter = parent.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = parent.AddComponent<MeshRenderer>();

            Material[] mats = new Material[node.MeshIndices.Count];

            CombineInstance[] combine = new CombineInstance[node.MeshIndices.Count];

            int i = 0;
            foreach (int indice in node.MeshIndices)
            {
                combine[i].mesh = meshes[indice].mesh;
                combine[i].transform = Matrix4x4.identity;
                mats[i] = materials[meshes[indice].materialIndex];
                i++;
            }

            meshFilter.mesh = new Mesh();
            meshFilter.mesh.CombineMeshes(combine, false);
            meshFilter.name = meshes[node.MeshIndices[0]].name;
            meshRenderer.sharedMaterials = mats;
        }

        private void DecomposeMatrix(Matrix4x4 m, out Vector3 scale, out Quaternion rotation, out Vector3 position)
        {
            // Extract new local position
            position = m.GetColumn(3);

            // Extract new local rotation
            rotation = Quaternion.LookRotation(
                m.GetColumn(2),
                m.GetColumn(1)
            );

            // Extract new local scale
            scale = new Vector3(
                m.GetColumn(0).magnitude,
                m.GetColumn(1).magnitude,
                m.GetColumn(2).magnitude
            );
        }

        private GameObject ImportHierarchy(Assimp.Node node, Transform parent)
        {
            GameObject go = new GameObject();
            if(parent != null)
                go.transform.parent = parent;

            // Do not use Assimp Decompose function, it does not work properly
            // use unity decomposition instead
            Matrix4x4 mat = new Matrix4x4(
                new Vector4(node.Transform.A1, node.Transform.B1, node.Transform.C1, node.Transform.D1),
                new Vector4(node.Transform.A2, node.Transform.B2, node.Transform.C2, node.Transform.D2),
                new Vector4(node.Transform.A3, node.Transform.B3, node.Transform.C3, node.Transform.D3),
                new Vector4(node.Transform.A4, node.Transform.B4, node.Transform.C4, node.Transform.D4)
                );

            Vector3 position, scale;
            Quaternion rotation;
            DecomposeMatrix(mat, out scale, out rotation, out position);

            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;

            go.name = node.Name;

            AssignMeshes(node, go);

            foreach (Assimp.Node assimpChild in node.Children)
            {
                GameObject child = ImportHierarchy(assimpChild, go.transform);
            }

            return go;
        }

        private GameObject ImportScene(Transform root = null)
        {
            Debug.Log("Mesh count : " + scene.MeshCount);
            Debug.Log("Texture count : " + scene.TextureCount);
            Debug.Log("Material count : " + scene.MaterialCount);
            Debug.Log("Caemera count : " + scene.CameraCount);
            Debug.Log("Light count : " + scene.LightCount);
            Debug.Log("Material count : " + scene.MaterialCount);

            ImportMaterials();
            ImportMeshes();
            GameObject objectRoot = ImportHierarchy(scene.RootNode, root);

            // Right handed to Left Handed
            objectRoot.transform.localScale = new Vector3(-1, 1, 1);
            objectRoot.transform.localRotation = Quaternion.Euler(0, 180, 0);

            return objectRoot;
        }

        public static GameObject Import(string fileName, Transform root = null)
        {
            GameObject go = null;
            try
            {
                Assimp.AssimpContext ctx = new Assimp.AssimpContext();
                Assimp.Scene aScene = ctx.ImportFile(fileName,
                    Assimp.PostProcessSteps.Triangulate | 
                    Assimp.PostProcessSteps.GenerateNormals |
                    Assimp.PostProcessSteps.GenerateUVCoords);

                AssimpIO assimpIO = new AssimpIO();
                assimpIO.scene = aScene;
                assimpIO.directoryName = Path.GetDirectoryName(fileName);
                go = assimpIO.ImportScene(root);
            }
            catch(Assimp.AssimpException e)
            {
                Debug.LogError(e.Message);
            }

            return go;
        }
    }

}