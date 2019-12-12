using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class SelectionChangedArgs : EventArgs
    {
        public Dictionary<int, GameObject> selectionBefore = new Dictionary<int, GameObject>();
    }
    public class Selection
    {
        public static Color SelectedColor = new Color(57f / 255f, 124f / 255f, 212f / 255f);
        public static Color UnselectedColor = Color.white;

        public static Dictionary<int, GameObject> selection = new Dictionary<int, GameObject>();
        public static event EventHandler<SelectionChangedArgs> OnSelectionChanged;

        public static Material selectionMaterial;

        public static void TriggerSelectionChanged()
        {
            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);
            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }
        }

        public static void fillSelection(ref Dictionary<int, GameObject> s)
        {
            foreach (KeyValuePair<int, GameObject> data in selection)
                s[data.Key] = data.Value;
        }

        private static void CleanDuplicatedObject(GameObject gObject)
        {
            Component[] components = gObject.GetComponents<Component>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component component = components[i];
                if (component.GetType() != typeof(MeshFilter) &&
                    component.GetType() != typeof(MeshRenderer) &&
                    component.GetType() != typeof(Transform) &&
                    component.GetType() != typeof(Outline))
                    GameObject.Destroy(component);
            }

            for (int i = 0; i < gObject.transform.childCount; i++)
            {
                CleanDuplicatedObject(gObject.transform.GetChild(i).gameObject);
            }
        }

        private static void ApplyMaterial(GameObject gobject)
        {
            Renderer renderer = gobject.GetComponent<Renderer>();
            if (renderer)
            {
                var mats = renderer.materials;
                for (int i = mats.Length - 1; i >= 0; i--)
                {
                    mats[i] = selectionMaterial;
                }

                gobject.GetComponent<Renderer>().materials = mats;
                gobject.AddComponent<Outline>();
            }

            for (int i = 0; i < gobject.transform.childCount; i++)
            {
                ApplyMaterial(gobject.transform.GetChild(i).gameObject);
            }
        }

        public static bool AddToSelection(GameObject gObject)
        {
            if (selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Add(gObject.GetInstanceID(), gObject);

            GameObject newGObject = GameObject.Instantiate(gObject, gObject.transform);

            newGObject.name = "__Selected__";
            newGObject.transform.localPosition = Vector3.zero;
            newGObject.transform.localRotation = Quaternion.identity;
            newGObject.transform.localScale = Vector3.one;

            ApplyMaterial(newGObject);
            CleanDuplicatedObject(newGObject);



            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }

            return true;
        }

        public static bool RemoveFromSelection(GameObject gObject)
        {
            if (!selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            Transform selected = gObject.transform.Find("__Selected__");
            if (selected)
            {
                selected.parent = null;
                GameObject.Destroy(selected.gameObject);
            }

            selection.Remove(gObject.GetInstanceID());

            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }

            return true;
        }

        public static void ClearSelection()
        {
            foreach (KeyValuePair<int, GameObject> data in selection)
            {
                Transform selected = data.Value.transform.Find("__Selected__");
                if (selected)
                {
                    selected.parent = null;
                    GameObject.Destroy(selected.gameObject);
                }
            }

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Clear();

            EventHandler<SelectionChangedArgs> handler = OnSelectionChanged;
            if (handler != null)
            {
                handler(null, args);
            }
        }
    }

}