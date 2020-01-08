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

        public static bool IsSelected(GameObject gObject)
        {
            return selection.ContainsKey(gObject.GetInstanceID());
        }

        public static bool AddToSelection(GameObject gObject)
        {
            if (selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Add(gObject.GetInstanceID(), gObject);

            gObject.layer = LayerMask.NameToLayer("Selection"); // TODO: init in one of the singletons

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

            selection.Remove(gObject.GetInstanceID());

            gObject.layer = LayerMask.NameToLayer("Default"); // 0

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
                data.Value.layer = LayerMask.NameToLayer("Default");
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
