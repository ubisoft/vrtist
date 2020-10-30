using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRtist
{
    public class ImageData
    {
        public bool isEmbedded;
        public int width;
        public int height;
        public byte[] buffer;
    }
    public class MixerUtils
    {
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();

        public static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, List<MaterialParameters>> meshesMaterials = new Dictionary<string, List<MaterialParameters>>();

        public static Dictionary<MaterialType, Material> baseMaterials = new Dictionary<MaterialType, Material>();
        public static Dictionary<string, MaterialParameters> materialsParameters = new Dictionary<string, MaterialParameters>();

        public static Dictionary<string, GreasePencilData> greasePencils = new Dictionary<string, GreasePencilData>();
        public static HashSet<string> materialsFillEnabled = new HashSet<string>();
        public static HashSet<string> materialStrokesEnabled = new HashSet<string>();
        public static Dictionary<string, Dictionary<string, int>> greasePencilLayerIndices = new Dictionary<string, Dictionary<string, int>>();

        public static Dictionary<string, ImageData> textureData = new Dictionary<string, ImageData>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static HashSet<string> texturesFlipY = new HashSet<string>();

        // Converts string array to byte buffer
        public static byte[] StringsToBytes(string[] values, bool storeSize = true)
        {
            int size = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(values[i]);
                size += sizeof(int) + utf8.Length;
            }


            byte[] bytes = new byte[size];
            int index = 0;
            if (storeSize)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
                index += sizeof(int);
            }
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
                Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, index, sizeof(int));
                Buffer.BlockCopy(utf8, 0, bytes, index + sizeof(int), value.Length);
                index += sizeof(int) + value.Length;
            }
            return bytes;
        }

        // Converts string to byte buffer
        public static byte[] StringToBytes(string value)
        {
            byte[] bytes = new byte[sizeof(int) + value.Length];
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, 0, sizeof(int));
            Buffer.BlockCopy(utf8, 0, bytes, sizeof(int), value.Length);
            return bytes;
        }

        // Converts triangle indice array to byte buffer
        public static byte[] TriangleIndicesToBytes(int[] vectors)
        {
            byte[] bytes = new byte[sizeof(int) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length / 3), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        // Converts Vector3 array to byte buffer
        public static byte[] Vector3ToBytes(Vector3[] vectors)
        {
            byte[] bytes = new byte[3 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector3 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, index + 2 * sizeof(float), sizeof(float));
                index += 3 * sizeof(float);
            }
            return bytes;
        }

        // Converts byte buffer to Color
        public static Color GetColor(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Color(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        // Converts Color to byte buffer
        public static byte[] ColorToBytes(Color color)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(color.r), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.g), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.b), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.a), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        // Convert byte buffer to Vector3
        public static Vector3 GetVector3(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[3];
            int size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector3(buffer[0], buffer[1], buffer[2]);
        }

        // Convert Vector3 to byte buffer
        public static byte[] Vector3ToBytes(Vector3 vector)
        {
            byte[] bytes = new byte[3 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            return bytes;
        }

        // Convert byte buffer to Vector3
        public static Vector4 GetVector4(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector4(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        // Convert Vector3 to byte buffer
        public static byte[] Vector4ToBytes(Vector4 vector)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        // Convert byte buffer to Vector2
        public static Vector2 GetVector2(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[2];
            int size = 2 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector2(buffer[0], buffer[1]);
        }

        // Convert Vector2 array to byte buffer
        public static byte[] Vector2ToBytes(Vector2[] vectors)
        {
            byte[] bytes = new byte[2 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector2 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                index += 2 * sizeof(float);
            }
            return bytes;
        }

        // Convert byte buffer to Matrix4x4
        public static Matrix4x4 GetMatrix(byte[] data, ref int index)
        {
            float[] matrixBuffer = new float[16];

            int size = 4 * 4 * sizeof(float);
            Buffer.BlockCopy(data, index, matrixBuffer, 0, size);
            Matrix4x4 m = new Matrix4x4(new Vector4(matrixBuffer[0], matrixBuffer[1], matrixBuffer[2], matrixBuffer[3]),
                                        new Vector4(matrixBuffer[4], matrixBuffer[5], matrixBuffer[6], matrixBuffer[7]),
                                        new Vector4(matrixBuffer[8], matrixBuffer[9], matrixBuffer[10], matrixBuffer[11]),
                                        new Vector4(matrixBuffer[12], matrixBuffer[13], matrixBuffer[14], matrixBuffer[15])
                                        );
            index += size;
            return m;
        }

        // Convert Matrix4x4 to byte buffer
        public static byte[] MatrixToBytes(Matrix4x4 matrix)
        {
            byte[] column0Buffer = Vector4ToBytes(matrix.GetColumn(0));
            byte[] column1Buffer = Vector4ToBytes(matrix.GetColumn(1));
            byte[] column2Buffer = Vector4ToBytes(matrix.GetColumn(2));
            byte[] column3Buffer = Vector4ToBytes(matrix.GetColumn(3));
            List<byte[]> buffers = new List<byte[]> { column0Buffer, column1Buffer, column2Buffer, column3Buffer };
            return ConcatenateBuffers(buffers);
        }

        // Convert byte buffer to Quaternion
        public static Quaternion GetQuaternion(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        // Convert Quaternion to byte buffer
        public static byte[] QuaternionToBytes(Quaternion quaternion)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        // convert byte buffer to bool
        public static bool GetBool(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0] == 1;
        }

        // convert bool to byte buffer
        public static byte[] BoolToBytes(bool value)
        {
            byte[] bytes = new byte[sizeof(int)];
            int v = value ? 1 : 0;
            Buffer.BlockCopy(BitConverter.GetBytes(v), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        // convert byte buffer to int
        public static int GetInt(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0];
        }

        // convert int to byte buffer
        public static byte[] IntToBytes(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static byte[] IntsToBytes(int[] values)
        {
            byte[] bytes = new byte[sizeof(int) * values.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        // convert byte buffer to float
        public static float GetFloat(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(float));
            currentIndex += sizeof(float);
            return buffer[0];
        }

        // convert float to byte buffer
        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(float));
            return bytes;
        }

        public static byte[] FloatsToBytes(float[] values)
        {
            byte[] bytes = new byte[sizeof(float) * values.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(values[i]), 0, bytes, index, sizeof(float));
                index += sizeof(float);
            }
            return bytes;
        }

        // concatenate byte buffers
        public static byte[] ConcatenateBuffers(List<byte[]> buffers)
        {
            int totalLength = 0;
            foreach (byte[] buffer in buffers)
            {
                totalLength += buffer.Length;
            }
            byte[] resultBuffer = new byte[totalLength];
            int index = 0;
            foreach (byte[] buffer in buffers)
            {
                int size = buffer.Length;
                Buffer.BlockCopy(buffer, 0, resultBuffer, index, size);
                index += size;
            }
            return resultBuffer;
        }

        public static void BuildClientId(byte[] data)
        {
            string clientId = ConvertToString(data);
            SyncData.mixer.SetClientId(clientId);
        }

        public static void Rename(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string[] srcPath = GetString(data, ref bufferIndex).Split('/');
            string[] dstPath = GetString(data, ref bufferIndex).Split('/');

            string srcName = srcPath[srcPath.Length - 1];
            string dstName = dstPath[dstPath.Length - 1];

            SyncData.Rename(srcName, dstName);
        }

        public static void Delete(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string[] ObjectPath = GetString(data, ref bufferIndex).Split('/');
            string objectName = ObjectPath[ObjectPath.Length - 1];

            SyncData.Delete(objectName);
        }

        public static void Duplicate(Transform prefab, byte[] data)
        {
            int bufferIndex = 0;
            Transform srcPath = FindPath(prefab, data, ref bufferIndex);
            if (srcPath == null)
                return;

            string name = GetString(data, ref bufferIndex);

            Matrix4x4 mat = GetMatrix(data, ref bufferIndex);
            Maths.DecomposeMatrix(mat, out Vector3 position, out Quaternion rotation, out Vector3 scale);

            GameObject newGameObject = SyncData.Duplicate(srcPath.gameObject, name);
            newGameObject.transform.localPosition = position;
            newGameObject.transform.localRotation = rotation;
            newGameObject.transform.localScale = scale;
        }

        public static void BuildSendToTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform objectPath = FindPath(root, data, ref bufferIndex);
            if (null == objectPath)
                return;
            objectPath.parent.parent = SyncData.GetTrash().transform;

            Node node = SyncData.nodes[objectPath.name];
            node.RemoveInstance(objectPath.gameObject);
        }
        public static void BuildRestoreFromTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string objectName = GetString(data, ref bufferIndex);
            Transform parent = FindPath(root, data, ref bufferIndex);
            Transform trf = SyncData.GetTrash().transform.Find(objectName + "_parent");
            if (null != trf)
            {
                trf.parent = parent;

                Node node = SyncData.nodes[objectName];
                node.AddInstance(trf.GetChild(0).gameObject);
            }
        }

        public static void BuildTexture(byte[] data)
        {
            int bufferIndex = 0;
            string path = GetString(data, ref bufferIndex);

            bool packed = GetBool(data, ref bufferIndex);
            int width = GetInt(data, ref bufferIndex);
            int height = GetInt(data, ref bufferIndex);
            int size = GetInt(data, ref bufferIndex);

            byte[] buffer = new byte[size];
            Buffer.BlockCopy(data, bufferIndex, buffer, 0, size);

            textureData[path] = new ImageData
            {
                isEmbedded = packed,
                width = width,
                height = height,
                buffer = buffer
            };
        }

        public static void BuildCollection(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);
            bool visible = GetBool(data, ref bufferIndex);
            Vector3 offset = GetVector3(data, ref bufferIndex);

            bool tempVisible = GetBool(data, ref bufferIndex);

            SyncData.AddCollection(collectionName, offset, visible, tempVisible);
        }

        public static void BuildCollectionRemoved(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.RemoveCollection(collectionName);
        }

        public static void BuildAddCollectionToCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = GetString(data, ref bufferIndex);
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.AddCollectionToCollection(parentCollectionName, collectionName);

        }

        public static void BuildRemoveCollectionFromCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = GetString(data, ref bufferIndex);
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.RemoveCollectionFromCollection(parentCollectionName, collectionName);
        }

        public static void BuildAddObjectToCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);
            string objectName = GetString(data, ref bufferIndex);

            SyncData.AddObjectToCollection(collectionName, objectName);
        }

        public static void BuildRemoveObjectFromCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, ref bufferIndex);
            string objectName = GetString(data, ref bufferIndex);

            SyncData.RemoveObjectFromCollection(collectionName, objectName);
        }

        public static void BuildCollectionInstance(byte[] data)
        {
            int bufferIndex = 0;
            Transform transform = BuildPath(data, ref bufferIndex, true);
            string collectionName = GetString(data, ref bufferIndex);

            SyncData.AddCollectionInstance(transform, collectionName);
        }

        public static void BuildAddObjectToDocument(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = GetString(data, ref bufferIndex);
            if (sceneName != SyncData.currentSceneName)
                return;
            string objectName = GetString(data, ref bufferIndex);
            SyncData.AddObjectToDocument(root, objectName, "/");
        }

        public static void BuilAddCollectionToScene(byte[] data)
        {
            int bufferIndex = 0;
            string _ = GetString(data, ref bufferIndex);
            string collectionName = GetString(data, ref bufferIndex);
            SyncData.sceneCollections.Add(collectionName);
        }

        public static void BuilSetScene(byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = GetString(data, ref bufferIndex);
            SyncData.SetScene(sceneName);
        }

        public static MaterialParameters DefaultMaterial()
        {
            string name = "defaultMaterial";

            if (materialsParameters.TryGetValue(name, out MaterialParameters materialParameters))
                return materialParameters;

            MaterialType materialType;
#if UNITY_EDITOR
            materialType = MaterialType.EditorOpaque;
#else
            materialType = MaterialType.Opaque;
#endif
            materialParameters = new MaterialParameters();
            materialsParameters[name] = materialParameters;
            materialParameters.name = name;
            materialParameters.materialType = materialType;
            materialParameters.opacity = 0;
            materialParameters.opacityTexturePath = "";
            materialParameters.baseColor = new Color(0.8f, 0.8f, 0.8f);
            materialParameters.baseColorTexturePath = "";
            materialParameters.metallic = 0f;
            materialParameters.metallicTexturePath = "";
            materialParameters.roughness = 0.5f;
            materialParameters.roughnessTexturePath = "";
            materialParameters.normalTexturePath = "";
            materialParameters.emissionColor = new Color(0, 0, 0); ;
            materialParameters.emissionColorTexturePath = "";

            return materialParameters;
        }

        public static Texture2D CreateSmallImage()
        {
            Texture2D smallImage = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            smallImage.LoadRawTextureData(new byte[] { 0, 0, 0, 255 });
            return smallImage;
        }

        public static Texture2D GetTexture(string filePath, bool isLinear)
        {

            if (textureData.TryGetValue(filePath, out ImageData imageData))
            {
                textureData.Remove(filePath);
                return SyncData.mixer.LoadTexture(filePath, imageData, isLinear);
            }
            if (textures.ContainsKey(filePath))
            {
                return textures[filePath];
            }
            return null;
        }


        public static Material GetMaterial(MaterialType materialType)
        {
            if (baseMaterials.Count == 0)
            {
                baseMaterials.Add(MaterialType.Opaque, Resources.Load<Material>("Materials/BlenderImport"));
                baseMaterials.Add(MaterialType.Transparent, Resources.Load<Material>("Materials/BlenderImportTransparent"));
                baseMaterials.Add(MaterialType.EditorOpaque, Resources.Load<Material>("Materials/BlenderImportEditor"));
                baseMaterials.Add(MaterialType.EditorTransparent, Resources.Load<Material>("Materials/BlenderImportTransparentEditor"));
                baseMaterials.Add(MaterialType.GreasePencil, Resources.Load<Material>("Materials/GreasePencilMat"));
                baseMaterials.Add(MaterialType.Paint, Resources.Load<Material>("Materials/Paint"));
            }
            return baseMaterials[materialType];
        }

        public static void BuildMaterial(byte[] data)
        {
            int currentIndex = 0;
            string name = GetString(data, ref currentIndex);
            float opacity = GetFloat(data, ref currentIndex);
            string opacityTexturePath = GetString(data, ref currentIndex);

            if (!materialsParameters.TryGetValue(name, out MaterialParameters materialParameters))
            {
                MaterialType materialType;
#if UNITY_EDITOR
                materialType = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? MaterialType.EditorTransparent : MaterialType.EditorOpaque;
#else
                materialType = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? MaterialType.Transparent : MaterialType.Opaque;
#endif
                materialParameters = new MaterialParameters
                {
                    name = name,
                    materialType = materialType
                };
                materialsParameters[name] = materialParameters;
            }


            materialParameters.opacity = opacity;
            materialParameters.opacityTexturePath = opacityTexturePath;
            materialParameters.baseColor = GetColor(data, ref currentIndex);
            materialParameters.baseColorTexturePath = GetString(data, ref currentIndex);
            materialParameters.metallic = GetFloat(data, ref currentIndex);
            materialParameters.metallicTexturePath = GetString(data, ref currentIndex);
            materialParameters.roughness = GetFloat(data, ref currentIndex);
            materialParameters.roughnessTexturePath = GetString(data, ref currentIndex);
            materialParameters.normalTexturePath = GetString(data, ref currentIndex);
            materialParameters.emissionColor = GetColor(data, ref currentIndex);
            materialParameters.emissionColorTexturePath = GetString(data, ref currentIndex);
        }

        public static void ApplyMaterialParameters(MeshRenderer meshRenderer, List<MaterialParameters> meshMaterials)
        {
            MaterialParameters[] materialParameters = meshMaterials.ToArray();
            Material[] materials = new Material[materialParameters.Length];
            for (int i = 0; i < materialParameters.Length; i++)
            {
                materials[i] = GetMaterial(materialParameters[i].materialType);
            }

            Material[] materialsToDestroy = meshRenderer.materials;
            for (int i = 0; i < materialsToDestroy.Length; i++)
                GameObject.Destroy(materialsToDestroy[i]);

            meshRenderer.sharedMaterials = materials;
            Material[] instanceMaterials = meshRenderer.materials;
            for (int i = 0; i < materialParameters.Length; i++)
            {
                ApplyMaterialParameters(instanceMaterials[i], materialParameters[i]);
            }
            meshRenderer.materials = instanceMaterials;
        }

        public static void ApplyMaterialParameters(Material material, MaterialParameters parameters)
        {
            if (parameters.materialType == MaterialType.Paint)
            {
                material.SetColor("_BaseColor", parameters.baseColor);
                return;
            }

            if (parameters.materialType == MaterialType.GreasePencil)
            {
                material.SetColor("_UnlitColor", parameters.baseColor);
                return;
            }

            //
            // OPACITY
            //
            material.SetFloat("_Opacity", parameters.opacity);
            if (parameters.opacityTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(parameters.opacityTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseOpacityMap", 1f);
                    material.SetTexture("_OpacityMap", tex);
                    if (texturesFlipY.Contains(parameters.opacityTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // BASE COLOR
            //
            Color baseColor = parameters.baseColor;
            material.SetColor("_BaseColor", baseColor);
            string baseColorTexturePath = parameters.baseColorTexturePath;
            if (baseColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(baseColorTexturePath, false);
                if (tex != null)
                {
                    material.SetFloat("_UseColorMap", 1f);
                    material.SetTexture("_ColorMap", tex);
                    if (texturesFlipY.Contains(baseColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }

            }

            //
            // METALLIC
            //
            float metallic = parameters.metallic;
            material.SetFloat("_Metallic", metallic);
            string metallicTexturePath = parameters.metallicTexturePath;
            if (metallicTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(metallicTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseMetallicMap", 1f);
                    material.SetTexture("_MetallicMap", tex);
                    if (texturesFlipY.Contains(metallicTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // ROUGHNESS
            //
            float roughness = parameters.roughness;
            material.SetFloat("_Roughness", roughness);
            string roughnessTexturePath = parameters.roughnessTexturePath;
            if (roughnessTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(roughnessTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseRoughnessMap", 1f);
                    material.SetTexture("_RoughnessMap", tex);
                    if (texturesFlipY.Contains(roughnessTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // NORMAL
            //
            string normalTexturePath = parameters.normalTexturePath;
            if (normalTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(normalTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseNormalMap", 1f);
                    material.SetTexture("_NormalMap", tex);
                    if (texturesFlipY.Contains(normalTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // EMISSION
            //
            Color emissionColor = parameters.emissionColor;
            material.SetColor("_EmissiveColor", emissionColor);
            string emissionColorTexturePath = parameters.emissionColorTexturePath;
            if (emissionColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(emissionColorTexturePath, false);
                if (tex != null)
                {
                    material.SetFloat("_UseEmissiveMap", 1f);
                    material.SetTexture("_EmissiveMap", tex);
                    if (texturesFlipY.Contains(emissionColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

        }

        public static void BuildAssignMaterial(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string materialName = GetString(data, ref currentIndex);

            if (!materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
            {
                Debug.LogError("Could not assign material " + materialName + " to " + objectName);
                return;
            }

            Material material = GetMaterial(materialParameters.materialType);
            Node prefabNode = SyncData.nodes[objectName];
            MeshRenderer[] renderers = prefabNode.prefab.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    renderer.sharedMaterial = material;
                    Material instanceMaterial = renderer.material;
                    ApplyMaterialParameters(instanceMaterial, materialParameters);
                    renderer.material = instanceMaterial;
                }
                foreach (Tuple<GameObject, string> item in prefabNode.instances)
                {
                    MeshRenderer[] rends = item.Item1.GetComponentsInChildren<MeshRenderer>();
                    if (rends.Length > 0)
                    {
                        foreach (MeshRenderer rend in rends)
                        {
                            rend.sharedMaterial = material;
                            Material instanceMaterial = rend.material;
                            ApplyMaterialParameters(instanceMaterial, materialParameters);
                            rend.material = instanceMaterial;
                        }
                    }
                }
            }
        }

        public static Transform FindPath(Transform root, byte[] data, ref int bufferIndex)
        {
            string path = MixerUtils.GetString(data, ref bufferIndex);
            if (path == "")
                return root;

            char[] separator = { '/' };
            string[] splitted = path.Split(separator);
            Transform parent = root;
            foreach (string subPath in splitted)
            {
                Transform transform = SyncData.FindChild(parent, subPath);
                if (transform == null)
                {
                    return null;
                }
                parent = transform;
            }
            return parent;
        }

        public static string ConvertToString(byte[] data)
        {
            return System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public static string GetString(byte[] data, ref int bufferIndex)
        {
            int strLength = (int) BitConverter.ToUInt32(data, bufferIndex);
            string str = System.Text.Encoding.UTF8.GetString(data, bufferIndex + 4, strLength);
            bufferIndex = bufferIndex + strLength + 4;
            return str;
        }

        public static string GetPathName(Transform root, Transform transform)
        {
            if (root == transform)
                return "";

            string result = transform.name;
            while (transform.parent && transform.parent.parent && transform.parent.parent != root)
            {
                transform = transform.parent.parent; // skip blender pseudo-parent
                result = transform.name + "/" + result;
            }
            return result;
        }

        public static Transform BuildPath(byte[] data, ref int bufferIndex, bool includeLeaf)
        {
            string path = GetString(data, ref bufferIndex);
            if (!includeLeaf)
            {
                int index = path.LastIndexOf('/');
                if (index == -1)
                    return null;
                path = path.Substring(0, index);
            }
            return SyncData.GetOrCreatePrefabPath(path);
        }

        public static void BuildObjectVisibility(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            bool visible = !GetBool(data, ref currentIndex);
            GetBool(data, ref currentIndex);
            GetBool(data, ref currentIndex);
            bool tempVisible = !GetBool(data, ref currentIndex);

            if (SyncData.nodes.ContainsKey(objectName))
            {
                Node node = SyncData.nodes[objectName];
                node.visible = visible;
                node.tempVisible = tempVisible;
                SyncData.ApplyVisibilityToInstances(root, node.prefab.transform);
            }
        }

        public static Transform BuildTransform(byte[] data)
        {
            int currentIndex = 0;

            Transform transform = BuildPath(data, ref currentIndex, true);

            Matrix4x4 parentInverse = GetMatrix(data, ref currentIndex);
            Matrix4x4 _ = GetMatrix(data, ref currentIndex);
            Matrix4x4 local = GetMatrix(data, ref currentIndex);

            Maths.DecomposeMatrix(parentInverse, out Vector3 t, out Quaternion r, out Vector3 s);
            transform.parent.localPosition = t;
            transform.parent.localRotation = r;
            transform.parent.localScale = s;

            Matrix4x4 localMatrix = parentInverse.inverse * local;
            Maths.DecomposeMatrix(localMatrix, out t, out r, out s);
            transform.localPosition = t;
            transform.localRotation = r;
            transform.localScale = s;

            if (SyncData.nodes.TryGetValue(transform.name, out Node node))
            {
                foreach (Tuple<GameObject, string> instance in node.instances)
                {
                    GameObject obj = instance.Item1;
                    if (SyncData.mixer.IsObjectInUse(obj))
                        continue;
                    obj.transform.localPosition = transform.localPosition;
                    obj.transform.localRotation = transform.localRotation;
                    obj.transform.localScale = transform.localScale;
                }
            }

            return transform;
        }


        /* --------------------------------------------------------------------------------------------
         * 
         *   COMMANDS
         * 
         * -------------------------------------------------------------------------------------------*/

        public static NetCommand BuildObjectVisibilityCommand(Transform transform)
        {
            bool tempVisible = true;
            string parentName = "";
            if (SyncData.nodes.ContainsKey(transform.name))
            {
                Node node = SyncData.nodes[transform.name];
                tempVisible = node.tempVisible;
                if (null != node.parent)
                    parentName = node.parent.prefab.name + "/";
            }
            byte[] name = StringToBytes(parentName + transform.name);
            byte[] hideBuffer = BoolToBytes(!transform.gameObject.activeSelf);
            byte[] hideSelect = BoolToBytes(false);
            byte[] hideInViewport = BoolToBytes(false);
            byte[] hideGet = BoolToBytes(!tempVisible);

            List<byte[]> buffers = new List<byte[]> { name, hideBuffer, hideSelect, hideInViewport, hideGet };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ObjectVisibility);
            return command;
        }
        public static NetCommand BuildTransformCommand(Transform transform)
        {
            string parentName = "";
            if (SyncData.nodes.ContainsKey(transform.name))
            {
                Node node = SyncData.nodes[transform.name];
                if (null != node.parent)
                    parentName = node.parent.prefab.name + "/";
            }
            byte[] name = StringToBytes(parentName + transform.name);
            Matrix4x4 parentMatrix = Matrix4x4.TRS(transform.parent.localPosition, transform.parent.localRotation, transform.parent.localScale);
            Matrix4x4 basisMatrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            byte[] invertParentMatrixBuffer = MatrixToBytes(parentMatrix);
            byte[] basisMatrixBuffer = MatrixToBytes(basisMatrix);
            byte[] localMatrixBuffer = MatrixToBytes(parentMatrix * basisMatrix);

            List<byte[]> buffers = new List<byte[]> { name, invertParentMatrixBuffer, basisMatrixBuffer, localMatrixBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Transform);
            return command;
        }

        public static NetCommand BuildMaterialCommand(Material material)
        {
            byte[] name = StringToBytes(material.name);
            float op = 1f;
            if (material.HasProperty("_Opacity"))
                op = material.GetFloat("_Opacity");
            byte[] opacity = FloatToBytes(op);
            byte[] opacityMapTexture = StringToBytes("");
            byte[] baseColor = ColorToBytes(material.GetColor("_BaseColor"));
            byte[] baseColorTexture = StringToBytes("");
            byte[] metallic = FloatToBytes(material.GetFloat("_Metallic"));
            byte[] metallicTexture = StringToBytes("");
            byte[] roughness = FloatToBytes(material.HasProperty("_Smoothness") ? 1f - material.GetFloat("_Smoothness") : material.GetFloat("_Roughness"));
            byte[] roughnessTexture = StringToBytes("");
            byte[] normalMapTexture = StringToBytes("");
            byte[] emissionColor = ColorToBytes(Color.black);
            byte[] emissionColorTexture = StringToBytes("");

            List<byte[]> buffers = new List<byte[]> { name, opacity, opacityMapTexture, baseColor, baseColorTexture, metallic, metallicTexture, roughness, roughnessTexture, normalMapTexture, emissionColor, emissionColorTexture };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Material);
            return command;
        }

        public static NetCommand BuildAssignMaterialCommand(AssignMaterialInfo info)
        {
            byte[] objectName = StringToBytes(info.objectName);
            byte[] materialName = StringToBytes(info.materialName);
            List<byte[]> buffers = new List<byte[]> { objectName, materialName };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AssignMaterial);
            return command;
        }

        public static NetCommand BuildCameraCommand(Transform root, CameraInfo cameraInfo)
        {
            Transform current = cameraInfo.transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] bpath = StringToBytes(path);

            SyncData.mixer.GetCameraInfo(cameraInfo.transform.gameObject, out float focal, out float near, out float far);
            byte[] bname = StringToBytes(cameraInfo.transform.name);

            Camera cam = cameraInfo.transform.GetComponentInChildren<Camera>(true);
            int sensorFit = (int) cam.gateFit;

            byte[] paramsBuffer = new byte[6 * sizeof(float) + 1 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(focal), 0, paramsBuffer, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(near), 0, paramsBuffer, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(far), 0, paramsBuffer, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(1.8f), 0, paramsBuffer, 3 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(sensorFit), 0, paramsBuffer, 4 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.x), 0, paramsBuffer, 4 * sizeof(float) + sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.y), 0, paramsBuffer, 5 * sizeof(float) + sizeof(int), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { bpath, bname, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Camera);
            return command;
        }

        public static NetCommand BuildLightCommand(Transform root, LightInfo lightInfo)
        {
            Transform current = lightInfo.transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] bpath = StringToBytes(path);
            byte[] bname = StringToBytes(lightInfo.transform.name);

            SyncData.mixer.GetLightInfo(lightInfo.transform.gameObject, out LightType lightType, out bool castShadows, out float power, out Color color, out float _, out float innerAngle, out float outerAngle);

            byte[] paramsBuffer = new byte[2 * sizeof(int) + 7 * sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes((int) lightType), 0, paramsBuffer, 0 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(castShadows ? 1 : 0), 0, paramsBuffer, 1 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(color.r), 0, paramsBuffer, 2 * sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.g), 0, paramsBuffer, 2 * sizeof(int) + 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.b), 0, paramsBuffer, 2 * sizeof(int) + 2 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(color.a), 0, paramsBuffer, 2 * sizeof(int) + 3 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(power), 0, paramsBuffer, 2 * sizeof(int) + 4 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(innerAngle), 0, paramsBuffer, 2 * sizeof(int) + 5 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(outerAngle), 0, paramsBuffer, 2 * sizeof(int) + 6 * sizeof(float), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { bpath, bname, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Light);
            return command;
        }

        public static NetCommand BuildSkyCommand(SkySettings skyInfo)
        {
            byte[] skyNameBuffer = StringToBytes("Sky"); // optimized commands need a name
            byte[] topBuffer = ColorToBytes(skyInfo.topColor);
            byte[] middleBuffer = ColorToBytes(skyInfo.middleColor);
            byte[] bottomBuffer = ColorToBytes(skyInfo.bottomColor);

            List<byte[]> buffers = new List<byte[]> { skyNameBuffer, topBuffer, middleBuffer, bottomBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Sky);
            return command;
        }

        public static NetCommand BuildRenameCommand(Transform root, RenameInfo rename)
        {
            string src = GetPathName(root, rename.srcTransform);
            byte[] srcPath = StringToBytes(src);
            byte[] dstName = StringToBytes(rename.newName);
            Debug.Log($"{rename.srcTransform.name}: {src} --> {rename.newName}");

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Rename);
            return command;
        }

        public static NetCommand BuildDuplicateCommand(Transform root, DuplicateInfos duplicate)
        {
            byte[] srcPath = StringToBytes(GetPathName(root, duplicate.srcObject.transform));
            byte[] dstName = StringToBytes(duplicate.dstObject.name);

            Transform transform = duplicate.dstObject.transform;
            byte[] matrixBuffer = MatrixToBytes(Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale));

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName, matrixBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Duplicate);
            return command;
        }

        public static NetCommand BuildSendToTrashCommand(Transform root, SendToTrashInfo sendToTrash)
        {
            byte[] path = StringToBytes(GetPathName(root, sendToTrash.transform));
            List<byte[]> buffers = new List<byte[]> { path };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.SendToTrash);
            return command;
        }

        public static NetCommand BuildRestoreFromTrashCommand(Transform root, RestoreFromTrashInfo restoreFromTrash)
        {
            string parentPath = GetPathName(root, restoreFromTrash.parent);

            byte[] nameBuffer = StringToBytes(restoreFromTrash.transform.name);
            byte[] pathBuffer = StringToBytes(parentPath);

            List<byte[]> buffers = new List<byte[]> { nameBuffer, pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.RestoreFromTrash);
            return command;
        }


        public static NetCommand BuildMeshCommand(Transform root, MeshInfos meshInfos)
        {
            Mesh mesh = meshInfos.meshFilter.mesh;
            byte[] name = StringToBytes(mesh.name);

            byte[] baseMeshSize = IntToBytes(0);

            byte[] positions = Vector3ToBytes(mesh.vertices);

            int[] baseTriangles = mesh.triangles;
            Vector3[] baseNormals = mesh.normals;
            Vector3[] splittedNormals = new Vector3[baseTriangles.Length];
            for (int i = 0; i < splittedNormals.Length; i++)
            {
                int id = baseTriangles[i];
                splittedNormals[i] = baseNormals[id];

            }
            byte[] normals = Vector3ToBytes(splittedNormals);

            Vector2[] baseUVs = mesh.uv;
            Vector2[] splittedUVs;
            if (null != mesh.uv && mesh.uv.Length > 0)
            {
                splittedUVs = new Vector2[baseTriangles.Length];
                for (int i = 0; i < splittedNormals.Length; i++)
                {
                    int id = baseTriangles[i];
                    splittedUVs[i] = baseUVs[id];
                }
            }
            else
            {
                splittedUVs = new Vector2[0];
            }
            byte[] uvs = Vector2ToBytes(splittedUVs);

            int[] materialIndices;
            if (mesh.subMeshCount <= 1)
            {
                materialIndices = new int[0];
            }
            else
            {
                materialIndices = new int[baseTriangles.Length / 3];
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    SubMeshDescriptor subMesh = mesh.GetSubMesh(i);
                    for (int j = subMesh.indexStart / 3; j < (subMesh.indexStart + subMesh.indexCount) / 3; j++)
                    {
                        materialIndices[j] = i;
                    }
                }

            }

            byte[] materialIndicesBuffer = new byte[materialIndices.Length * sizeof(int) + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(materialIndices.Length), 0, materialIndicesBuffer, 0, sizeof(int));
            Buffer.BlockCopy(materialIndices, 0, materialIndicesBuffer, sizeof(int), materialIndices.Length * sizeof(int));

            byte[] triangles = TriangleIndicesToBytes(baseTriangles);

            Material[] materials = meshInfos.meshRenderer.materials;
            string[] materialNames = new string[materials.Length];
            int index = 0;
            foreach (Material material in materials)
            {
                materialNames[index++] = material.name;
            }
            byte[] materialsBuffer = StringsToBytes(materialNames);

            Transform transform = meshInfos.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = StringToBytes(path);

            byte[] bakedMeshSize = IntToBytes(positions.Length + normals.Length + uvs.Length + materialIndicesBuffer.Length + triangles.Length);

            // necessary to satisfy baked mesh server format
            //////////////////////////////////////////////////
            int materialCount = materials.Length;
            byte[] materialLinksBuffer = new byte[sizeof(int) * materialCount];
            index = 0;
            for (int i = 0; i < materialCount; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(1), 0, materialLinksBuffer, index, sizeof(int));
                index += sizeof(int);
            }
            byte[] materialLinkNamesBuffer = StringsToBytes(materialNames, false);
            //////////////////////////////////////////////////

            List<byte[]> buffers = new List<byte[]> { pathBuffer, name, baseMeshSize, bakedMeshSize, positions, normals, uvs, materialIndicesBuffer, triangles, materialsBuffer, materialLinksBuffer, materialLinkNamesBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Mesh);
            return command;
        }

        public static NetCommand BuildAddCollecitonCommand(string collectionName)
        {
            byte[] collectionNameBuffer = StringToBytes(collectionName);
            byte[] visible = BoolToBytes(true);
            byte[] offset = Vector3ToBytes(Vector3.zero);
            byte[] temporaryVisible = BoolToBytes(true);
            List<byte[]> buffers = new List<byte[]> { collectionNameBuffer, visible, offset, temporaryVisible };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Collection);
            return command;
        }

        public static NetCommand BuildAddCollectionToScene(string collectionName)
        {
            byte[] sceneNameBuffer = StringToBytes(SyncData.currentSceneName);
            byte[] collectionNameBuffer = StringToBytes(collectionName);
            List<byte[]> buffers = new List<byte[]> { sceneNameBuffer, collectionNameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AddCollectionToScene);
            SyncData.sceneCollections.Add(collectionName);
            return command;
        }


        public static NetCommand BuildAddObjectToCollecitonCommand(AddToCollectionInfo info)
        {
            byte[] collectionNameBuffer = StringToBytes(info.collectionName);
            byte[] objectNameBuffer = StringToBytes(info.transform.name);

            List<byte[]> buffers = new List<byte[]> { collectionNameBuffer, objectNameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AddObjectToCollection);
            return command;
        }

        public static NetCommand BuildAddObjectToScene(AddObjectToSceneInfo info)
        {
            byte[] sceneNameBuffer = StringToBytes(SyncData.currentSceneName);
            byte[] objectNameBuffer = StringToBytes(info.transform.name);
            List<byte[]> buffers = new List<byte[]> { sceneNameBuffer, objectNameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.AddObjectToDocument);
            return command;
        }

        public static NetCommand BuildDeleteCommand(Transform root, DeleteInfo deleteInfo)
        {
            Transform transform = deleteInfo.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = StringToBytes(path);

            List<byte[]> buffers = new List<byte[]> { pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Delete);
            return command;
        }

        public static void BuildAnimation(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string animationChannel = GetString(data, ref currentIndex);
            int channelIndex = BitConverter.ToInt32(data, currentIndex);
            currentIndex += 4;

            int keyCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            int[] intBuffer = new int[keyCount];
            float[] floatBuffer = new float[keyCount];
            int[] interpolationBuffer = new int[keyCount];

            Buffer.BlockCopy(data, currentIndex, intBuffer, 0, keyCount * sizeof(int));
            Buffer.BlockCopy(data, currentIndex + keyCount * sizeof(int), floatBuffer, 0, keyCount * sizeof(float));
            Buffer.BlockCopy(data, currentIndex + (keyCount * sizeof(int)) + (keyCount * sizeof(float)), interpolationBuffer, 0, keyCount * sizeof(int));

            SyncData.mixer.CreateAnimationCurve(objectName, animationChannel, channelIndex, intBuffer, floatBuffer, interpolationBuffer);
        }

        public static void BuildAddKeyframe(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string channelName = GetString(data, ref currentIndex);
            int channelIndex = GetInt(data, ref currentIndex);
            int frame = GetInt(data, ref currentIndex);
            float value = GetFloat(data, ref currentIndex);
            int interpolation = GetInt(data, ref currentIndex);

            SyncData.mixer.CreateAnimationKey(objectName, channelName, channelIndex, frame, value, interpolation);
        }

        public static void BuildRemoveKeyframe(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string channelName = GetString(data, ref currentIndex);
            int channelIndex = GetInt(data, ref currentIndex);
            int frame = GetInt(data, ref currentIndex);

            SyncData.mixer.RemoveAnimationKey(objectName, channelName, channelIndex, frame);
        }

        public static void BuildMoveKeyframe(byte[] data)
        {
            int currentIndex = 0;
            string objectName = GetString(data, ref currentIndex);
            string channelName = GetString(data, ref currentIndex);
            int channelIndex = GetInt(data, ref currentIndex);
            int frame = GetInt(data, ref currentIndex);
            int newFrame = GetInt(data, ref currentIndex);

            SyncData.mixer.MoveAnimationKey(objectName, channelName, channelIndex, frame, newFrame);
        }

        public static void BuildCameraAttributes(byte[] data)
        {
            int currentIndex = 0;
            string cameraName = GetString(data, ref currentIndex);

            Node node = SyncData.nodes[cameraName];
            float focal = GetFloat(data, ref currentIndex);
            SyncData.mixer.SetCameraInfo(node.prefab, focal);

            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                SyncData.mixer.SetCameraInfo(t.Item1, focal);
            }
        }

        public static void BuildLightAttributes(byte[] data)
        {
            int currentIndex = 0;
            string lightName = GetString(data, ref currentIndex);

            Node node = SyncData.nodes[lightName];
            SyncData.mixer.GetLightInfo(node.prefab, out LightType lightType, out bool castShadows, out float _, out Color _, out float range, out float innerAngle, out float outerAngle);

            float power = GetFloat(data, ref currentIndex);
            Color color = GetColor(data, ref currentIndex);

            SyncData.mixer.SetLightInfo(node.prefab, lightType, castShadows, power, color, range, innerAngle, outerAngle);

            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                SyncData.mixer.SetLightInfo(t.Item1, lightType, castShadows, power, color, range, innerAngle, outerAngle);
            }
        }

        public static void BuildCamera(Transform root, GameObject cameraPrefab, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }
            string name = GetString(data, ref currentIndex);

            GameObject camGameObject;
            Node node;
            if (!SyncData.nodes.ContainsKey(leafName))
            {
                camGameObject = SyncData.CreateInstance(cameraPrefab, transform, leafName, isPrefab: true);
                node = SyncData.CreateNode(name);
                node.prefab = camGameObject;
            }
            else // TODO: found a case where a camera was found (don't know when it was created???), but had no Camera child object.
            {
                node = SyncData.nodes[leafName];
                camGameObject = node.prefab;
            }

            float focal = BitConverter.ToSingle(data, currentIndex);
            BitConverter.ToSingle(data, currentIndex + sizeof(float));
            BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            currentIndex += 4 * sizeof(float);

            Camera.GateFitMode gateFit = (Camera.GateFitMode) BitConverter.ToInt32(data, currentIndex);
            if (gateFit == Camera.GateFitMode.None)
                gateFit = Camera.GateFitMode.Horizontal;
            currentIndex += sizeof(Int32);

            float sensorWidth = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float sensorHeight = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            Camera cam = camGameObject.GetComponentInChildren<Camera>(true);

            // TMP fix for a weird case.
            if (cam == null)
                return;

            SyncData.mixer.SetCameraInfo(camGameObject, focal);

            cam.focalLength = focal;
            cam.gateFit = gateFit;
            cam.focalLength = focal;
            cam.sensorSize = new Vector2(sensorWidth, sensorHeight);
        }

        public static void BuildLight(Transform root, GameObject sunPrefab, GameObject pointPrefab, GameObject spotPrefab, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }
            GetString(data, ref currentIndex);

            LightType lightType = (LightType) BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            GameObject lightGameObject;
            Node node;
            if (!SyncData.nodes.ContainsKey(leafName))
            {
                switch (lightType)
                {
                    case LightType.Directional:
                        lightGameObject = SyncData.CreateInstance(sunPrefab, transform, leafName);
                        break;
                    case LightType.Point:
                        lightGameObject = SyncData.CreateInstance(pointPrefab, transform, leafName);
                        break;
                    case LightType.Spot:
                        lightGameObject = SyncData.CreateInstance(spotPrefab, transform, leafName);
                        break;
                    default:
                        return;
                }
                node = SyncData.CreateNode(leafName);
                node.prefab = lightGameObject;
            }
            else
            {
                node = SyncData.nodes[leafName];
                lightGameObject = node.prefab;
            }

            // Read data
            bool castShadows = BitConverter.ToInt32(data, currentIndex) != 0;
            currentIndex += sizeof(Int32);

            float ColorR = BitConverter.ToSingle(data, currentIndex);
            float ColorG = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float ColorB = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float ColorA = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            Color lightColor = new Color(ColorR, ColorG, ColorB, ColorA);
            currentIndex += 4 * sizeof(float);

            float power = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotSize = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotBlend = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            // Set data to all instances
            float range = 5f;
            float innerAngle = (1f - spotBlend) * 100f;
            float outerAngle = spotSize * 180f / 3.14f;
            SyncData.mixer.SetLightInfo(lightGameObject, lightType, castShadows, power, lightColor, range, innerAngle, outerAngle);

            foreach (Tuple<GameObject, string> t in node.instances)
            {
                SyncData.mixer.SetLightInfo(t.Item1, lightType, castShadows, power, lightColor, 5f, innerAngle, outerAngle);
            }
        }

        public static void BuildSky(byte[] data)
        {
            int currentIndex = 0;
            GetString(data, ref currentIndex);
            Color topColor = GetColor(data, ref currentIndex);
            Color middleColor = GetColor(data, ref currentIndex);
            Color bottomColor = GetColor(data, ref currentIndex);
            SyncData.mixer.SetSkyColors(topColor, middleColor, bottomColor);
        }

        public static NetCommand BuildSendClearAnimations(ClearAnimationInfo info)
        {
            NetCommand command = new NetCommand(StringToBytes(info.gObject.name), MessageType.ClearAnimations);
            return command;
        }

        public static NetCommand BuildSendMontageMode(bool montage)
        {
            NetCommand command = new NetCommand(BoolToBytes(montage), MessageType.ShotManagerMontageMode);
            return command;
        }

        public static NetCommand BuildSendShotManagerAction(ShotManagerActionInfo info)
        {
            NetCommand command = null;
            List<byte[]> buffers;
            byte[] shotName;
            byte[] start;
            byte[] end;
            byte[] camera;
            byte[] color;
            byte[] enabled;

            byte[] action = IntToBytes((int) info.action);
            byte[] shotIndex = IntToBytes(info.shotIndex);

            switch (info.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        byte[] nextShotIndex = IntToBytes(info.shotIndex + 1);
                        shotName = StringToBytes(info.shotName);
                        start = IntToBytes(info.shotStart);
                        end = IntToBytes(info.shotEnd);
                        camera = StringToBytes(info.cameraName);
                        color = ColorToBytes(info.shotColor);
                        buffers = new List<byte[]> { action, nextShotIndex, shotName, start, end, camera, color };
                        command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    buffers = new List<byte[]> { action, shotIndex };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.DuplicateShot:
                    shotName = StringToBytes(info.shotName);
                    buffers = new List<byte[]> { action, shotIndex, shotName };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.MoveShot:
                    byte[] offset = IntToBytes(info.moveOffset);
                    buffers = new List<byte[]> { action, shotIndex, offset };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.UpdateShot:
                    start = IntToBytes(info.shotStart);
                    end = IntToBytes(info.shotEnd);
                    camera = StringToBytes(info.cameraName);
                    color = ColorToBytes(info.shotColor);
                    enabled = IntToBytes(info.shotEnabled);
                    buffers = new List<byte[]> { action, shotIndex, start, end, camera, color, enabled };
                    command = new NetCommand(ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
            }
            return command;
        }

        public static NetCommand BuildSendPlayerTransform(ConnectedUser playerInfo)
        {
            if (null == SyncData.mixer)
                return null;
            string json = SyncData.mixer.CreateJsonPlayerInfo(playerInfo);
            if (null == json) { return null; }
            byte[] buffer = StringToBytes(json);
            NetCommand command = new NetCommand(buffer, MessageType.SetClientCustomAttribute);
            return command;
        }

        public static MeshFilter GetOrCreateMeshFilter(GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = obj.AddComponent<MeshFilter>();
            return meshFilter;
        }

        public static MeshRenderer GetOrCreateMeshRenderer(GameObject obj)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = obj.AddComponent<MeshRenderer>();
            return meshRenderer;
        }
        public static MeshCollider GetOrCreateMeshCollider(GameObject obj)
        {
            MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = obj.AddComponent<MeshCollider>();
                //meshCollider.convex = true;
            }
            return meshCollider;
        }

        public static Transform ConnectMesh(Transform transform, Mesh mesh)
        {
            GameObject gobject = transform.gameObject;

            SyncData.mixer.UpdateTag(gobject);

            MeshFilter filter = GetOrCreateMeshFilter(gobject);
            string meshName = mesh.name;

            //foreach (MeshFilter filter in meshInstances[meshName])
            {
                filter.mesh = mesh;
                GameObject obj = filter.gameObject;
                MeshRenderer meshRenderer = GetOrCreateMeshRenderer(obj);

                ApplyMaterialParameters(meshRenderer, meshesMaterials[meshName]);
                GetOrCreateMeshCollider(obj);

                if (SyncData.nodes.ContainsKey(obj.name))
                {
                    foreach (Tuple<GameObject, string> t in SyncData.nodes[obj.name].instances)
                    {
                        GameObject instance = t.Item1;
                        MeshFilter instanceMeshFilter = GetOrCreateMeshFilter(instance);
                        instanceMeshFilter.mesh = mesh;

                        MeshRenderer instanceMeshRenderer = GetOrCreateMeshRenderer(instance);
                        ApplyMaterialParameters(instanceMeshRenderer, meshesMaterials[meshName]);

                        MeshCollider meshCollider = GetOrCreateMeshCollider(instance);
                        meshCollider.sharedMesh = null;
                        meshCollider.sharedMesh = mesh;
                    }
                }
            }

            MeshCollider collider = gobject.GetComponent<MeshCollider>();
            if (null != collider)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = mesh;
            }

            return transform;
        }

        public static Mesh BuildMesh(byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, true);
            string meshName = GetString(data, ref currentIndex);

            int baseMeshDataSize = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4 + baseMeshDataSize;

            int bakedMeshDataSize = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            if (bakedMeshDataSize == 0)
                return null;

            int rawVerticesCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int size = rawVerticesCount * sizeof(float) * 3;
            Vector3[] rawVertices = new Vector3[rawVerticesCount];
            float[] float3Values = new float[rawVerticesCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            int idx = 0;
            for (int i = 0; i < rawVerticesCount; i++)
            {
                rawVertices[i].x = float3Values[idx++];
                rawVertices[i].y = float3Values[idx++];
                rawVertices[i].z = float3Values[idx++];
            }
            currentIndex += size;

            int normalsCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            size = normalsCount * sizeof(float) * 3;
            Vector3[] normals = new Vector3[normalsCount];
            float3Values = new float[normalsCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < normalsCount; i++)
            {
                normals[i].x = float3Values[idx++];
                normals[i].y = float3Values[idx++];
                normals[i].z = float3Values[idx++];
            }
            currentIndex += size;

            UInt32 UVsCount = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            size = (int) UVsCount * sizeof(float) * 2;
            Vector2[] uvs = new Vector2[UVsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < UVsCount; i++)
            {
                uvs[i].x = float3Values[idx++];
                uvs[i].y = float3Values[idx++];
            }
            currentIndex += size;

            int materialIndicesCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int[] materialIndices = new int[materialIndicesCount];
            size = materialIndicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, materialIndices, 0, size);
            currentIndex += size;

            int rawIndicesCount = (int) BitConverter.ToUInt32(data, currentIndex) * 3;
            currentIndex += 4;
            int[] rawIndices = new int[rawIndicesCount];
            size = rawIndicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, rawIndices, 0, size);
            currentIndex += size;

            Vector3[] vertices = new Vector3[rawIndicesCount];
            for (int i = 0; i < rawIndicesCount; i++)
            {
                vertices[i] = rawVertices[rawIndices[i]];
            }

            int materialCount = (int) BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            List<MaterialParameters> meshMaterialParameters = new List<MaterialParameters>();
            if (materialCount == 0)
            {
                meshMaterialParameters.Add(DefaultMaterial());
                materialCount = 1;
            }
            else
            {
                for (int i = 0; i < materialCount; i++)
                {
                    int materialNameSize = (int) BitConverter.ToUInt32(data, currentIndex);
                    string materialName = System.Text.Encoding.UTF8.GetString(data, currentIndex + 4, materialNameSize);
                    currentIndex += materialNameSize + 4;

                    if (materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
                    {
                        meshMaterialParameters.Add(materialParameters);
                    }
                    else
                    {
                        meshMaterialParameters.Add(DefaultMaterial());
                    }
                }
            }

            Mesh mesh = new Mesh
            {
                name = meshName,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            if (materialCount == 1) // only one submesh
            {
                int[] indices = new int[rawIndicesCount];
                for (int i = 0; i < rawIndicesCount; i++)
                {
                    indices[i] = i;
                }

                mesh.triangles = indices;
            }
            else
            {
                List<int>[] subIndicesArray = new List<int>[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    subIndicesArray[i] = new List<int>();
                }

                for (int i = 0; i < materialIndicesCount; i++)
                {
                    int materialIndex = materialIndices[i];
                    List<int> subIndices = subIndicesArray[materialIndex];
                    int index = 3 * i;
                    subIndices.Add(index);
                    subIndices.Add(index + 1);
                    subIndices.Add(index + 2);
                }

                mesh.subMeshCount = materialCount;
                int matIndex = 0;
                for (int i = 0; i < materialCount; i++)
                {
                    List<int> subIndices = subIndicesArray[i];
                    if (subIndices.Count == 0)
                    {
                        meshMaterialParameters.RemoveAt(matIndex);
                    }
                    else
                    {
                        mesh.SetTriangles(subIndices.ToArray(), matIndex++);
                    }
                }

            }

            mesh.RecalculateBounds();
            meshes[meshName] = mesh;
            meshesMaterials[meshName] = meshMaterialParameters;

            ConnectMesh(transform, mesh);
            return mesh;
        }

        public static void BuildGreasePencil(byte[] data)
        {
            int currentIndex = 0;
            string greasePencilPath = GetString(data, ref currentIndex);
            string greasePencilName = GetString(data, ref currentIndex);
            string[] path = greasePencilPath.Split('/');
            path[path.Length - 1] = greasePencilName;
            Transform prefab = SyncData.GetOrCreatePrefabPath(String.Join("/", path));

            SyncData.greasePencilsNameToPrefab[greasePencilName] = prefab.name;
        }


        private static bool IsFillEnabled(string materialName)
        {
            string name = materialName + "_fill";
            return materialsFillEnabled.Contains(name);
        }

        private static bool IsStrokeEnabled(string materialName)
        {
            string name = materialName + "_stroke";
            return materialStrokesEnabled.Contains(name);
        }

        public static MaterialParameters BuildGreasePencilMaterial(string materialName, Color color)
        {
            if (!materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
            {
                materialParameters = new MaterialParameters
                {
                    materialType = MaterialType.GreasePencil,
                    name = materialName
                };
                materialsParameters[materialName] = materialParameters;
            }

            materialParameters.baseColor = color;
            return materialParameters;
        }


        public static void BuildGreasePencilMaterial(byte[] data)
        {
            int currentIndex = 0;
            string materialName = GetString(data, ref currentIndex);
            bool strokeEnabled = GetBool(data, ref currentIndex);
            GetString(data, ref currentIndex);
            GetString(data, ref currentIndex);
            Color strokeColor = GetColor(data, ref currentIndex);
            GetBool(data, ref currentIndex);
            bool fillEnabled = GetBool(data, ref currentIndex);
            GetString(data, ref currentIndex);
            Color fillColor = GetColor(data, ref currentIndex);

            string materialStrokeName = materialName + "_stroke";
            string materialFillName = materialName + "_fill";
            BuildGreasePencilMaterial(materialStrokeName, strokeColor);
            BuildGreasePencilMaterial(materialFillName, fillColor);

            // stroke enable
            if (strokeEnabled)
            {
                materialStrokesEnabled.Add(materialStrokeName);
            }
            else
            {
                if (materialStrokesEnabled.Contains(materialStrokeName))
                    materialStrokesEnabled.Remove(materialStrokeName);
            }

            // fill
            if (fillEnabled)
            {
                materialsFillEnabled.Add(materialFillName);
            }
            else
            {
                if (materialsFillEnabled.Contains(materialFillName))
                    materialsFillEnabled.Remove(materialFillName);
            }
        }

        public static void BuildStroke(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, int strokeIndex, ref GPFrame frame)
        {
            int materialIndex = GetInt(data, ref currentIndex);
            int lineWidth = GetInt(data, ref currentIndex);
            int numPoints = GetInt(data, ref currentIndex);
            float[] points = new float[5 * numPoints];

            int dataSize = 5 * sizeof(float) * numPoints;
            Buffer.BlockCopy(data, currentIndex, points, 0, dataSize);
            currentIndex += dataSize;

            float layerOffset = 0.001f * layerIndex;
            float strokeOffset = 0.0001f * strokeIndex;

            if ((materialIndex < materialNames.Length) && IsStrokeEnabled(materialNames[materialIndex]))
            {
                Vector3 offset = new Vector3(0.0f, -(strokeOffset + layerOffset), 0.0f);
                GPStroke subMesh = new GPStroke();
                SyncData.mixer.CreateStroke(points, numPoints, lineWidth, offset, ref subMesh);
                subMesh.materialParameters = materialsParameters[materialNames[materialIndex] + "_stroke"];
                frame.strokes.Add(subMesh);
            }

            if ((materialIndex < materialNames.Length) && IsFillEnabled(materialNames[materialIndex]))
            {
                Vector3 offset = new Vector3(0.0f, -(strokeOffset + layerOffset), 0.0f);
                GPStroke subMesh = new GPStroke();
                SyncData.mixer.CreateFill(points, numPoints, offset, ref subMesh);
                subMesh.materialParameters = materialsParameters[materialNames[materialIndex] + "_fill"];
                frame.strokes.Add(subMesh);
            }
        }

        public static void BuildFrame(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, ref GPLayer layer, int frameIndex)
        {
            int frameNumber = GetInt(data, ref currentIndex);
            if (frameIndex == 0)
                frameNumber = 0;
            GPFrame frame = new GPFrame(frameNumber);
            layer.frames.Add(frame);

            int strokeCount = GetInt(data, ref currentIndex);
            for (int strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
            {
                BuildStroke(data, ref currentIndex, materialNames, layerIndex, strokeIndex, ref frame);
            }
        }


        public static void BuildLayer(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, ref List<GPLayer> layers)
        {
            string layerName = GetString(data, ref currentIndex);
            bool hidden = GetBool(data, ref currentIndex);
            GPLayer layer = new GPLayer(layerName)
            {
                visible = !hidden
            };
            layers.Add(layer);

            int frameCount = GetInt(data, ref currentIndex);
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                BuildFrame(data, ref currentIndex, materialNames, layerIndex, ref layer, frameIndex);
            }
        }

        public static Tuple<Mesh, List<MaterialParameters>> BuildGPFrameMesh(List<GPStroke> strokes)
        {
            // Build mesh from sub-meshes
            int vertexCount = 0;
            foreach (var meshMaterial in strokes)
            {
                if (null == meshMaterial.vertices)
                    return null;
                vertexCount += meshMaterial.vertices.Length;
            }

            Vector3[] vertices = new Vector3[vertexCount];
            int currentVertexIndex = 0;

            foreach (var subMesh in strokes)
            {
                Array.Copy(subMesh.vertices, 0, vertices, currentVertexIndex, subMesh.vertices.Length);
                currentVertexIndex += subMesh.vertices.Length;
            }

            Mesh mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                subMeshCount = strokes.Count,
                vertices = vertices
            };

            int currentSubMesh = 0;
            List<MaterialParameters> mats = new List<MaterialParameters>();

            Tuple<Mesh, List<MaterialParameters>> result = new Tuple<Mesh, List<MaterialParameters>>(mesh, mats);

            int currentIndexIndex = 0;
            foreach (var subMesh in strokes)
            {
                int verticesCount = subMesh.vertices.Length;
                int[] triangles = new int[subMesh.triangles.Length];
                for (int i = 0; i < subMesh.triangles.Length; i++)
                {
                    triangles[i] = subMesh.triangles[i] + currentIndexIndex;
                }

                mesh.SetTriangles(triangles, currentSubMesh++);
                mats.Add(subMesh.materialParameters);

                currentIndexIndex += verticesCount;
            }
            return result;
        }

        static SortedSet<int> GetFrames(List<GPLayer> layers)
        {
            SortedSet<int> frames = new SortedSet<int>();
            foreach (GPLayer layer in layers)
            {
                foreach (GPFrame frame in layer.frames)
                    frames.Add(frame.frame);
            }

            return frames;
        }

        static List<GPFrame> GetGPFrames(List<GPLayer> layers, int f)
        {
            List<GPFrame> frames = new List<GPFrame>();
            foreach (GPLayer layer in layers)
            {
                if (!layer.visible)
                    continue;
                for (int i = layer.frames.Count - 1; i >= 0; --i)
                {
                    GPFrame gpframe = layer.frames[i];
                    if (gpframe.frame <= f)
                    {
                        frames.Add(gpframe);
                        break;
                    }
                }
            }
            return frames;
        }

        static List<GPStroke> GetStrokes(List<GPFrame> frames)
        {
            List<GPStroke> strokes = new List<GPStroke>();
            foreach (GPFrame frame in frames)
            {
                foreach (GPStroke stroke in frame.strokes)
                    strokes.Add(stroke);
            }
            return strokes;
        }

        public static void BuildGreasePencilMesh(byte[] data)
        {
            int currentIndex = 0;
            string name = GetString(data, ref currentIndex);

            int materialCount = GetInt(data, ref currentIndex);
            string[] materialNames = new string[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                materialNames[i] = GetString(data, ref currentIndex);
            }

            List<GPLayer> layers = new List<GPLayer>();

            int layerCount = GetInt(data, ref currentIndex);
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                BuildLayer(data, ref currentIndex, materialNames, layerIndex, ref layers);
            }

            SortedSet<int> frames = GetFrames(layers);

            GreasePencilData gpdata = new GreasePencilData();
            greasePencils[name] = gpdata;

            if (frames.Count == 0)
                return;
            foreach (int frame in frames)
            {
                List<GPFrame> gpframes = GetGPFrames(layers, frame);
                List<GPStroke> strokes = GetStrokes(gpframes);

                Tuple<Mesh, List<MaterialParameters>> meshData = BuildGPFrameMesh(strokes);
                if (null == meshData)
                    continue;

                meshData.Item1.RecalculateBounds();
                gpdata.AddMesh(frame, new Tuple<Mesh, List<MaterialParameters>>(meshData.Item1, meshData.Item2));
            }
        }
        public static void BuildGreasePencilConnection(byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, true);
            string greasePencilName = GetString(data, ref currentIndex);

            GreasePencilData gpdata = greasePencils[greasePencilName];

            SyncData.mixer.BuildGreasePencilConnection(transform.gameObject, gpdata);
        }

        public static void BuildGreasePencilTimeOffset(byte[] data)
        {
            int currentIndex = 0;
            string name = GetString(data, ref currentIndex);
            GreasePencilData gpData = greasePencils[name];
            gpData.frameOffset = GetInt(data, ref currentIndex);
            gpData.frameScale = GetFloat(data, ref currentIndex);
            gpData.hasCustomRange = GetBool(data, ref currentIndex);
            gpData.rangeStartFrame = GetInt(data, ref currentIndex);
            gpData.rangeEndFrame = GetInt(data, ref currentIndex);

        }

        public static void BuildPlay()
        {
            SyncData.mixer.SetPlaying(true);
        }

        public static void BuildPause()
        {
            SyncData.mixer.SetPlaying(false);
        }

        public static NetCommand BuildSendFrameStartEndCommand(int start, int end)
        {
            byte[] startBuffer = IntToBytes((int) start);
            byte[] endBuffer = IntToBytes((int) end);
            List<byte[]> buffers = new List<byte[]> { startBuffer, endBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.FrameStartEnd);
        }

        private static void VRtistToBlenderAnimation(AnimatableProperty property, out string channelName, out int channelIndex)
        {
            channelName = "";
            channelIndex = -1;
            switch (property)
            {
                case AnimatableProperty.PositionX: channelName = "location"; channelIndex = 0; break;
                case AnimatableProperty.PositionY: channelName = "location"; channelIndex = 1; break;
                case AnimatableProperty.PositionZ: channelName = "location"; channelIndex = 2; break;

                case AnimatableProperty.RotationX: channelName = "rotation_euler"; channelIndex = 0; break;
                case AnimatableProperty.RotationY: channelName = "rotation_euler"; channelIndex = 1; break;
                case AnimatableProperty.RotationZ: channelName = "rotation_euler"; channelIndex = 2; break;

                case AnimatableProperty.ScaleX: channelName = "scale"; channelIndex = 0; break;
                case AnimatableProperty.ScaleY: channelName = "scale"; channelIndex = 1; break;
                case AnimatableProperty.ScaleZ: channelName = "scale"; channelIndex = 2; break;

                case AnimatableProperty.CameraFocal: channelName = "lens"; channelIndex = -1; break;

                case AnimatableProperty.LightIntensity: channelName = "energy"; channelIndex = -1; break;
                case AnimatableProperty.ColorR: channelName = "color"; channelIndex = 0; break;
                case AnimatableProperty.ColorG: channelName = "color"; channelIndex = 1; break;
                case AnimatableProperty.ColorB: channelName = "color"; channelIndex = 2; break;
            }
        }

        public static NetCommand BuildSendSetKey(SetKeyInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.property, out string channelName, out int channelIndex);

            byte[] channelNameBuffer = StringToBytes(channelName);
            byte[] channelIndexBuffer = IntToBytes(channelIndex);
            byte[] frameBuffer = IntToBytes(data.key.frame);

            float value = data.key.value;
            if (data.property == AnimatableProperty.RotationX || data.property == AnimatableProperty.RotationY || data.property == AnimatableProperty.RotationZ)
            {
                value = Mathf.Deg2Rad * value;
            }
            byte[] valueBuffer = FloatToBytes(value);
            byte[] interpolationBuffer = IntToBytes((int) data.key.interpolation);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer, valueBuffer, interpolationBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.AddKeyframe);
        }

        public static NetCommand BuildSendRemoveKey(SetKeyInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.property, out string channelName, out int channelIndex);
            byte[] channelNameBuffer = StringToBytes(channelName);
            byte[] channelIndexBuffer = IntToBytes(channelIndex);
            byte[] frameBuffer = IntToBytes(data.key.frame);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.RemoveKeyframe);
        }

        public static NetCommand BuildSendMoveKey(MoveKeyInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.property, out string channelName, out int channelIndex);
            byte[] channelNameBuffer = StringToBytes(channelName);
            byte[] channelIndexBuffer = IntToBytes(channelIndex);
            byte[] frameBuffer = IntToBytes(data.frame);
            byte[] newFrameBuffer = IntToBytes(data.newFrame);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer, newFrameBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.MoveKeyframe);
        }

        public static NetCommand BuildSendAnimationCurve(CurveInfo data)
        {
            byte[] objectNameBuffer = StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.curve.property, out string channelName, out int channelIndex);
            byte[] channelNameBuffer = StringToBytes(channelName);
            byte[] channelIndexBuffer = IntToBytes(channelIndex);

            int count = data.curve.keys.Count;
            int[] frames = new int[count];
            for (int i = 0; i < count; ++i)
            {
                frames[i] = data.curve.keys[i].frame;
            }
            byte[] framesBuffer = IntsToBytes(frames);

            float[] values = new float[count];
            for (int i = 0; i < count; ++i)
            {
                values[i] = data.curve.keys[i].value;
            }
            byte[] valuesBuffer = FloatsToBytes(values);

            int[] interpolations = new int[count];
            for (int i = 0; i < count; ++i)
            {
                interpolations[i] = (int) data.curve.keys[i].interpolation;
            }
            byte[] interpolationsBuffer = IntsToBytes(interpolations);

            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, framesBuffer, valuesBuffer, interpolationsBuffer };
            byte[] buffer = ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.Animation);
        }

        public static NetCommand BuildSendQueryAnimationData(string name)
        {
            return new NetCommand(StringToBytes(name), MessageType.QueryAnimationData);
        }

        public static void BuildFrameStartEnd(byte[] data)
        {
            int index = 0;
            int start = GetInt(data, ref index);
            int end = GetInt(data, ref index);
            SyncData.mixer.SetFrameRange(start, end);
        }

        public static void BuildCurrentCamera(byte[] data)
        {
            int index = 0;
            string cameraName = GetString(data, ref index);
            GameObject cameraObject = null;
            if (cameraName.Length > 0)
            {
                Node prefabNode = SyncData.nodes[cameraName];
                cameraObject = prefabNode.instances[0].Item1;
            }
            SyncData.mixer.SetActiveCamera(cameraObject);
        }

        public static void BuildShotManagerCurrentShot(byte[] data)
        {
            int index = 0;
            int shotIndex = GetInt(data, ref index);
            SyncData.mixer.SetShotManagerCurrentShot(shotIndex);
        }

        public static void BuildShotManagerMontageMode(byte[] data)
        {
            int index = 0;
            bool montageMode = GetBool(data, ref index);
            SyncData.mixer.EnableShotManagerMontage(montageMode);
        }
        public static void BuildShotManager(byte[] data)
        {
            List<Shot> shots = new List<Shot>();
            int index = 0;
            int shotCount = GetInt(data, ref index);
            for (int i = 0; i < shotCount; ++i)
            {
                string shotName = GetString(data, ref index);
                string cameraName = GetString(data, ref index);
                int start = GetInt(data, ref index);
                int end = GetInt(data, ref index);
                bool enabled = GetBool(data, ref index);

                GameObject camera = null;
                if (cameraName.Length > 0 && SyncData.nodes.ContainsKey(cameraName))
                    camera = SyncData.nodes[cameraName].instances[0].Item1;

                Shot shot = new Shot { name = shotName, camera = camera, start = start, end = end, enabled = enabled };
                shots.Add(shot);
            }
            SyncData.mixer.UpdateShotManager(shots);
        }

        public static void BuildShotManagerAction(byte[] data)
        {
            int index = 0;
            ShotManagerAction action = (ShotManagerAction) GetInt(data, ref index);
            int shotIndex = GetInt(data, ref index);

            switch (action)
            {
                case ShotManagerAction.AddShot:
                    {
                        string shotName = GetString(data, ref index);
                        int start = GetInt(data, ref index);
                        int end = GetInt(data, ref index);
                        string cameraName = GetString(data, ref index);
                        Color color = GetColor(data, ref index);
                        GameObject cam = null;
                        if (cameraName.Length > 0)
                            cam = SyncData.nodes[cameraName].instances[0].Item1;
                        Shot shot = new Shot { name = shotName, camera = cam, color = color, start = start, end = end };
                        SyncData.mixer.ShotManagerInsertShot(shot, shotIndex);
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    SyncData.mixer.ShotManagerDeleteShot(shotIndex);
                    break;
                case ShotManagerAction.DuplicateShot:
                    {
                        GetString(data, ref index);
                        SyncData.mixer.ShotManagerDuplicateShot(shotIndex);
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        int offset = GetInt(data, ref index);
                        SyncData.mixer.ShotManagerMoveShot(offset);
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                        int start = GetInt(data, ref index);
                        int end = GetInt(data, ref index);
                        string cameraName = GetString(data, ref index);
                        Color color = GetColor(data, ref index);
                        int enabled = GetInt(data, ref index);
                        SyncData.mixer.ShotManagerUpdateShot(shotIndex, start, end, cameraName, color, enabled);
                    }
                    break;
            }

        }
        public static void BuildClientAttribute(byte[] data)
        {
            int index = 0;
            string json = GetString(data, ref index);
            SyncData.mixer.UpdateClient(json);
        }

        public static void BuildListAllClients(byte[] data)
        {
            int index = 0;
            string json = GetString(data, ref index);
            SyncData.mixer.ListAllClients(json);
        }
    }
}
