using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;

using VRtist.Serialization;

namespace VRtist
{
    public class ImageData
    {
        public bool isEmbedded;
        public int width;
        public int height;
        public byte[] buffer;
    }

    public enum MessageConstraintType
    {
        Parent,
        LookAt
    }

    public class MixerUtils
    {
        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();

        public static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, List<MaterialParameters>> meshesMaterials = new Dictionary<string, List<MaterialParameters>>();

        public static Dictionary<MaterialID, Material> baseMaterials = new Dictionary<MaterialID, Material>();
        public static Dictionary<string, MaterialParameters> materialsParameters = new Dictionary<string, MaterialParameters>();

        public static Dictionary<string, GreasePencilData> greasePencils = new Dictionary<string, GreasePencilData>();
        public static HashSet<string> materialsFillEnabled = new HashSet<string>();
        public static HashSet<string> materialStrokesEnabled = new HashSet<string>();
        public static Dictionary<string, Dictionary<string, int>> greasePencilLayerIndices = new Dictionary<string, Dictionary<string, int>>();

        public static Dictionary<string, ImageData> textureData = new Dictionary<string, ImageData>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static HashSet<string> texturesFlipY = new HashSet<string>();



        public static void BuildClientId(byte[] data)
        {
            string clientId = Converter.ConvertToString(data);
            SyncData.mixer.SetClientId(clientId);
        }

        public static void Rename(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string[] srcPath = Converter.GetString(data, ref bufferIndex).Split('/');
            string[] dstPath = Converter.GetString(data, ref bufferIndex).Split('/');

            string srcName = srcPath[srcPath.Length - 1];
            string dstName = dstPath[dstPath.Length - 1];

            SyncData.Rename(srcName, dstName);
        }

        public static void Delete(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string[] ObjectPath = Converter.GetString(data, ref bufferIndex).Split('/');
            string objectName = ObjectPath[ObjectPath.Length - 1];

            SyncData.Delete(objectName);
        }

        public static void Duplicate(Transform prefab, byte[] data)
        {
            int bufferIndex = 0;

            // find source prefab
            string path = Converter.GetString(data, ref bufferIndex);
            if (path == "")
                return;
            char[] separator = { '/' };
            string[] splitted = path.Split(separator);
            string srcPrefabName = splitted[splitted.Length - 1];
            GameObject srcPrefab = SyncData.nodes[srcPrefabName].prefab;

            // duplicata name
            string name = Converter.GetString(data, ref bufferIndex);
            Matrix4x4 mat = Converter.GetMatrix(data, ref bufferIndex);
            Maths.DecomposeMatrix(mat, out Vector3 position, out Quaternion rotation, out Vector3 scale);

            GameObject newInstance = SyncData.Duplicate(srcPrefab, name);
            Node duplicateNode = SyncData.nodes[newInstance.name];
            duplicateNode.prefab.transform.localPosition = position;
            duplicateNode.prefab.transform.localRotation = rotation;
            duplicateNode.prefab.transform.localScale = scale;
            foreach (var instanceItem in duplicateNode.instances)
            {
                instanceItem.Item1.transform.localPosition = position;
                instanceItem.Item1.transform.localRotation = rotation;
                instanceItem.Item1.transform.localScale = scale;
            }
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
            string objectName = Converter.GetString(data, ref bufferIndex);
            Transform parent = FindPath(root, data, ref bufferIndex);
            Transform trf = SyncData.GetTrash().transform.Find(objectName + Utils.blenderHiddenParent);
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
            string path = Converter.GetString(data, ref bufferIndex);

            bool packed = Converter.GetBool(data, ref bufferIndex);
            int width = Converter.GetInt(data, ref bufferIndex);
            int height = Converter.GetInt(data, ref bufferIndex);
            int size = Converter.GetInt(data, ref bufferIndex);

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
            string collectionName = Converter.GetString(data, ref bufferIndex);
            bool visible = Converter.GetBool(data, ref bufferIndex);
            Vector3 offset = Converter.GetVector3(data, ref bufferIndex);

            bool tempVisible = Converter.GetBool(data, ref bufferIndex);

            SyncData.AddCollection(collectionName, offset, visible, tempVisible);
        }

        public static void BuildCollectionRemoved(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = Converter.GetString(data, ref bufferIndex);

            SyncData.RemoveCollection(collectionName);
        }

        public static void BuildAddCollectionToCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = Converter.GetString(data, ref bufferIndex);
            string collectionName = Converter.GetString(data, ref bufferIndex);

            SyncData.AddCollectionToCollection(parentCollectionName, collectionName);

        }

        public static void BuildRemoveCollectionFromCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = Converter.GetString(data, ref bufferIndex);
            string collectionName = Converter.GetString(data, ref bufferIndex);

            SyncData.RemoveCollectionFromCollection(parentCollectionName, collectionName);
        }

        public static void BuildAddObjectToCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = Converter.GetString(data, ref bufferIndex);
            string objectName = Converter.GetString(data, ref bufferIndex);

            SyncData.AddObjectToCollection(collectionName, objectName);
        }

        public static void BuildRemoveObjectFromCollection(Transform _, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = Converter.GetString(data, ref bufferIndex);
            string objectName = Converter.GetString(data, ref bufferIndex);

            SyncData.RemoveObjectFromCollection(collectionName, objectName);
        }

        public static void BuildCollectionInstance(byte[] data)
        {
            int bufferIndex = 0;
            Transform transform = BuildPath(data, ref bufferIndex, true);
            string collectionName = Converter.GetString(data, ref bufferIndex);

            SyncData.AddCollectionInstance(transform, collectionName);
        }

        public static void BuildAddObjectToDocument(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = Converter.GetString(data, ref bufferIndex);
            if (sceneName != SyncData.currentSceneName)
                return;
            string objectName = Converter.GetString(data, ref bufferIndex);
            SyncData.AddObjectToDocument(root, objectName, "/");
        }

        public static void BuilAddCollectionToScene(byte[] data)
        {
            int bufferIndex = 0;
            string _ = Converter.GetString(data, ref bufferIndex);
            string collectionName = Converter.GetString(data, ref bufferIndex);
            SyncData.sceneCollections.Add(collectionName);
        }

        public static void BuilSetScene(byte[] data)
        {
            int bufferIndex = 0;
            string sceneName = Converter.GetString(data, ref bufferIndex);
            SyncData.SetScene(sceneName);
        }

        public static MaterialParameters DefaultMaterial()
        {
            string name = "defaultMaterial";

            if (materialsParameters.TryGetValue(name, out MaterialParameters materialParameters))
                return materialParameters;

            MaterialID materialType;
            materialType = MaterialID.ObjectOpaque;

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

        public static void BuildMaterial(byte[] data)
        {
            int currentIndex = 0;
            string name = Converter.GetString(data, ref currentIndex);
            float opacity = Converter.GetFloat(data, ref currentIndex);
            string opacityTexturePath = Converter.GetString(data, ref currentIndex);

            if (!materialsParameters.TryGetValue(name, out MaterialParameters materialParameters))
            {
                MaterialID materialType;
                materialType = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? MaterialID.ObjectTransparent : MaterialID.ObjectOpaque;

                materialParameters = new MaterialParameters
                {
                    name = name,
                    materialType = materialType
                };
                materialsParameters[name] = materialParameters;
            }

            materialParameters.opacity = opacity;
            materialParameters.opacityTexturePath = opacityTexturePath;
            materialParameters.baseColor = Converter.GetColor(data, ref currentIndex);
            materialParameters.baseColorTexturePath = Converter.GetString(data, ref currentIndex);
            materialParameters.metallic = Converter.GetFloat(data, ref currentIndex);
            materialParameters.metallicTexturePath = Converter.GetString(data, ref currentIndex);
            materialParameters.roughness = Converter.GetFloat(data, ref currentIndex);
            materialParameters.roughnessTexturePath = Converter.GetString(data, ref currentIndex);
            materialParameters.normalTexturePath = Converter.GetString(data, ref currentIndex);
            materialParameters.emissionColor = Converter.GetColor(data, ref currentIndex);
            materialParameters.emissionColorTexturePath = Converter.GetString(data, ref currentIndex);
        }

        public static void ApplyMaterialParameters(MeshRenderer meshRenderer, List<MaterialParameters> meshMaterials)
        {
            MaterialParameters[] materialParameters = meshMaterials.ToArray();
            Material[] materials = new Material[materialParameters.Length];
            for (int i = 0; i < materialParameters.Length; i++)
            {
                materials[i] = ResourceManager.GetMaterial(materialParameters[i].materialType);
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
            material.name = parameters.name;
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
            material.SetColor("_Emissive", emissionColor);
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
            string objectName = Converter.GetString(data, ref currentIndex);
            string materialName = Converter.GetString(data, ref currentIndex);

            if (!materialsParameters.TryGetValue(materialName, out MaterialParameters materialParameters))
            {
                Debug.LogError("Could not assign material " + materialName + " to " + objectName);
                return;
            }

            Material material = ResourceManager.GetMaterial(materialParameters.materialType);
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
            string path = Converter.GetString(data, ref bufferIndex);
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
            string path = Converter.GetString(data, ref bufferIndex);
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
            string objectName = Converter.GetString(data, ref currentIndex);
            bool visible = !Converter.GetBool(data, ref currentIndex);
            Converter.GetBool(data, ref currentIndex);
            Converter.GetBool(data, ref currentIndex);
            bool tempVisible = !Converter.GetBool(data, ref currentIndex);

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

            Matrix4x4 parentInverse = Converter.GetMatrix(data, ref currentIndex);
            Matrix4x4 _ = Converter.GetMatrix(data, ref currentIndex);
            Matrix4x4 local = Converter.GetMatrix(data, ref currentIndex);

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

        public static void BuildEmpty(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = Converter.GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }

            if (!SyncData.nodes.ContainsKey(leafName))
            {
                GameObject locatorGameObject;
                Node node;
                locatorGameObject = SyncData.CreateInstance(ResourceManager.GetPrefab(PrefabID.Locator), root, leafName, isPrefab: true);
                node = SyncData.CreateNode(leafName, SyncData.nodes[transform.name]);
                node.prefab = locatorGameObject;
            }
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
            byte[] name = Converter.StringToBytes(parentName + transform.name);
            byte[] hideBuffer = Converter.BoolToBytes(!transform.gameObject.activeSelf);
            byte[] hideSelect = Converter.BoolToBytes(false);
            byte[] hideInViewport = Converter.BoolToBytes(false);
            byte[] hideGet = Converter.BoolToBytes(!tempVisible);

            List<byte[]> buffers = new List<byte[]> { name, hideBuffer, hideSelect, hideInViewport, hideGet };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.ObjectVisibility);
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
            byte[] name = Converter.StringToBytes(parentName + transform.name);
            Matrix4x4 parentMatrix = Matrix4x4.TRS(transform.parent.localPosition, transform.parent.localRotation, transform.parent.localScale);
            Matrix4x4 basisMatrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale);
            byte[] invertParentMatrixBuffer = Converter.MatrixToBytes(parentMatrix);
            byte[] basisMatrixBuffer = Converter.MatrixToBytes(basisMatrix);
            byte[] localMatrixBuffer = Converter.MatrixToBytes(parentMatrix * basisMatrix);

            List<byte[]> buffers = new List<byte[]> { name, invertParentMatrixBuffer, basisMatrixBuffer, localMatrixBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Transform);
            return command;
        }

        public static NetCommand BuildMaterialCommand(Material material)
        {
            byte[] name = Converter.StringToBytes(material.name);
            float op = 1f;
            if (material.HasProperty("_Opacity"))
                op = material.GetFloat("_Opacity");
            byte[] opacity = Converter.FloatToBytes(op);
            byte[] opacityMapTexture = Converter.StringToBytes("");
            byte[] baseColor = Converter.ColorToBytes(material.GetColor("_BaseColor"));
            byte[] baseColorTexture = Converter.StringToBytes("");
            byte[] metallic = Converter.FloatToBytes(material.GetFloat("_Metallic"));
            byte[] metallicTexture = Converter.StringToBytes("");
            byte[] roughness = Converter.FloatToBytes(material.HasProperty("_Smoothness") ? 1f - material.GetFloat("_Smoothness") : material.GetFloat("_Roughness"));
            byte[] roughnessTexture = Converter.StringToBytes("");
            byte[] normalMapTexture = Converter.StringToBytes("");
            byte[] emissionColor = Converter.ColorToBytes(Color.black);
            byte[] emissionColorTexture = Converter.StringToBytes("");

            List<byte[]> buffers = new List<byte[]> { name, opacity, opacityMapTexture, baseColor, baseColorTexture, metallic, metallicTexture, roughness, roughnessTexture, normalMapTexture, emissionColor, emissionColorTexture };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Material);
            return command;
        }

        public static NetCommand BuildAssignMaterialCommand(AssignMaterialInfo info)
        {
            byte[] objectName = Converter.StringToBytes(info.objectName);
            byte[] materialName = Converter.StringToBytes(info.materialName);
            List<byte[]> buffers = new List<byte[]> { objectName, materialName };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.AssignMaterial);
            return command;
        }

        public static void AddObjectToScene(GameObject gObject)
        {
            AddToCollectionInfo addObjectToCollection = new AddToCollectionInfo
            {
                collectionName = "VRtistCollection",
                transform = gObject.transform
            };
            CommandManager.SendEvent(MessageType.AddObjectToCollection, addObjectToCollection);

            AddObjectToSceneInfo addObjectToScene = new AddObjectToSceneInfo
            {
                transform = gObject.transform
            };
            CommandManager.SendEvent(MessageType.AddObjectToScene, addObjectToScene);
        }
        public static NetCommand BuildEmptyCommand(Transform root, Transform transform)
        {
            Transform current = transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] bpath = Converter.StringToBytes(path);
            NetCommand command = new NetCommand(bpath, MessageType.Empty);
            return command;
        }

        public static void ReceiveAddConstraint(byte[] data)
        {
            int currentIndex = 0;
            MessageConstraintType constraintType = (MessageConstraintType)Converter.GetInt(data, ref currentIndex);
            string objectName = Converter.GetString(data, ref currentIndex);
            string targetName = Converter.GetString(data, ref currentIndex);

            // Apply to instances
            Node objectNode = SyncData.nodes[objectName];
            Node targetNode = SyncData.nodes[targetName];
            for (int i = 0; i < objectNode.instances.Count; ++i)
            {
                switch (constraintType)
                {
                    case MessageConstraintType.Parent:
                        ConstraintManager.AddParentConstraint(objectNode.instances[i].Item1, targetNode.instances[i].Item1);
                        break;
                    case MessageConstraintType.LookAt:
                        ConstraintManager.AddLookAtConstraint(objectNode.instances[i].Item1, targetNode.instances[i].Item1);
                        break;
                }
            }
        }

        public static void ReceiveRemoveConstraint(byte[] data)
        {
            int currentIndex = 0;
            MessageConstraintType constraintType = (MessageConstraintType)Converter.GetInt(data, ref currentIndex);
            string objectName = Converter.GetString(data, ref currentIndex);

            // Apply to instances
            Node objectNode = SyncData.nodes[objectName];
            for (int i = 0; i < objectNode.instances.Count; ++i)
            {
                switch (constraintType)
                {
                    case MessageConstraintType.Parent:
                        ConstraintManager.RemoveConstraint<ParentConstraint>(objectNode.instances[i].Item1);
                        break;
                    case MessageConstraintType.LookAt:
                        ConstraintManager.RemoveConstraint<LookAtConstraint>(objectNode.instances[i].Item1);
                        break;
                }
            }
        }

        public static NetCommand BuildSendAddParentConstraintCommand(GameObject gobject, GameObject target)
        {
            byte[] constraintType = Converter.IntToBytes((int)MessageConstraintType.Parent);
            byte[] objectName = Converter.StringToBytes(gobject.name);
            byte[] targetName = Converter.StringToBytes(target.name);
            List<byte[]> buffers = new List<byte[]> { constraintType, objectName, targetName };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.AddConstraint);
            return command;
        }

        public static NetCommand BuildSendAddLookAtConstraintCommand(GameObject gobject, GameObject target)
        {
            byte[] constraintType = Converter.IntToBytes((int)MessageConstraintType.LookAt);
            byte[] objectName = Converter.StringToBytes(gobject.name);
            byte[] targetName = Converter.StringToBytes(target.name);
            List<byte[]> buffers = new List<byte[]> { constraintType, objectName, targetName };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.AddConstraint);
            return command;
        }

        public static NetCommand BuildSendRemoveParentConstraintCommand(GameObject gobject)
        {
            byte[] constraintType = Converter.IntToBytes((int)MessageConstraintType.Parent);
            byte[] objectName = Converter.StringToBytes(gobject.name);
            List<byte[]> buffers = new List<byte[]> { constraintType, objectName };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.RemoveConstraint);
            return command;
        }

        public static NetCommand BuildSendRemoveLookAtConstraintCommand(GameObject gobject)
        {
            byte[] constraintType = Converter.IntToBytes((int)MessageConstraintType.LookAt);
            byte[] objectName = Converter.StringToBytes(gobject.name);
            List<byte[]> buffers = new List<byte[]> { constraintType, objectName };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.RemoveConstraint);
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
            byte[] bpath = Converter.StringToBytes(path);

            SyncData.mixer.GetCameraInfo(cameraInfo.transform.gameObject, out float focal, out float near, out float far, out bool dofEnabled, out float aperture, out Transform colimator);
            byte[] bname = Converter.StringToBytes(cameraInfo.transform.name);

            Camera cam = cameraInfo.transform.GetComponentInChildren<Camera>(true);
            int sensorFit = (int)cam.gateFit;

            byte[] focalBuffer = Converter.FloatToBytes(focal);
            byte[] nearBuffer = Converter.FloatToBytes(near);
            byte[] farBuffer = Converter.FloatToBytes(far);
            byte[] dofEnabledBuffer = Converter.BoolToBytes(dofEnabled);
            byte[] apertureBuffer = Converter.FloatToBytes(aperture);
            byte[] colimatorBuffer = null != colimator ? Converter.StringToBytes(colimator.name) : Converter.StringToBytes("");
            byte[] sensorFitBuffer = Converter.IntToBytes(sensorFit);
            byte[] sensorSizeBuffer = Converter.Vector2ToBytes(cam.sensorSize);

            List<byte[]> buffers = new List<byte[]> { bpath, bname, focalBuffer, nearBuffer, farBuffer, dofEnabledBuffer, apertureBuffer, colimatorBuffer, sensorFitBuffer, sensorSizeBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Camera);
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
            byte[] bpath = Converter.StringToBytes(path);
            byte[] bname = Converter.StringToBytes(lightInfo.transform.name);

            SyncData.mixer.GetLightInfo(lightInfo.transform.gameObject, out LightType lightType, out bool castShadows, out float power, out Color color, out float _, out float innerAngle, out float outerAngle);

            byte[] lightTypeBuffer = Converter.IntToBytes((int)lightType);
            byte[] castShadowsBuffer = Converter.BoolToBytes(castShadows);
            byte[] colorBuffer = Converter.ColorToBytes(color);
            byte[] powerBuffer = Converter.FloatToBytes(power);
            byte[] innerAngleBuffer = Converter.FloatToBytes(innerAngle);
            byte[] outerAngleBuffer = Converter.FloatToBytes(outerAngle);

            List<byte[]> buffers = new List<byte[]> { bpath, bname, lightTypeBuffer, castShadowsBuffer, colorBuffer, powerBuffer, innerAngleBuffer, outerAngleBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Light);
            return command;
        }

        public static NetCommand BuildSkyCommand(SkySettings skyInfo)
        {
            byte[] skyNameBuffer = Converter.StringToBytes("Sky"); // optimized commands need a name
            byte[] topBuffer = Converter.ColorToBytes(skyInfo.topColor);
            byte[] middleBuffer = Converter.ColorToBytes(skyInfo.middleColor);
            byte[] bottomBuffer = Converter.ColorToBytes(skyInfo.bottomColor);

            List<byte[]> buffers = new List<byte[]> { skyNameBuffer, topBuffer, middleBuffer, bottomBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Sky);
            return command;
        }

        public static NetCommand BuildRenameCommand(Transform root, RenameInfo rename)
        {
            string src = GetPathName(root, rename.srcTransform);
            byte[] srcPath = Converter.StringToBytes(src);
            byte[] dstName = Converter.StringToBytes(rename.newName);
            Debug.Log($"{rename.srcTransform.name}: {src} --> {rename.newName}");

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Rename);
            return command;
        }

        public static NetCommand BuildDuplicateCommand(Transform root, DuplicateInfos duplicate)
        {
            byte[] srcPath = Converter.StringToBytes(GetPathName(root, duplicate.srcObject.transform));
            byte[] dstName = Converter.StringToBytes(duplicate.dstObject.name);

            Transform transform = duplicate.dstObject.transform;
            byte[] matrixBuffer = Converter.MatrixToBytes(Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.localScale));

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName, matrixBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Duplicate);
            return command;
        }

        public static NetCommand BuildSendToTrashCommand(Transform root, SendToTrashInfo sendToTrash)
        {
            byte[] path = Converter.StringToBytes(GetPathName(root, sendToTrash.transform));
            List<byte[]> buffers = new List<byte[]> { path };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.SendToTrash);
            return command;
        }

        public static NetCommand BuildRestoreFromTrashCommand(Transform root, RestoreFromTrashInfo restoreFromTrash)
        {
            string parentPath = GetPathName(root, restoreFromTrash.parent);

            byte[] nameBuffer = Converter.StringToBytes(restoreFromTrash.transform.name);
            byte[] pathBuffer = Converter.StringToBytes(parentPath);

            List<byte[]> buffers = new List<byte[]> { nameBuffer, pathBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.RestoreFromTrash);
            return command;
        }


        public static NetCommand BuildMeshCommand(Transform root, MeshInfos meshInfos)
        {
            Mesh mesh = meshInfos.meshFilter.mesh;
            byte[] name = Converter.StringToBytes(mesh.name);

            byte[] baseMeshSize = Converter.IntToBytes(0);

            byte[] positions = Converter.Vectors3ToBytes(mesh.vertices);

            int[] baseTriangles = mesh.triangles;
            Vector3[] baseNormals = mesh.normals;
            Vector3[] splittedNormals = new Vector3[baseTriangles.Length];
            for (int i = 0; i < splittedNormals.Length; i++)
            {
                int id = baseTriangles[i];
                splittedNormals[i] = baseNormals[id];

            }
            byte[] normals = Converter.Vectors3ToBytes(splittedNormals);

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
            byte[] uvs = Converter.Vectors2ToBytes(splittedUVs);

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

            byte[] triangles = Converter.TriangleIndicesToBytes(baseTriangles);

            Material[] materials = meshInfos.meshRenderer.materials;
            string[] materialNames = new string[materials.Length];
            int index = 0;
            foreach (Material material in materials)
            {
                materialNames[index++] = material.name;
            }
            byte[] materialsBuffer = Converter.StringsToBytes(materialNames);

            Transform transform = meshInfos.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = Converter.StringToBytes(path);

            byte[] bakedMeshSize = Converter.IntToBytes(positions.Length + normals.Length + uvs.Length + materialIndicesBuffer.Length + triangles.Length);

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
            byte[] materialLinkNamesBuffer = Converter.StringsToBytes(materialNames, false);
            //////////////////////////////////////////////////

            List<byte[]> buffers = new List<byte[]> { pathBuffer, name, baseMeshSize, bakedMeshSize, positions, normals, uvs, materialIndicesBuffer, triangles, materialsBuffer, materialLinksBuffer, materialLinkNamesBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Mesh);
            return command;
        }

        public static NetCommand BuildAddCollecitonCommand(string collectionName)
        {
            byte[] collectionNameBuffer = Converter.StringToBytes(collectionName);
            byte[] visible = Converter.BoolToBytes(true);
            byte[] offset = Converter.Vector3ToBytes(Vector3.zero);
            byte[] temporaryVisible = Converter.BoolToBytes(true);
            List<byte[]> buffers = new List<byte[]> { collectionNameBuffer, visible, offset, temporaryVisible };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Collection);
            return command;
        }

        public static NetCommand BuildAddCollectionToScene(string collectionName)
        {
            byte[] sceneNameBuffer = Converter.StringToBytes(SyncData.currentSceneName);
            byte[] collectionNameBuffer = Converter.StringToBytes(collectionName);
            List<byte[]> buffers = new List<byte[]> { sceneNameBuffer, collectionNameBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.AddCollectionToScene);
            SyncData.sceneCollections.Add(collectionName);
            return command;
        }


        public static NetCommand BuildAddObjectToCollecitonCommand(AddToCollectionInfo info)
        {
            byte[] collectionNameBuffer = Converter.StringToBytes(info.collectionName);
            byte[] objectNameBuffer = Converter.StringToBytes(info.transform.name);

            List<byte[]> buffers = new List<byte[]> { collectionNameBuffer, objectNameBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.AddObjectToCollection);
            return command;
        }

        public static NetCommand BuildAddObjectToScene(AddObjectToSceneInfo info)
        {
            byte[] sceneNameBuffer = Converter.StringToBytes(SyncData.currentSceneName);
            byte[] objectNameBuffer = Converter.StringToBytes(info.transform.name);
            List<byte[]> buffers = new List<byte[]> { sceneNameBuffer, objectNameBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.AddObjectToDocument);
            return command;
        }

        public static NetCommand BuildDeleteCommand(Transform root, DeleteInfo deleteInfo)
        {
            Transform transform = deleteInfo.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = Converter.StringToBytes(path);

            List<byte[]> buffers = new List<byte[]> { pathBuffer };
            NetCommand command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.Delete);
            return command;
        }

        public static void BuildAnimation(byte[] data)
        {
            int currentIndex = 0;
            string objectName = Converter.GetString(data, ref currentIndex);
            string animationChannel = Converter.GetString(data, ref currentIndex);
            int channelIndex = Converter.GetInt(data, ref currentIndex);

            int keyCount = Converter.GetInt(data, ref currentIndex);
            if (0 == keyCount)
                return;

            int[] intBuffer = new int[keyCount];
            float[] floatBuffer = new float[keyCount];
            int[] interpolationBuffer = new int[keyCount];

            Buffer.BlockCopy(data, currentIndex, intBuffer, 0, keyCount * sizeof(int));
            Converter.GetInt(data, ref currentIndex);
            Buffer.BlockCopy(data, currentIndex + keyCount * sizeof(int), floatBuffer, 0, keyCount * sizeof(float));
            Converter.GetInt(data, ref currentIndex);
            Buffer.BlockCopy(data, currentIndex + (keyCount * sizeof(int)) + (keyCount * sizeof(float)), interpolationBuffer, 0, keyCount * sizeof(int));

            SyncData.mixer.CreateAnimationCurve(objectName, animationChannel, channelIndex, intBuffer, floatBuffer, interpolationBuffer);
        }

        public static void BuildAddKeyframe(byte[] data)
        {
            int currentIndex = 0;
            string objectName = Converter.GetString(data, ref currentIndex);
            string channelName = Converter.GetString(data, ref currentIndex);
            int channelIndex = Converter.GetInt(data, ref currentIndex);
            int frame = Converter.GetInt(data, ref currentIndex);
            float value = Converter.GetFloat(data, ref currentIndex);
            int interpolation = Converter.GetInt(data, ref currentIndex);

            SyncData.mixer.CreateAnimationKey(objectName, channelName, channelIndex, frame, value, interpolation);
        }

        public static void BuildRemoveKeyframe(byte[] data)
        {
            int currentIndex = 0;
            string objectName = Converter.GetString(data, ref currentIndex);
            string channelName = Converter.GetString(data, ref currentIndex);
            int channelIndex = Converter.GetInt(data, ref currentIndex);
            int frame = Converter.GetInt(data, ref currentIndex);

            SyncData.mixer.RemoveAnimationKey(objectName, channelName, channelIndex, frame);
        }

        public static void BuildMoveKeyframe(byte[] data)
        {
            int currentIndex = 0;
            string objectName = Converter.GetString(data, ref currentIndex);
            string channelName = Converter.GetString(data, ref currentIndex);
            int channelIndex = Converter.GetInt(data, ref currentIndex);
            int frame = Converter.GetInt(data, ref currentIndex);
            int newFrame = Converter.GetInt(data, ref currentIndex);

            SyncData.mixer.MoveAnimationKey(objectName, channelName, channelIndex, frame, newFrame);
        }

        public static void BuildClearAnimations(byte[] data)
        {
            int currentIndex = 0;
            string objectName = Converter.GetString(data, ref currentIndex);
            // Apply to instances
            Node node = SyncData.nodes[objectName];
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                SyncData.mixer.ClearAnimations(t.Item1);
            }
        }

        public static void BuildLightAttributes(byte[] data)
        {
            int currentIndex = 0;
            string lightName = Converter.GetString(data, ref currentIndex);

            Node node = SyncData.nodes[lightName];
            SyncData.mixer.GetLightInfo(node.prefab, out LightType lightType, out bool castShadows, out float _, out Color _, out float range, out float innerAngle, out float outerAngle);

            float power = Converter.GetFloat(data, ref currentIndex);
            Color color = Converter.GetColor(data, ref currentIndex);

            SyncData.mixer.SetLightInfo(node.prefab, lightType, castShadows, power, color, range, innerAngle, outerAngle);

            // Apply to instances
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                SyncData.mixer.SetLightInfo(t.Item1, lightType, castShadows, power, color, range, innerAngle, outerAngle);
            }
        }

        public static void BuildCamera(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = Converter.GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }
            string name = Converter.GetString(data, ref currentIndex);

            GameObject camGameObject;
            Node node;
            if (!SyncData.nodes.ContainsKey(leafName))
            {
                camGameObject = SyncData.CreateInstance(ResourceManager.GetPrefab(PrefabID.Camera), root, leafName, isPrefab: true);
                node = SyncData.CreateNode(name, SyncData.nodes[transform.name]);
                node.prefab = camGameObject;
            }
            else // TODO: found a case where a camera was found (don't know when it was created???), but had no Camera child object.
            {
                node = SyncData.nodes[leafName];
                camGameObject = node.prefab;
            }

            float focal = Converter.GetFloat(data, ref currentIndex);
            float near = Converter.GetFloat(data, ref currentIndex);
            float far = Converter.GetFloat(data, ref currentIndex);
            bool dofEnabled = Converter.GetBool(data, ref currentIndex);
            float aperture = Converter.GetFloat(data, ref currentIndex);
            string colimatorName = Converter.GetString(data, ref currentIndex);
            Camera.GateFitMode gateFit = (Camera.GateFitMode)Converter.GetInt(data, ref currentIndex);
            if (gateFit == Camera.GateFitMode.None)
                gateFit = Camera.GateFitMode.Horizontal;
            Vector2 sensorSize = Converter.GetVector2(data, ref currentIndex);

            SyncData.mixer.SetCameraInfo(camGameObject, focal, near, far, dofEnabled, aperture, colimatorName, gateFit, sensorSize);
        }

        public static void BuildLight(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(data, ref currentIndex, false);
            if (transform == null)
                transform = root;

            currentIndex = 0;
            string leafName = Converter.GetString(data, ref currentIndex);
            int index = leafName.LastIndexOf('/');
            if (index != -1)
            {
                leafName = leafName.Substring(index + 1, leafName.Length - index - 1);
            }
            Converter.GetString(data, ref currentIndex);

            LightType lightType = (LightType)Converter.GetInt(data, ref currentIndex);

            GameObject lightGameObject;
            Node node;
            if (!SyncData.nodes.ContainsKey(leafName))
            {
                switch (lightType)
                {
                    case LightType.Directional:
                        lightGameObject = SyncData.CreateInstance(ResourceManager.GetPrefab(PrefabID.SunLight), root, leafName);
                        break;
                    case LightType.Point:
                        lightGameObject = SyncData.CreateInstance(ResourceManager.GetPrefab(PrefabID.PointLight), root, leafName);
                        break;
                    case LightType.Spot:
                        lightGameObject = SyncData.CreateInstance(ResourceManager.GetPrefab(PrefabID.SpotLight), root, leafName);
                        break;
                    default:
                        return;
                }
                node = SyncData.CreateNode(leafName, SyncData.nodes[transform.name]);
                node.prefab = lightGameObject;
            }
            else
            {
                node = SyncData.nodes[leafName];
                lightGameObject = node.prefab;
            }

            // Read data
            bool castShadows = Converter.GetBool(data, ref currentIndex);

            Color lightColor = Converter.GetColor(data, ref currentIndex);
            float power = Converter.GetFloat(data, ref currentIndex);
            float spotSize = Converter.GetFloat(data, ref currentIndex);
            float spotBlend = Converter.GetFloat(data, ref currentIndex);

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
            Converter.GetString(data, ref currentIndex);
            Color topColor = Converter.GetColor(data, ref currentIndex);
            Color middleColor = Converter.GetColor(data, ref currentIndex);
            Color bottomColor = Converter.GetColor(data, ref currentIndex);
            SyncData.mixer.SetSkyColors(topColor, middleColor, bottomColor);
        }

        public static NetCommand BuildSendClearAnimations(ClearAnimationInfo info)
        {
            NetCommand command = new NetCommand(Converter.StringToBytes(info.gObject.name), MessageType.ClearAnimations);
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

            byte[] action = Converter.IntToBytes((int)info.action);
            byte[] shotIndex = Converter.IntToBytes(info.shotIndex);

            switch (info.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        byte[] nextShotIndex = Converter.IntToBytes(info.shotIndex + 1);
                        shotName = Converter.StringToBytes(info.shotName);
                        start = Converter.IntToBytes(info.shotStart);
                        end = Converter.IntToBytes(info.shotEnd);
                        camera = Converter.StringToBytes(info.cameraName);
                        color = Converter.ColorToBytes(info.shotColor);
                        buffers = new List<byte[]> { action, nextShotIndex, shotName, start, end, camera, color };
                        command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    buffers = new List<byte[]> { action, shotIndex };
                    command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.DuplicateShot:
                    shotName = Converter.StringToBytes(info.shotName);
                    buffers = new List<byte[]> { action, shotIndex, shotName };
                    command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.MoveShot:
                    byte[] offset = Converter.IntToBytes(info.moveOffset);
                    buffers = new List<byte[]> { action, shotIndex, offset };
                    command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
                case ShotManagerAction.UpdateShot:
                    start = Converter.IntToBytes(info.shotStart);
                    end = Converter.IntToBytes(info.shotEnd);
                    camera = Converter.StringToBytes(info.cameraName);
                    color = Converter.ColorToBytes(info.shotColor);
                    enabled = Converter.IntToBytes(info.shotEnabled);
                    buffers = new List<byte[]> { action, shotIndex, start, end, camera, color, enabled };
                    command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.ShotManagerAction);
                    break;
            }
            return command;
        }

        public static NetCommand BuildSendBlenderBank(BlenderBankInfo info)
        {
            NetCommand command = null;
            byte[] actionBuffer = Converter.IntToBytes((int)info.action);

            switch (info.action)
            {
                case BlenderBankAction.ImportRequest:
                    byte[] nameBuffer = Converter.StringToBytes(info.name);
                    List<byte[]> buffers = new List<byte[]> { actionBuffer, nameBuffer };
                    command = new NetCommand(Converter.ConcatenateBuffers(buffers), MessageType.BlenderBank);
                    break;
                case BlenderBankAction.ListRequest:
                    command = new NetCommand(actionBuffer, MessageType.BlenderBank);
                    break;
            }
            return command;
        }

        public static void ReceiveBlenderBank(byte[] data)
        {
            int index = 0;
            BlenderBankAction action = (BlenderBankAction)Converter.GetInt(data, ref index);
            switch (action)
            {
                case BlenderBankAction.ListResponse:
                    {
                        List<string> names = Converter.GetStrings(data, ref index);
                        List<string> tags = Converter.GetStrings(data, ref index);
                        List<string> thumbnails = Converter.GetStrings(data, ref index);
                        GlobalState.blenderBankListEvent.Invoke(names, tags, thumbnails);
                    }
                    break;
                case BlenderBankAction.ImportResponse:
                    {
                        string objectName = Converter.GetString(data, ref index);
                        string niceName = Converter.GetString(data, ref index);
                        GlobalState.blenderBankImportObjectEvent.Invoke(objectName, niceName);
                    }
                    break;
            }
        }

        public static NetCommand BuildSendPlayerTransform(ConnectedUser playerInfo)
        {
            if (null == SyncData.mixer)
                return null;
            string json = SyncData.mixer.CreateJsonPlayerInfo(playerInfo);
            if (null == json) { return null; }
            byte[] buffer = Converter.StringToBytes(json);
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

                if (meshesMaterials.TryGetValue(meshName, out List<MaterialParameters> materialParameters))
                {
                    ApplyMaterialParameters(meshRenderer, materialParameters);
                }
                GetOrCreateMeshCollider(obj);

                if (SyncData.nodes.ContainsKey(obj.name))
                {
                    foreach (Tuple<GameObject, string> t in SyncData.nodes[obj.name].instances)
                    {
                        GameObject instance = t.Item1;
                        MeshFilter instanceMeshFilter = GetOrCreateMeshFilter(instance);
                        instanceMeshFilter.mesh = mesh;

                        MeshRenderer instanceMeshRenderer = GetOrCreateMeshRenderer(instance);
                        if (meshesMaterials.TryGetValue(meshName, out List<MaterialParameters> instanceMaterialParameters))
                        {
                            ApplyMaterialParameters(instanceMeshRenderer, instanceMaterialParameters);
                        }

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
            string meshName = Converter.GetString(data, ref currentIndex);

            int _ = Converter.GetInt(data, ref currentIndex);  // baseMeshDataSize
            int bakedMeshDataSize = Converter.GetInt(data, ref currentIndex);
            if (bakedMeshDataSize == 0)
                return null;

            Vector3[] rawVertices = Converter.GetVectors3(data, ref currentIndex);
            Vector3[] normals = Converter.GetVectors3(data, ref currentIndex);
            Vector2[] uvs = Converter.GetVectors2(data, ref currentIndex);
            int[] materialIndices = Converter.GetInts(data, ref currentIndex);

            int rawIndicesCount = (int)BitConverter.ToUInt32(data, currentIndex) * 3;
            currentIndex += 4;
            int[] rawIndices = new int[rawIndicesCount];
            int size = rawIndicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, rawIndices, 0, size);
            currentIndex += size;

            Vector3[] vertices = new Vector3[rawIndicesCount];
            for (int i = 0; i < rawIndicesCount; i++)
            {
                vertices[i] = rawVertices[rawIndices[i]];
            }

            int materialCount = (int)BitConverter.ToUInt32(data, currentIndex);
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
                    int materialNameSize = (int)BitConverter.ToUInt32(data, currentIndex);
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

                for (int i = 0; i < materialIndices.Length; i++)
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
            string greasePencilPath = Converter.GetString(data, ref currentIndex);
            string greasePencilName = Converter.GetString(data, ref currentIndex);
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
                    materialType = MaterialID.ObjectOpaqueUnlit,  // Check if we have to create an unlit material
                    name = materialName,
                    opacity = 1f
                };
                materialsParameters[materialName] = materialParameters;
            }

            materialParameters.baseColor = color;
            return materialParameters;
        }


        public static void BuildGreasePencilMaterial(byte[] data)
        {
            int currentIndex = 0;
            string materialName = Converter.GetString(data, ref currentIndex);
            bool strokeEnabled = Converter.GetBool(data, ref currentIndex);
            Converter.GetString(data, ref currentIndex);
            Converter.GetString(data, ref currentIndex);
            Color strokeColor = Converter.GetColor(data, ref currentIndex);
            Converter.GetBool(data, ref currentIndex);
            bool fillEnabled = Converter.GetBool(data, ref currentIndex);
            Converter.GetString(data, ref currentIndex);
            Color fillColor = Converter.GetColor(data, ref currentIndex);

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
            int materialIndex = Converter.GetInt(data, ref currentIndex);
            int lineWidth = Converter.GetInt(data, ref currentIndex);
            int numPoints = Converter.GetInt(data, ref currentIndex);
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
            int frameNumber = Converter.GetInt(data, ref currentIndex);
            if (frameIndex == 0)
                frameNumber = 0;
            GPFrame frame = new GPFrame(frameNumber);
            layer.frames.Add(frame);

            int strokeCount = Converter.GetInt(data, ref currentIndex);
            for (int strokeIndex = 0; strokeIndex < strokeCount; strokeIndex++)
            {
                BuildStroke(data, ref currentIndex, materialNames, layerIndex, strokeIndex, ref frame);
            }
        }


        public static void BuildLayer(byte[] data, ref int currentIndex, string[] materialNames, int layerIndex, ref List<GPLayer> layers)
        {
            string layerName = Converter.GetString(data, ref currentIndex);
            bool hidden = Converter.GetBool(data, ref currentIndex);
            GPLayer layer = new GPLayer(layerName)
            {
                visible = !hidden
            };
            layers.Add(layer);

            int frameCount = Converter.GetInt(data, ref currentIndex);
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
            string name = Converter.GetString(data, ref currentIndex);

            int materialCount = Converter.GetInt(data, ref currentIndex);
            string[] materialNames = new string[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                materialNames[i] = Converter.GetString(data, ref currentIndex);
            }

            List<GPLayer> layers = new List<GPLayer>();

            int layerCount = Converter.GetInt(data, ref currentIndex);
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
            string greasePencilName = Converter.GetString(data, ref currentIndex);

            GreasePencilData gpdata = greasePencils[greasePencilName];

            SyncData.mixer.BuildGreasePencilConnection(transform.gameObject, gpdata);
        }

        public static void BuildGreasePencilTimeOffset(byte[] data)
        {
            int currentIndex = 0;
            string name = Converter.GetString(data, ref currentIndex);
            GreasePencilData gpData = greasePencils[name];
            gpData.frameOffset = Converter.GetInt(data, ref currentIndex);
            gpData.frameScale = Converter.GetFloat(data, ref currentIndex);
            gpData.hasCustomRange = Converter.GetBool(data, ref currentIndex);
            gpData.rangeStartFrame = Converter.GetInt(data, ref currentIndex);
            gpData.rangeEndFrame = Converter.GetInt(data, ref currentIndex);

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
            byte[] startBuffer = Converter.IntToBytes((int)start);
            byte[] endBuffer = Converter.IntToBytes((int)end);
            List<byte[]> buffers = new List<byte[]> { startBuffer, endBuffer };
            byte[] buffer = Converter.ConcatenateBuffers(buffers);
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
            byte[] objectNameBuffer = Converter.StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.property, out string channelName, out int channelIndex);

            byte[] channelNameBuffer = Converter.StringToBytes(channelName);
            byte[] channelIndexBuffer = Converter.IntToBytes(channelIndex);
            byte[] frameBuffer = Converter.IntToBytes(data.key.frame);

            float value = data.key.value;
            if (data.property == AnimatableProperty.RotationX || data.property == AnimatableProperty.RotationY || data.property == AnimatableProperty.RotationZ)
            {
                value = Mathf.Deg2Rad * value;
            }
            byte[] valueBuffer = Converter.FloatToBytes(value);
            byte[] interpolationBuffer = Converter.IntToBytes((int)data.key.interpolation);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer, valueBuffer, interpolationBuffer };
            byte[] buffer = Converter.ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.AddKeyframe);
        }

        public static NetCommand BuildSendRemoveKey(SetKeyInfo data)
        {
            byte[] objectNameBuffer = Converter.StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.property, out string channelName, out int channelIndex);
            byte[] channelNameBuffer = Converter.StringToBytes(channelName);
            byte[] channelIndexBuffer = Converter.IntToBytes(channelIndex);
            byte[] frameBuffer = Converter.IntToBytes(data.key.frame);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer };
            byte[] buffer = Converter.ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.RemoveKeyframe);
        }

        public static NetCommand BuildSendMoveKey(MoveKeyInfo data)
        {
            byte[] objectNameBuffer = Converter.StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.property, out string channelName, out int channelIndex);
            byte[] channelNameBuffer = Converter.StringToBytes(channelName);
            byte[] channelIndexBuffer = Converter.IntToBytes(channelIndex);
            byte[] frameBuffer = Converter.IntToBytes(data.frame);
            byte[] newFrameBuffer = Converter.IntToBytes(data.newFrame);
            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, frameBuffer, newFrameBuffer };
            byte[] buffer = Converter.ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.MoveKeyframe);
        }

        public static NetCommand BuildSendAnimationCurve(CurveInfo data)
        {
            byte[] objectNameBuffer = Converter.StringToBytes(data.objectName);
            VRtistToBlenderAnimation(data.curve.property, out string channelName, out int channelIndex);
            byte[] channelNameBuffer = Converter.StringToBytes(channelName);
            byte[] channelIndexBuffer = Converter.IntToBytes(channelIndex);

            int count = data.curve.keys.Count;
            int[] frames = new int[count];
            for (int i = 0; i < count; ++i)
            {
                frames[i] = data.curve.keys[i].frame;
            }
            byte[] framesBuffer = Converter.IntsToBytes(frames);

            float[] values = new float[count];
            float coef = 1f;
            if (data.curve.property == AnimatableProperty.RotationX ||
                data.curve.property == AnimatableProperty.RotationY ||
                data.curve.property == AnimatableProperty.RotationZ)
                coef = Mathf.Deg2Rad;
            for (int i = 0; i < count; ++i)
            {
                values[i] = data.curve.keys[i].value * coef;
            }
            byte[] valuesBuffer = Converter.FloatsToBytes(values);

            int[] interpolations = new int[count];
            for (int i = 0; i < count; ++i)
            {
                interpolations[i] = (int)data.curve.keys[i].interpolation;
            }
            byte[] interpolationsBuffer = Converter.IntsToBytes(interpolations);

            List<byte[]> buffers = new List<byte[]> { objectNameBuffer, channelNameBuffer, channelIndexBuffer, framesBuffer, valuesBuffer, interpolationsBuffer };
            byte[] buffer = Converter.ConcatenateBuffers(buffers);
            return new NetCommand(buffer, MessageType.Animation);
        }

        public static NetCommand BuildSendQueryAnimationData(string name)
        {
            return new NetCommand(Converter.StringToBytes(name), MessageType.QueryAnimationData);
        }

        public static void BuildFrameStartEnd(byte[] data)
        {
            int index = 0;
            int start = Converter.GetInt(data, ref index);
            int end = Converter.GetInt(data, ref index);
            SyncData.mixer.SetFrameRange(start, end);
        }

        public static void BuildCurrentCamera(byte[] data)
        {
            int index = 0;
            string cameraName = Converter.GetString(data, ref index);
            GameObject cameraObject = null;
            if (cameraName.Length > 0)
            {
                Node prefabNode = SyncData.nodes[cameraName];
                cameraObject = prefabNode.instances[0].Item1;
            }
            SyncData.mixer.SetActiveCamera(cameraObject);
        }

        public static void BuildShotManager(byte[] data)
        {
            List<Shot> shots = new List<Shot>();
            int index = 0;
            int shotCount = Converter.GetInt(data, ref index);
            for (int i = 0; i < shotCount; ++i)
            {
                string shotName = Converter.GetString(data, ref index);
                string cameraName = Converter.GetString(data, ref index);
                int start = Converter.GetInt(data, ref index);
                int end = Converter.GetInt(data, ref index);
                bool enabled = Converter.GetBool(data, ref index);

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
            ShotManagerAction action = (ShotManagerAction)Converter.GetInt(data, ref index);
            int shotIndex = Converter.GetInt(data, ref index);

            switch (action)
            {
                case ShotManagerAction.AddShot:
                    {
                        string shotName = Converter.GetString(data, ref index);
                        int start = Converter.GetInt(data, ref index);
                        int end = Converter.GetInt(data, ref index);
                        string cameraName = Converter.GetString(data, ref index);
                        Color color = Converter.GetColor(data, ref index);
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
                        Converter.GetString(data, ref index);
                        SyncData.mixer.ShotManagerDuplicateShot(shotIndex);
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        int offset = Converter.GetInt(data, ref index);
                        SyncData.mixer.ShotManagerMoveShot(shotIndex, offset);
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                        int start = Converter.GetInt(data, ref index);
                        int end = Converter.GetInt(data, ref index);
                        string cameraName = Converter.GetString(data, ref index);
                        Color color = Converter.GetColor(data, ref index);
                        int enabled = Converter.GetInt(data, ref index);
                        SyncData.mixer.ShotManagerUpdateShot(shotIndex, start, end, cameraName, color, enabled);
                    }
                    break;
            }

        }
        public static void BuildClientAttribute(byte[] data)
        {
            int index = 0;
            string json = Converter.GetString(data, ref index);
            SyncData.mixer.UpdateClient(json);
        }

        public static void BuildListAllClients(byte[] data)
        {
            int index = 0;
            string json = Converter.GetString(data, ref index);
            SyncData.mixer.ListAllClients(json);
        }
    }
}
