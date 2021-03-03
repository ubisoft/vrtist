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
using UnityEngine.UI;

namespace VRtist
{
    public class LightItem : ListItemContent
    {
        [HideInInspector] public GameObject lightObject;
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
            if (Selection.IsSelected(lightObject))
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
            TextMeshProUGUI text = transform.Find("Canvas/TextPanel/Text").GetComponent<TextMeshProUGUI>();
            text.text = name;
        }

        public void SetLightObject(GameObject lightObject, LightController controller)
        {
            this.lightObject = lightObject;
            SetColor(UIOptions.BackgroundColor);
            SetItemName(lightObject.name);

            Sprite sprite = null;
            switch (controller.Type)
            {
                case LightType.Directional:
                    sprite = UIUtils.LoadIcon("sun");
                    break;
                case LightType.Spot:
                    sprite = UIUtils.LoadIcon("spot");
                    break;
                case LightType.Point:
                    sprite = UIUtils.LoadIcon("light");
                    break;
            }
            if (null != sprite)
            {
                gameObject.GetComponentInChildren<Image>(true).sprite = sprite;
            }
        }
    }
}
