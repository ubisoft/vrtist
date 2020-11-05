using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AssetBankItem
    {
        public int uid;               // uid
        public string name;           // item name or filename
        public GameObject original;   // original object (loaded from disk or Unity.Resources)
        public GameObject prefab;     // the prefab to instantiate (in the __Prefabs__)
        public GameObject thumbnail;  // may be an image or 3D thumbnail
    }

    public class Category
    {
        public string name;
        List<AssetBankItem> items = new List<AssetBankItem>();

        public void AddItem(AssetBankItem item)
        {
            items.Add(item);
        }
    }

    public class AssetBank : SelectorBase
    {
        [Header("Parameters")]
        public Transform container;
        public int maxPages = 3;
        private bool useDefaultInstantiationScale = false;
        private Transform[] pages = null;
        private int current_page = 0;

        private string rootDirectory;
        private Dictionary<string, Category> categories = new Dictionary<string, Category>();  // category's name -> category
        private Dictionary<int, AssetBankItem> items = new Dictionary<int, AssetBankItem>();   // uid -> asset bank item
        private GameObject bank;  // contains all prefabs from the asset bank
        private int selectedItem = -1;

        private static int nextObjectId = 0;

        void Start()
        {
            Init();

            // Create our storage for loaded objects
            bank = new GameObject("__VRtist_Asset_Bank__");
            bank.SetActive(false);

            // Add our predifined objects
            // TODO: parse them from Resources folder at editor time then create a db of those resources (scriptableObject)
            // available at runtime to finally parse that db
            AddBuiltinObject("Rocks", "Rock A", "Prefabs/UI/ROCKS/Rock_A", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_A_PRIM");
            AddBuiltinObject("Rocks", "Rock B", "Prefabs/UI/ROCKS/Rock_B", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_B_PRIM");
            AddBuiltinObject("Rocks", "Rock C", "Prefabs/UI/ROCKS/Rock_C", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_C_PRIM");
            AddBuiltinObject("Rocks", "Rock D", "Prefabs/UI/ROCKS/Rock_D", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_D_PRIM");
            AddBuiltinObject("Rocks", "Rock E", "Prefabs/UI/ROCKS/Rock_E", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_E_PRIM");
            AddBuiltinObject("Rocks", "Rock F", "Prefabs/UI/ROCKS/Rock_F", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_F_PRIM");
            AddBuiltinObject("Rocks", "Rock G", "Prefabs/UI/ROCKS/Rock_G", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_G_PRIM");
            AddBuiltinObject("Rocks", "Rock J", "Prefabs/UI/ROCKS/Rock_J", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_J_PRIM");
            AddBuiltinObject("Rocks", "Rock K", "Prefabs/UI/ROCKS/Rock_K", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_K_PRIM");

            // Add user defined objects
            GlobalState.GeometryImporter.objectLoaded.AddListener(InstantiateObject);
            rootDirectory = GlobalState.Settings.assetBankDirectory;
            ScanUserObjectDirectory();
        }

        private void ScanUserObjectDirectory()
        {
            // Manage only one sub-folder level, each sub-folder defining a category of assets
            if (Directory.Exists(rootDirectory))
            {
                string[] directories = Directory.GetDirectories(rootDirectory);
                foreach (var path in directories)
                {
                    ScanUserSubDirectory(path, path);
                }
                // Put all objects in the root directory into an "Other" section
                ScanUserSubDirectory(rootDirectory, "Other");
            }
        }

        private void ScanUserSubDirectory(string directory, string categoryName)
        {
            categoryName = Path.GetFileName(categoryName);

            // Search for FBX only in the directory
            string[] filenames = Directory.GetFiles(directory, "*.fbx");
            foreach (string filename in filenames)
            {
                AddUserObject(categoryName, filename);
            }
        }

        private void AddBuiltinObject(string categoryName, string name, string uiPath, string originalPath)
        {
            GameObject original = Resources.Load<GameObject>(originalPath);
            GameObject thumbnail = Resources.Load<GameObject>(uiPath);
            AddObject(categoryName, name, thumbnail, original);
        }

        private void AddUserObject(string categoryName, string filename)
        {
            GameObject thumbnail = Resources.Load<GameObject>("Prefabs/UI/AssetBankGenericItem");
            filename = Path.GetFileName(filename);
            filename = filename.Replace('_', ' ');
            thumbnail.transform.Find("Canvas/Name").GetComponent<TextMeshProUGUI>().text = filename;
            AddObject(categoryName, filename, thumbnail, null);
        }

        private void AddObject(string categoryName, string name, GameObject thumbnail, GameObject original)
        {
            if (!categories.TryGetValue(categoryName, out Category category))
            {
                category = new Category { name = categoryName };
                categories.Add(categoryName, category);
            }
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            uiGrabber.prefab = original;
            uiGrabber.SetAssetBankLinks(nextObjectId, original);
            uiGrabber.onEnterUI3DObject.AddListener(OnUIObjectEnter);
            uiGrabber.onExitUI3DObject.AddListener(OnUIObjectExit);
            AssetBankItem item = new AssetBankItem { uid = nextObjectId, name = name, thumbnail = thumbnail, original = original };
            category.AddItem(item);
            items.Add(nextObjectId, item);
            ++nextObjectId;
        }

        public void OnPrevPage()
        {
            pages[current_page].gameObject.SetActive(false);
            current_page = (current_page + maxPages - 1) % maxPages;
            pages[current_page].gameObject.SetActive(true);
        }

        public void OnNextPage()
        {
            pages[current_page].gameObject.SetActive(false);
            current_page = (current_page + 1) % maxPages;
            pages[current_page].gameObject.SetActive(true);
        }

        //public void SetGrabbedObject(GameObject gObject)
        //{
        //    UIObject = gObject;
        //}

        public override void OnUIObjectEnter(int uid)
        {
            selectedItem = uid;
        }

        public override void OnUIObjectExit(int uid)
        {
            selectedItem = -1;
        }

        public void OnUseDefaultScale(bool value)
        {
            useDefaultInstantiationScale = value;
        }

        public void OnInstantiateUIObject()
        {
            if (selectedItem == -1) { return; }

            if (!items.TryGetValue(selectedItem, out AssetBankItem item)) { return; }

            // If the original doesn't exist, load it
            GameObject original = item.original;
            if (null == original)
            {
                GlobalState.GeometryImporter.ImportObject(item.name, bank.transform);
            }
            else
            {
                InstantiateObject(original);
            }
        }

        private void InstantiateObject(GameObject gobject)
        {
            AssetBankItem item = items[selectedItem];

            // Coming from an imported object, set the original
            if (null == item.original)
            {
                item.original = gobject;
            }

            // If it's the first time we instantiate it, first create a runtime prefab
            if (null == item.prefab)
            {
                item.prefab = SyncData.CreateInstance(gobject, SyncData.prefab);
            }

            // Instantiate the runtime prefab
            GameObject newObject = SyncData.InstantiatePrefab(item.prefab);

            MeshFilter meshFilter = newObject.GetComponentInChildren<MeshFilter>();
            if (null != meshFilter)
            {
                meshFilter.mesh.name = newObject.name;
            }

            Matrix4x4 matrix = container.worldToLocalMatrix * mouthpiece.localToWorldMatrix;
            if (!useDefaultInstantiationScale)
            {
                SyncData.SetTransform(newObject.name, matrix * Matrix4x4.Scale(10f * gobject.transform.localScale));
            }
            else
            {
                Maths.DecomposeMatrix(matrix, out Vector3 t, out _, out _);
                // Cancel scale
                Quaternion quarterRotation = Quaternion.Euler(new Vector3(-90f, 180f, 0));
                SyncData.SetTransform(newObject.name, Matrix4x4.TRS(t, Quaternion.identity, new Vector3(10, 10, 10)) * Matrix4x4.Rotate(quarterRotation));
            }

            CommandGroup group = new CommandGroup("Instantiate Bank Object");
            try
            {
                ClearSelection();
                new CommandAddGameObject(newObject).Submit();
                AddToSelection(newObject);
                Selection.SetHoveredObject(newObject);
            }
            finally
            {
                group.Submit();
                selectedItem = -1;
            }
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                OnInstantiateUIObject();
                OnStartGrip();
            }, OnEndGrip);
        }
    }
}
