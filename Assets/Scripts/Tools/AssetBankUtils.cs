using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using TMPro;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VRtist
{
    public class AssetBankItem : ListItemContent
    {
        public GameObject thumbnail;
        public AssetBankItemData data;
        public UIDynamicListItem uiItem;
    }

    public class AssetBankItemData
    {
        public int uid;               // uid
        public string assetName;      // item name or filename
        public AssetBankUtils.ThumbnailType thumbnailType;
        public string thumbnailPath;
        public HashSet<string> tags = new HashSet<string>();  // list of tags for filtering
        public GameObject prefab;     // original object (loaded from disk or Unity.Resources)
        public bool builtin = false;
        public Func<AssetBankItemData, Task<GameObject>> importFunction = null;
        public bool skipInstantiation = false;
        public bool imported = false;

        public void AddTags(string path)
        {
            path = path.Replace("\\", "/");
            foreach (var tag in path.Split(new char[] { '/', '-', '_', ' ' }))
            {
                tags.Add(tag.ToLower());
            }
        }
    }

    public static class AssetBankUtils
    {
        public enum ThumbnailType
        {
            Object,
            Image,
            LazyImage,
            Text
        }

        private const string ASSET_BANK_NAME = "__VRtist_Asset_Bank__";

        public static bool loadingAsset = false;
        public static GameObject bank;
        private static readonly Dictionary<int, AssetBankItemData> items = new Dictionary<int, AssetBankItemData>();   // uid -> asset bank item data
        private static readonly HashSet<string> tagList = new HashSet<string>();

        private static GameObject textThumbnailPrefab;
        private static GameObject imageThumbnailPrefab;
        public static Quaternion thumbnailRotation = Quaternion.Euler(25f, -35f, 0f);

        private static int nextUID = 0;
        public static int NextUID
        {
            get { return ++nextUID; }
        }

        public static void LoadAssets()
        {
            if (items.Count != 0) { return; }

            // Create our storage for loaded objects
            bank = new GameObject(ASSET_BANK_NAME);
            bank.SetActive(false);

            // Add our predifined objects
            AddBuiltinAssets();

            // Add user defined objects
            GlobalState.Instance.StartCoroutine(ScanDirectory(GlobalState.Settings.assetBankDirectory, () =>
            {
                SceneManager.ListImportableObjects();
            }));
        }

        public static void ScanAssetBank()
        {
            // Remove all user assets
            foreach (var item in items.Values)
            {
                if (!item.builtin)
                {
                    if (item.prefab) { GameObject.Destroy(item.prefab); }
                    items.Remove(item.uid);
                }
            }

            // Scan user directory
            GlobalState.Instance.StartCoroutine(ScanDirectory(GlobalState.Settings.assetBankDirectory));

            // Rebuild tags list
            tagList.Clear();
            foreach (var item in items.Values)
            {
                foreach (var tag in item.tags)
                {
                    tagList.Add(tag);
                }
            }
        }

        private static void AddBuiltinAssets()
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

        private static IEnumerator ScanDirectory(string path, Action onEndScan = null)
        {
            if (Directory.Exists(path))
            {
                string[] directories = Directory.GetDirectories(path);
                foreach (var directory in directories)
                {
                    GlobalState.Instance.StartCoroutine(ScanDirectory(directory));
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

        private static void AddBuiltinAsset(string tags, string name, string uiPath, string prefabPath)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            AddAsset(name, ThumbnailType.Object, uiPath, prefab, tags, builtin: true);
        }

        private static void AddFileAsset(string filename)
        {
            string name = Path.GetFileNameWithoutExtension(filename).Replace('_', ' ');
            string tags = filename.Substring(GlobalState.Settings.assetBankDirectory.Length);
            tags = tags.Substring(0, tags.LastIndexOf('.'));  // remove file extension
            AddAsset(filename, ThumbnailType.Text, name, null, tags, importFunction: ImportObjectAsync);
        }

        public static AssetBankItemData AddAsset(
            string name,
            ThumbnailType thumbnailType,
            string thumbnailPath,
            GameObject prefab,
            string tags,
            Func<AssetBankItemData, Task<GameObject>> importFunction = null,
            bool builtin = false,
            bool skipInstantiation = false)
        {
            int uid = NextUID;
            AssetBankItemData data = new AssetBankItemData
            {
                uid = uid,
                assetName = name,
                thumbnailType = thumbnailType,
                thumbnailPath = thumbnailPath,
                prefab = prefab,
                builtin = builtin,
                importFunction = importFunction,
                skipInstantiation = skipInstantiation
            };
            data.AddTags(tags);
            foreach (var tag in data.tags)
            {
                tagList.Add(tag);
            }
            items.Add(uid, data);
            return data;
        }

        public static bool TryGetItem(int uid, out AssetBankItemData item)
        {
            return items.TryGetValue(uid, out item);
        }

        public static void PopulateUIList(UIDynamicList uiList, UnityAction<int> onEnter, UnityAction<int> onExit)
        {
            foreach (AssetBankItemData data in items.Values)
            {
                // Create thumbnail
                GameObject thumbnail = CreateThumbnail(data);
                UIGrabber uiGrabber = data.thumbnailType switch
                {
                    ThumbnailType.Object => UIGrabber.Create3DGrabber(thumbnail),
                    ThumbnailType.Image => UIGrabber.CreateImageGrabber(thumbnail),
                    ThumbnailType.LazyImage => UIGrabber.CreateLazyImageGrabber(thumbnail, data.thumbnailPath),
                    ThumbnailType.Text => UIGrabber.CreateTextGrabber(thumbnail),
                    _ => throw new NotImplementedException()
                };
                uiGrabber.uid = data.uid;
                uiGrabber.onEnterUI3DObject.AddListener(onEnter);
                uiGrabber.onExitUI3DObject.AddListener(onExit);

                // Create item
                GameObject root = new GameObject(data.assetName)
                {
                    layer = LayerMask.NameToLayer("CameraHidden")
                };
                AssetBankItem item = root.AddComponent<AssetBankItem>();
                item.thumbnail = thumbnail;
                item.thumbnail.transform.parent = root.transform;
                item.thumbnail.transform.localPosition += new Vector3(0, 0, -0.001f);
                item.data = data;

                // Add it to the list
                uiList.AddItem(item.transform);
            }
        }

        public static GameObject CreateThumbnail(AssetBankItemData data)
        {
            if (null == textThumbnailPrefab)
            {
                textThumbnailPrefab = Resources.Load<GameObject>("Prefabs/UI/AssetBankGenericItem");
                imageThumbnailPrefab = Resources.Load<GameObject>("Prefabs/UI/AssetBankImageItem");
            }

            GameObject thumbnail = data.thumbnailType switch
            {
                ThumbnailType.Object => Create3DThumbnail(data.thumbnailPath),
                ThumbnailType.Text => CreateTextThumbnail(data.thumbnailPath),
                ThumbnailType.Image => CreateImageThumbnail(data.thumbnailPath),
                ThumbnailType.LazyImage => CreateLazyImageThumbnail(data.thumbnailPath),
                _ => throw new NotImplementedException()
            };

            return thumbnail;
        }

        private static GameObject CreateTextThumbnail(string text)
        {
            GameObject thumbnail = GameObject.Instantiate(textThumbnailPrefab);
            thumbnail.transform.Find("Canvas/Panel/Name").GetComponent<TextMeshProUGUI>().text = text;
            return thumbnail;
        }

        private static GameObject CreateImageThumbnail(string thumbnailPath)
        {
            Sprite image = Resources.Load<Sprite>(thumbnailPath);
            GameObject thumbnail = GameObject.Instantiate(imageThumbnailPrefab);
            thumbnail.transform.Find("Canvas/Panel/Image").GetComponent<Image>().sprite = image;
            return thumbnail;
        }

        public static GameObject CreateLazyImageThumbnail(string path)
        {
            GameObject thumbnail = GameObject.Instantiate(imageThumbnailPrefab);
            return thumbnail;
        }

        public static GameObject Create3DThumbnail(string thumbnailPath)
        {
            GameObject thumbnail = GameObject.Instantiate(Resources.Load<GameObject>(thumbnailPath));
            thumbnail.transform.localRotation = thumbnailRotation;

            MeshRenderer[] meshRenderers = thumbnail.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return thumbnail;
        }

        public static async Task LoadPrefab(int uid)
        {
            if (loadingAsset) { return; }
            if (uid == -1) { return; }
            if (!items.TryGetValue(uid, out AssetBankItemData data)) { return; }

            if (null == data.prefab)
            {
                loadingAsset = true;
                GlobalState.Instance.messageBox.ShowMessage("Loading asset, please wait...");
                Selection.ClearSelection();
                data.prefab = await data.importFunction(data);
                Utils.NameObjectMeshes(data.prefab);
                data.imported = true;
                GlobalState.Instance.messageBox.SetVisible(false);
                loadingAsset = false;
            }
        }

        private static Task<GameObject> ImportObjectAsync(AssetBankItemData data)
        {
            return GlobalState.GeometryImporter.ImportObjectAsync(data.assetName, bank.transform);
        }
    }
}
