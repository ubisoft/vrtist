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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AssetBankItem : ListItemContent
    {
        public int uid;               // uid
        public string assetName;      // item name or filename
        public HashSet<string> tags = new HashSet<string>();  // list of tags for filtering
        public GameObject prefab;   // original object (loaded from disk or Unity.Resources)
        public GameObject thumbnail;  // may be an image or 3D thumbnail
        public bool builtin = false;
        public Func<AssetBankItem, Task<GameObject>> importFunction = null;
        public bool skipInstantiation = false;
        public bool imported = false;

        public UIDynamicListItem uiItem;

        public void AddTags(string path)
        {
            path = path.Replace("\\", "/");
            foreach (var tag in path.Split(new char[] { '/', '-', '_', ' ' }))
            {
                tags.Add(tag.ToLower());
            }
        }
    }

    public class AssetBank : SelectorBase
    {
        private const string ASSET_BANK_NAME = "__VRtist_Asset_Bank__";

        [Header("Parameters")]
        private bool useDefaultInstantiationScale = false;

        private readonly Dictionary<int, AssetBankItem> items = new Dictionary<int, AssetBankItem>();   // uid -> asset bank item
        private readonly HashSet<string> tags = new HashSet<string>();
        private GameObject bank;  // contains all prefabs from the asset bank
        private int selectedItem = -1;
        private bool loadingAsset = false;

        private UIDynamicList uiList;
        private UILabel filterLabel;

        void Start()
        {
            Init();

            uiList = panel.GetComponentInChildren<UIDynamicList>();
            uiList.focusItemOnAdd = false;

            filterLabel = panel.Find("ListPanel/FilterLabel").GetComponent<UILabel>();

            // Create our storage for loaded objects
            bank = new GameObject(ASSET_BANK_NAME);
            bank.SetActive(false);

            // Add our predifined objects
            AddBuiltinAssets();

            // Add user defined objects
            StartCoroutine(ScanDirectory(GlobalState.Settings.assetBankDirectory, () =>
            {
                SceneManager.ListImportableObjects();
            }));
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

        public void ScanAssetBank()
        {
            // Remove all user assets
            foreach (var item in items.Values)
            {
                if (!item.builtin)
                {
                    if (item.prefab) { Destroy(item.prefab); }
                    if (item.thumbnail) { Destroy(item.thumbnail); }
                    items.Remove(item.uid);
                }
            }

            // Scan user directory
            StartCoroutine(ScanDirectory(GlobalState.Settings.assetBankDirectory, () => uiList.OnFirstPage()));

            // Rebuild tags list
            tags.Clear();
            foreach (var item in items.Values)
            {
                foreach (var tag in item.tags)
                {
                    tags.Add(tag);
                }
            }
        }

        private void AddBuiltinAssets()
        {
            AddBuiltinAsset("Empty", "Axis Locator", "Prefabs/UI/Axis Locator Item", "Prefabs/Primitives/Axis_locator");

            // TODO? parse them from Resources folder at editor time then create a db of those resources (scriptableObject)
            // available at runtime to finally parse that db
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/UI_cube", "Prefabs/Primitives/PRIMITIVES/cube");
            AddBuiltinAsset("Primitive", "Sphere", "Prefabs/UI/PRIMITIVES/UI_sphere", "Prefabs/Primitives/PRIMITIVES/sphere");
            AddBuiltinAsset("Primitive", "Cylinder", "Prefabs/UI/PRIMITIVES/UI_cylinder", "Prefabs/Primitives/PRIMITIVES/cylinder");
            AddBuiltinAsset("Primitive", "Cone", "Prefabs/UI/PRIMITIVES/UI_cone", "Prefabs/Primitives/PRIMITIVES/cone");
            AddBuiltinAsset("Primitive", "Torus", "Prefabs/UI/PRIMITIVES/UI_torus", "Prefabs/Primitives/PRIMITIVES/torus");
            AddBuiltinAsset("Primitive", "Plane", "Prefabs/UI/PRIMITIVES/UI_plane", "Prefabs/Primitives/PRIMITIVES/plane");
            AddBuiltinAsset("Primitive", "Prism", "Prefabs/UI/PRIMITIVES/UI_prism", "Prefabs/Primitives/PRIMITIVES/prism");
            AddBuiltinAsset("Primitive", "Pente", "Prefabs/UI/PRIMITIVES/UI_slope_963", "Prefabs/Primitives/PRIMITIVES/slope_963");
            AddBuiltinAsset("Primitive", "Window", "Prefabs/UI/PRIMITIVES/UI_window", "Prefabs/Primitives/PRIMITIVES/window");
            AddBuiltinAsset("Primitive", "Arch", "Prefabs/UI/PRIMITIVES/UI_arch_quarter", "Prefabs/Primitives/PRIMITIVES/arch_quarter");
            AddBuiltinAsset("Primitive", "Stairs", "Prefabs/UI/PRIMITIVES/UI_stairs", "Prefabs/Primitives/PRIMITIVES/stairs");
            AddBuiltinAsset("Primitive", "Circle Stairs", "Prefabs/UI/PRIMITIVES/UI_circle_square_stairs", "Prefabs/Primitives/PRIMITIVES/circle_square_stairs");

            AddBuiltinAsset("Vegetation", "Aloe Vera", "Prefabs/UI/VEGETATION/UI_aloevera", "Prefabs/Primitives/VEGETATION/aloevera");
            AddBuiltinAsset("Vegetation", "Big Tree", "Prefabs/UI/VEGETATION/UI_big_tree", "Prefabs/Primitives/VEGETATION/big_tree");
            AddBuiltinAsset("Vegetation", "Small Tree", "Prefabs/UI/VEGETATION/UI_small_tree", "Prefabs/Primitives/VEGETATION/small_tree");
            AddBuiltinAsset("Vegetation", "Big Big Tree", "Prefabs/UI/VEGETATION/UI_big_big_tree", "Prefabs/Primitives/VEGETATION/big_big_tree");
            AddBuiltinAsset("Vegetation", "Banana Tree Hard", "Prefabs/UI/VEGETATION/UI_banana_tree_hard", "Prefabs/Primitives/VEGETATION/banana_tree_hard");
            AddBuiltinAsset("Vegetation", "Coconut Tree Hard", "Prefabs/UI/VEGETATION/UI_coconut_tree_hard", "Prefabs/Primitives/VEGETATION/coconut_tree_hard");
            AddBuiltinAsset("Vegetation", "Green Grass", "Prefabs/UI/VEGETATION/UI_green_grass", "Prefabs/Primitives/VEGETATION/green_grass");
            AddBuiltinAsset("Vegetation", "Yellow Grass", "Prefabs/UI/VEGETATION/UI_yellow_grass", "Prefabs/Primitives/VEGETATION/yellow_grass");
            AddBuiltinAsset("Vegetation", "Mushroom Amanita", "Prefabs/UI/VEGETATION/UI_mushroom_amanita", "Prefabs/Primitives/VEGETATION/mushroom_amanita");
            AddBuiltinAsset("Vegetation", "Mushroom Morel", "Prefabs/UI/VEGETATION/UI_mushroom_morel", "Prefabs/Primitives/VEGETATION/mushroom_morel");
            AddBuiltinAsset("Vegetation", "Wood A", "Prefabs/UI/VEGETATION/UI_wood_A", "Prefabs/Primitives/VEGETATION/wood_A");
            AddBuiltinAsset("Vegetation", "Wood B", "Prefabs/UI/VEGETATION/UI_wood_B", "Prefabs/Primitives/VEGETATION/wood_B");
            AddBuiltinAsset("Vegetation", "Wood C", "Prefabs/UI/VEGETATION/UI_wood_C", "Prefabs/Primitives/VEGETATION/wood_C");
            AddBuiltinAsset("Vegetation", "Log Wood", "Prefabs/UI/VEGETATION/UI_log_wood", "Prefabs/Primitives/VEGETATION/log_wood");

            AddBuiltinAsset("Rock", "Rock A", "Prefabs/UI/ROCKS/UI_rocks_round_A", "Prefabs/Primitives/ROCKS/rocks_round_A");
            AddBuiltinAsset("Rock", "Rock B", "Prefabs/UI/ROCKS/UI_rocks_round_B", "Prefabs/Primitives/ROCKS/rocks_round_B");
            AddBuiltinAsset("Rock", "Rock C", "Prefabs/UI/ROCKS/UI_rocks_round_C", "Prefabs/Primitives/ROCKS/rocks_round_C");
            AddBuiltinAsset("Rock", "Rock D", "Prefabs/UI/ROCKS/UI_rocks_round_D", "Prefabs/Primitives/ROCKS/rocks_round_D");
            AddBuiltinAsset("Rock", "Rock E", "Prefabs/UI/ROCKS/UI_rocks_round_E", "Prefabs/Primitives/ROCKS/rocks_round_E");
            AddBuiltinAsset("Rock", "Rock F", "Prefabs/UI/ROCKS/UI_rocks_sharp_F", "Prefabs/Primitives/ROCKS/rocks_sharp_F");
            AddBuiltinAsset("Rock", "Rock G", "Prefabs/UI/ROCKS/UI_rocks_sharp_G", "Prefabs/Primitives/ROCKS/rocks_sharp_G");
            AddBuiltinAsset("Rock", "Rock J", "Prefabs/UI/ROCKS/UI_rocks_round_J", "Prefabs/Primitives/ROCKS/rocks_round_J");
            AddBuiltinAsset("Rock", "Rock K", "Prefabs/UI/ROCKS/UI_rocks_round_K", "Prefabs/Primitives/ROCKS/rocks_round_K");
            AddBuiltinAsset("Rock", "Asteroid", "Prefabs/UI/ROCKS/UI_asteroid", "Prefabs/Primitives/ROCKS/asteroid");

            AddBuiltinAsset("Furniture", "Small Crate", "Prefabs/UI/FURNITURE/UI_small_crate", "Prefabs/Primitives/FURNITURE/small_crate");
            AddBuiltinAsset("Furniture", "Big Crate", "Prefabs/UI/FURNITURE/UI_big_crate", "Prefabs/Primitives/FURNITURE/big_crate");
            AddBuiltinAsset("Furniture", "Cactus pot", "Prefabs/UI/FURNITURE/UI_cactus_pot", "Prefabs/Primitives/FURNITURE/cactus_pot");
            AddBuiltinAsset("Furniture", "Stepladder", "Prefabs/UI/FURNITURE/UI_stepladder", "Prefabs/Primitives/FURNITURE/stepladder");
            AddBuiltinAsset("Furniture", "Armchair", "Prefabs/UI/FURNITURE/UI_armchair", "Prefabs/Primitives/FURNITURE/armchair");
            AddBuiltinAsset("Furniture", "Fishing Chair", "Prefabs/UI/FURNITURE/UI_fishing_chair", "Prefabs/Primitives/FURNITURE/fishing_chair");
            AddBuiltinAsset("Furniture", "Fridge", "Prefabs/UI/FURNITURE/UI_fridge", "Prefabs/Primitives/FURNITURE/fridge");
            AddBuiltinAsset("Furniture", "TV", "Prefabs/UI/FURNITURE/UI_tv", "Prefabs/Primitives/FURNITURE/tv");

            AddBuiltinAsset("Prop", "Barrel", "Prefabs/UI/JUNK/UI_barrel", "Prefabs/Primitives/JUNK/barrel");
            AddBuiltinAsset("Prop", "Barricade", "Prefabs/UI/JUNK/UI_barricade", "Prefabs/Primitives/JUNK/barricade");
            AddBuiltinAsset("Prop", "Bench", "Prefabs/UI/JUNK/UI_bench", "Prefabs/Primitives/JUNK/bench");
            AddBuiltinAsset("Prop", "Bottle", "Prefabs/UI/JUNK/UI_bottle", "Prefabs/Primitives/JUNK/bottle");
            AddBuiltinAsset("Prop", "Bucket", "Prefabs/UI/JUNK/UI_bucket", "Prefabs/Primitives/JUNK/bucket");
            AddBuiltinAsset("Prop", "Dumpster", "Prefabs/UI/JUNK/UI_dumpster", "Prefabs/Primitives/JUNK/dumpster");
            AddBuiltinAsset("Prop", "Fence 1", "Prefabs/UI/JUNK/UI_fence_1", "Prefabs/Primitives/JUNK/fence_1");
            AddBuiltinAsset("Prop", "Fence 2", "Prefabs/UI/JUNK/UI_fence_2", "Prefabs/Primitives/JUNK/fence_2");
            AddBuiltinAsset("Prop", "Hydrant", "Prefabs/UI/JUNK/UI_hydrant", "Prefabs/Primitives/JUNK/hydrant");
            AddBuiltinAsset("Prop", "Metalsheet", "Prefabs/UI/JUNK/UI_metalsheet", "Prefabs/Primitives/JUNK/metalsheet");
            AddBuiltinAsset("Prop", "Paint", "Prefabs/UI/JUNK/UI_paint", "Prefabs/Primitives/JUNK/paint");
            AddBuiltinAsset("Prop", "Plank", "Prefabs/UI/JUNK/UI_plank", "Prefabs/Primitives/JUNK/plank");
            AddBuiltinAsset("Prop", "Tire", "Prefabs/UI/JUNK/UI_tire", "Prefabs/Primitives/JUNK/tire");
            AddBuiltinAsset("Prop", "Tole", "Prefabs/UI/JUNK/UI_tole", "Prefabs/Primitives/JUNK/tole");
            AddBuiltinAsset("Prop", "Trunk Army", "Prefabs/UI/JUNK/UI_trunk_army", "Prefabs/Primitives/JUNK/trunk_army");
            AddBuiltinAsset("Prop", "Warn Cone", "Prefabs/UI/JUNK/UI_warn_cone", "Prefabs/Primitives/JUNK/warn_cone");

            AddBuiltinAsset("Vehicle", "Car", "Prefabs/UI/JUNK/UI_car", "Prefabs/Primitives/JUNK/car");
        }

        private IEnumerator ScanDirectory(string path, Action onEndScan = null)
        {
            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (var directory in directories)
                {
                    StartCoroutine(ScanDirectory(directory));
                }

                string[] filenames = Directory.GetFiles(path, "*.fbx");
                foreach (var filename in filenames)
                {
                    AddFileAsset(filename);
                    yield return null;
                }
            }

            onEndScan?.Invoke();
        }

        private void AddBuiltinAsset(string tags, string name, string uiPath, string prefabPath)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);

            GameObject thumbnail = UIGrabber.Create3DThumbnail(Instantiate(Resources.Load<GameObject>(uiPath)), OnUIObjectEnter, OnUIObjectExit);
            AssetBankItem item = AddAsset(name, thumbnail, prefab, tags);
            item.builtin = true;
        }

        private void AddFileAsset(string filename)
        {
            string name = Path.GetFileNameWithoutExtension(filename).Replace('_', ' ');
            GameObject thumbnail = UIGrabber.CreateTextThumbnail(name, OnUIObjectEnter, OnUIObjectExit);
            string tags = filename.Substring(GlobalState.Settings.assetBankDirectory.Length);
            tags = tags.Substring(0, tags.LastIndexOf('.'));  // remove file extension
            AddAsset(filename, thumbnail, null, tags, importFunction: ImportObjectAsync);
        }

        public AssetBankItem AddAsset(string name, GameObject thumbnail, GameObject prefab, string tags, Func<AssetBankItem, Task<GameObject>> importFunction = null, bool skipInstantiation = false)
        {
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            if (null == uiGrabber)
            {
                Debug.LogError("Thumbnail game object must have a UIGrabber component. Use the UIGrabber.CreateXXXThumbnail helper functions to create such a thumbnail");
                return null;
            }
            int uid = thumbnail.GetHashCode();
            GameObject root = new GameObject("AssetBankItem")
            {
                layer = LayerMask.NameToLayer("CameraHidden")
            };
            AssetBankItem item = root.AddComponent<AssetBankItem>();
            item.uid = uid;
            item.assetName = name;
            item.gameObject.name = name;
            item.thumbnail = thumbnail;
            item.prefab = prefab;
            item.thumbnail.transform.parent = root.transform;
            item.thumbnail.transform.localPosition += new Vector3(0, 0, -0.001f);
            item.builtin = false;
            item.importFunction = importFunction;
            item.skipInstantiation = skipInstantiation;
            item.AddTags(tags);
            foreach (var tag in item.tags)
            {
                this.tags.Add(tag);
            }
            item.uiItem = uiList.AddItem(item.transform);
            items.Add(uid, item);
            return item;
        }

        public override void OnUIObjectEnter(int uid)
        {
            if (!loadingAsset)
            {
                selectedItem = uid;
            }
        }

        public override void OnUIObjectExit(int uid)
        {
            if (!loadingAsset)
            {
                selectedItem = -1;
            }
        }

        public void OnUseDefaultScale(bool value)
        {
            useDefaultInstantiationScale = value;
        }

        public async Task OnInstantiateUIObject()
        {
            if (loadingAsset) { return; }
            if (selectedItem == -1) { return; }
            if (!items.TryGetValue(selectedItem, out AssetBankItem item)) { return; }
            if (!item.thumbnail.GetComponent<UIGrabber>().isValid) { return; }

            // If the original doesn't exist, load it
            if (null == item.prefab)
            {
                loadingAsset = true;
                GlobalState.Instance.messageBox.ShowMessage("Loading asset, please wait...");
                Selection.ClearSelection();
                item.prefab = await item.importFunction(item);
                NameObjectMeshes(item.prefab);
                item.imported = true;

                // For blender assets, we don't want to instantiate objects, we will receive them
                if (!item.skipInstantiation)
                {
                    AddObject(item.prefab);
                }
                else
                {
                    item.prefab = null;
                }
                GlobalState.Instance.messageBox.SetVisible(false);
                loadingAsset = false;
            }
            else
            {
                if (!item.skipInstantiation)
                {
                    GameObject instance = SceneManager.InstantiateObject(item.prefab);
                    AddObject(instance);
                }
            }
            selectedItem = -1;
        }

        private Task<GameObject> ImportObjectAsync(AssetBankItem item)
        {
            return GlobalState.GeometryImporter.ImportObjectAsync(item.assetName, bank.transform);
        }

        private void NameObjectMeshes(GameObject gobject)
        {
            foreach (Transform child in gobject.transform)
            {
                MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                if (null != meshFilter)
                {
                    Mesh mesh = meshFilter.mesh;
                    if (null != mesh)
                    {
                        mesh.name = child.name;
                    }
                }
            }
        }

        private void AddObject(GameObject gobject)
        {
            if (!items.TryGetValue(selectedItem, out AssetBankItem item))
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
                await OnInstantiateUIObject();

                // Since OnInstantiateUIObject may take some time, check we are still gripped
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
