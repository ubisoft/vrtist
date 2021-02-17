
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class SceneManager
    {
        public static UnityEvent clearSceneEvent = new UnityEvent();
        public static BoolChangedEvent sceneDirtyEvent = new BoolChangedEvent();
        public static UnityEvent sceneSavedEvent = new UnityEvent();

        private static SceneManager instance;
        public static SceneManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new SceneManager();
                    VRtistScene scene = new VRtistScene();
                    SetSceneImpl(scene);
                }
                return instance;
            }
        }

        IScene scene;
        static GameObject trash = null;
        public static GameObject Trash
        {
            get
            {
                if (null == trash)
                {
                    trash = new GameObject("__Trash__");
                    trash.SetActive(false);
                }
                return trash;
            }
        }

        static Transform rightHanded;
        public static Transform RightHanded
        {
            get
            {
                if (null == rightHanded)
                {
                    rightHanded = Utils.FindRootGameObject("World").transform.Find("RightHanded");
                }
                return rightHanded;
            }
        }

        static Transform boundingBox;
        public static Transform BoundingBox
        {
            get
            {
                if (null == boundingBox)
                {
                    boundingBox = RightHanded.Find("__VRtist_BoundingBox__");
                }
                return boundingBox;
            }
        }


        public static bool firstSave = true;

        public static void SetSceneImpl(IScene scene)
        {
            Instance.scene = scene;
        }

        public static void ClearScene()
        {
            firstSave = true;
            CommandManager.SetSceneDirty(false);
            clearSceneEvent.Invoke();


            Instance.scene.ClearScene();
        }

        public static GameObject InstantiateObject(GameObject prefab)
        {
            return Instance.scene.InstantiateObject(prefab);
        }
        public static GameObject InstantiateUnityPrefab(GameObject prefab)
        {
            return Instance.scene.InstantiateUnityPrefab(prefab);
        }
        public static GameObject AddObject(GameObject gobject)
        {
            return Instance.scene.AddObject(gobject);
        }
        public static void RemoveObject(GameObject gobject)
        {
            Instance.scene.RemoveObject(gobject);
        }
        public static void RestoreObject(GameObject gobject, Transform parent)
        {
            Instance.scene.RestoreObject(gobject, parent);
        }
        public static GameObject DuplicateObject(GameObject gobject)
        {
            return Instance.scene.DuplicateObject(gobject);
        }
        public static void RenameObject(GameObject gobject, string newName)
        {
            Instance.scene.RenameObject(gobject, newName);
        }
        public static void SetObjectMatrix(GameObject gobject, Matrix4x4 matrix)
        {
            Instance.scene.SetObjectMatrix(gobject, matrix);
        }
        public static void SetObjectTransform(GameObject gobject, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Instance.scene.SetObjectTransform(gobject, position, rotation, scale);
        }
        public static GameObject GetParent(GameObject gobject)
        {
            return Instance.scene.GetParent(gobject);
        }
        public static void SetParent(GameObject gobject, GameObject parent)
        {
            Instance.scene.SetParent(gobject, parent);
        }
        public static void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue)
        {
            Instance.scene.SetObjectMaterialValue(gobject, materialValue);
        }

        public static void ListImportableObjects()
        {
            Instance.scene.ListImportableObjects();
        }

        // helper functions
        public static bool IsInTrash(GameObject obj)
        {
            if (obj.transform.parent.parent.gameObject == Trash)
                return true;
            return false;
        }

    }
}
