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

using TMPro;

using UnityEngine;

namespace VRtist
{
    public class GrassItem : ListItemContent
    {
        [HideInInspector] public GameObject grassObject;
        [HideInInspector] public UIDynamicListItem item;

        public void Start()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
        }

        public void OnDestroy()
        {
            Selection.onSelectionChanged.RemoveListener(OnSelectionChanged);
        }

        private void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> currentSelectedObjects)
        {
            if (Selection.IsSelected(grassObject))
            {
                SetColor(UIOptions.PushedColor);
            }
            else
            {
                SetColor(UIOptions.BackgroundColor);
            }
        }

        public void SetColor(Color color)
        {
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[0].SetColor("_BaseColor", color);
        }

        public void SetItemName(string name)
        {
            TextMeshProUGUI text = transform.Find("Canvas/Panel/Text").gameObject.GetComponent<TextMeshProUGUI>();
            text.text = name;
        }

        public void SetGrassObject(GameObject grassObject)
        {
            this.grassObject = grassObject;
            SetColor(UIOptions.BackgroundColor);
            //GrassController controller = grassObject.GetComponent<GrassController>();
            //gameObject.GetComponentInChildren<MeshRenderer>(true).materials[1].SetTexture("_UnlitColorMap", controller.Snapshot); // TODO: compute image
            SetItemName(grassObject.name);
        }
    }
}
