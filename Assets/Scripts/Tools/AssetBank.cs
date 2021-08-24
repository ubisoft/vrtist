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

using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AssetBank : SelectorBase
    {
        [Header("Parameters")]
        private bool useDefaultInstantiationScale = false;

        private UIDynamicList uiList;
        private UILabel filterLabel;
        private int selectedItem;

        void Start()
        {
            Init();

            uiList = panel.GetComponentInChildren<UIDynamicList>();
            uiList.focusItemOnAdd = false;

            filterLabel = panel.Find("ListPanel/FilterLabel").GetComponent<UILabel>();

            AssetBankUtils.LoadAssets();
            AssetBankUtils.PopulateUIList(uiList, OnUIObjectEnter, OnUIObjectExit);
        }

        public override void OnUIObjectEnter(int uid)
        {
            if (!AssetBankUtils.loadingAsset)
            {
                selectedItem = uid;
            }
        }

        public override void OnUIObjectExit(int uid)
        {
            if (!AssetBankUtils.loadingAsset)
            {
                selectedItem = -1;
            }
        }

        public void OnEditFilter()
        {
            ToolsUIManager.Instance.OpenKeyboard(OnValidateFilter, panel, uiList.GetFilter());
        }

        private void OnValidateFilter(string value)
        {
            filterLabel.Text = value;
            uiList.OnFilterList(value);
        }

        public void OnClearFilter()
        {
            filterLabel.Text = "";
            uiList.OnFilterList(null);
        }

        public void OnUseDefaultScale(bool value)
        {
            useDefaultInstantiationScale = value;
        }

        public async Task OnGrabUIObject()
        {
            await AssetBankUtils.LoadPrefab(selectedItem);
            if (AssetBankUtils.TryGetItem(selectedItem, out AssetBankItemData item))
            {
                if (item.skipInstantiation)
                {
                    // Assets coming from Blender Asset Bank add-on
                    // We will receive them, no need to instantiate them
                    item.prefab = null;
                }
                if (null != item.prefab)
                {
                    GameObject instance = SceneManager.InstantiateObject(item.prefab);
                    AddObject(instance);
                }
            }
            selectedItem = -1;
        }

        private void AddObject(GameObject gobject)
        {
            if (!AssetBankUtils.TryGetItem(selectedItem, out AssetBankItemData item))
            {
                Debug.LogWarning($"Item {gobject.name} not found in Asset Bank (id: {selectedItem})");
                return;
            }

            // Get the position of the mouthpiece into matrix
            Matrix4x4 matrix = SceneManager.RightHanded.worldToLocalMatrix * mouthpiece.localToWorldMatrix;
            Maths.DecomposeMatrix(matrix, out Vector3 t, out _, out _);
            Vector3 scale = Vector3.one;

            CommandGroup group = new CommandGroup("Instantiate Bank Object");
            try
            {
                // Add the object to scene
                ClearSelection();
                CommandAddGameObject command = new CommandAddGameObject(gobject);
                command.Submit();
                GameObject newObject = command.newObject;
                if (item.imported)
                {
                    ParametersController controller = newObject.GetComponent<ParametersController>();
                    if (null == controller)
                    {
                        controller = newObject.AddComponent<ParametersController>();
                        controller.isImported = true;
                        controller.importPath = item.assetName;
                    }
                }

                // Set the object size to 20cm in the user space
                Bounds bounds = new Bounds();
                foreach (var subMeshFilter in newObject.GetComponentsInChildren<MeshFilter>())
                {
                    if (!useDefaultInstantiationScale)
                    {
                        bounds.Encapsulate(subMeshFilter.mesh.bounds);
                    }
                }
                if (bounds.size.magnitude > 0)
                    scale *= (0.2f / bounds.size.magnitude) / GlobalState.WorldScale;  // 0.2: 20cm

                AddToSelection(newObject);
                SceneManager.SetObjectMatrix(newObject, Matrix4x4.TRS(t, Quaternion.identity, scale));
                Selection.HoveredObject = newObject;
            }
            finally
            {
                group.Submit();
            }
        }

        protected override void DoUpdateGui()
        {
            bool gripped = false;
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.gripButton, async () =>
            {
                await OnGrabUIObject();

                // Since OnGrabUIObject may take some time, check we are still gripped
                gripped = VRInput.GetValue(VRInput.primaryController, CommonUsages.gripButton);
                if (gripped) { OnStartGrip(); }
            }, () =>
            {
                // Only end grip if we were effectively gripped
                if (gripped) { OnEndGrip(); }
            });
        }
    }
}
