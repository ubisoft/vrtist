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
        void InsertObjectConstraint(int index, Constraint constraint);
        void AddObjectConstraint(GameObject gobject, ConstraintType constraintType, GameObject target);
        void RemoveObjectConstraint(GameObject gobject, ConstraintType constraintType);
        void SetSky(SkySettings sky);
        void ApplyShotManagerAction(ShotManagerActionInfo info);
        void ListImportableObjects();
        void SendUserInfo(Vector3 cameraPosition, Vector3 cameraForward, Vector3 cameraUp, Vector3 cameraRight);
        void RemoteSave();
    }
}
