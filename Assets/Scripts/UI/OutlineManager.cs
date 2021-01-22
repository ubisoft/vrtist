using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Animations;

namespace VRtist
{
    public class OutlineManager
    {
        static OutlineManager instance = null;
        public static OutlineManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new OutlineManager();
                }
                return instance;
            }
        }

        OutlineManager()
        {
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            Selection.onHoveredChanged.AddListener(OnHoveredChanged);
        }

        bool HasParentOrConstraintSelected(Transform t, ref string parentLayerName)
        {
            Transform parent = t.parent;
            while (null != parent && parent.name != "RightHanded")
            {
                if (Selection.IsSelected(parent.gameObject))
                {
                    parentLayerName = LayerMask.LayerToName(parent.gameObject.layer);
                    return true;
                }
                parent = parent.parent;
            }

            ParentConstraint constraint = t.gameObject.GetComponent<ParentConstraint>();
            if (null != constraint)
            {
                if (constraint.sourceCount > 0)
                {
                    GameObject sourceObject = constraint.GetSource(0).sourceTransform.gameObject;
                    if (Selection.IsSelected(sourceObject))
                    {
                        parentLayerName = LayerMask.LayerToName(sourceObject.layer);
                        return true;
                    }
                }
            }

            LookAtConstraint lookAtConstraint = t.gameObject.GetComponent<LookAtConstraint>();
            if (null != lookAtConstraint)
            {
                if (lookAtConstraint.sourceCount > 0)
                {
                    GameObject sourceObject = constraint.GetSource(0).sourceTransform.gameObject;
                    if (Selection.IsSelected(lookAtConstraint.GetSource(0).sourceTransform.gameObject))
                    {
                        parentLayerName = LayerMask.LayerToName(sourceObject.layer);
                        return true;
                    }
                }
            }

            return false;
        }

        void SetRecursiveLayerSmart(GameObject gObject, LayerType layerType, bool isChild = false)
        {
            string layerName = LayerMask.LayerToName(gObject.layer);

            //
            // SELECT
            //
            if (layerType == LayerType.Selection)
            {
                if (layerName == "Default")
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "SelectionChild";
                    else
                        layerName = "Selection";
                }
                else if (layerName == "Hover" || layerName == "HoverChild")
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "SelectionChild";
                    else
                        layerName = "Selection";
                }
                else if (layerName == "CameraHidden") { layerName = "SelectionCameraHidden"; }
                else if (layerName == "HoverCameraHidden") { layerName = "SelectionCameraHidden"; }
            }
            //
            // HOVER
            //
            else if (layerType == LayerType.Hover)
            {
                if (layerName == "Default")
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "HoverChild";
                    else
                        layerName = "Hover";
                }
                else if (layerName == "Selection" || layerName == "SelectionChild")
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "HoverChild";
                    else
                        layerName = "Hover";
                }
                else if (layerName == "CameraHidden") { layerName = "HoverCameraHidden"; }
                else if (layerName == "SelectionCameraHidden") { layerName = "HoverCameraHidden"; }
            }
            //
            // RESET layer
            //
            else if (layerType == LayerType.Default)
            {
                if (layerName == "SelectionCameraHidden") { layerName = "CameraHidden"; }
                else if (layerName == "Hover" || layerName == "HoverChild")
                {
                    string parentLayer = "";
                    if (HasParentOrConstraintSelected(gObject.transform, ref parentLayer))
                    {
                        layerName = parentLayer + "Child";
                    }
                    else
                    {
                        layerName = "Default";
                    }
                }
                else if (layerName == "HoverCameraHidden") { layerName = "CameraHidden"; }
                else if (layerName == "Selection" || layerName == "SelectionChild")
                {
                    string parentLayer = "";
                    if (HasParentOrConstraintSelected(gObject.transform, ref parentLayer))
                    {
                        layerName = parentLayer + "Child";
                    }
                    else
                    {
                        layerName = "Default";
                    }
                }
            }

            gObject.layer = LayerMask.NameToLayer(layerName);
            for (int i = 0; i < gObject.transform.childCount; i++)
            {
                SetRecursiveLayerSmart(gObject.transform.GetChild(i).gameObject, layerType, true);
            }

            ParametersController parametersConstroller = gObject.GetComponent<ParametersController>();
            if (null != parametersConstroller)
            {
                foreach (GameObject sourceConstraint in parametersConstroller.constraintHolders)
                {
                    if (!Selection.IsSelected(sourceConstraint))
                        SetRecursiveLayerSmart(sourceConstraint, layerType, true);
                }
            }
        }
        private void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            HashSet<GameObject> prev = previousSelection;
            prev.ExceptWith(currentSelection);
            foreach (GameObject o in prev)
            {
                if (Selection.IsHovered(o))
                    SetRecursiveLayerSmart(o, LayerType.Hover);
                else
                    SetRecursiveLayerSmart(o, LayerType.Default);
            }

            HashSet<GameObject> curr = currentSelection;
            curr.ExceptWith(previousSelection);
            foreach (GameObject o in curr)
            {
                SetRecursiveLayerSmart(o, LayerType.Selection);
            }
        }

        private void OnHoveredChanged(GameObject previousHover, GameObject currentHover)
        {
            if (null != previousHover)
            {
                if (Selection.IsSelected(previousHover))
                    SetRecursiveLayerSmart(previousHover, LayerType.Selection);
                else
                    SetRecursiveLayerSmart(previousHover, LayerType.Default);
            }

            if (null != currentHover && !Selection.IsSelected(currentHover))
                SetRecursiveLayerSmart(currentHover, LayerType.Hover);
        }
    }
}
