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
        public GameObject original;   // original object (loaded from disk or Unity.Resources)
        public GameObject thumbnail;  // may be an image or 3D thumbnail
        public bool builtin = false;
        public Func<AssetBankItem, Task<GameObject>> importFunction = null;
        public bool skipInstantiation = false;

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
        [Header("Parameters")]
        public Transform container;
        private bool useDefaultInstantiationScale = false;

        private Dictionary<int, AssetBankItem> items = new Dictionary<int, AssetBankItem>();   // uid -> asset bank item
        private HashSet<string> tags = new HashSet<string>();
        private GameObject bank;  // contains all prefabs from the asset bank
        private int selectedItem = -1;
        private bool loadingAsset = false;

        private UIDynamicList uiList;

        void Start()
        {
            Init();

            uiList = panel.GetComponentInChildren<UIDynamicList>();

            // Create our storage for loaded objects
            bank = new GameObject("__VRtist_Asset_Bank__");
            bank.SetActive(false);

            // Add our predifined objects
            AddBuiltinAssets();

            // Add user defined objects
            StartCoroutine(ScanDirectory(GlobalState.Settings.assetBankDirectory));
        }

        public void ScanAssetBank()
        {
            // Remove all user assets
            foreach (var item in items.Values)
            {
                if (!item.builtin)
                {
                    if (item.original) { Destroy(item.original); }
                    if (item.thumbnail) { Destroy(item.thumbnail); }
                    items.Remove(item.uid);
                }
            }

            // Scan user directory
            StartCoroutine(ScanDirectory(GlobalState.Settings.assetBankDirectory));

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
            // TODO? parse them from Resources folder at editor time then create a db of those resources (scriptableObject)
            // available at runtime to finally parse that db
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CUBE_UI", "Prefabs/Primitives/PRIMITIVES/CUBE_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/SPHERE_UI", "Prefabs/Primitives/PRIMITIVES/SPHERE_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CYLINDER_UI", "Prefabs/Primitives/PRIMITIVES/CYLINDER_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CONE_UI", "Prefabs/Primitives/PRIMITIVES/CONE_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/TORUS_UI", "Prefabs/Primitives/PRIMITIVES/TORUS_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/PLANE_UI", "Prefabs/Primitives/PRIMITIVES/PLANE_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/PRISM_UI", "Prefabs/Primitives/PRIMITIVES/PRISM_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/PENTE_963_UI", "Prefabs/Primitives/PRIMITIVES/PENTE_963_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/WINDOW_UI", "Prefabs/Primitives/PRIMITIVES/WINDOW_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/ARCH_QUARTER_UI", "Prefabs/Primitives/PRIMITIVES/ARCH_QUARTER_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/STRAIGHT_STAIRS_UI", "Prefabs/Primitives/PRIMITIVES/STRAIGHT_STAIRS_PRIM");
            AddBuiltinAsset("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CIRCLE_SQUARE_STAIRS_UI", "Prefabs/Primitives/PRIMITIVES/CIRCLE_SQUARE_STAIRS_PRIM");

            AddBuiltinAsset("Vegetation", "Banana Tree", "Prefabs/UI/VEGETATION/UI_Banana_Tree", "Prefabs/Primitives/VEGETATION/BANANA_TREE_PRIM");
            AddBuiltinAsset("Vegetation", "Coconut Tree", "Prefabs/UI/VEGETATION/UI_Coconut_Tree", "Prefabs/Primitives/VEGETATION/COCONUT_TREE_PRIM");
            AddBuiltinAsset("Vegetation", "Grass", "Prefabs/UI/VEGETATION/UI_Grass", "Prefabs/Primitives/VEGETATION/GRASS_PRIM");
            AddBuiltinAsset("Vegetation", "Wood A", "Prefabs/UI/VEGETATION/Wood_A", "Prefabs/Primitives/VEGETATION/WOOD_A_PRIM");
            AddBuiltinAsset("Vegetation", "Wood B", "Prefabs/UI/VEGETATION/Wood_B", "Prefabs/Primitives/VEGETATION/WOOD_B_PRIM");
            AddBuiltinAsset("Vegetation", "Wood C", "Prefabs/UI/VEGETATION/Wood_C", "Prefabs/Primitives/VEGETATION/WOOD_C_PRIM");
            AddBuiltinAsset("Vegetation", "Log Wood", "Prefabs/UI/VEGETATION/LogWood", "Prefabs/Primitives/VEGETATION/LOG_WOOD_PRIM");

            AddBuiltinAsset("Rock", "Rock A", "Prefabs/UI/ROCKS/Rock_A", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_A_PRIM");
            AddBuiltinAsset("Rock", "Rock B", "Prefabs/UI/ROCKS/Rock_B", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_B_PRIM");
            AddBuiltinAsset("Rock", "Rock C", "Prefabs/UI/ROCKS/Rock_C", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_C_PRIM");
            AddBuiltinAsset("Rock", "Rock D", "Prefabs/UI/ROCKS/Rock_D", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_D_PRIM");
            AddBuiltinAsset("Rock", "Rock E", "Prefabs/UI/ROCKS/Rock_E", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_E_PRIM");
            AddBuiltinAsset("Rock", "Rock F", "Prefabs/UI/ROCKS/Rock_F", "Prefabs/Primitives/ROCKS/ROCKS_SHARP_F_PRIM");
            AddBuiltinAsset("Rock", "Rock G", "Prefabs/UI/ROCKS/Rock_G", "Prefabs/Primitives/ROCKS/ROCKS_SHARP_G_PRIM");
            AddBuiltinAsset("Rock", "Rock J", "Prefabs/UI/ROCKS/Rock_J", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_J_PRIM");
            AddBuiltinAsset("Rock", "Rock K", "Prefabs/UI/ROCKS/Rock_K", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_K_PRIM");
            AddBuiltinAsset("Rock", "Asteroid", "Prefabs/UI/ROCKS/UI_Asteroid", "Prefabs/Primitives/ROCKS/Asteroid_PRIM");

            AddBuiltinAsset("Character", "Rabbid", "Prefabs/UI/CHARACTERS/UI_Lapin", "Prefabs/Primitives/CHARACTERS/LAPIN_PRIM");

            AddBuiltinAsset("Prop", "Barrel", "Prefabs/UI/JUNK/Barrel", "Prefabs/Primitives/JUNK/BARREL_PRIM");
            AddBuiltinAsset("Prop", "Bench", "Prefabs/UI/JUNK/Bench", "Prefabs/Primitives/JUNK/BENCH_PRIM");
            AddBuiltinAsset("Prop", "Bottle", "Prefabs/UI/JUNK/Bottle", "Prefabs/Primitives/JUNK/BOTTLE_PRIM");
            AddBuiltinAsset("Prop", "Box", "Prefabs/UI/JUNK/UI_Box", "Prefabs/Primitives/JUNK/BOX_PRIM");
            AddBuiltinAsset("Prop", "Bucket", "Prefabs/UI/JUNK/Bucket", "Prefabs/Primitives/JUNK/BUCKET_PRIM");
            AddBuiltinAsset("Prop", "Caddie", "Prefabs/UI/JUNK/Caddie", "Prefabs/Primitives/JUNK/CADDIE_PRIM");
            AddBuiltinAsset("Prop", "Dumpster", "Prefabs/UI/JUNK/Dumpster", "Prefabs/Primitives/JUNK/DUMPSTER_PRIM");
            AddBuiltinAsset("Prop", "Fence", "Prefabs/UI/JUNK/Fence", "Prefabs/Primitives/JUNK/FENCE_PRIM");
            AddBuiltinAsset("Prop", "Fridge", "Prefabs/UI/JUNK/Fridge", "Prefabs/Primitives/JUNK/FRIDGE_PRIM");
            AddBuiltinAsset("Prop", "Hydrant", "Prefabs/UI/JUNK/Hydrant", "Prefabs/Primitives/JUNK/HYDRANT_PRIM");
            AddBuiltinAsset("Prop", "Paint", "Prefabs/UI/JUNK/Paint", "Prefabs/Primitives/JUNK/PAINT_PRIM");
            AddBuiltinAsset("Prop", "Plank", "Prefabs/UI/JUNK/Plank", "Prefabs/Primitives/JUNK/PLANK_PRIM");
            AddBuiltinAsset("Prop", "Ticket", "Prefabs/UI/JUNK/Ticket", "Prefabs/Primitives/JUNK/TICKET_PRIM");
            AddBuiltinAsset("Prop", "Tire", "Prefabs/UI/JUNK/Tire", "Prefabs/Primitives/JUNK/TIRE_PRIM");
            AddBuiltinAsset("Prop", "Tole", "Prefabs/UI/JUNK/Tole", "Prefabs/Primitives/JUNK/TOLE_PRIM");
            AddBuiltinAsset("Prop", "Trunk Army", "Prefabs/UI/JUNK/TrunkArmy", "Prefabs/Primitives/JUNK/TRUNK_ARMY_PRIM");
            AddBuiltinAsset("Prop", "Warn Cone", "Prefabs/UI/JUNK/WarnCone", "Prefabs/Primitives/JUNK/WARN_CONE_PRIM");
            AddBuiltinAsset("Prop", "Washing Machine", "Prefabs/UI/JUNK/WashingMachine", "Prefabs/Primitives/JUNK/WASHING_MACHINE_PRIM");

            AddBuiltinAsset("Vehicle", "Submarine", "Prefabs/UI/JUNK/Submarine", "Prefabs/Primitives/JUNK/SUBMARINE_PRIM");
        }

        private IEnumerator ScanDirectory(string path)
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
        }

        private void AddBuiltinAsset(string tags, string name, string uiPath, string originalPath)
        {
            GameObject original = Instantiate(Resources.Load<GameObject>(originalPath));
            original.transform.parent = bank.transform;
            GameObject thumbnail = UIGrabber.Create3DThumbnail(Resources.Load<GameObject>(uiPath), OnUIObjectEnter, OnUIObjectExit);
            AssetBankItem item = AddAsset(name, thumbnail, original, tags);
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

        public AssetBankItem AddAsset(string name, GameObject thumbnail, GameObject original, string tags, Func<AssetBankItem, Task<GameObject>> importFunction = null, bool skipInstantiation = false)
        {
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            if (null == uiGrabber)
            {
                Debug.LogError("Thumbnail game object must have a UIGrabber component. Use the UIGrabber.CreateXXXThumbnail helper functions to create such a thumbnail");
                return null;
            }
            int uid = thumbnail.GetHashCode();
            GameObject root = new GameObject("AssetBankItem");
            root.layer = LayerMask.NameToLayer("UI");
            AssetBankItem item = root.AddComponent<AssetBankItem>();
            item.uid = uid;
            item.assetName = name;
            item.thumbnail = thumbnail;
            item.original = original;
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

            // If the original doesn't exist, load it
            if (null == item.original)
            {
                loadingAsset = true;
                GlobalState.Instance.messageBox.ShowMessage("Loading asset, please wait...");
                Selection.ClearSelection();
                item.original = await item.importFunction(item);
                if (!item.skipInstantiation)
                {
                    InstantiateObject(item.original);
                }
                GlobalState.Instance.messageBox.SetVisible(false);
                loadingAsset = false;
            }
            else
            {
                if (!item.skipInstantiation)
                {
                    InstantiateObject(item.original);
                }
            }
            selectedItem = -1;
        }

        private Task<GameObject> ImportObjectAsync(AssetBankItem item)
        {
            return GlobalState.GeometryImporter.ImportObjectAsync(item.assetName, bank.transform);
        }

        private void InstantiateObject(GameObject gobject)
        {
            if (!items.TryGetValue(selectedItem, out AssetBankItem item))
            {
                Debug.LogWarning($"Item {gobject.name} not found in Asset Bank (id: {selectedItem})");
                return;
            }

            // Create the prefab and instantiate it
            GameObject newObject = SyncData.InstantiateFullHierarchyPrefab(SyncData.CreateFullHierarchyPrefab(gobject));

            // Is this still required?
            MeshFilter meshFilter = newObject.GetComponentInChildren<MeshFilter>();
            if (null != meshFilter)
            {
                meshFilter.mesh.name = newObject.name;
            }

            Matrix4x4 matrix = container.worldToLocalMatrix * mouthpiece.localToWorldMatrix;
            if (!useDefaultInstantiationScale)
            {
                SyncData.SetTransform(newObject.name, matrix * Matrix4x4.Scale(gobject.transform.localScale));
            }
            else
            {
                Maths.DecomposeMatrix(matrix, out Vector3 t, out _, out _);
                // Cancel scale
                Quaternion quarterRotation = Quaternion.Euler(new Vector3(-90f, 180f, 0));
                SyncData.SetTransform(newObject.name, Matrix4x4.TRS(t, Quaternion.identity, Vector3.one) * Matrix4x4.Rotate(quarterRotation));
            }

            CommandGroup group = new CommandGroup("Instantiate Bank Object");
            try
            {
                ClearSelection();
                if (null == newObject.GetComponent<MeshFilter>())
                {
                    // Send transform
                    new CommandAddGameObject(newObject).Submit();
                }
                foreach (var subMeshFilter in newObject.GetComponentsInChildren<MeshFilter>())
                {
                    new CommandAddGameObject(subMeshFilter.gameObject).Submit();
                }
                AddToSelection(newObject);
                Selection.SetHoveredObject(newObject);
            }
            finally
            {
                group.Submit();
            }
        }

        protected override void DoUpdateGui()
        {
            bool gripped = false;
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, async () =>
            {
                await OnInstantiateUIObject();

                // Since OnInstantiateUIObject may take some time, check we are still gripped
                gripped = VRInput.GetValue(VRInput.rightController, CommonUsages.gripButton);
                if (gripped) { OnStartGrip(); }
            }, () =>
            {
                // Only end grip if we were effectively gripped
                if (gripped) { OnEndGrip(); }
            });
        }
    }
}
