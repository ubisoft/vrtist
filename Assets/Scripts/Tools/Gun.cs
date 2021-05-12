using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace VRtist
{
    public class GunPrefabItem : ListItemContent
    {
        [HideInInspector] public UIDynamicListItem dlItem;

        GameObject thumbnail;
        GameObject prefab;

        UIPanel panel;
        UIButton deleteButton;
        public int assetBankId;

        public void SetListItem(UIDynamicListItem listItem)
        {
            dlItem = listItem;
            deleteButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
        }

        public void AddListener(UnityAction deleteAction)
        {
            deleteButton.onReleaseEvent.AddListener(deleteAction);
        }

        public static GunPrefabItem Create(AssetBankItemData data)
        {
            GameObject root = new GameObject("GunPrefabItem");
            GunPrefabItem item = root.AddComponent<GunPrefabItem>();
            root.layer = LayerMask.NameToLayer("CameraHidden");

            //
            // Background Panel
            //
            UIPanel panel = UIPanel.Create(new UIPanel.CreatePanelParams
            {
                parent = root.transform,
                widgetName = "GunPrefabPreviewBackgroundPanel",
                relativeLocation = new Vector3(0.01f, -0.01f, -UIPanel.default_element_thickness),
                width = 0.10f,
                height = 0.10f,
                margin = 0.005f
            });
            panel.SetLightLayer(3);

            //
            // Thumbnail & prefab
            //
            item.assetBankId = data.uid;
            item.prefab = data.prefab;
            item.thumbnail = AssetBankUtils.CreateThumbnail(data);
            if (item.thumbnail.TryGetComponent(out UIGrabber uiGrabber))
            {
                Destroy(uiGrabber);
            }
            item.thumbnail.transform.parent = root.transform;

            //
            // Delete Button
            //
            UIButton deleteButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = panel.transform,
                widgetName = "DeleteButton",
                relativeLocation = new Vector3(0.07f, -0.07f, -UIButton.default_thickness),
                width = 0.03f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("trash"),
                buttonContent = UIButton.ButtonContent.ImageOnly,
                margin = 0.001f,
            });
            deleteButton.SetLightLayer(3);

            item.deleteButton = deleteButton;
            item.panel = panel;

            return item;
        }
    }

    public class Gun : ToolBase
    {
        float fireRate = 5f;
        float power = 10f;
        float objectScale = 1f;
        List<GameObject> prefabs = new List<GameObject>();

        private float prevTime;
        CommandGroup group;

        UIDynamicList prefabList;
        UIDynamicList bankList;
        int selectedItem;

        void Start()
        {
            prefabList = panel.Find("Prefabs/List").GetComponent<UIDynamicList>();
            bankList = panel.Find("BankPanel/List").GetComponent<UIDynamicList>();

            AssetBankUtils.LoadAssets();
            AssetBankUtils.PopulateUIList(bankList, OnUIObjectEnter, OnUIObjectExit);
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

        protected override void OnDisable()
        {
            if (null != group)
            {
                group.Submit();
                group = null;
            }
        }

        protected override void DoUpdate()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton,
                () =>
                {
                    group = new CommandGroup("Gun");
                },
                () =>
                {
                    group.Submit();
                    group = null;
                }
            );

            bool triggered = VRInput.GetValue(VRInput.primaryController, CommonUsages.triggerButton);
            if (triggered && prefabs.Count > 0)
            {
                if (Time.time - prevTime > 1f / fireRate)
                {
                    int prefabIndex = UnityEngine.Random.Range(0, prefabs.Count);
                    GameObject spawned = Instantiate(prefabs[prefabIndex]);
                    ThrowedObject throwed = spawned.AddComponent<ThrowedObject>();
                    throwed.AddForce(transform.forward * power);
                    throwed.SetScale(objectScale);
                    new CommandAddGameObject(spawned).Submit();
                    Matrix4x4 matrix = SceneManager.RightHanded.worldToLocalMatrix * mouthpiece.localToWorldMatrix;
                    Maths.DecomposeMatrix(matrix, out Vector3 t, out _, out _);
                    Vector3 scale = Vector3.one;
                    SceneManager.SetObjectMatrix(spawned, Matrix4x4.TRS(t, Quaternion.identity, scale));
                    prevTime = Time.time;
                }
            }
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.triggerButton, () =>
            {

            }, () =>
            {
                // Manage click on an asset bank item
                if (selectedItem != -1)
                {
                    if (AssetBankUtils.TryGetItem(selectedItem, out AssetBankItemData data))
                    {
                        AddPrefab(data);
                    }
                }
            });
        }

        public void SetFireRate(float value)
        {
            fireRate = value;
        }

        public void SetPower(float value)
        {
            power = value;
        }

        public void SetObjectScale(float value)
        {
            objectScale = value;
        }

        public async void AddPrefab(AssetBankItemData data)
        {
            GunPrefabItem gunItem = GunPrefabItem.Create(data);
            gunItem.AddListener(OnDeletePrefab);
            UIDynamicListItem dlItem = prefabList.AddItem(gunItem.transform);
            dlItem.UseColliderForUI = false; // dont use the default global collider, sub-widget will catch UI events and propagate them.
            gunItem.transform.localScale = Vector3.one; // Items are hidden (scale 0) while they are not added into a list, so activate the item here.
            gunItem.SetListItem(dlItem); // link i

            await AssetBankUtils.LoadPrefab(selectedItem);
            prefabs.Add(data.prefab);
        }

        public void ClearPrefabs()
        {
            prefabs.Clear();
            prefabList.Clear();
        }

        public void OnDeletePrefab()
        {
            prefabs.RemoveAt(prefabList.CurrentIndex);

            var currentDLItem = prefabList.GetItems()[prefabList.CurrentIndex];
            prefabList.RemoveItem(currentDLItem);

            prefabList.CurrentIndex = -1;
            RebuildPrefabList();
        }

        private void RebuildPrefabList()
        {
            List<int> uids = new List<int>();
            foreach (var uiItem in prefabList.GetItems())
            {
                uids.Add(uiItem.Content.GetComponent<GunPrefabItem>().assetBankId);
            }

            ClearPrefabs();
            foreach (int uid in uids)
            {
                if (AssetBankUtils.TryGetItem(uid, out AssetBankItemData data))
                {
                    AddPrefab(data);
                }
            }
        }
    }
}
