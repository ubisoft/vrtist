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

using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace VRtist.Mixer
{
    public class MixerScene : IScene
    {
        private string requestedBlenderImportName;
        private TaskCompletionSource<GameObject> blenderImportTask = null;
        private AssetBank assetBank;

        public string GetSceneType()
        {
            return "Mixer";
        }

        public void ClearScene()
        {
            Utils.DeleteTransformChildren(SceneManager.RightHanded);
            Utils.DeleteTransformChildren(SyncData.prefab);
            SyncData.nodes.Clear();
        }

        public GameObject InstantiateObject(GameObject prefab)
        {
            // if it does not exist in 'Prefab', create it
            if (!SyncData.nodes.TryGetValue(prefab.name, out Node prefabNode))
            {
                return SyncData.CreateFullHierarchyPrefab(prefab);
            }

            // it already exists in Prefab, duplicate it
            GameObject newPrefab = SyncData.DuplicatePrefab(prefab);
            Node newPrefabNode = SyncData.nodes[newPrefab.name];
            foreach (Node childNode in prefabNode.children)
            {
                GameObject newChildPrefab = InstantiateObject(childNode.prefab);
                Node newChildNode = SyncData.nodes[newChildPrefab.name];
                newPrefabNode.AddChild(newChildNode);
            }
            return newPrefab;
        }

        public GameObject InstantiateUnityPrefab(GameObject unityPrefab)
        {
            GameObject prefab = SyncData.CreateInstance(unityPrefab, SyncData.prefab, isPrefab: true);
            Node node = SyncData.CreateNode(prefab.name);
            node.prefab = prefab;
            return prefab;
        }

        public GameObject AddObject(GameObject gobject)
        {
            return SyncData.InstantiateFullHierarchyPrefab(gobject);
        }

        private void _RemoveObject(GameObject gobject)
        {
            SendToTrashInfo trashInfo = new SendToTrashInfo
            {
                transform = gobject.transform
            };
            MixerClient.Instance.SendEvent<SendToTrashInfo>(MessageType.SendToTrash, trashInfo);

            Node node = SyncData.nodes[gobject.name];
            node.RemoveInstance(gobject);

            LightController lightController = gobject.GetComponent<LightController>();
            if (null != lightController)
                return;
            CameraController cameraController = gobject.GetComponent<CameraController>();
            if (null != cameraController)
                return;
            foreach (Transform child in gobject.transform)
            {
                _RemoveObject(child.GetChild(0).gameObject);
            }
        }

        public void RemoveObject(GameObject gobject)
        {
            gobject.transform.parent.SetParent(SceneManager.Trash.transform, false);
            _RemoveObject(gobject);
        }

        public void _RestoreObject(GameObject gobject, Transform parent)
        {
            Node node = SyncData.nodes[gobject.name];
            node.AddInstance(gobject);

            RestoreFromTrashInfo trashInfo = new RestoreFromTrashInfo
            {
                transform = gobject.transform,
                parent = parent
            };
            MixerClient.Instance.SendEvent<RestoreFromTrashInfo>(MessageType.RestoreFromTrash, trashInfo);

            LightController lightController = gobject.GetComponent<LightController>();
            if (null != lightController)
                return;
            CameraController cameraController = gobject.GetComponent<CameraController>();
            if (null != cameraController)
                return;

            foreach (Transform child in gobject.transform)
            {
                _RestoreObject(child.GetChild(0).gameObject, gobject.transform);
            }
        }

        public void RestoreObject(GameObject gobject, Transform parent)
        {
            gobject.transform.parent.SetParent(parent, false);
            _RestoreObject(gobject, parent);
        }

        public GameObject DuplicateObject(GameObject gobject)
        {
            GameObject res = SyncData.Duplicate(gobject);
            DuplicateInfos duplicateInfos = new DuplicateInfos
            {
                srcObject = gobject,
                dstObject = res
            };
            MixerClient.Instance.SendEvent<DuplicateInfos>(MessageType.Duplicate, duplicateInfos);

            return res;
        }

        public void RenameObject(GameObject gobject, string newName)
        {
            MixerClient.Instance.SendEvent<RenameInfo>(MessageType.Rename, new RenameInfo { srcTransform = gobject.transform, newName = gobject.name });
            SyncData.Rename(newName, gobject.name);
        }

        public void SetObjectMatrix(GameObject gobject, Matrix4x4 matrix)
        {
            string objectName = gobject.name;
            SyncData.SetTransform(gobject.name, matrix);
            foreach (var instance in SyncData.nodes[objectName].instances)
                GlobalState.FireObjectMoving(instance.Item1);
            MixerClient.Instance.SendEvent<Transform>(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
        }

        public void SetObjectTransform(GameObject gobject, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            string objectName = gobject.name;
            SyncData.SetTransform(objectName, position, rotation, scale);
            foreach (var instance in SyncData.nodes[objectName].instances)
                GlobalState.FireObjectMoving(instance.Item1);
            MixerClient.Instance.SendEvent<Transform>(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
        }

        public GameObject GetObjectParent(GameObject gobject)
        {
            Transform parentTransform = gobject.transform.parent.parent;
            if (null == parentTransform)
                return null;
            return parentTransform.gameObject;
        }

        public void SetObjectParent(GameObject gobject, GameObject parent)
        {
            Node parentNode = SyncData.nodes[parent.name];
            Node childNode = SyncData.nodes[gobject.name];
            parentNode.AddChild(childNode);
            gobject.transform.parent.SetParent(parent.transform, false);

            MixerClient.Instance.SendTransform(gobject.transform);
        }

        private void InformModification(GameObject gobject)
        {
            MeshRenderer renderer = gobject.GetComponentInChildren<MeshRenderer>();
            renderer.material.name = $"Mat_{gobject.name}";
            MixerClient.Instance.SendEvent<Material>(MessageType.Material, renderer.material);
            MixerClient.Instance.SendEvent<AssignMaterialInfo>(MessageType.AssignMaterial, new AssignMaterialInfo { objectName = gobject.name, materialName = renderer.material.name });
        }

        public void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue)
        {
            Node node = SyncData.nodes[gobject.name];
            Utils.SetMaterialValue(node.prefab, materialValue);
            Utils.SetMaterialValue(gobject, materialValue);

            InformModification(gobject);
        }

        public void AddMaterialParameters(string materialName, MaterialID materialID, Color color)
        {
            MaterialParameters parameters = new MaterialParameters
            {
                materialType = materialID,
                baseColor = color
            };
            MixerUtils.materialsParameters[materialName] = parameters;
        }

        public void SendCameraInfo(Transform camera)
        {
            MixerClient.Instance.SendEvent(MessageType.Camera, new CameraInfo { transform = camera });
        }

        public void SendLightInfo(Transform light)
        {
            MixerClient.Instance.SendEvent(MessageType.Light, new LightInfo { transform = light });
        }

        public void ListImportableObjects()
        {
            assetBank = ToolsManager.GetTool("AssetBank").GetComponent<AssetBank>();
            // Add Blender asset bank assets
            GlobalState.blenderBankImportObjectEvent.AddListener(OnBlenderBankObjectImported);
            GlobalState.blenderBankListEvent.AddListener(OnBlenderBank);
            BlenderBankInfo info = new BlenderBankInfo { action = BlenderBankAction.ListRequest };
            MixerClient.Instance.SendBlenderBank(info);
        }

        // Used for blender bank to know if a blender asset has been imported
        private void OnBlenderBankObjectImported(string objectName, string niceName)
        {
            if (niceName == requestedBlenderImportName && null != blenderImportTask && !blenderImportTask.Task.IsCompleted)
            {
                GameObject instance = SyncData.nodes[objectName].instances[0].Item1;
                blenderImportTask.TrySetResult(instance);
                Selection.AddToSelection(instance);
            }
        }

        public void OnBlenderBank(List<string> names, List<string> tags, List<string> thumbnails)
        {
            // Load only once the whole asset bank data base from Blender
            GlobalState.blenderBankListEvent.RemoveListener(OnBlenderBank);
            for (int i = 0; i < names.Count; i++)
            {
                AddBlenderAsset(names[i], tags[i], thumbnails[i]);
            }
        }

        private void AddBlenderAsset(string name, string tags, string thumbnailPath)
        {
            GameObject thumbnail = UIGrabber.CreateLazyImageThumbnail(thumbnailPath, assetBank.OnUIObjectEnter, assetBank.OnUIObjectExit);
            assetBank.AddAsset(name, thumbnail, null, tags, importFunction: ImportBlenderAsset, skipInstantiation: true);
        }

        private Task<GameObject> ImportBlenderAsset(AssetBankItem item)
        {
            requestedBlenderImportName = item.assetName;
            blenderImportTask = new TaskCompletionSource<GameObject>();
            BlenderBankInfo info = new BlenderBankInfo { action = BlenderBankAction.ImportRequest, name = item.assetName };
            MixerClient.Instance.SendBlenderBank(info);
            return blenderImportTask.Task;
        }

        public void ClearObjectAnimations(GameObject gobject)
        {
            MixerClient.Instance.SendClearAnimations(new ClearAnimationInfo { gObject = gobject });
        }

        public void SetObjectAnimations(GameObject gobject, AnimationSet animationSet)
        {
            foreach (Curve curve in animationSet.curves.Values)
            {
                MixerClient.Instance.SendAnimationCurve(new CurveInfo { objectName = gobject.name, curve = curve });
            }
        }

        public void AddKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            MixerClient.Instance.SendAddKeyframe(new SetKeyInfo { objectName = gobject.name, property = property, key = key });
        }

        public void RemoveKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            MixerClient.Instance.SendRemoveKeyframe(new SetKeyInfo { objectName = gobject.name, property = property, key = key });
        }

        public void MoveKeyframe(GameObject gobject, AnimatableProperty property, int oldTime, int newTime)
        {
            MixerClient.Instance.SendMoveKeyframe(new MoveKeyInfo { objectName = gobject.name, property = property, frame = oldTime, newFrame = newTime });
        }

        public void SetFrameRange(int start, int end)
        {
            FrameStartEnd info = new FrameStartEnd() { start = start, end = end };
            MixerClient.Instance.SendEvent(MessageType.FrameStartEnd, info);
        }

        public void InsertObjectConstraint(int _, Constraint constraint)
        {
            AddObjectConstraint(constraint.gobject, constraint.constraintType, constraint.target.gameObject);
        }

        public void AddObjectConstraint(GameObject gobject, ConstraintType constraintType, GameObject target)
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    MixerClient.Instance.SendAddParentConstraint(gobject, target);
                    break;
                case ConstraintType.LookAt:
                    MixerClient.Instance.SendAddLookAtConstraint(gobject, target);
                    break;
            }
        }

        public void RemoveObjectConstraint(GameObject gobject, ConstraintType constraintType)
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    MixerClient.Instance.SendRemoveParentConstraint(gobject);
                    break;
                case ConstraintType.LookAt:
                    MixerClient.Instance.SendRemoveLookAtConstraint(gobject);
                    break;
            }
        }

        public void SetSky(SkySettings sky)
        {
            MixerClient.Instance.SendEvent<SkySettings>(MessageType.Sky, sky);
        }

        public void ApplyShotManagerAction(ShotManagerActionInfo info)
        {
            MixerClient.Instance.SendEvent<ShotManagerActionInfo>(MessageType.ShotManagerAction, info);
        }

        public void SendUserInfo(Vector3 cameraPosition, Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraRight)
        {
            MixerUser user = (MixerUser)GlobalState.networkUser;
            if (null == user) { return; }

            Vector3 upRight = cameraPosition + cameraForward + cameraUp + cameraRight;
            Vector3 upLeft = cameraPosition + cameraForward + cameraUp - cameraRight;
            Vector3 bottomRight = cameraPosition + cameraForward - cameraUp + cameraRight;
            Vector3 bottomLeft = cameraPosition + cameraForward - cameraUp - cameraRight;

            user.corners[0] = SceneManager.RightHanded.InverseTransformPoint(upLeft);
            user.corners[1] = SceneManager.RightHanded.InverseTransformPoint(upRight);
            user.corners[2] = SceneManager.RightHanded.InverseTransformPoint(bottomRight);
            user.corners[3] = SceneManager.RightHanded.InverseTransformPoint(bottomLeft);
            MixerClient.Instance.SendPlayerTransform(user);
        }

        public void RemoteSave()
        {
            MixerClient.Instance.SendBlenderSave();
        }
    }
}
