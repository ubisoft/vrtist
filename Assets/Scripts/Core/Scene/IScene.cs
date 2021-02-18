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
        GameObject GetObjectParent(GameObject gobject);
        void SetObjectParent(GameObject gobject, GameObject parent);
        void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue);
        void ClearObjectAnimations(GameObject gobject);
        void SetObjectAnimations(GameObject gobject, AnimationSet animationSet);
        void AddKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key);
        void RemoveKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key);
        void MoveKeyframe(GameObject gobject, AnimatableProperty property, int oldTime, int newTime);
        void AddObjectConstraint(GameObject gobject, ConstraintType constraintType, GameObject target);
        void RemoveObjectConstraint(GameObject gobject, ConstraintType constraintType);
        void SetSky(SkySettings sky);
        void ApplyShotManagerAction(ShotManagerActionInfo info);
        void ListImportableObjects();
    }
}