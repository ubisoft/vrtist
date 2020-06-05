using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;
using UnityEngine.Events;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    //[RequireComponent(typeof(MeshFilter)),
    // RequireComponent(typeof(MeshRenderer))]
    public class UIDynamicList : UIElement
    {
        [SpaceHeader("Panel Shape Parmeters", 6, 0.5f, 0.5f, 0.5f)]
        [CentimeterFloat] public float margin = 0.02f;
        [CentimeterFloat] public float itemWidth = 0.1f;
        [CentimeterFloat] public float itemHeight = 0.1f;

        public UILabel pageCountLabel = null;

        public GameObjectHashChangedEvent onClicked = new GameObjectHashChangedEvent();

        // TMP visible
        [SpaceHeader("TMP Panel Layout Parameters", 6, 0.5f, 0.5f, 0.5f)]
        [SerializeField] private List<UIDynamicListItem> items = new List<UIDynamicListItem>();

        [SerializeField] [CentimeterFloat] private float innerTotalHeight = 0.0f;
        [SerializeField] [CentimeterFloat] private float innerTotalWidth = 0.0f;
        [SerializeField] private int maxNbItemCols = 0;
        [SerializeField] private int maxNbItemRows = 0;
        [SerializeField] [CentimeterFloat] private float itemVMargin = 0.0f;
        [SerializeField] [CentimeterFloat] private float itemHMargin = 0.0f;

        [SerializeField] private int nbItemsPerPage = 1;
        [SerializeField] private int nbItemsInLastPage = 0;
        [SerializeField] private int pagesCount = 0;
        [SerializeField] private int currentPage = 0;


        private bool needRebuild = false;

        private void OnValidate()
        {
            const float min_width = 0.01f;
            const float min_height = 0.01f;
            const float min_item_width = 0.01f;
            const float min_item_height = 0.01f;

            if (width < min_width)
                width = min_width;
            if (height < min_height)
                height = min_height;
            if (margin > width / 2.0f || margin > height / 2.0f)
                margin = Mathf.Min(width / 2.0f, height / 2.0f);
            if (itemWidth < min_item_width)
                itemWidth = min_item_width;
            if (itemHeight < min_item_height)
                itemHeight = min_item_height;

            // NOTE: relou, on peut plus rien toucher, tout le monde se censure.
            // dont let margin become too big.
            //if (width - 2 * margin < itemWidth)
            //    margin = (width - itemWidth) / 2.0f;
            //if (height - 2 * margin < itemHeight)
            //    margin = (height - itemHeight) / 2.0f;

            needRebuild = true;
        }

        private void Update()
        {
            if (needRebuild)
            {
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();

                UpdateItemPositions();
                UpdatePageCountLabel();

                needRebuild = false;
            }
        }

        //
        // API
        //

        public void AddItem(Transform t)
        {
            // full page, add one page
            if (nbItemsInLastPage == nbItemsPerPage)
                pagesCount++;
            currentPage = pagesCount == 0 ? 0 : pagesCount - 1;

            GameObject gObj = new GameObject(t.gameObject.name);
            UIDynamicListItem item = gObj.AddComponent<UIDynamicListItem>();
            item.Width = itemWidth;
            item.Height = itemHeight;
            item.Content = t;
            item.onObjectClickedEvent.AddListener(OnItemClicked);
            item.transform.parent = transform;
            items.Add(item);
            
            needRebuild = true;
        }

        public UIDynamicListItem DEBUG_GetLastItemTransform()
        {
            return items.Count > 0 ? items[items.Count - 1] : null;
        }

        public void DEBUG_Reset()
        {
            for (int i = items.Count - 1; i >= 0; --i)
            {
                UIDynamicListItem item = items[i];
                item.onObjectClickedEvent.RemoveAllListeners();
                items.RemoveAt(i);
                GameObject.DestroyImmediate(item.gameObject);
            }
            items.Clear(); // ceinture et bretelle.

            nbItemsPerPage = 1;
            nbItemsInLastPage = 0;
            pagesCount = 0;
            currentPage = 0;

            needRebuild = true;
        }

        public void RemoveItem(UIDynamicListItem item)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i] == item)
                {
                    item.onClickEvent.RemoveAllListeners();
                    items.RemoveAt(i);
                    GameObject.DestroyImmediate(item.gameObject);

                    if (nbItemsInLastPage == 1)
                    {
                        pagesCount--;
                        if (currentPage == pagesCount)
                            currentPage = pagesCount - 1;
                    }
                    
                    needRebuild = true;
                    return;
                }
            }
        }

        public void OnNextPage()
        {
            currentPage = (currentPage + 1) % pagesCount;
            needRebuild = true;
        }

        public void OnPreviousPage()
        {
            currentPage = (currentPage + pagesCount - 1) % pagesCount;
            needRebuild = true;
        }

        public void OnFirstPage()
        {
            currentPage = 0;
            needRebuild = true;
        }

        public void OnLastPage()
        {
            currentPage = pagesCount == 0 ? 0 : pagesCount - 1;
            needRebuild = true;
        }

        public void OnItemClicked(int gohash)
        {
            GameObject gObj = ToolsUIManager.Instance.GetUI3DObject(gohash);
            if (gObj != null)
            {
                UIDynamicListItem item = gObj.GetComponent<UIDynamicListItem>();
                if (item != null)
                {
                    Transform t = item.Content;
                    if (t != null)
                    {
                        int hash = t.gameObject.GetHashCode();
                        onClicked.Invoke(hash);
                    }
                }
            }
        }

        // reposition every item depending on the itemWidth/Height, width/height, margin, and current page.
        private void UpdateItemPositions()
        {
            //
            // Update items layout variables
            //
            innerTotalHeight = height - 2 * margin;
            innerTotalWidth = width - 2 * margin;
            maxNbItemCols = Mathf.FloorToInt(innerTotalWidth / itemWidth);
            if (maxNbItemCols * itemWidth + (maxNbItemCols - 1) * margin > innerTotalWidth) // add margins and check if it fits
                maxNbItemCols--;
            maxNbItemRows = Mathf.FloorToInt(innerTotalHeight / itemHeight);
            if (maxNbItemRows * itemHeight + (maxNbItemRows - 1) * margin > innerTotalHeight) // add margins and check if it fits
                maxNbItemRows--;
            itemVMargin = maxNbItemRows > 1 ? ((innerTotalHeight - ((float)maxNbItemRows * itemHeight)) / (maxNbItemRows - 1)) : 0.0f;
            itemHMargin = maxNbItemCols > 1 ? ((innerTotalWidth - ((float)maxNbItemCols * itemWidth)) / (maxNbItemCols - 1)) : 0.0f;
            nbItemsPerPage = maxNbItemCols * maxNbItemRows;
            
            pagesCount = ((items.Count > 0) && (nbItemsPerPage > 0)) ? (items.Count - 1) / nbItemsPerPage + 1: 0;

            nbItemsInLastPage = pagesCount == 0 ? 0 : Math.Min(items.Count - nbItemsPerPage * (pagesCount - 1), nbItemsPerPage);

            float itemWidth2 = (float)itemWidth / 2.0f;
            float itemHeight2 = (float)itemHeight / 2.0f;

            //
            // Update items visibility, position, scale and collider.
            //
            for (int i = 0; i < items.Count; ++i)
            {
                UIDynamicListItem item = items[i];
                if (nbItemsPerPage > 0)
                {
                    bool isInCurrentPage = ((i / nbItemsPerPage) == currentPage);
                    item.gameObject.SetActive(isInCurrentPage);
                    if (isInCurrentPage)
                    {
                        int idxInCurrentPage = i % nbItemsPerPage;
                        int row = idxInCurrentPage / maxNbItemCols;
                        int col = idxInCurrentPage % maxNbItemCols;
                        item.transform.localPosition = new Vector3(
                            col == 0 ? margin + itemWidth2 : margin + itemWidth2 + col * (itemWidth + itemHMargin),
                            row == 0 ? -margin - itemHeight2 : -margin - itemHeight2 - row * (itemHeight + itemVMargin),
                            0.0f);
                        item.transform.localRotation = Quaternion.identity;
                        item.Width = itemWidth;
                        item.Height = itemHeight;
                        item.AdaptContent();
                    }
                }
                else
                {
                    item.gameObject.SetActive(false);
                }
            }
        }

        private void UpdatePageCountLabel()
        { 
            if (pageCountLabel != null)
            {
                Canvas canvas = pageCountLabel.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    Text text = canvas.gameObject.GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        text.text = pagesCount == 0 ? "0/0" : $"{currentPage + 1}/{pagesCount}";
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // TODO: draw boxes for the potential items

            Vector3 labelPosition = transform.TransformPoint(new Vector3(margin, -margin, 0.0f));

            Vector3 posTopLeft = transform.TransformPoint(new Vector3(margin, -margin, 0));
            Vector3 posTopRight = transform.TransformPoint(new Vector3(width - margin, -margin, 0));
            Vector3 posBottomLeft = transform.TransformPoint(new Vector3(+margin, -height + margin, 0));
            Vector3 posBottomRight = transform.TransformPoint(new Vector3(width - margin, -height + margin, 0));

            Gizmos.color = Color.white;
            Gizmos.DrawLine(posTopLeft, posTopRight);
            Gizmos.DrawLine(posTopRight, posBottomRight);
            Gizmos.DrawLine(posBottomRight, posBottomLeft);
            Gizmos.DrawLine(posBottomLeft, posTopLeft);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPosition, gameObject.name);
#endif
            for (int col = 0; col < maxNbItemCols; ++col)
            {
                // TODO: compute row invariant values.
                for (int row = 0; row < maxNbItemRows; ++row)
                {
                    Vector3 tl = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth),             -margin - row * (itemVMargin + itemHeight), 0));
                    Vector3 tr = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth) + itemWidth, -margin - row * (itemVMargin + itemHeight), 0));
                    Vector3 bl = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth),             -margin - row * (itemVMargin + itemHeight) - itemHeight, 0));
                    Vector3 br = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth) + itemWidth, -margin - row * (itemVMargin + itemHeight) - itemHeight, 0));

                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(tl, tr);
                    Gizmos.DrawLine(tr, br);
                    Gizmos.DrawLine(br, bl);
                    Gizmos.DrawLine(bl, tl);
                }
            }
        }

        public static void Create(
            string listName,
            Transform parent,
            Vector3 relativeLocation,
            float width,
            float height,
            float margin,
            float item_width,
            float item_height)
        {
            GameObject go = new GameObject(listName);

            // Find the anchor of the parent if it is a UIElement
            Vector3 parentAnchor = Vector3.zero;
            if (parent)
            {
                UIElement elem = parent.gameObject.GetComponent<UIElement>();
                if (elem)
                {
                    parentAnchor = elem.Anchor;
                }
            }

            UIDynamicList uiDynamicList = go.AddComponent<UIDynamicList>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
            uiDynamicList.relativeLocation = relativeLocation;
            uiDynamicList.transform.parent = parent;
            uiDynamicList.transform.localPosition = parentAnchor + relativeLocation;
            uiDynamicList.transform.localRotation = Quaternion.identity;
            uiDynamicList.transform.localScale = Vector3.one;
            uiDynamicList.width = width;
            uiDynamicList.height = height;
            uiDynamicList.margin = margin;
            uiDynamicList.itemWidth = item_width;
            uiDynamicList.itemHeight = item_height;
        }
    }
}
