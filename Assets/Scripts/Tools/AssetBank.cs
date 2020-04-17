using System;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class AssetBank : SelectorBase
    {
        [Header("Parameters")]
        public Transform container;
        public int maxPages = 3;
        private bool useDefaultInstantiationScale = false;
        private Transform[] pages = null;
        private int current_page = 0;

        void Start()
        {
            Init();

            panel.transform.Find("Primitives");

            Transform primitives = panel.transform.Find("Primitives");
            if (primitives == null)
            {
                Debug.LogWarning("AssetBankPanel needs an object named \"Primitives\"");
            }
            else
            {
                pages = new Transform[maxPages];
                for (int i = 0; i < maxPages; ++i)
                {
                    string page_name = "Page_" + i;
                    pages[i] = primitives.Find(page_name);
                    pages[i].gameObject.SetActive(false);
                }

                current_page = 0;
                pages[current_page].gameObject.SetActive(true);
            }
        }

        public void OnPrevPage()
        {
            pages[current_page].gameObject.SetActive(false);
            current_page = (current_page + maxPages - 1 ) % maxPages;
            pages[current_page].gameObject.SetActive(true);
        }

        public void OnNextPage()
        {
            pages[current_page].gameObject.SetActive(false);
            current_page = (current_page + 1) % maxPages;
            pages[current_page].gameObject.SetActive(true);
        }

        private GameObject UIObject = null;
        public void SetGrabbedObject(GameObject gObject)
        {
            UIObject = gObject;
        }

        public override void OnUIObjectEnter(int gohash)
        {
            UIObject = ToolsUIManager.Instance.GetUI3DObject(gohash);
        }

        public override void OnUIObjectExit(int gohash)
        {
            UIObject = null;
        }

        public void OnUseDefaultScale(bool value)
        {
            useDefaultInstantiationScale = value;
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (null != UIObject)
                {
                    GameObject newObject = SyncData.InstantiatePrefab(Utils.CreateInstance(UIObject, SyncData.prefab));
                    
                    if (useDefaultInstantiationScale)
                    {
                        Matrix4x4 matrix = container.worldToLocalMatrix * selectorBrush.localToWorldMatrix * Matrix4x4.Scale(new Vector3(100f, 100f, 100f));

                        Vector3 t, s;
                        Quaternion r;
                        Maths.DecomposeMatrix(matrix, out t, out r, out s);
                        SyncData.SetTransform(newObject.name, Matrix4x4.TRS(t, Quaternion.identity, new Vector3(10, 10, 10)));
                    }
                    else
                    {
                        Matrix4x4 matrix = container.worldToLocalMatrix * selectorBrush.localToWorldMatrix * Matrix4x4.Scale(10f * UIObject.transform.localScale);
                        SyncData.SetTransform(newObject.name, matrix);
                    }

                    new CommandAddGameObject(newObject).Submit();

                    ClearSelection();
                    AddToSelection(newObject);
                }
                OnStartGrip();
            }, OnEndGrip);
        }
    }
}
