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

using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class SelectionHelper : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private Image image;

        private Sprite selectImage;
        private Sprite grabImage;

        private Plane[] frustumPlanes;
        //private bool[] hidden;
        private bool hasSelection;
        // key: invisible selected game object, value: selectionLink game object
        private Dictionary<GameObject, GameObject> selectionLinks = new Dictionary<GameObject, GameObject>();
        private GameObject selectionLinkPrefab;
        private Collider controllerCollider;

        float outOfFrustumLineWidth = 0.001f;

        void Start()
        {
            frustumPlanes = new Plane[6];
            //hidden = new bool[6];

            text = GetComponentInChildren<TextMeshProUGUI>();
            image = transform.Find("Canvas/Panel/Image").GetComponent<Image>();

            selectImage = UIUtils.LoadIcon("select");
            grabImage = UIUtils.LoadIcon("grab-icon");

            selectionLinkPrefab = Resources.Load<GameObject>("Prefabs/UI/SelectionLink");

            controllerCollider = GetComponent<Collider>();

            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            Selection.onAuxiliarySelectionChanged.AddListener(OnAuxiliarySelectedObjectChanged);
            SetSelectionCount();
        }

        private void OnDestroy()
        {
            Selection.onSelectionChanged.RemoveListener(OnSelectionChanged);
            Selection.onAuxiliarySelectionChanged.RemoveListener(OnAuxiliarySelectedObjectChanged);
        }

        private void Update()
        {
            if (!hasSelection) { return; }

            // Clear line renderers
            foreach (GameObject link in selectionLinks.Values)
            {
                link.SetActive(false);
            }

            // Ordering: [0] = Left, [1] = Right, [2] = Down, [3] = Up, [4] = Near, [5] = Far
            //Array.Clear(hidden, 0, hidden.Length);
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            // Check that the controller is inside the view frustum
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, controllerCollider.bounds))
            {
                return;
            }

            foreach (GameObject gobj in Selection.ActiveObjects)
            {
                // Check if the object is outside the view frustum
                Bounds bounds;
                Collider collider = gobj.GetComponent<Collider>();
                if (null == collider)
                {
                    bounds = new Bounds(gobj.transform.position, Vector3.one * 0.001f);
                }
                else
                {
                    bounds = collider.bounds;
                }
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                {
                    //// Get direction of the hidden object
                    //for (int i = 0; i < hidden.Length; i++)
                    //{
                    //    if (hidden[i]) { continue; }  // we already have a hidden object in this direction

                    //    Vector3 direction = gobj.transform.position - Camera.main.transform.position;

                    //}

                    // Add a LineRenderer from the controller to the invisible selected object
                    if (!selectionLinks.ContainsKey(gobj))
                    {
                        selectionLinks.Add(gobj, Instantiate(selectionLinkPrefab, transform));
                    }
                    GameObject selectionLink = selectionLinks[gobj];
                    LineRenderer lineRenderer = selectionLink.GetComponent<LineRenderer>();
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, transform.position);
                    lineRenderer.SetPosition(1, gobj.transform.position);
                    lineRenderer.startWidth = outOfFrustumLineWidth / GlobalState.WorldScale;
                    lineRenderer.endWidth = outOfFrustumLineWidth / GlobalState.WorldScale;
                    selectionLink.SetActive(true);
                }
            }

            // Remove unused line renderers
            List<GameObject> toRemove = new List<GameObject>();
            foreach (KeyValuePair<GameObject, GameObject> item in selectionLinks)
            {
                if (!item.Value.activeSelf)
                {
                    Destroy(item.Value);
                    toRemove.Add(item.Key);
                }
            }
            foreach (GameObject gobj in toRemove)
            {
                selectionLinks.Remove(gobj);
            }
        }

        private void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            SetSelectionCount();
        }

        private void SetSelectionCount()
        {
            int count = Selection.SelectedObjects.Count;
            text.text = count.ToString();
            GlobalState.SetPrimaryControllerDisplayText("Sel " + count.ToString());
            hasSelection = count > 0;
            gameObject.SetActive(hasSelection);
        }

        private void OnAuxiliarySelectedObjectChanged(GameObject previousAuxiliarySelectedObject, GameObject auxiliarySelectedObject)
        {
            if (null != auxiliarySelectedObject)
            {
                image.sprite = grabImage;
                text.text = "1";  // we can grab only one object outside of the selection
                GlobalState.SetPrimaryControllerDisplayText("Grab 1");
            }
            else
            {
                image.sprite = selectImage;
                SetSelectionCount();
            }
        }
    }
}
