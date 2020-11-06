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
        public GameObject prefab;     // the prefab to instantiate (in the __Prefabs__)
        public GameObject thumbnail;  // may be an image or 3D thumbnail

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

        private UIDynamicList uiList;

        private static int nextObjectId = 0;

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
            AddBuiltinObject("Rocks", "Rock A", "Prefabs/UI/ROCKS/Rock_A", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_A_PRIM");
            AddBuiltinObject("Rocks", "Rock B", "Prefabs/UI/ROCKS/Rock_B", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_B_PRIM");
            AddBuiltinObject("Rocks", "Rock C", "Prefabs/UI/ROCKS/Rock_C", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_C_PRIM");
            AddBuiltinObject("Rocks", "Rock D", "Prefabs/UI/ROCKS/Rock_D", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_D_PRIM");
            AddBuiltinObject("Rocks", "Rock E", "Prefabs/UI/ROCKS/Rock_E", "Prefabs/Primitives/ROCKS/ROCKS_ROUND_E_PRIM");

            // Add user defined objects
            GlobalState.GeometryImporter.objectLoaded.AddListener(InstantiateObject);
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
            GameObject thumbnail = Instantiate(Resources.Load<GameObject>(uiPath));
            AddObject(tags, name, thumbnail, original);
        }

        private void AddUserObject(string filename)
        {
            GameObject thumbnail = Instantiate(Resources.Load<GameObject>("Prefabs/UI/AssetBankGenericItem"));
            string name = Path.GetFileNameWithoutExtension(filename).Replace('_', ' ');
            thumbnail.transform.Find("Canvas/Panel/Name").GetComponent<TextMeshProUGUI>().text = name;
            thumbnail.transform.Find("Canvas/Panel/Type").GetComponent<TextMeshProUGUI>().text = Path.GetExtension(filename).Substring(1);
            string tags = filename.Substring(rootDirectory.Length);
            tags = tags.Substring(0, tags.LastIndexOf('.'));
            AddObject(tags, filename, thumbnail, null);
        }

        private void AddObject(string tags, string name, GameObject thumbnail, GameObject original)
        {
            UIGrabber uiGrabber = thumbnail.GetComponent<UIGrabber>();
            uiGrabber.SetAssetBankLinks(nextObjectId, original);
            uiGrabber.onEnterUI3DObject.AddListener(OnUIObjectEnter);
            uiGrabber.onExitUI3DObject.AddListener(OnUIObjectExit);
            GameObject root = new GameObject("AssetBankItem");
            root.layer = LayerMask.NameToLayer("UI");
            AssetBankItem item = root.AddComponent<AssetBankItem>();
            item.uid = nextObjectId;
            item.assetName = name;
            item.thumbnail = thumbnail;
            item.original = original;
            item.thumbnail.transform.parent = root.transform;
            item.AddTags(tags);
            foreach (var tag in item.tags)
            {
                this.tags.Add(tag);
            }
            item.uiItem = uiList.AddItem(item.transform);
            items.Add(nextObjectId, item);
            ++nextObjectId;
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
                GlobalState.GeometryImporter.ImportObject(item.assetName, bank.transform);
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
