using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VRtist
{
    public class AssimpIO
    {
        private Assimp.Scene scene;
        private string directoryName;
        private List<Material> materials = new List<Material>();
        private List<GameObject> meshes = new List<GameObject>();
        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        private struct MeshStruct
        {
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector2[] Uv;
            public int[] Triangles;
        }
        private Mesh ImportMesh(Assimp.Mesh assimpMesh)
        {
            MeshStruct meshs = new MeshStruct();

            meshs.Vertices = new Vector3[assimpMesh.VertexCount];
            meshs.Uv = new Vector2[assimpMesh.VertexCount];
            meshs.Normals = new Vector3[assimpMesh.VertexCount];
            meshs.Triangles = new int[assimpMesh.FaceCount * 3];

            int i = 0;
            foreach (Assimp.Vector3D v in assimpMesh.Vertices)
            {
                meshs.Vertices[i].x = v.X;
                meshs.Vertices[i].y = v.Y;
                meshs.Vertices[i].z = v.Z;
                i++;
            }

            // TODO handle multiple UV set
            i = 0;
            foreach (Assimp.Vector3D uv in assimpMesh.TextureCoordinateChannels[0])
            {
                meshs.Uv[i].x = uv.X;
                meshs.Uv[i].y = uv.Y;
                i++;
            }

            i = 0;
            foreach (Assimp.Vector3D n in assimpMesh.Normals)
            {
                meshs.Normals[i].x = n.X;
                meshs.Normals[i].y = n.Y;
                meshs.Normals[i].z = n.Z;
                i++;
            }

            i = 0;
            foreach (Assimp.Face face in assimpMesh.Faces)
            {
                meshs.Triangles[i + 0] = face.Indices[0];
                meshs.Triangles[i + 1] = face.Indices[1];
                meshs.Triangles[i + 2] = face.Indices[2];
                i+=3;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = meshs.Vertices;
            mesh.uv = meshs.Uv;
            mesh.normals = meshs.Normals;
            mesh.triangles = meshs.Triangles;

            mesh.RecalculateBounds();
            return mesh;
        }

        private void ImportMeshes()
        {
            int i = 0;
            foreach(Assimp.Mesh assimpMesh in scene.Meshes)
            {
                meshes.Add(new GameObject());
                meshes[i].name = assimpMesh.Name;
                var filter = meshes[i].AddComponent<MeshFilter>();
                filter.mesh = ImportMesh(assimpMesh);
                MeshRenderer renderer = meshes[i].AddComponent<MeshRenderer>();
                renderer.sharedMaterial = materials[assimpMesh.MaterialIndex];

                i++;
            }
        }

        private Texture2D GetOrCreateTextureFromFile(string filename)
        {
            if (!filename.EndsWith(".jpg") && !filename.EndsWith(".png"))
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
                // does not work...
                material.SetInt("_DoubleSidedEnable", 1);
                material.EnableKeyword("_DOUBLESIDED_ON");
                i++;
            }            
        }

        private void AssignMeshes(Assimp.Node node, GameObject parent)
        {
            foreach(int indice in node.MeshIndices)
            {
                GameObject mesh = GameObject.Instantiate(meshes[indice]);
                mesh.name = meshes[indice].name;
                mesh.transform.parent = parent.transform;
                mesh.transform.localPosition = Vector3.zero;
                mesh.transform.localScale = Vector3.one;
                mesh.transform.localRotation = Quaternion.identity;
            }
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

            foreach (GameObject o in meshes)
                GameObject.Destroy(o);

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