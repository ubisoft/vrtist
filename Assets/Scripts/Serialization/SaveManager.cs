using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace VRtist.Serialization
{
    public class MeshInfo
    {
        public string meshPath;
        public Mesh mesh;
        public List<MaterialInfo> materialsInfo;
    }


    public class MaterialInfo
    {
        public string materialPath;
        public Material material;
    }


    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                return instance;
            }
        }

        private string saveFolder;
        private string currentProjectName;

        private readonly Dictionary<string, MeshInfo> meshes = new Dictionary<string, MeshInfo>();  // meshes to save in separated files

        private void Awake()
        {
            if (null == instance)
            {
                instance = this;
            }

            saveFolder = Application.persistentDataPath + "/saves/";
        }

        public static Material GetMaterial(bool opaque)
        {
            return opaque ? ResourceManager.GetMaterial(MaterialID.ObjectOpaque) : ResourceManager.GetMaterial(MaterialID.ObjectTransparent);
        }

        private string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        private string GetScenePath(string projectName)
        {
            return saveFolder + projectName + "/scene.vrtist";
        }

        private string GetMeshPath(string projectName, string meshName)
        {
            return saveFolder + projectName + "/" + ReplaceInvalidChars(meshName) + ".mesh";
        }

        private string GetMaterialPath(string projectName, string materialName)
        {
            return saveFolder + projectName + "/" + ReplaceInvalidChars(materialName) + "/";
        }

        public void Save(string projectName)
        {
            currentProjectName = projectName;

            // Parse RightHanded transform
            Transform root = Utils.FindWorld().transform.Find("RightHanded");
            string path = "";
            StartCoroutine(SerializeChildren(root, path, save: true));
        }

        private IEnumerator SerializeChildren(Transform root, string path, bool save = false)
        {
            foreach (Transform emptyParent in root)
            {
                yield return null;

                // We should only have [gameObjectName]_parent game objects
                if (!emptyParent.name.EndsWith("_parent"))
                {
                    Debug.LogWarning("Ignoring the serialization of a non parent game object: " + transform.name);
                    continue;
                }

                // All the children should be an empty parent container, so get its child
                Transform child = emptyParent.GetChild(0);
                string childPath = path + "/" + child.name;

                // Depending on its type (which controller we can find on it) create data objects to be serialized
                LightController lightController = child.GetComponent<LightController>();
                if (null != lightController)
                {
                    LightData lightData = new LightData();
                    SetCommonData(child, childPath, lightController, lightData);
                    SetLightData(child, lightController, lightData);
                    SceneData.Current.lights.Add(lightData);
                    continue;
                }

                CameraController cameraController = child.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    CameraData cameraData = new CameraData();
                    SetCommonData(child, childPath, cameraController, cameraData);
                    SetCameraData(child, cameraController, cameraData);
                    SceneData.Current.cameras.Add(cameraData);
                    continue;
                }

                ColimatorController colimatorController = child.GetComponent<ColimatorController>();
                if (null != colimatorController)
                {
                    // Nothing to do here, ignore the object
                    continue;
                }

                // Do this one at the end, because other controllers inherits from ParametersController
                ParametersController controller = child.GetComponent<ParametersController>();
                ObjectData data = new ObjectData();
                SetCommonData(child, childPath, controller, data);
                SetMeshData(child, controller, data);
                SceneData.Current.objects.Add(data);

                // Serialize children
                if (!data.isImported)
                {
                    // We consider here that we can't change objects hierarchy
                    _ = SerializeChildren(child, childPath);
                }
            }

            // Save
            if (save)
            {
                // Save shot manager data

                // Save animation data

                // Save scene
                SerializationManager.Save(GetScenePath(currentProjectName), SceneData.Current);

                // Save meshes
                foreach (var meshInfo in meshes.Values)
                {
                    yield return null;
                    SerializationManager.Save(meshInfo.meshPath, new MeshData(meshInfo));

                    // Save materials
                    foreach (MaterialInfo materialInfo in meshInfo.materialsInfo)
                    {
                        yield return null;
                        SaveMaterial(materialInfo);
                    }
                }
            }
        }

        private void SaveMaterial(MaterialInfo materialInfo)
        {
            string shaderName = materialInfo.material.shader.name;
            if (shaderName != "VRtist/BlenderImport" &&
                shaderName != "VRtist/BlenderImportTransparent" &&
                shaderName != "VRtist/BlenderImportEditor" &&
                shaderName != "VRtist/BlenderImportTransparentEditor")
            {
                Debug.LogWarning($"Unsupported material {shaderName}. Expected VRtist/BlenderImport***.");
                return;
            }

            SaveTexture("_ColorMap", "_UseColorMap", materialInfo);
            SaveTexture("_NormalMap", "_UseNormalMap", materialInfo);
            SaveTexture("_MetallicMap", "_UseMetallicMap", materialInfo);
            SaveTexture("_RoughnessMap", "_UseRoughnessMap", materialInfo);
            SaveTexture("_EmissiveMap", "_UseEmissiveMap", materialInfo);
            SaveTexture("_AoMap", "_UseAoMap", materialInfo);
            SaveTexture("_OpacityMap", "_UseOpacityMap", materialInfo);
        }

        private void SaveTexture(string textureName, string useName, MaterialInfo materialInfo)
        {
            if (materialInfo.material.GetInt(useName) == 1)
            {
                string path = materialInfo.materialPath + name + ".tex";
                Texture2D texture = (Texture2D)materialInfo.material.GetTexture(textureName);
                Utils.SavePNG(texture, path);
            }
        }

        private void SetMeshData(Transform trans, ParametersController controller, ObjectData data)
        {
            // Mesh for non-imported objects
            if (null == controller || !controller.isImported)
            {
                MeshRenderer meshRenderer = trans.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = trans.GetComponent<MeshFilter>();
                if (null != meshFilter && null != meshRenderer)
                {
                    // Materials
                    List<MaterialInfo> materialsInfo = new List<MaterialInfo>();
                    foreach (Material material in meshRenderer.materials)
                    {
                        string materialPath = GetMaterialPath(currentProjectName, material.name);
                        materialsInfo.Add(new MaterialInfo { materialPath = materialPath, material = material });
                    }

                    // Mesh
                    string meshPath = GetMeshPath(currentProjectName, meshFilter.mesh.name);
                    meshes[meshPath] = new MeshInfo { meshPath = meshPath, mesh = meshFilter.mesh, materialsInfo = materialsInfo };
                    data.meshPath = meshPath;
                }
                data.isImported = false;
            }
            else if (null != controller && controller.isImported)
            {
                data.meshPath = controller.importPath;
                data.isImported = true;
            }
        }

        private void SetCommonData(Transform trans, string path, ParametersController controller, ObjectData data)
        {
            data.path = path;
            data.tag = trans.gameObject.tag;

            // Transform
            data.position = trans.localPosition;
            data.rotation = trans.localRotation;
            data.scale = trans.localScale;

            // TODO constraints

            if (null != controller)
            {
                data.lockPosition = controller.lockPosition;
                data.lockRotation = controller.lockRotation;
                data.lockScale = controller.lockScale;
            }
        }

        private void SetLightData(Transform trans, LightController controller, LightData data)
        {
            data.lightType = controller.lightType;
            data.intensity = controller.intensity;
            data.minIntensity = controller.minIntensity;
            data.maxIntensity = controller.maxIntensity;
            data.color = controller.color;
            data.castShadows = controller.castShadows;
            data.near = controller.near;
            data.range = controller.range;
            data.minRange = controller.minRange;
            data.maxRange = controller.maxRange;
            data.outerAngle = controller.outerAngle;
            data.innerAngle = controller.innerAngle;
        }

        private void SetCameraData(Transform trans, CameraController controller, CameraData data)
        {
            data.focal = controller.focal;
            data.focus = controller.focus;
            data.aperture = controller.aperture;
            data.enableDOF = controller.enableDOF;
            data.near = controller.near;
            data.far = controller.far;
            data.filmHeight = controller.filmHeight;
        }

        public void Load(string projectName)
        {
            currentProjectName = projectName;
            Transform root = GlobalState.Instance.world.Find("RightHanded");

            // Clear current scene
            DeleteTransformChildren(root);
            DeleteTransformChildren(SyncData.prefab);

            // Load data from file
            string path = GetScenePath(projectName);
            SceneData saveData = (SceneData)SerializationManager.Load(path);

            // Objects
            Transform importedParent = new GameObject("__VRtist_tmp_load__").transform;
            foreach (ObjectData data in saveData.objects)
            {
                LoadObject(data, importedParent);
            }
            Destroy(importedParent.gameObject);

            // Lights
            foreach (LightData data in saveData.lights)
            {
                LoadLight(data);
            }

            // Cameras
            foreach (CameraData data in saveData.cameras)
            {
                LoadCamera(data);
            }
        }

        private void DeleteTransformChildren(Transform trans)
        {
            foreach (Transform child in trans)
            {
                Destroy(child.gameObject);
            }
        }

        private void LoadCommonData(GameObject gobject, ObjectData data)
        {
            if (null != data.tag && data.tag.Length > 0)
            {
                gobject.tag = data.tag;
            }

            gobject.transform.localPosition = data.position;
            gobject.transform.localRotation = data.rotation;
            gobject.transform.localScale = data.scale;

            if (data.lockPosition || data.lockRotation || data.lockScale)
            {
                ParametersController controller = gobject.AddComponent<ParametersController>();
                controller.lockPosition = data.lockPosition;
                controller.lockRotation = data.lockRotation;
                controller.lockScale = data.lockScale;
            }
        }

        private async void LoadObject(ObjectData data, Transform importedParent)
        {
            string[] splitted = data.path.Split('/');
            string name = splitted[splitted.Length - 1];
            string parentPath = data.path.Substring(0, data.path.Length - name.Length - 1);  // -1: remove "/"

            GameObject gobject;

            // Check for import
            if (data.isImported)
            {
                try
                {
                    gobject = await GlobalState.GeometryImporter.ImportObjectAsync(data.meshPath, importedParent);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to load external object: " + e.Message);
                    return;
                }
            }
            else
            {
                gobject = new GameObject(name);
            }

            LoadCommonData(gobject, data);

            // Mesh
            if (null != data.meshPath && data.meshPath.Length > 0)
            {
                if (!data.isImported)
                {
                    MeshData meshData = (MeshData)SerializationManager.Load(data.meshPath);
                    gobject.AddComponent<MeshFilter>().mesh = meshData.CreateMesh();
                    gobject.AddComponent<MeshRenderer>().materials = meshData.GetMaterials();
                    MeshCollider collider = gobject.AddComponent<MeshCollider>();
                    collider.convex = true;
                    collider.isTrigger = true;
                }
            }

            // Instantiate using the SyncData API
            GameObject newObject = null;
            if (data.isImported)
            {
                newObject = SyncData.InstantiateFullHierarchyPrefab(SyncData.CreateFullHierarchyPrefab(gobject, "__VRtist_tmp_load__"));
            }
            else
            {
                // TODO node hierarchy
                newObject = SyncData.InstantiatePrefab(SyncData.CreateInstance(gobject, SyncData.prefab));
            }

            // Then delete the original loaded object
            Destroy(gobject);
        }

        private void LoadLight(LightData data)
        {
            GameObject lightPrefab = null;

            switch (data.lightType)
            {
                case LightType.Directional:
                    lightPrefab = ResourceManager.GetPrefab(PrefabID.SunLight);
                    break;
                case LightType.Spot:
                    lightPrefab = ResourceManager.GetPrefab(PrefabID.SpotLight);
                    break;
                case LightType.Point:
                    lightPrefab = ResourceManager.GetPrefab(PrefabID.PointLight);
                    break;
            }

            if (lightPrefab)
            {
                GameObject newPrefab = SyncData.CreateInstance(lightPrefab, SyncData.prefab, isPrefab: true);

                LoadCommonData(newPrefab, data);

                LightController controller = newPrefab.GetComponent<LightController>();
                controller.intensity = data.intensity;
                controller.minIntensity = data.minIntensity;
                controller.maxIntensity = data.maxIntensity;
                controller.color = data.color;
                controller.castShadows = data.castShadows;
                controller.near = data.near;
                controller.range = data.range;
                controller.minRange = data.minRange;
                controller.maxRange = data.maxRange;
                controller.outerAngle = data.outerAngle;
                controller.innerAngle = data.innerAngle;

                GameObject instance = SyncData.InstantiatePrefab(newPrefab);
            }
        }

        private void LoadCamera(CameraData data)
        {
            GameObject cameraPrefab = ResourceManager.GetPrefab(PrefabID.Camera);
            GameObject newPrefab = SyncData.CreateInstance(cameraPrefab, SyncData.prefab, isPrefab: true);

            LoadCommonData(newPrefab, data);

            CameraController controller = newPrefab.GetComponent<CameraController>();
            controller.focal = data.focal;
            controller.focus = data.focus;
            controller.aperture = data.aperture;
            controller.enableDOF = data.enableDOF;
            controller.near = data.near;
            controller.far = data.far;
            controller.filmHeight = data.filmHeight;

            GameObject instance = SyncData.InstantiatePrefab(newPrefab);
        }
    }
}
