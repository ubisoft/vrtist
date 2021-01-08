using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VRtist
{
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

        private Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();  // meshes to save in separate files

        private void Awake()
        {
            if (null == instance)
            {
                instance = this;
            }

            saveFolder = Application.persistentDataPath + "/saves/";
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

                // All the children should be an empty parent container, so get its child
                Transform child = emptyParent.GetChild(0);
                string chilPath = path + "/" + child.name;

                // Depending on its type (which controller we can find on it) create data objects to be serialized
                LightController lightController = child.GetComponent<LightController>();
                if (null != lightController)
                {
                    LightData lightData = new LightData();
                    SetCommonData(child, chilPath, lightController, lightData);
                    //...light params
                    SceneData.Current.AddLight(lightData);
                    continue;
                }

                CameraController cameraController = child.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    // Build camera data
                    //...
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
                SetCommonData(child, chilPath, controller, data);
                SceneData.Current.AddObject(data);

                // Serialize children
                StartCoroutine(SerializeChildren(child, chilPath));
            }

            // Save
            if (save)
            {
                SerializationManager.Save(GetScenePath(currentProjectName), SceneData.Current);
                foreach (var mesh in meshes.Values)
                {
                    yield return null;

                    string meshPath = GetMeshPath(currentProjectName, mesh.name);
                    SerializationManager.Save(meshPath, new MeshData(mesh));
                }
            }
        }

        private void SetCommonData(Transform trans, string path, ParametersController controller, ObjectData data)
        {
            data.path = path;
            data.tag = trans.gameObject.tag;

            data.position = trans.localPosition;
            data.rotation = trans.localRotation;
            data.scale = trans.localScale;

            MeshFilter meshFilter = trans.GetComponent<MeshFilter>();
            if (null != meshFilter)
            {
                string meshPath = GetMeshPath(currentProjectName, meshFilter.mesh.name);
                meshes[meshPath] = meshFilter.mesh;
                data.meshPath = meshPath;
            }

            // TODO same for materials

            if (null != controller)
            {
                data.lockPosition = controller.lockPosition;
                data.lockRotation = controller.lockRotation;
                data.lockScale = controller.lockScale;
            }
        }

        public void Load(string projectName)
        {
            currentProjectName = projectName;

            // Clear current scene

            // Load
            string path = GetScenePath(projectName);
            SceneData saveData = (SceneData) SerializationManager.Load(path);
            Transform root = GlobalState.Instance.world.Find("RightHanded");

            Transform parent;
            foreach (ObjectData data in saveData.GetObjects())
            {
                string[] splitted = data.path.Split('/');
                string name = splitted[splitted.Length - 1];
                string parentPath = data.path.Substring(0, data.path.Length - name.Length - 1);  // -1: remove "/"

                GameObject gobject = new GameObject(name);

                if (null != data.tag && data.tag.Length > 0)
                {
                    gobject.tag = data.tag;
                }

                // Set parent and transform
                if (parentPath.Length > 0)
                {
                    parent = root.Find(parentPath);
                }
                else
                {
                    parent = root;
                }
                gobject.transform.localPosition = data.position;
                gobject.transform.localRotation = data.rotation;
                gobject.transform.localScale = data.scale;

                // Mesh
                if (null != data.meshPath && data.meshPath.Length > 0)
                {
                    MeshData meshData = (MeshData) SerializationManager.Load(data.meshPath);
                    gobject.AddComponent<MeshFilter>().mesh = meshData.CreateMesh();
                    gobject.AddComponent<MeshRenderer>();
                    MeshCollider collider = gobject.AddComponent<MeshCollider>();
                    collider.isTrigger = true;
                    collider.convex = true;
                }

                // Common properties
                if (data.lockPosition || data.lockRotation || data.lockScale)
                {
                    ParametersController controller = gobject.AddComponent<ParametersController>();
                    controller.lockPosition = data.lockPosition;
                    controller.lockRotation = data.lockRotation;
                    controller.lockScale = data.lockScale;
                }

                // Instantiate using the SyncData API
                // TODO node hierarchy
                GameObject newObject = SyncData.InstantiatePrefab(SyncData.CreateInstance(gobject, SyncData.prefab));

                // Then delete the original loaded object
                Destroy(gobject);
            }
        }
    }
}
