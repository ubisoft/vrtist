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

using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ProjectItem : ListItemContent
    {
        [HideInInspector] public UIDynamicListItem item;

        private float rotation = 0.0f;
        public float rotationSpeedAnglesPerSecond = 2.0f;

        public void OnDestroy()
        {
        }

        public override void SetSelected(bool value)
        {
        }

        public void SetListItem(UIDynamicListItem dlItem, string path)
        {
            item = dlItem;
            
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            Texture2D texture = Utils.LoadTexture(path, true);
            mat.SetTexture("_EquiRect", texture);
            mat.SetVector("_CamInitWorldPos", Camera.main.transform.position);

            string projectName = Directory.GetParent(path).Name;
            transform.Find("Canvas/Text").gameObject.GetComponent<TextMeshProUGUI>().text = projectName;
        }

        public void SetCameraRef(Vector3 cameraPosition)
        {
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            mat.SetVector("_CamInitWorldPos", cameraPosition);
        }

        public void Rotate()
        {
            rotation += Time.unscaledDeltaTime * rotationSpeedAnglesPerSecond * Mathf.PI / 180.0f;
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            mat.SetFloat("_Rotation", rotation);
        }
        public void ResetRotation(float lobbyRotation)
        {
            rotation = -lobbyRotation * Mathf.PI / 180.0f;
            Material mat = transform.Find("Content").gameObject.GetComponent<MeshRenderer>().material;
            mat.SetFloat("_Rotation", rotation);
        }

        public void AddListeners(UnityAction duplicateAction, UnityAction deleteAction)
        {
        }
    }
}
