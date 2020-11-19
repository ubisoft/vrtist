using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class SelectionHelper : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private TextMeshProUGUI rightControllerText;
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

        void Start()
        {
            frustumPlanes = new Plane[6];
            //hidden = new bool[6];

            text = GetComponentInChildren<TextMeshProUGUI>();
            rightControllerText = transform.parent.Find("right_controller/Canvas/Text").GetComponent<TextMeshProUGUI>();
            image = transform.Find("Canvas/Panel/Image").GetComponent<Image>();

            selectImage = UIUtils.LoadIcon("select");
            grabImage = UIUtils.LoadIcon("grab-icon");

            selectionLinkPrefab = Resources.Load<GameObject>("Prefabs/UI/SelectionLink");

            controllerCollider = GetComponent<Collider>();

            Selection.OnSelectionChanged += OnSelectionChanged;
            Selection.OnGrippedObjectChanged += OnGrippedObjectChanged;
            SetSelectionCount();
        }

        private void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
            Selection.OnGrippedObjectChanged -= OnGrippedObjectChanged;
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

            foreach (GameObject gobj in Selection.GetGrippedOrSelection())
            {
                // Check if the object is outside the view frustum
                Bounds bounds;
                Collider collider = gobj.GetComponent<Collider>();
                if(null == collider)
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

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            SetSelectionCount();
        }

        private void SetSelectionCount()
        {
            int count = Selection.selection.Count;
            text.text = count.ToString();
            rightControllerText.text = "Sel " + count.ToString();
            hasSelection = count > 0;
            gameObject.SetActive(hasSelection);
        }

        private void OnGrippedObjectChanged(object sender, GameObjectArgs args)
        {
            if (null != args.gobject && !Selection.IsSelected(args.gobject))
            {
                image.sprite = grabImage;
                text.text = "1";  // we can grab only one object outside of the selection
                rightControllerText.text = "Grab 1";
            }
            else
            {
                image.sprite = selectImage;
                SetSelectionCount();
            }
        }
    }
}
