using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace VRtist.Serialization
{
    public class MeshInfo
    {
        public string relativePath;
        public string absolutePath;
        public Mesh mesh;
        public List<MaterialInfo> materialsInfo;
    }


    public class MaterialInfo
    {
        public string relativePath;
        public string absolutePath;
        public Material material;
    }


    /// <summary>
    /// Save current scene.
    /// Warning: this class has to be a monobehaviour in order to iterate transforms of the scene.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public Camera screenshotCamera;
        public RenderTexture cubeMapRT;
        public RenderTexture equiRectRT;

        private string saveFolder;
        private string currentProjectName;

        private readonly Dictionary<string, MeshInfo> meshes = new Dictionary<string, MeshInfo>();  // meshes to save in separated files
        private readonly Dictionary<string, MaterialInfo> materials = new Dictionary<string, MaterialInfo>();  // all materials

        private readonly string DEFAULT_PROJECT_NAME = "newProject";

        #region Singleton
        // ----------------------------------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------------------------------

        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                return instance;
            }
        }

        private void Awake()
        {
            if (null == instance)
            {
                instance = this;
            }

            saveFolder = Application.persistentDataPath + "/saves/";
        }
        #endregion

        #region Path Management
        // ----------------------------------------------------------------------------------------
        // Path Management
        // ----------------------------------------------------------------------------------------

        private string ReplaceInvalidChars(string filename)
        {
            return string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));
        }

        public string NormalizeProjectName(string name)
        {
            return ReplaceInvalidChars(name);
        }

        private string GetScenePath(string projectName)
        {
            return saveFolder + projectName + "/scene.vrtist";
        }

        private void GetMeshPath(string projectName, string meshName, out string absolutePath, out string relativePath)
        {
            relativePath = ReplaceInvalidChars(meshName) + ".mesh";
            absolutePath = saveFolder + projectName + "/" + ReplaceInvalidChars(meshName) + ".mesh";
        }

        private string GetScreenshotPath(string projectName)
        {
            return saveFolder + projectName + "/thumbnail.png";
        }

        private void GetMaterialPath(string projectName, string materialName, out string absolutePath, out string relativePath)
        {
            relativePath = ReplaceInvalidChars(materialName) + "/";
            absolutePath = saveFolder + projectName + "/" + ReplaceInvalidChars(materialName) + "/";
        }

        private string GetSaveFolderPath(string projectName)
        {
            return saveFolder + projectName + "/";
        }

        public List<string> GetProjectThumbnailPaths()
        {
            List<string> paths = new List<string>();

            if (!Directory.Exists(saveFolder)) { return paths; }

            foreach (string directory in Directory.GetDirectories(saveFolder))
            {
                string thumbnail = Path.Combine(directory, "thumbnail.png");
                if (File.Exists(thumbnail))
                {
                    paths.Add(thumbnail);
                }
            }
            return paths;
        }

        public string GetNextValidProjectName()
        {
            string name = DEFAULT_PROJECT_NAME;

            if (!Directory.Exists(saveFolder)) { return name; }

            int number = 1;
            foreach (string directory in Directory.GetDirectories(saveFolder, $"{DEFAULT_PROJECT_NAME}*"))
            {
                string dirname = Path.GetFileName(directory);
                if (name == dirname)
                {
                    name = $"{DEFAULT_PROJECT_NAME}_{number,0:D3}";
                    ++number;
                }
            }

            return name;
        }
        #endregion

        #region Save
        // ----------------------------------------------------------------------------------------
        // Save
        // ----------------------------------------------------------------------------------------

        System.Diagnostics.Stopwatch stopwatch;
        System.Diagnostics.Stopwatch totalStopwatch;

        private void LogElapsedTime(string what, System.Diagnostics.Stopwatch timer)
        {
            TimeSpan ts = timer.Elapsed;
            string elapsedTime = String.Format("{0:00}m {1:00}s {2:00}ms", ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            Debug.Log($"{what}: {elapsedTime}");
        }

        public void Save(string projectName)
        {
            if (!CommandManager.IsSceneDirty()) { return; }

            totalStopwatch = new System.Diagnostics.Stopwatch();
            totalStopwatch.Start();

            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            GlobalState.Instance.messageBox.ShowMessage("Saving scene, please wait...");
            CommandManager.SetSceneDirty(false);

            currentProjectName = projectName;
            meshes.Clear();
            materials.Clear();
            SceneData.Current.Clear();

            stopwatch.Stop();
            LogElapsedTime("Pre Save", stopwatch);

            // Parse RightHanded transform
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Transform root = Utils.FindWorld().transform.Find("RightHanded");
            string path = "";
            SerializeChildren(root, path, save: true);
        }

        private void SerializeChildren(Transform root, string path, bool save = false)
        {
            foreach (Transform emptyParent in root)
            {
                // We should only have [gameObjectName]_parent game objects
                if (!emptyParent.name.EndsWith("_parent"))
                {
                    Debug.LogWarning("Ignoring the serialization of a non parent game object: " + emptyParent.name);
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
                    SetLightData(lightController, lightData);
                    SceneData.Current.lights.Add(lightData);
                    continue;
                }

                CameraController cameraController = child.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    CameraData cameraData = new CameraData();
                    SetCommonData(child, childPath, cameraController, cameraData);
                    SetCameraData(cameraController, cameraData);
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
                SetObjectData(child, controller, data);
                SceneData.Current.objects.Add(data);

                // Serialize children
                if (!data.isImported)
                {
                    // We consider here that we can't change objects hierarchy
                    SerializeChildren(child, childPath);
                }
            }

            // Save
            if (save)
            {
                stopwatch.Stop();
                LogElapsedTime($"Scene Traversal ({SceneData.Current.objects.Count} objects)", stopwatch);

                // Save shot manager data

                // Save animation data

                // Save skybox
                SceneData.Current.skyData = GlobalState.Instance.SkySettings;

                // Save scene
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                SerializationManager.Save(GetScenePath(currentProjectName), SceneData.Current, deleteFolder: true);
                stopwatch.Stop();
                LogElapsedTime($"Write Scene", stopwatch);

                // Save meshes
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                foreach (var meshInfo in meshes.Values)
                {
                    SerializationManager.Save(meshInfo.absolutePath, new MeshData(meshInfo));
                }
                stopwatch.Stop();
                LogElapsedTime($"Write Meshes ({meshes.Count})", stopwatch);

                // Save materials
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                foreach (MaterialInfo materialInfo in materials.Values)
                {
                    SaveMaterial(materialInfo);
                }
                stopwatch.Stop();
                LogElapsedTime($"Write Materials ({materials.Count})", stopwatch);

                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                SaveScreenshot();
                stopwatch.Stop();
                LogElapsedTime($"Snapshot", stopwatch);

                totalStopwatch.Stop();
                LogElapsedTime("Total Time", totalStopwatch);

                GlobalState.Instance.messageBox.SetVisible(false);
            }
        }

        private void SaveScreenshot()
        {
            screenshotCamera.gameObject.SetActive(true);
            screenshotCamera.RenderToCubemap(cubeMapRT);
            cubeMapRT.ConvertToEquirect(equiRectRT);
            Texture2D texture = new Texture2D(equiRectRT.width, equiRectRT.height);
            RenderTexture previousActiveRT = RenderTexture.active;
            RenderTexture.active = equiRectRT;
            texture.ReadPixels(new Rect(0, 0, equiRectRT.width, equiRectRT.height), 0, 0);
            RenderTexture.active = previousActiveRT;
            Utils.SavePNG(texture, GetScreenshotPath(currentProjectName));
            screenshotCamera.gameObject.SetActive(false);
        }

        private void SaveMaterial(MaterialInfo materialInfo)
        {
            string shaderName = materialInfo.material.shader.name;
            if (shaderName != "VRtist/ObjectOpaque" && shaderName != "VRtist/ObjectTransparent")
            {
                Debug.LogWarning($"Unsupported material {shaderName}. Expected VRtist/ObjectOpaque or VRtist/ObjectTransparent.");
                return;
            }

            SaveTexture("_ColorMap", "_UseColorMap", "color", materialInfo);
            SaveTexture("_NormalMap", "_UseNormalMap", "normal", materialInfo);
            SaveTexture("_MetallicMap", "_UseMetallicMap", "metallic", materialInfo);
            SaveTexture("_RoughnessMap", "_UseRoughnessMap", "roughness", materialInfo);
            SaveTexture("_EmissiveMap", "_UseEmissiveMap", "emissive", materialInfo);
            SaveTexture("_AoMap", "_UseAoMap", "ao", materialInfo);
            SaveTexture("_OpacityMap", "_UseOpacityMap", "opacity", materialInfo);
        }

        private void SaveTexture(string textureName, string boolName, string baseName, MaterialInfo materialInfo)
        {
            if (materialInfo.material.GetInt(boolName) == 1)
            {
                string path = materialInfo.absolutePath + baseName + ".tex";
                Texture2D texture = (Texture2D)materialInfo.material.GetTexture(textureName);
                TextureUtils.WriteRawTexture(path, texture);
            }
        }

        private void SetObjectData(Transform trans, ParametersController controller, ObjectData data)
        {
            // Mesh for non-imported objects
            if (null == controller || !controller.isImported)
            {
                MeshRenderer meshRenderer = trans.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = trans.GetComponent<MeshFilter>();
                if (null != meshFilter && null != meshRenderer)
                {
                    // Materials
                    foreach (Material material in meshRenderer.materials)
                    {
                        GetMaterialPath(currentProjectName, material.name, out string materialAbsolutePath, out string materialRelativePath);
                        MaterialInfo materialInfo = new MaterialInfo { relativePath = materialRelativePath, absolutePath = materialAbsolutePath, material = material };
                        materials.Add(trans.name + "." + material.name, materialInfo);
                        data.materialsData.Add(new MaterialData(materialInfo));
                    }

                    // Mesh
                    GetMeshPath(currentProjectName, meshFilter.mesh.name, out string meshAbsolutePath, out string meshRelativePath);
                    meshes[meshRelativePath] = new MeshInfo { relativePath = meshRelativePath, absolutePath = meshAbsolutePath, mesh = meshFilter.mesh };
                    data.meshPath = meshRelativePath;
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
            data.name = trans.name;
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

        private void SetLightData(LightController controller, LightData data)
        {
            data.lightType = controller.Type;
            data.intensity = controller.Intensity;
            data.minIntensity = controller.minIntensity;
            data.maxIntensity = controller.maxIntensity;
            data.color = controller.Color;
            data.castShadows = controller.CastShadows;
            data.near = controller.ShadowNearPlane;
            data.range = controller.Range;
            data.minRange = controller.minRange;
            data.maxRange = controller.maxRange;
            data.outerAngle = controller.OuterAngle;
            data.innerAngle = controller.InnerAngle;
        }

        private void SetCameraData(CameraController controller, CameraData data)
        {
            data.focal = controller.focal;
            data.focus = controller.focus;
            data.aperture = controller.aperture;
            data.enableDOF = controller.enableDOF;
            data.near = controller.near;
            data.far = controller.far;
            data.filmHeight = controller.filmHeight;
        }
        #endregion

        #region Load
        // ----------------------------------------------------------------------------------------
        // Load
        // ----------------------------------------------------------------------------------------

        public void Load(string projectName)
        {
            GlobalState.Instance.messageBox.ShowMessage("Loading scene, please wait...");
            currentProjectName = projectName;
            GlobalState.Settings.ProjectName = projectName;

            // Clear current scene
            Utils.ClearScene();

            // TODO remove shotitems
            // TODO remove animations data

            // TODO position user

            // Load data from file
            string path = GetScenePath(projectName);
            SceneData saveData = (SceneData)SerializationManager.Load(path);

            // Sky
            GlobalState.Instance.SkySettings = saveData.skyData;

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

            GlobalState.Instance.messageBox.SetVisible(false);
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

        private Material[] LoadMaterials(ObjectData data)
        {
            Material[] materials = new Material[data.materialsData.Count];
            for (int i = 0; i < data.materialsData.Count; ++i)
            {
                materials[i] = data.materialsData[i].CreateMaterial(GetSaveFolderPath(currentProjectName));
            }
            return materials;
        }

        private async void LoadObject(ObjectData data, Transform importedParent)
        {
            GameObject gobject;
            string absoluteMeshPath = GetSaveFolderPath(currentProjectName) + data.meshPath;

            // Check for import
            if (data.isImported)
            {
                try
                {
                    gobject = await GlobalState.GeometryImporter.ImportObjectAsync(absoluteMeshPath, importedParent);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to load external object: " + e.Message);
                    return;
                }
            }
            else
            {
                gobject = new GameObject(data.name);
            }

            LoadCommonData(gobject, data);
            gobject.name = data.name;

            // Mesh
            if (null != data.meshPath && data.meshPath.Length > 0)
            {
                if (!data.isImported)
                {
                    MeshData meshData = (MeshData)SerializationManager.Load(absoluteMeshPath);
                    gobject.AddComponent<MeshFilter>().mesh = meshData.CreateMesh();
                    gobject.AddComponent<MeshRenderer>().materials = LoadMaterials(data);
                    MeshCollider collider = gobject.AddComponent<MeshCollider>();
                    collider.convex = true;
                    collider.isTrigger = true;
                }
            }

            // Instantiate using the SyncData API
            if (data.isImported)
            {
                SyncData.InstantiateFullHierarchyPrefab(SyncData.CreateFullHierarchyPrefab(gobject, "__VRtist_tmp_load__"));
            }
            else
            {
                // TODO node hierarchy
                GameObject newObject = SyncData.InstantiatePrefab(SyncData.CreateInstance(gobject, SyncData.prefab));

                // Name the mesh
                newObject.GetComponentInChildren<MeshFilter>().mesh.name = gobject.GetComponentInChildren<MeshFilter>().mesh.name;
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
                GameObject clone = SyncData.InstantiatePrefab(newPrefab);

                LoadCommonData(clone, data);

                LightController controller = clone.GetComponent<LightController>();
                Debug.Log($"From Load: {data.intensity}");
                controller.Intensity = data.intensity;
                controller.minIntensity = data.minIntensity;
                controller.maxIntensity = data.maxIntensity;
                controller.Color = data.color;
                controller.CastShadows = data.castShadows;
                controller.ShadowNearPlane = data.near;
                controller.Range = data.range;
                controller.minRange = data.minRange;
                controller.maxRange = data.maxRange;
                controller.OuterAngle = data.outerAngle;
                controller.InnerAngle = data.innerAngle;
            }
        }

        private void LoadCamera(CameraData data)
        {
            GameObject cameraPrefab = ResourceManager.GetPrefab(PrefabID.Camera);
            GameObject newPrefab = SyncData.CreateInstance(cameraPrefab, SyncData.prefab, isPrefab: true);

            LoadCommonData(newPrefab, data);

            GameObject clone = SyncData.InstantiatePrefab(newPrefab);

            CameraController controller = clone.GetComponent<CameraController>();
            controller.focal = data.focal;
            controller.focus = data.focus;
            controller.aperture = data.aperture;
            controller.enableDOF = data.enableDOF;
            controller.near = data.near;
            controller.far = data.far;
            controller.filmHeight = data.filmHeight;
        }
        #endregion

        #region Delete
        // ----------------------------------------------------------------------------------------
        // Delete
        // ----------------------------------------------------------------------------------------

        public void Delete(string projectName)
        {
            string path = saveFolder + projectName;
            if (!Directory.Exists(path)) { return; }

            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to delete project " + projectName + ": " + e.Message);
            }
        }
        #endregion

        #region Duplicate
        // ----------------------------------------------------------------------------------------
        // Load
        // ----------------------------------------------------------------------------------------

        public void Duplicate(string projectName, string newName)
        {
            string srcPath = saveFolder + projectName;
            if (!Directory.Exists(srcPath))
            {
                Debug.LogError($"Failed to duplicate project {projectName}: project doesn't exist.");
                return;
            }

            string dstPath = saveFolder + newName;
            if (Directory.Exists(dstPath))
            {
                Debug.LogError($"Failed to duplicate project {projectName} as {newName}: a project already exists.");
                return;
            }

            DirectoryCopy(srcPath, dstPath);
        }

        private void DirectoryCopy(string srcPath, string dstPath)
        {
            DirectoryInfo directory = new DirectoryInfo(srcPath);
            Directory.CreateDirectory(dstPath);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(dstPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Copy subdirs
            DirectoryInfo[] subdirs = directory.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
            {
                string tempPath = Path.Combine(dstPath, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath);
            }
        }
        #endregion
    }
}
