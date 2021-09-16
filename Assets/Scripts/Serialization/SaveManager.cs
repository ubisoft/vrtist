/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections;
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
    }

    public class SkinMeshInfo
    {
        public string relativePath;
        public string absolutePath;
        public MeshInfo mesh;
        public SkinnedMeshRenderer skinMesh;
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

        private Transform cameraRig;
        private Transform rootTransform;

        private string defaultSaveFolder;
        private string saveFolder;
        private string currentProjectName;

        private readonly Dictionary<string, MeshInfo> meshes = new Dictionary<string, MeshInfo>();  // meshes to save in separated files
        private readonly Dictionary<string, MaterialInfo> materials = new Dictionary<string, MaterialInfo>();  // all materials
        private readonly Dictionary<string, SkinMeshInfo> skinMeshes = new Dictionary<string, SkinMeshInfo>();

        private readonly Dictionary<string, Mesh> loadedMeshes = new Dictionary<string, Mesh>();
        private readonly List<CameraController> loadedCameras = new List<CameraController>();
        private readonly Dictionary<GameObject, SkinMeshData> loadedSkinMeshes = new Dictionary<GameObject, SkinMeshData>();
        private readonly Dictionary<GameObject, Material[]> skinMeshMaterials = new Dictionary<GameObject, Material[]>();

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

            defaultSaveFolder = saveFolder = Application.persistentDataPath + "/saves/";
            cameraRig = Utils.FindRootGameObject("Camera Rig").transform;
            rootTransform = Utils.FindWorld().transform.Find("RightHanded");
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
            totalStopwatch = new System.Diagnostics.Stopwatch();
            totalStopwatch.Start();

            // Pre save
            stopwatch = System.Diagnostics.Stopwatch.StartNew();

            GlobalState.Instance.messageBox.ShowMessage("Saving scene, please wait...");

            currentProjectName = projectName;
            meshes.Clear();
            materials.Clear();
            skinMeshes.Clear();
            SceneData.Current.Clear();

            stopwatch.Stop();
            LogElapsedTime("Pre Save", stopwatch);

            // Scene traversal
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            TraverseScene(rootTransform, "");
            stopwatch.Stop();
            LogElapsedTime($"Scene Traversal ({SceneData.Current.objects.Count} objects)", stopwatch);

            // Retrieve shot manager data
            SetShotManagerData();

            // Retrieve animation data
            SetAnimationsData();

            // Set constraints data
            SetConstraintsData();

            // Retrieve skybox
            SceneData.Current.skyData = GlobalState.Instance.SkySettings;

            // Set player data
            SetPlayerData();

            // Save scene on disk
            SaveScene();
            SaveMeshes();
            SaveMaterials();
            StartCoroutine(SaveScreenshot());

            totalStopwatch.Stop();
            LogElapsedTime("Total Time", totalStopwatch);

            SceneManager.sceneSavedEvent.Invoke();
            CommandManager.SetSceneDirty(false);
            GlobalState.Instance.messageBox.SetVisible(false);
        }

        private void TraverseScene(Transform root, string parentPath)
        {
            foreach (Transform currentTransform in root)
            {
                if (currentTransform == SceneManager.BoundingBox)
                    continue;

                string path = parentPath;
                path += "/" + currentTransform.name;


                // Depending on its type (which controller we can find on it) create data objects to be serialized
                LightController lightController = currentTransform.GetComponent<LightController>();
                if (null != lightController)
                {
                    LightData lightData = new LightData();
                    SetCommonData(currentTransform, parentPath, path, lightController, lightData);
                    SetLightData(lightController, lightData);
                    SceneData.Current.lights.Add(lightData);
                    continue;
                }

                CameraController cameraController = currentTransform.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    CameraData cameraData = new CameraData();
                    SetCommonData(currentTransform, parentPath, path, cameraController, cameraData);
                    SetCameraData(cameraController, cameraData);
                    SceneData.Current.cameras.Add(cameraData);
                    continue;
                }

                ColimatorController colimatorController = currentTransform.GetComponent<ColimatorController>();
                if (null != colimatorController)
                {
                    // Nothing to do here, ignore the object
                    continue;
                }

                RigController skinController = currentTransform.GetComponent<RigController>();
                if (null != skinController && !skinController.isImported)
                {
                    SceneData.Current.rigs.Add(new RigData(skinController));
                }

                // Do this one at the end, because other controllers inherits from ParametersController
                ParametersController controller = currentTransform.GetComponent<ParametersController>();
                ObjectData data = new ObjectData();
                SetCommonData(currentTransform, parentPath, path, controller, data);
                try
                {
                    SetObjectData(currentTransform, controller, data);
                    SceneData.Current.objects.Add(data);
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to set object data: " + e.Message);
                }

                // Serialize children
                if (!data.isImported)
                {
                    TraverseScene(currentTransform, path);
                }
            }
        }

        private void SaveScene()
        {
            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            SerializationManager.Save(GetScenePath(currentProjectName), SceneData.Current, deleteFolder: true);
            stopwatch.Stop();
            LogElapsedTime($"Write Scene", stopwatch);
        }

        private void SaveMeshes()
        {

            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            foreach (var meshInfo in meshes.Values)
            {
                SerializationManager.Save(meshInfo.absolutePath, new MeshData(meshInfo));
            }
            foreach (var skinMesh in skinMeshes.Values)
            {
                SerializationManager.Save(skinMesh.absolutePath, new SkinMeshData(skinMesh));
            }
            timer.Stop();
            LogElapsedTime($"Write Meshes ({meshes.Count})", timer);
        }

        private void SaveMaterials()
        {
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
            foreach (MaterialInfo materialInfo in materials.Values)
            {
                SaveMaterial(materialInfo);
            }
            timer.Stop();
            LogElapsedTime($"Write Materials ({meshes.Count})", timer);
        }

        private IEnumerator SaveScreenshot()
        {
            yield return new WaitForEndOfFrame();
            screenshotCamera.gameObject.SetActive(true);

            // -- FIX ------------------------------
            // FIRST Render to RenderTarget is black. Do a fake render before the real render.
            RenderTexture fixRT = new RenderTexture(16, 16, 24);
            screenshotCamera.targetTexture = fixRT;
            screenshotCamera.Render();
            // -- ENDFIX ----------------------------

            screenshotCamera.RenderToCubemap(cubeMapRT);
            cubeMapRT.ConvertToEquirect(equiRectRT);
            Texture2D texture = new Texture2D(equiRectRT.width, equiRectRT.height);
            RenderTexture previousActiveRT = RenderTexture.active;
            RenderTexture.active = equiRectRT;
            texture.ReadPixels(new Rect(0, 0, equiRectRT.width, equiRectRT.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActiveRT;
            Utils.SavePNG(texture, GetScreenshotPath(currentProjectName));
            screenshotCamera.gameObject.SetActive(false);
        }

        private void SaveMaterial(MaterialInfo materialInfo)
        {
            string shaderName = materialInfo.material.shader.name;
            if (shaderName != "VRtist/ObjectOpaque" &&
                shaderName != "VRtist/ObjectTransparent" &&
                shaderName != "VRtist/ObjectOpaqueUnlit" &&
                shaderName != "VRtist/ObjectTransparentUnlit")
            {
                Debug.LogWarning($"Unsupported material {shaderName}. Expected VRtist/Object*.");
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

        private void SetPlayerData()
        {
            SceneData.Current.playerData = new PlayerData
            {
                position = cameraRig.localPosition,
                rotation = cameraRig.localRotation,
                scale = GlobalState.WorldScale
            };
        }

        private void SetShotManagerData()
        {
            foreach (Shot shot in ShotManager.Instance.shots)
            {
                ShotData shotData = new ShotData
                {
                    name = shot.name,
                    start = shot.start,
                    end = shot.end,
                    enabled = shot.enabled
                };

                shotData.cameraName = "";
                if (null != shot.camera)
                    Utils.GetTransformRelativePathTo(shot.camera.transform, rootTransform, out shotData.cameraName);

                SceneData.Current.shots.Add(shotData);
            }
        }

        private void SetAnimationsData()
        {
            SceneData.Current.fps = AnimationEngine.Instance.fps;
            SceneData.Current.startFrame = AnimationEngine.Instance.StartFrame;
            SceneData.Current.endFrame = AnimationEngine.Instance.EndFrame;
            SceneData.Current.currentFrame = AnimationEngine.Instance.CurrentFrame;

            foreach (AnimationSet animSet in AnimationEngine.Instance.GetAllAnimations().Values)
            {
                AnimationData animData = new AnimationData();
                Utils.GetTransformRelativePathTo(animSet.transform, rootTransform, out animData.objectPath);
                foreach (Curve curve in animSet.curves.Values)
                {
                    CurveData curveData = new CurveData
                    {
                        property = curve.property
                    };
                    foreach (AnimationKey key in curve.keys)
                    {
                        KeyframeData keyData = new KeyframeData
                        {
                            frame = key.frame,
                            value = key.value,
                            interpolation = key.interpolation,
                            inTangent = key.inTangent,
                            outTangent = key.outTangent
                        };
                        curveData.keyframes.Add(keyData);
                    }
                    animData.curves.Add(curveData);
                }
                SceneData.Current.animations.Add(animData);
            }
        }

        private void SetConstraintsData()
        {
            foreach (Constraint constraint in ConstraintManager.GetAllConstraints())
            {
                ConstraintData constraintData = new ConstraintData
                {
                    type = constraint.constraintType
                };
                Utils.GetTransformRelativePathTo(constraint.gobject.transform, rootTransform, out constraintData.source);
                Utils.GetTransformRelativePathTo(constraint.target.transform, rootTransform, out constraintData.target);
                SceneData.Current.constraints.Add(constraintData);
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
                        string materialId = trans.name + "_" + material.name;
                        GetMaterialPath(currentProjectName, materialId, out string materialAbsolutePath, out string materialRelativePath);
                        MaterialInfo materialInfo = new MaterialInfo { relativePath = materialRelativePath, absolutePath = materialAbsolutePath, material = material };
                        if (!materials.ContainsKey(materialId))
                            materials.Add(materialId, materialInfo);
                        data.materialsData.Add(new MaterialData(materialInfo));
                    }
                    // Mesh
                    GetMeshPath(currentProjectName, meshFilter.sharedMesh.name, out string meshAbsolutePath, out string meshRelativePath);
                    meshes[meshRelativePath] = new MeshInfo { relativePath = meshRelativePath, absolutePath = meshAbsolutePath, mesh = meshFilter.sharedMesh };
                    data.meshPath = meshRelativePath;
                }
                data.isImported = false;
                if (trans.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinRenderer))
                {
                    Debug.Log("saved skin mesh " + skinRenderer.name);
                    foreach (Material material in skinRenderer.materials)
                    {
                        string materialId = trans.name + "_" + material.name;
                        GetMaterialPath(currentProjectName, materialId, out string materialAbsolutePath, out string materialRelativePath);
                        MaterialInfo materialInfo = new MaterialInfo { relativePath = materialRelativePath, absolutePath = materialAbsolutePath, material = material };
                        if (!materials.ContainsKey(materialId))
                            materials.Add(materialId, materialInfo);
                        data.materialsData.Add(new MaterialData(materialInfo));
                    }
                    GetMeshPath(currentProjectName, skinRenderer.sharedMesh.name, out string meshAbsolutePath, out string meshRelativePath);
                    MeshInfo meshI = new MeshInfo { absolutePath = meshAbsolutePath, relativePath = meshRelativePath, mesh = skinRenderer.sharedMesh };
                    skinMeshes[meshRelativePath] = new SkinMeshInfo { absolutePath = meshAbsolutePath, relativePath = meshRelativePath, skinMesh = skinRenderer, mesh = meshI };
                    data.meshPath = meshRelativePath;
                    data.isSkinMesh = true;
                }
            }
            else if (null != controller && controller.isImported)
            {
                data.meshPath = controller.importPath;
                data.isImported = true;
            }
        }

        private void SetCommonData(Transform trans, string parentPath, string path, ParametersController controller, ObjectData data)
        {
            data.name = trans.name;
            data.parentPath = parentPath == "" ? "" : parentPath.Substring(1);
            data.path = path.Substring(1);
            data.tag = trans.gameObject.tag;

            data.visible = true;
            MeshRenderer mesh = trans.GetComponent<MeshRenderer>();
            if (null != mesh && !mesh.enabled)
                data.visible = false;
            if (trans.gameObject.activeSelf == false)
                data.visible = false;

            // Transform
            data.position = trans.localPosition;
            data.rotation = trans.localRotation;
            data.scale = trans.localScale;

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
            data.sharpness = controller.Sharpness;
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

        public void Load(string projectName, string saveFolderOverride = null)
        {
            if (null != saveFolderOverride)
                saveFolder = saveFolderOverride;


            loadedMeshes.Clear();
            loadedCameras.Clear();
            loadedSkinMeshes.Clear();
            skinMeshMaterials.Clear();

            bool gizmoVisible = GlobalState.Settings.DisplayGizmos;
            bool errorLoading = false;

            try
            {
                GlobalState.Instance.messageBox.ShowMessage("Loading scene, please wait...");
                GlobalState.SetDisplayGizmos(true);

                currentProjectName = projectName;
                GlobalState.Settings.ProjectName = projectName;

                // Clear current scene
                SceneManager.ClearScene();

                // ensure VRtist scene
                VRtistScene scene = new VRtistScene();
                SceneManager.SetSceneImpl(scene);
                GlobalState.SetClientId(null);

                // Load data from file
                string path = GetScenePath(projectName);
                SceneData sceneData = new SceneData();
                SerializationManager.Load(path, sceneData);

                // Position user
                LoadPlayerData(sceneData.playerData);

                // Sky
                GlobalState.Instance.SkySettings = sceneData.skyData;

                // Objects            
                foreach (ObjectData data in sceneData.objects)
                {
                    LoadObject(data);
                }

                // Lights
                foreach (LightData data in sceneData.lights)
                {
                    LoadLight(data);
                }

                // Cameras
                foreach (CameraData data in sceneData.cameras)
                {
                    LoadCamera(data);
                }



                foreach (KeyValuePair<GameObject, SkinMeshData> pair in loadedSkinMeshes)
                {
                    SkinnedMeshRenderer renderer = pair.Key.AddComponent<SkinnedMeshRenderer>();
                    pair.Value.SetSkinnedMeshRenderer(renderer, pair.Key.transform.parent);
                    renderer.materials = skinMeshMaterials[pair.Key];
                }

                // Load animations & constraints
                AnimationEngine.Instance.fps = sceneData.fps;
                AnimationEngine.Instance.StartFrame = sceneData.startFrame;
                AnimationEngine.Instance.EndFrame = sceneData.endFrame;

                foreach (AnimationData data in sceneData.animations)
                {
                    LoadAnimation(data);
                }

                foreach (RigData data in sceneData.rigs)
                {
                    LoadRig(data);
                }

                foreach (ConstraintData data in sceneData.constraints)
                {
                    LoadConstraint(data);
                }

                // Load shot manager
                foreach (ShotData data in sceneData.shots)
                {
                    LoadShot(data);
                }
                ShotManager.Instance.FireChanged();

                AnimationEngine.Instance.CurrentFrame = sceneData.currentFrame;

                // Load camera snapshots
                StartCoroutine(LoadCameraSnapshots());
            }
            catch (Exception)
            {
                GlobalState.Instance.messageBox.ShowMessage("Error loading file", 5f);
                errorLoading = true;
            }
            finally
            {
                saveFolder = defaultSaveFolder;
                if (!gizmoVisible)
                    GlobalState.SetDisplayGizmos(false);
                if (!errorLoading)
                {
                    GlobalState.Instance.messageBox.SetVisible(false);
                    SceneManager.sceneLoadedEvent.Invoke();
                }
            }
        }

        private void LoadPlayerData(PlayerData data)
        {
            cameraRig.localPosition = data.position;
            cameraRig.localRotation = data.rotation;
            GlobalState.WorldScale = data.scale;
            cameraRig.localScale = Vector3.one * (1f / data.scale);
            Camera.main.nearClipPlane = 0.1f * cameraRig.localScale.x;
            Camera.main.farClipPlane = 1000f * cameraRig.localScale.x;
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
            gobject.name = data.name;

            if (data.lockPosition || data.lockRotation || data.lockScale)
            {
                ParametersController controller = gobject.GetComponent<ParametersController>();
                if (null == controller)
                    controller = gobject.AddComponent<ParametersController>();
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

        private void LoadObject(ObjectData data)
        {
            GameObject gobject;
            string absoluteMeshPath;
            Transform importedParent = null;

            // Check for import
            if (data.isImported)
            {
                try
                {
                    importedParent = new GameObject("__VRtist_tmp_load__").transform;
                    absoluteMeshPath = data.meshPath;
                    // Don't use async import since we may reference the game object for animations or constraints
                    // and the object must be loaded before we do so
                    GlobalState.GeometryImporter.ImportObject(absoluteMeshPath, importedParent, true);
                    if (importedParent.childCount == 0)
                        return;
                    gobject = importedParent.GetChild(0).gameObject;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to load external object: " + e.Message);
                    return;
                }
            }
            else
            {
                absoluteMeshPath = GetSaveFolderPath(currentProjectName) + data.meshPath;
                gobject = new GameObject(data.name);
            }

            LoadCommonData(gobject, data);
            gobject.name = data.name;

            // Mesh
            if (null != data.meshPath && data.meshPath.Length > 0)
            {
                if (!data.isImported && !data.isSkinMesh)
                {
                    if (!loadedMeshes.TryGetValue(absoluteMeshPath, out Mesh mesh))
                    {
                        MeshData meshData = new MeshData();
                        SerializationManager.Load(absoluteMeshPath, meshData);
                        mesh = meshData.CreateMesh();
                        loadedMeshes.Add(absoluteMeshPath, mesh);
                    }
                    gobject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    gobject.AddComponent<MeshRenderer>().materials = LoadMaterials(data);
                    gobject.AddComponent<MeshCollider>();
                }
                if (data.isSkinMesh)
                {
                    SkinMeshData skinData = new SkinMeshData();
                    SerializationManager.Load(absoluteMeshPath, skinData);
                    loadedSkinMeshes.Add(gobject, skinData);
                    skinMeshMaterials.Add(gobject, LoadMaterials(data));
                }

                if (!data.visible)
                {
                    foreach (Component component in gobject.GetComponents<Component>())
                    {
                        Type componentType = component.GetType();
                        var prop = componentType.GetProperty("enabled");
                        if (null != prop)
                        {
                            prop.SetValue(component, data.visible);
                        }
                    }
                }
            }

            SceneManager.AddObject(gobject);

            if (data.parentPath.Length > 0)
                SceneManager.SetObjectParent(gobject, rootTransform.Find(data.parentPath).gameObject);

            if (data.isImported)
            {
                if (!gobject.TryGetComponent<ParametersController>(out ParametersController controller))
                {
                    controller = gobject.AddComponent<ParametersController>();
                }
                controller.isImported = true;
                controller.importPath = data.meshPath;

                if (null != importedParent)
                    Destroy(importedParent.gameObject);
            }
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
                GameObject newPrefab = SceneManager.InstantiateUnityPrefab(lightPrefab);
                GameObject newObject = SceneManager.AddObject(newPrefab);

                if (data.parentPath.Length > 0)
                    SceneManager.SetObjectParent(newObject, rootTransform.Find(data.parentPath).gameObject);

                LoadCommonData(newObject, data);

                LightController controller = newObject.GetComponent<LightController>();
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
                controller.Sharpness = data.sharpness;
            }
        }

        private void LoadCamera(CameraData data)
        {
            GameObject cameraPrefab = ResourceManager.GetPrefab(PrefabID.Camera);

            GameObject newPrefab = SceneManager.InstantiateUnityPrefab(cameraPrefab);
            GameObject newObject = SceneManager.AddObject(newPrefab);

            if (data.parentPath.Length > 0)
                SceneManager.SetObjectParent(newObject, rootTransform.Find(data.parentPath).gameObject);

            LoadCommonData(newObject, data);

            CameraController controller = newObject.GetComponent<CameraController>();
            controller.enableDOF = data.enableDOF;
            if (controller.enableDOF)
                controller.CreateColimator();
            controller.focal = data.focal;
            controller.Focus = data.focus;
            controller.aperture = data.aperture;
            controller.near = data.near;
            controller.far = data.far;
            controller.filmHeight = data.filmHeight;
            controller.filmWidth = data.filmWidth;
            controller.gateFit = (Camera.GateFitMode)data.gateFit;

            loadedCameras.Add(controller);
        }

        private IEnumerator LoadCameraSnapshots()
        {
            foreach (CameraController controller in loadedCameras)
            {
                yield return null;  // wait next frame
                CameraManager.Instance.ActiveCamera = controller.gameObject;
                yield return new WaitForEndOfFrame();
                controller.SetVirtualCamera(null);
            }
            CameraManager.Instance.ActiveCamera = null;
        }

        private void LoadAnimation(AnimationData data)
        {
            Transform animTransform = rootTransform.Find(data.objectPath);
            if (null == animTransform)
            {
                Debug.LogWarning($"Object name not found for animation: {data.objectPath}");
                return;
            }
            GameObject gobject = animTransform.gameObject;

            // Create animation
            AnimationSet animSet = new AnimationSet(gobject);
            foreach (CurveData curve in data.curves)
            {
                List<AnimationKey> keys = new List<AnimationKey>();
                foreach (KeyframeData keyData in curve.keyframes)
                {
                    keys.Add(new AnimationKey(keyData.frame, keyData.value, keyData.interpolation, keyData.inTangent, keyData.outTangent));
                }

                animSet.SetCurve(curve.property, keys);
            }
            SceneManager.SetObjectAnimations(gobject, animSet);
        }

        private void LoadConstraint(ConstraintData data)
        {
            Transform sourceTransform = rootTransform.Find(data.source);
            if (null == sourceTransform)
            {
                Debug.LogWarning($"Object name not found for animation: {data.source}");
                return;
            }
            Transform targetTransform = rootTransform.Find(data.target);
            if (null == targetTransform)
            {
                Debug.LogWarning($"Object name not found for animation: {data.target}");
                return;
            }

            // Create constraint
            ConstraintManager.AddConstraint(sourceTransform.gameObject, targetTransform.gameObject, data.type);
        }

        private void LoadShot(ShotData data)
        {
            GameObject cam = null;
            if (data.cameraName.Length > 0)
            {
                Transform cameraTransform = rootTransform.Find(data.cameraName);
                if (null == cameraTransform)
                {
                    Debug.LogWarning($"Object name not found for camera: {data.cameraName}");
                    return;
                }
                cam = cameraTransform.gameObject;
            }

            ShotManager.Instance.AddShot(new Shot
            {
                name = data.name,
                start = data.start,
                end = data.end,
                enabled = data.enabled,
                camera = cam
            });
        }

        private void LoadRig(RigData data)
        {
            GameObject obj = rootTransform.Find(data.ObjectName).gameObject;
            if (null != obj)
            {
                GameObject rendererObj = obj.transform.Find(data.meshPath).gameObject;
                if (null != rendererObj)
                {
                    if (rendererObj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer renderer))
                    {
                        RigController controller = obj.AddComponent<RigController>();
                        controller.SkinMesh = renderer;
                        controller.RootObject = renderer.rootBone;

                        obj.tag = "PhysicObject";
                        BoxCollider objectCollider = obj.AddComponent<BoxCollider>();
                        objectCollider.center = renderer.bounds.center;
                        objectCollider.size = renderer.bounds.size;
                        renderer.updateWhenOffscreen = true;
                        controller.Collider = objectCollider;

                        GlobalState.GeometryImporter.GenerateImportSkeleton(controller.RootObject, controller);

                    }
                }
            }
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
