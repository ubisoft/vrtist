using System.Collections.Generic;
using System.IO;
using TMPro;
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
        [Header("Parameters")]
        public Transform container;
        private bool useDefaultInstantiationScale = false;

        private string rootDirectory;
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
            // TODO? parse them from Resources folder at editor time then create a db of those resources (scriptableObject)
            // available at runtime to finally parse that db
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CUBE_UI", "Prefabs/Primitives/PRIMITIVES/CUBE_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/SPHERE_UI", "Prefabs/Primitives/PRIMITIVES/SPHERE_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CYLINDER_UI", "Prefabs/Primitives/PRIMITIVES/CYLINDER_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CONE_UI", "Prefabs/Primitives/PRIMITIVES/CONE_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/TORUS_UI", "Prefabs/Primitives/PRIMITIVES/TORUS_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/PLANE_UI", "Prefabs/Primitives/PRIMITIVES/PLANE_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/PRISM_UI", "Prefabs/Primitives/PRIMITIVES/PRISM_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/PENTE_963_UI", "Prefabs/Primitives/PRIMITIVES/PENTE_963_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/WINDOW_UI", "Prefabs/Primitives/PRIMITIVES/WINDOW_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/ARCH_QUARTER_UI", "Prefabs/Primitives/PRIMITIVES/ARCH_QUARTER_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/STRAIGHT_STAIRS_UI", "Prefabs/Primitives/PRIMITIVES/STRAIGHT_STAIRS_PRIM");
            AddBuiltinObject("Primitive", "Cube", "Prefabs/UI/PRIMITIVES/CIRCLE_SQUARE_STAIRS_UI", "Prefabs/Primitives/PRIMITIVES/CIRCLE_SQUARE_STAIRS_PRIM");

            AddBuiltinObject("Vegetation", "Banana Tree", "Prefabs/UI/VEGETATION/UI_Banana_Tree", "Prefabs/Primitives/VEGETATION/BANANA_TREE_PRIM");
            AddBuiltinObject("Vegetation", "Coconut Tree", "Prefabs/UI/VEGETATION/UI_Coconut_Tree", "Prefabs/Primitives/VEGETATION/COCONUT_TREE_PRIM");
            AddBuiltinObject("Vegetation", "Grass", "Prefabs/UI/VEGETATION/UI_Grass", "Prefabs/Primitives/VEGETATION/GRASS_PRIM");
            AddBuiltinObject("Vegetation", "Wood A", "Prefabs/UI/VEGETATION/Wood_A", "Prefabs/Primitives/VEGETATION/WOOD_A_PRIM");
            AddBuiltinObject("Vegetation", "Wood B", "Prefabs/UI/VEGETATION/Wood_B", "Prefabs/Primitives/VEGETATION/WOOD_B_PRIM");
            AddBuiltinObject("Vegetation", "Wood C", "Prefabs/UI/VEGETATION/Wood_C", "Prefabs/Primitives/VEGETATION/WOOD_C_PRIM");
            AddBuiltinObject("Vegetation", "Log Wood", "Prefabs/UI/VEGETATION/LogWood", "Prefabs/Primitives/VEGETATION/LOG_WOOD_PRIM");

            AddBuiltinObject("Rock", "Rock A", "Prefabs/UI/ROCKS/Rock_A", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_A_PRIM");
            AddBuiltinObject("Rock", "Rock B", "Prefabs/UI/ROCKS/Rock_B", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_B_PRIM");
            AddBuiltinObject("Rock", "Rock C", "Prefabs/UI/ROCKS/Rock_C", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_C_PRIM");
            AddBuiltinObject("Rock", "Rock D", "Prefabs/UI/ROCKS/Rock_D", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_D_PRIM");
            AddBuiltinObject("Rock", "Rock E", "Prefabs/UI/ROCKS/Rock_E", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_E_PRIM");
            AddBuiltinObject("Rock", "Rock F", "Prefabs/UI/ROCKS/Rock_F", "Prefabs/Primitives/ROCKS/ROCKS_SHARP_F_PRIM");
            AddBuiltinObject("Rock", "Rock G", "Prefabs/UI/ROCKS/Rock_G", "Prefabs/Primitives/ROCKS/ROCKS_SHARP_G_PRIM");
            AddBuiltinObject("Rock", "Rock J", "Prefabs/UI/ROCKS/Rock_J", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_J_PRIM");
            AddBuiltinObject("Rock", "Rock K", "Prefabs/UI/ROCKS/Rock_K", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_K_PRIM");
            AddBuiltinObject("Rock", "Asteroid", "Prefabs/UI/ROCKS/UI_Asteroid", "Prefabs/Primitives/ROCKS/Asteroid_PRIM");

            AddBuiltinObject("Character", "Rabbid", "Prefabs/UI/CHARACTERS/UI_Lapin", "Prefabs/Primitives/CHARACTERS/LAPIN_PRIM");

            AddBuiltinObject("Prop", "Barrel", "Prefabs/UI/JUNK/Barrel", "Prefabs/Primitives/JUNK/BARREL_PRIM");
            AddBuiltinObject("Prop", "Bench", "Prefabs/UI/JUNK/Bench", "Prefabs/Primitives/JUNK/BENCH_PRIM");
            AddBuiltinObject("Prop", "Bottle", "Prefabs/UI/JUNK/Bottle", "Prefabs/Primitives/JUNK/BOTTLE_PRIM");
            AddBuiltinObject("Prop", "Box", "Prefabs/UI/JUNK/UI_Box", "Prefabs/Primitives/JUNK/BOX_PRIM");
            AddBuiltinObject("Prop", "Bucket", "Prefabs/UI/JUNK/Bucket", "Prefabs/Primitives/JUNK/BUCKET_PRIM");
            AddBuiltinObject("Prop", "Caddie", "Prefabs/UI/JUNK/Caddie", "Prefabs/Primitives/JUNK/CADDIE_PRIM");
            AddBuiltinObject("Prop", "Dumpster", "Prefabs/UI/JUNK/Dumpster", "Prefabs/Primitives/JUNK/DUMPSTER_PRIM");
            AddBuiltinObject("Prop", "Fence", "Prefabs/UI/JUNK/Fence", "Prefabs/Primitives/JUNK/FENCE_PRIM");
            AddBuiltinObject("Prop", "Fridge", "Prefabs/UI/JUNK/Fridge", "Prefabs/Primitives/JUNK/FRIDGE_PRIM");
            AddBuiltinObject("Prop", "Hydrant", "Prefabs/UI/JUNK/Hydrant", "Prefabs/Primitives/JUNK/HYDRANT_PRIM");
            AddBuiltinObject("Prop", "Paint", "Prefabs/UI/JUNK/Paint", "Prefabs/Primitives/JUNK/PAINT_PRIM");
            AddBuiltinObject("Prop", "Plank", "Prefabs/UI/JUNK/Plank", "Prefabs/Primitives/JUNK/PLANK_PRIM");
            AddBuiltinObject("Prop", "Ticket", "Prefabs/UI/JUNK/Ticket", "Prefabs/Primitives/JUNK/TICKET_PRIM");
            AddBuiltinObject("Prop", "Tire", "Prefabs/UI/JUNK/Tire", "Prefabs/Primitives/JUNK/TIRE_PRIM");
            AddBuiltinObject("Prop", "Tole", "Prefabs/UI/JUNK/Tole", "Prefabs/Primitives/JUNK/TOLE_PRIM");
            AddBuiltinObject("Prop", "Trunk Army", "Prefabs/UI/JUNK/TrunkArmy", "Prefabs/Primitives/JUNK/TRUNK_ARMY_PRIM");
            AddBuiltinObject("Prop", "Warn Cone", "Prefabs/UI/JUNK/WarnCone", "Prefabs/Primitives/JUNK/WARN_CONE_PRIM");
            AddBuiltinObject("Prop", "Washing Machine", "Prefabs/UI/JUNK/WashingMachine", "Prefabs/Primitives/JUNK/WASHING_MACHINE_PRIM");

            AddBuiltinObject("Vehicle", "Submarine", "Prefabs/UI/JUNK/Submarine", "Prefabs/Primitives/JUNK/SUBMARINE_PRIM");

            // Add user defined objects
            GlobalState.GeometryImporter.objectLoaded.AddListener((GameObject gobject) => InstantiateObject(gobject));
            rootDirectory = GlobalState.Settings.assetBankDirectory;
            ScanUserObjectDirectory(rootDirectory);
        }

        private void ScanUserObjectDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (var directory in directories)
                {
                    ScanUserObjectDirectory(directory);
                }

                string[] filenames = Directory.GetFiles(path, "*.fbx");
                foreach (var filename in filenames)
                {
                    AddUserObject(filename);
                }
            }
        }

        private void AddBuiltinObject(string tags, string name, string uiPath, string originalPath)
        {
            GameObject original = Instantiate(Resources.Load<GameObject>(originalPath));
            original.transform.parent = bank.transform;
            GameObject thumbnail = Instantiate(Resources.Load<GameObject>(uiPath));
            AddObject(tags, name, thumbnail, original, true);
        }

        private void AddUserObject(string filename)
        {
            GameObject thumbnail = Instantiate(Resources.Load<GameObject>("Prefabs/UI/AssetBankGenericItem"));
            string name = Path.GetFileNameWithoutExtension(filename).Replace('_', ' ');
            thumbnail.transform.Find("Canvas/Panel/Name").GetComponent<TextMeshProUGUI>().text = name;
            thumbnail.transform.Find("Canvas/Panel/Type").GetComponent<TextMeshProUGUI>().text = Path.GetExtension(filename).Substring(1);
            string tags = filename.Substring(rootDirectory.Length);
            tags = tags.Substring(0, tags.LastIndexOf('.'));
            AddObject(tags, filename, thumbnail, null, false);
        }

        private void AddObject(string tags, string name, GameObject thumbnail, GameObject original, bool hasRotation)
        {
            int uid = thumbnail.GetHashCode();
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            uiGrabber.uid = uid;
            uiGrabber.rotateOnHover = hasRotation;
            uiGrabber.onEnterUI3DObject.AddListener(OnUIObjectEnter);
            uiGrabber.onExitUI3DObject.AddListener(OnUIObjectExit);
            GameObject root = new GameObject("AssetBankItem");
            root.layer = LayerMask.NameToLayer("UI");
            AssetBankItem item = root.AddComponent<AssetBankItem>();
            item.uid = uid;
            item.assetName = name;
            item.thumbnail = thumbnail;
            item.original = original;
            item.thumbnail.transform.parent = root.transform;
            item.thumbnail.transform.localPosition += new Vector3(0, 0, -0.001f);
            item.AddTags(tags);
            foreach (var tag in item.tags)
            {
                this.tags.Add(tag);
            }
            item.uiItem = uiList.AddItem(item.transform);
            items.Add(uid, item);
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

        public bool OnInstantiateUIObject()
        {
            if (loadingAsset) { return true; }
            if (selectedItem == -1) { return false; }
            if (!items.TryGetValue(selectedItem, out AssetBankItem item)) { return false; }

            // If the original doesn't exist, load it
            GameObject original = item.original;
            if (null == original)
            {
                GlobalState.Instance.messageBox.ShowMessage("Loading asset, please wait...");
                loadingAsset = true;
                Selection.ClearSelection();
                item.imported = true;
                GlobalState.GeometryImporter.ImportObject(item.assetName, bank.transform);
                return true;
            }
            else
            {
                return InstantiateObject(original);
            }
        }

        private bool InstantiateObject(GameObject gobject)
        {
            if (!items.TryGetValue(selectedItem, out AssetBankItem item))
            {
                Debug.LogWarning($"Item {gobject.name} not found in Asset Bank (id: {selectedItem})");
                return false;
            }

            GameObject newObject;

            // Coming from an imported object
            if (item.imported)
            {
                item.original = gobject;
                newObject = SyncData.InstantiateFullHierarchyPrefab(SyncData.CreateFullHierarchyPrefab(gobject));
            }
            // Coming from a built-in object
            else
            {
                newObject = SyncData.InstantiatePrefab(SyncData.CreateInstance(gobject, SyncData.prefab));
            }

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
                selectedItem = -1;
            }

            // OnStartGrip must be called after object instantiation 
            if (item.imported)
            {
                OnStartGrip();
                loadingAsset = false;
                GlobalState.Instance.messageBox.SetVisible(false);
                return true;
            }
            return false;
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (!OnInstantiateUIObject())
                {
                    OnStartGrip();
                }
            }, OnEndGrip);
        }
    }
}
