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
        //public static Color SelectedColor = new Color(57f / 255f, 124f / 255f, 212f / 255f);
        public static Color SelectedColor = new Color(0f / 255f, 167f / 255f, 255f / 255f);
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

        public static bool IsSelected(GameObject gObject)
        {
            return selection.ContainsKey(gObject.GetInstanceID());
        }

        private static void SetRecursiveLayer(GameObject gObject, string layerName)
        {
            gObject.layer = LayerMask.NameToLayer(layerName); // TODO: init in one of the singletons
            for(int i = 0; i < gObject.transform.childCount; i++)
            {
                SetRecursiveLayer(gObject.transform.GetChild(i).gameObject, layerName);
            }
        }

        public static bool AddToSelection(GameObject gObject)
        {
            if (selection.ContainsKey(gObject.GetInstanceID()))
                return false;

            SelectionChangedArgs args = new SelectionChangedArgs();
            fillSelection(ref args.selectionBefore);

            selection.Add(gObject.GetInstanceID(), gObject);

            Camera cam = gObject.GetComponentInChildren<Camera>(true);
            if (cam)
                cam.gameObject.SetActive(true);

            SetRecursiveLayer(gObject, "Selection");

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

            Camera cam = gObject.GetComponentInChildren<Camera>(true);
            if (cam)
                cam.gameObject.SetActive(false);

            selection.Remove(gObject.GetInstanceID());

            string layerName = "Default";
            if (gObject.GetComponent<LightController>() || gObject.GetComponent<CameraController>())
                layerName = "UI";

            SetRecursiveLayer(gObject, layerName);

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
                string layerName = "Default";
                if (data.Value.GetComponent<LightController>() || data.Value.GetComponent<CameraController>())
                    layerName = "UI";

                Camera cam = data.Value.GetComponentInChildren<Camera>(true);
                if (cam)
                    cam.gameObject.SetActive(false);


                SetRecursiveLayer(data.Value, layerName);
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
