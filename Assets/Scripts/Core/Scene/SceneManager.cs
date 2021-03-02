
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

namespace VRtist
{
    public class SceneManager
    {
        IScene scene;

        public static UnityEvent clearSceneEvent = new UnityEvent();
        public static BoolChangedEvent sceneDirtyEvent = new BoolChangedEvent();
        public static UnityEvent sceneSavedEvent = new UnityEvent();

        public static bool firstSave = true;

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

        public static void SetSceneImpl(IScene scene)
        {
            Instance.scene = scene;
        }

        public static string GetSceneType()
        {
            return Instance.scene.GetSceneType();
        }

        public static void ClearScene()
        {
            firstSave = true;
            CommandManager.SetSceneDirty(false);

            AnimationEngine.Instance.Clear();
            Selection.Clear();
            ConstraintManager.Clear();
            ShotManager.Instance.Clear();

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

        public static GameObject GetObjectParent(GameObject gobject)
        {
            return Instance.scene.GetObjectParent(gobject);
        }

        public static void SetObjectParent(GameObject gobject, GameObject parent)
        {
            Instance.scene.SetObjectParent(gobject, parent);
        }

        public static void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue)
        {
            Instance.scene.SetObjectMaterialValue(gobject, materialValue);
        }

        public static void AddMaterialParameters(string materialName, MaterialID materialID, Color color)
        {
            Instance.scene.AddMaterialParameters(materialName, materialID, color);
        }

        public static void SendCameraInfo(Transform camera)
        {
            Instance.scene.SendCameraInfo(camera);
        }

        public static void SendLightInfo(Transform light)
        {
            Instance.scene.SendLightInfo(light);
        }

        // Animation
        public static void ClearObjectAnimations(GameObject gobject)
        {
            GlobalState.Animation.ClearAnimations(gobject);
            Instance.scene.ClearObjectAnimations(gobject);
        }

        public static void SetObjectAnimations(GameObject gobject, AnimationSet animationSet)
        {
            GlobalState.Animation.SetObjectAnimations(gobject, animationSet);
            Instance.scene.SetObjectAnimations(gobject, animationSet);
        }

        public static void AddObjectKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            GlobalState.Animation.AddFilteredKeyframe(gobject, property, key);
            Instance.scene.AddKeyframe(gobject, property, key);
        }

        public static void RemoveKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            GlobalState.Animation.RemoveKeyframe(gobject, property, key.frame);
            Instance.scene.RemoveKeyframe(gobject, property, key);
        }

        public static void MoveKeyframe(GameObject gobject, AnimatableProperty property, int oldTime, int newTime)
        {
            GlobalState.Animation.MoveKeyframe(gobject, property, oldTime, newTime);
            Instance.scene.MoveKeyframe(gobject, property, oldTime, newTime);
        }

        public static void SetFrameRange(int start, int end)
        {
            GlobalState.Animation.StartFrame = start;
            GlobalState.Animation.EndFrame = end;
            Instance.scene.SetFrameRange(start, end);
        }

        // Constraints
        public static void AddObjectConstraint(GameObject gobject, ConstraintType constraintType, GameObject target)
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintManager.AddParentConstraint(gobject, target);
                    break;
                case ConstraintType.LookAt:
                    ConstraintManager.AddLookAtConstraint(gobject, target);
                    break;
            }
            Instance.scene.AddObjectConstraint(gobject, constraintType, target);
        }

        public static void RemoveObjectConstraint(GameObject gobject, ConstraintType constraintType)
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintManager.RemoveConstraint<ParentConstraint>(gobject);
                    break;
                case ConstraintType.LookAt:
                    ConstraintManager.RemoveConstraint<LookAtConstraint>(gobject);
                    break;
            }
            Instance.scene.RemoveObjectConstraint(gobject, constraintType);
        }

        // Sky
        public static void SetSky(SkySettings sky)
        {
            GlobalState.Instance.SkySettings = sky;
            Instance.scene.SetSky(sky);
        }

        // Shot Manager
        public static void ApplyShotManagegrAction(ShotManagerActionInfo info)
        {
            switch (info.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        GameObject cam = info.camera;
                        Shot shot = new Shot()
                        {
                            name = info.shotName,
                            camera = cam,
                            color = info.shotColor,
                            start = info.shotStart,
                            end = info.shotEnd,
                            enabled = info.shotEnabled == 1
                        };
                        ShotManager.Instance.InsertShot(info.shotIndex + 1, shot);
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    {
                        ShotManager.Instance.RemoveShot(info.shotIndex);
                    }
                    break;
                case ShotManagerAction.DuplicateShot:
                    {
                        ShotManager.Instance.DuplicateShot(info.shotIndex);
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        ShotManager.Instance.SetCurrentShotIndex(info.shotIndex);
                        ShotManager.Instance.MoveShot(info.shotIndex, info.moveOffset);
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                        Shot shot = ShotManager.Instance.shots[info.shotIndex];
                        if (info.shotName.Length > 0)
                            shot.name = info.shotName;
                        if (null != info.camera)
                            shot.camera = info.camera;
                        if (info.shotColor.r != -1)
                            shot.color = info.shotColor;
                        if (info.shotStart != -1)
                            shot.start = info.shotStart;
                        if (info.shotEnd != -1)
                            shot.end = info.shotEnd;
                        if (info.shotEnabled != -1)
                            shot.enabled = info.shotEnabled == 1;
                    }
                    break;
            }
            ShotManager.Instance.FireChanged();
            Instance.scene.ApplyShotManagerAction(info);
        }

        public static void ListImportableObjects()
        {
            Instance.scene.ListImportableObjects();
        }

        // User
        public static void SendUserInfo(Vector3 cameraPosition, Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraRight)
        {
            Vector3 target = cameraPosition + cameraForward * 2f;

            GlobalState.networkUser.position = RightHanded.InverseTransformPoint(cameraPosition);
            GlobalState.networkUser.target = RightHanded.InverseTransformPoint(target);

            Instance.scene.SendUserInfo(cameraPosition, cameraForward, cameraUp, cameraRight);
        }

        public static void RemoteSave()
        {
            CommandManager.SetSceneDirty(false);
            sceneSavedEvent.Invoke();
            Instance.scene.RemoteSave();
        }

        // helper functions
        public static bool IsInTrash(GameObject obj)
        {
            Transform parent = obj.transform;
            while (parent != Trash && parent != null)
            {
                parent = parent.parent;
            }
            return parent == Trash;
        }
    }
}
