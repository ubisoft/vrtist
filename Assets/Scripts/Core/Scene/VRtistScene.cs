using UnityEngine;

namespace VRtist
{
    public class VRtistScene : IScene
    {
        public void ClearScene()
        {
            Utils.DeleteTransformChildren(SceneManager.RightHanded);
        }

        public GameObject InstantiateObject(GameObject prefab)
        {
            string instanceName = Utils.CreateUniqueName(prefab.name);
            GameObject instance = GameObject.Instantiate(prefab);
            instance.name = instanceName;
            return instance;
        }

        public GameObject InstantiateUnityPrefab(GameObject unityPrefab)
        {
            GameObject instance;
            GameObjectBuilder builder = unityPrefab.GetComponent<GameObjectBuilder>();
            if (builder)
            {
                string instanceName = Utils.CreateUniqueName(unityPrefab.name);
                instance = builder.CreateInstance(unityPrefab, null, true);
                instance.name = instanceName;
            }
            else
            {
                instance = InstantiateObject(unityPrefab);
            }

            return instance;
        }

        public GameObject AddObject(GameObject gobject)
        {
            gobject.transform.SetParent(SceneManager.RightHanded, false);
            return gobject;
        }

        public void RemoveObject(GameObject gobject)
        {
            gobject.transform.SetParent(SceneManager.Trash.transform, true);
        }

        public void RestoreObject(GameObject gobject, Transform parent)
        {
            gobject.transform.SetParent(parent, true);
        }
        public GameObject DuplicateObject(GameObject gobject)
        {
            GameObject copy = InstantiateUnityPrefab(gobject);
            copy.transform.SetParent(SceneManager.RightHanded, false);
            return copy;
        }
        public void RenameObject(GameObject gobject, string newName)
        {
            gobject.name = newName;
        }
        public void SetObjectMatrix(GameObject gobject, Matrix4x4 matrix)
        {
            Maths.DecomposeMatrix(matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale);
            SetObjectTransform(gobject, position, rotation, scale);
        }

        public void SetObjectTransform(GameObject gobject, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            gobject.transform.localPosition = position;
            gobject.transform.localRotation = rotation;
            gobject.transform.localScale = scale;
            GlobalState.FireObjectMoving(gobject);
        }
        public GameObject GetParent(GameObject gobject)
        {
            Transform parentTransform = gobject.transform.parent;
            if (null == parentTransform)
                return null;
            return parentTransform.gameObject;
        }
        public void SetParent(GameObject gobject, GameObject parent)
        {
            gobject.transform.SetParent(parent.transform, false);
        }

        public void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue)
        {
            Utils.SetMaterialValue(gobject, materialValue);
        }

        public void ListImportableObjects()
        {
        }
    }
}