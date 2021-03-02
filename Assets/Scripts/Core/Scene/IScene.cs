using UnityEngine;

namespace VRtist
{
    public interface IScene
    {
        string GetSceneType();
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
        GameObject GetObjectParent(GameObject gobject);
        void SetObjectParent(GameObject gobject, GameObject parent);
        void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue);
        void AddMaterialParameters(string materialName, MaterialID materialID, Color color);
        void SendCameraInfo(Transform camera);
        void SendLightInfo(Transform light);
        void ClearObjectAnimations(GameObject gobject);
        void SetObjectAnimations(GameObject gobject, AnimationSet animationSet);
        void AddKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key);
        void RemoveKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key);
        void MoveKeyframe(GameObject gobject, AnimatableProperty property, int oldTime, int newTime);
        void SetFrameRange(int start, int end);
        void AddObjectConstraint(GameObject gobject, ConstraintType constraintType, GameObject target);
        void RemoveObjectConstraint(GameObject gobject, ConstraintType constraintType);
        void SetSky(SkySettings sky);
        void ApplyShotManagerAction(ShotManagerActionInfo info);
        void ListImportableObjects();
        void SendUserInfo(Vector3 cameraPosition, Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraRight);
        void RemoteSave();
    }
}
