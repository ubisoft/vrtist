using UnityEngine;

namespace VRtist
{
    public interface IScene
    {
        void ClearScene();
        GameObject InstantiateObject(GameObject prefab);
        GameObject InstantiateUnityPrefab(GameObject unityPrefab);
        GameObject AddObject(GameObject gobject);
        void RemoveObject(GameObject gobject);
        void RestoreObject(GameObject gobject, Transform parent);
        GameObject DuplicateObject(GameObject gobject);
        void RenameObject(GameObject gobject, string newName);
        void SetObjectMatrix(GameObject gobject, Matrix4x4 matrix);
        void SetObjectTransform(GameObject gobject, Vector3 position, Quaternion rotation, Vector3 scale);
        GameObject GetParent(GameObject gobject);
        void SetParent(GameObject gobject, GameObject parent);
        void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue);
        void ListImportableObjects();
    }
}