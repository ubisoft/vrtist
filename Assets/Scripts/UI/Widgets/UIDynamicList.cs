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
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    [ExecuteInEditMode]
    [SelectionBase]
    //[RequireComponent(typeof(MeshFilter)),
    // RequireComponent(typeof(MeshRenderer))]
    public class UIDynamicList : UIElement
    {
        public event EventHandler<IndexedGameObjectArgs> ItemClickedEvent;

        [SpaceHeader("Panel Shape Parmeters", 6, 0.5f, 0.5f, 0.5f)]
        [CentimeterFloat] public float margin = 0.02f;
        [CentimeterFloat] public float itemWidth = 0.1f;
        [CentimeterFloat] public float itemHeight = 0.1f;
        [CentimeterFloat] public float itemDepth = 0.1f;

        public bool autoResizeContent = true;
        public bool autoCenterContent = true;
        public bool focusItemOnAdd = true;

        public UILabel pageCountLabel = null;

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
        public int pagesCount = 0;
        public int currentPage = 0;

        private string filter = null;

        private int currentIndex = -1;
        public int CurrentIndex
        {
            get { return currentIndex; }
            set { currentIndex = value; UpdateSelection(); }
        }

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

            NeedsRebuild = true;
        }

        private void Update()
        {
            if (NeedsRebuild)
            {
                UpdateLocalPosition();
                UpdateAnchor();
                UpdateChildren();

                UpdateItemPositions();
                UpdatePageCountLabel();

                //ResetColor();

                NeedsRebuild = false;
            }
        }

        //
        // API
        //

        public List<UIDynamicListItem> GetItems()
        {
            return items;
        }

        public UIDynamicListItem AddItem(Transform t)
        {
            // full page, add one page
            if (nbItemsInLastPage == nbItemsPerPage)
                pagesCount++;
            if (focusItemOnAdd)
                currentPage = pagesCount == 0 ? 0 : pagesCount - 1;

            GameObject gObj = new GameObject("List Item");
            gObj.SetActive(false);  // gObj may be on another page, so by default not active
            UIDynamicListItem item = gObj.AddComponent<UIDynamicListItem>();
            gObj.tag = "UICollider";
            gObj.layer = LayerMask.NameToLayer("CameraHidden");
            item.list = this;
            item.autoResizeContent = autoResizeContent;
            item.autoCenterContent = autoCenterContent;
            item.Width = itemWidth;
            item.Height = itemHeight;
            item.Depth = itemDepth;
            item.Content = t;
            gObj.transform.parent = transform;
            gObj.transform.localPosition = Vector3.zero;
            gObj.transform.localRotation = Quaternion.identity;
            gObj.transform.localScale = Vector3.one;
            items.Add(item);

            NeedsRebuild = true;

            return item;
        }

        public UIDynamicListItem DEBUG_GetLastItemTransform()
        {
            return items.Count > 0 ? items[items.Count - 1] : null;
        }

        public void DEBUG_SetSecondItemAsLastClicked()
        {
            currentIndex = 1;
        }

        public void Clear()
        {
            for (int i = items.Count - 1; i >= 0; --i)
            {
                UIDynamicListItem item = items[i];
                if (item != null)
                {
                    //items.RemoveAt(i);
                    GameObject.Destroy(item.gameObject);
                }
            }
            items.Clear(); // ceinture et bretelle.

            nbItemsPerPage = 1;
            nbItemsInLastPage = 0;
            pagesCount = 0;
            currentPage = 0;

            NeedsRebuild = true;
        }

        public void RemoveItem(UIDynamicListItem item)
        {
            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i] == item)
                {
                    items.RemoveAt(i);
                    GameObject.Destroy(item.gameObject);

                    if (nbItemsInLastPage == 1)
                    {
                        pagesCount--;
                        if (currentPage == pagesCount)
                            currentPage = pagesCount - 1;
                    }

                    NeedsRebuild = true;
                    return;
                }
            }
        }

        public void FireItem(Transform t)
        {
            // last clicked
            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i].Content == t)
                    CurrentIndex = i;
            }

            IndexedGameObjectArgs args = new IndexedGameObjectArgs { gobject = t.gameObject, index = currentIndex };
            ItemClickedEvent?.Invoke(null, args);
        }

        public string GetFilter()
        {
            return filter;
        }

        public void OnFilterList(string filter)
        {
            this.filter = filter?.ToLower();
            currentPage = 0;
            NeedsRebuild = true;
        }

        public void OnNextPage()
        {
            currentPage = (pagesCount != 0) ? (currentPage + 1) % pagesCount : 0;
            NeedsRebuild = true;
        }

        public void OnPreviousPage()
        {
            currentPage = (pagesCount != 0) ? (currentPage + pagesCount - 1) % pagesCount : 0;
            NeedsRebuild = true;
        }

        public void OnFirstPage()
        {
            currentPage = 0;
            NeedsRebuild = true;
        }

        public void OnLastPage()
        {
            currentPage = pagesCount == 0 ? 0 : pagesCount - 1;
            NeedsRebuild = true;
        }

        public void OnCurrentItemUp()
        {
            if (items.Count > 1
                && currentIndex > 0
                && currentIndex < items.Count)
            {
                // swap with previous item.
                var tmp = items[currentIndex - 1];
                items[currentIndex - 1] = items[currentIndex];
                items[currentIndex] = tmp;
                --currentIndex;
                NeedsRebuild = true;
            }
        }

        public void OnCurrentItemDown()
        {
            if (items.Count > 1
                && currentIndex >= 0
                && currentIndex < items.Count - 1)
            {
                // swap with next item.
                var tmp = items[currentIndex + 1];
                items[currentIndex + 1] = items[currentIndex];
                items[currentIndex] = tmp;
                ++currentIndex;
                NeedsRebuild = true;
            }
        }

        public void UpdateSelection()
        {
            // TODO: add a geometry to highlight the current cell
            // For now we let the ListItemContent to highlight itself
            int index = 0;
            foreach (UIDynamicListItem item in items)
            {
                item.SetSelected(index == currentIndex);
                ++index;
            }
        }

        private bool MatchFilter(UIDynamicListItem item)
        {
            string name = item.Content.name.ToLower();
            return name.Contains(filter);
        }

        // reposition every item depending on the itemWidth/Height, width/height, margin, and current page.
        private void UpdateItemPositions()
        {
            // Filter items
            List<UIDynamicListItem> filteredItems;
            if (null != filter && filter.Length > 0)
            {
                filteredItems = new List<UIDynamicListItem>();
                foreach (var item in items)
                {
                    if (MatchFilter(item))
                    {
                        filteredItems.Add(item);
                    }
                    else
                    {
                        // Be sure to hide items that don't match the filter
                        item.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                filteredItems = items;
            }

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

            pagesCount = ((filteredItems.Count > 0) && (nbItemsPerPage > 0)) ? (filteredItems.Count - 1) / nbItemsPerPage + 1 : 0;

            nbItemsInLastPage = pagesCount == 0 ? 0 : Math.Min(filteredItems.Count - nbItemsPerPage * (pagesCount - 1), nbItemsPerPage);

            float itemWidth2 = (float)itemWidth / 2.0f;
            float itemHeight2 = (float)itemHeight / 2.0f;

            //
            // Update items visibility, position, scale and collider.
            //
            for (int i = 0; i < filteredItems.Count; ++i)
            {
                UIDynamicListItem item = filteredItems[i];
                if (nbItemsPerPage > 0)
                {
                    bool isInCurrentPage = ((i / nbItemsPerPage) == currentPage);
                    item.gameObject.SetActive(isInCurrentPage);
                    if (isInCurrentPage)
                    {
                        int idxInCurrentPage = i % nbItemsPerPage;
                        int row = idxInCurrentPage / maxNbItemCols;
                        int col = idxInCurrentPage % maxNbItemCols;
                        if (autoCenterContent) // TODO: always place pivot top-left, and let UIDynamicItem auto-center its content.
                        {
                            item.transform.localPosition = new Vector3(
                                col == 0 ? margin + itemWidth2 : margin + itemWidth2 + col * (itemWidth + itemHMargin),
                                row == 0 ? -margin - itemHeight2 : -margin - itemHeight2 - row * (itemHeight + itemVMargin),
                                0.0f);
                        }
                        else
                        {
                            item.transform.localPosition = new Vector3(
                                col == 0 ? margin : margin + col * (itemWidth + itemHMargin),
                                row == 0 ? -margin : -margin - row * (itemHeight + itemVMargin),
                                0.0f);
                        }
                        item.transform.localRotation = Quaternion.identity;
                        item.Width = itemWidth;
                        item.Height = itemHeight;
                        item.Depth = itemDepth;
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
                pageCountLabel.Text = pagesCount == 0 ? "0/0" : $"{currentPage + 1}/{pagesCount}";
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
                    Vector3 tl = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth), -margin - row * (itemVMargin + itemHeight), 0));
                    Vector3 tr = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth) + itemWidth, -margin - row * (itemVMargin + itemHeight), 0));
                    Vector3 bl = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth), -margin - row * (itemVMargin + itemHeight) - itemHeight, 0));
                    Vector3 br = transform.TransformPoint(new Vector3(margin + col * (itemHMargin + itemWidth) + itemWidth, -margin - row * (itemVMargin + itemHeight) - itemHeight, 0));

                    // TODO: draw depth

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
            float item_height,
            float item_depth)
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

            UIUtils.SetRecursiveLayer(go, "CameraHidden");
        }
    }
}
