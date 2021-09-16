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

using UnityEngine;

namespace VRtist
{
    public class VRtistScene : IScene
    {
        public string GetSceneType()
        {
            return "VRtist";
        }

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
            GlobalState.FireObjectAdded(gobject);
            return gobject;
        }

        public void RemoveObject(GameObject gobject)
        {
            gobject.transform.SetParent(SceneManager.Trash.transform, true);
            GlobalState.FireObjectRemoved(gobject);
        }

        public void RestoreObject(GameObject gobject, Transform parent)
        {
            gobject.transform.SetParent(parent, true);
            GlobalState.FireObjectAdded(gobject);
        }

        public GameObject DuplicateObject(GameObject gobject)
        {
            // Instantiate with the current transfrom
            GameObject copy = InstantiateUnityPrefab(gobject);
            copy.transform.SetParent(gobject.transform.parent, false);

            // Then put it to righthanded
            copy.transform.SetParent(SceneManager.RightHanded, true);

            // Be sure to keep original mesh name
            MeshFilter[] sourceMeshFilters = gobject.GetComponentsInChildren<MeshFilter>();
            if (sourceMeshFilters.Length > 0)
            {
                MeshFilter[] copyMeshFilters = copy.GetComponentsInChildren<MeshFilter>();
                for (int i = 0; i < sourceMeshFilters.Length; ++i)
                {
                    copyMeshFilters[i].mesh.name = sourceMeshFilters[i].mesh.name;
                }
            }

            GlobalState.FireObjectAdded(copy);
            GlobalState.Animation.CopyAnimation(gobject, copy);
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

        public GameObject GetObjectParent(GameObject gobject)
        {
            Transform parentTransform = gobject.transform.parent;
            if (null == parentTransform)
                return null;
            return parentTransform.gameObject;
        }

        public void SetObjectParent(GameObject gobject, GameObject parent)
        {
            gobject.transform.SetParent(parent.transform, false);
        }

        public void SetObjectMaterialValue(GameObject gobject, MaterialValue materialValue)
        {
            Utils.SetMaterialValue(gobject, materialValue);
        }

        public void AddMaterialParameters(string materialName, MaterialID materialID, Color color) { }

        public void SendCameraInfo(Transform camera) { }

        public void SendLightInfo(Transform light) { }

        public void ClearObjectAnimations(GameObject gobject)
        {
        }

        public void SetObjectAnimations(GameObject gobject, AnimationSet animationSet)
        {
        }

        public void AddKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
        }

        public void RemoveKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
        }

        public void MoveKeyframe(GameObject gobject, AnimatableProperty property, int oldTime, int newTime)
        {
        }

        public void SetFrameRange(int start, int end) { }

        public void InsertObjectConstraint(int index, Constraint constraint)
        {
        }

        public void AddObjectConstraint(GameObject gobject, ConstraintType constraintType, GameObject target)
        {
        }

        public void RemoveObjectConstraint(GameObject gobject, ConstraintType constraintType)
        {
        }

        public void SetSky(SkySettings sky)
        {
        }

        public void ApplyShotManagerAction(ShotManagerActionInfo info)
        {
        }

        public void ListImportableObjects()
        {
        }

        public void SendUserInfo(Vector3 cameraPosition, Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraRight) { }

        public void RemoteSave() { }
    }
}
