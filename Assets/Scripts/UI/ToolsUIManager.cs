using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRtist
{
    public delegate void OnToolChanged(object sender, ToolChangedArgs args);
    public class ToolChangedArgs : EventArgs
    {
        public string toolName;
    }

    public delegate void OnToolParameterChanged(object sender, ToolParameterChangedArgs args);
    public class ToolParameterChangedArgs : EventArgs
    {
        public string toolName;
        public string parameterName;
        public float value;
    }

    public delegate void OnBoolToolParameterChanged(object sender, BoolToolParameterChangedArgs args);
    public class BoolToolParameterChangedArgs : EventArgs
    {
        public string toolName;
        public string parameterName;
        public bool value;
    }

    public class ToolsUIManager : MonoBehaviour
    {
        [SerializeField] private Transform panelsParent;
        [SerializeField] private Transform palette;
        [SerializeField] private Vector3 paletteScale = Vector3.one;

        public event EventHandler<ToolChangedArgs> OnToolChangedEvent;
        public event EventHandler<ToolParameterChangedArgs> OnToolParameterChangedEvent;
        public event EventHandler<BoolToolParameterChangedArgs> OnBoolToolParameterChangedEvent;

        // DEBUG
        public bool forceShowPalette = false;

        [Tooltip("Panel shown when using the Force Show Palette feature.")]
        public string forceShowPanel = "Lighting";

        private string currentToolName;

        // Map of the 3d object widgets. Used for passing messages by int instead of GameObject. Key is a Hash.
        private Dictionary<int, GameObject> ui3DObjects = new Dictionary<int, GameObject>();

        // Singleton
        public static ToolsUIManager Instance { get; private set; }

        void Awake()
        {
            ToolsUIManager.Instance = this;
        }

        void Start()
        {
            currentToolName = ToolsManager.Instance.CurrentTool().name;

            TogglePanel(currentToolName);
            OnToolChangedEvent += ToolsManager.Instance.MainGameObject.OnChangeTool;
            OnToolParameterChangedEvent += ToolsManager.Instance.MainGameObject.OnChangeToolParameter;

            palette.transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            if (forceShowPalette)
            {
                forceShowPalette = false;
                SetForceShowPalette(true);
            }
        }

        public void ChangeTool(string toolName)
        {
            currentToolName = toolName;
            TogglePanel(currentToolName);

            var args = new ToolChangedArgs { toolName = currentToolName };
            EventHandler<ToolChangedArgs> handler = OnToolChangedEvent;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public void RegisterUI3DObject(GameObject go)
        {
            int key = go.GetHashCode();
            if (!ui3DObjects.ContainsKey(key))
            {
                ui3DObjects.Add(key, go);
            }
        }

        public GameObject GetUI3DObject(int hash)
        {
            if (ui3DObjects.ContainsKey(hash))
            {
                return ui3DObjects[hash];
            }
            return null;
        }

        public void OnUI3DObjectEnter(int gohash)
        {
            ToolBase tool = ToolsManager.Instance.CurrentTool().GetComponent<ToolBase>();
            if (tool != null)
            {
                tool.OnUIObjectEnter(gohash);
            }
        }

        public void OnUI3DObjectExit(int gohash)
        {
            ToolBase tool = ToolsManager.Instance.CurrentTool().GetComponent<ToolBase>();
            if (tool != null)
            {
                tool.OnUIObjectExit(gohash);
            }
        }

        public void OnTool()
        {
            ChangeTool(EventSystem.current.currentSelectedGameObject.name);
        }

        //
        // DEPRECATED: used by the old 2D Unity UI with the EventSystem.
        //
        //public void OnSliderChange()
        //{
        //    if (!EventSystem.current || !EventSystem.current.currentSelectedGameObject)
        //        return;
        //    SliderComp slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<SliderComp>();
        //    if (!slider)
        //        return;
        //    float value = slider.Value;
        //    var args = new ToolParameterChangedArgs { toolName = currentToolName, parameterName = slider.gameObject.name, value = value };
        //    EventHandler<ToolParameterChangedArgs> handler = OnToolParameterChangedEvent;
        //    if (handler != null) { handler(this, args); }
        //}

        //
        // DEPRECATED: used by the old 2D Unity UI with the EventSystem.
        //
        //public void OnCheckboxChange()
        //{
        //    Toggle checkbox = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Toggle>();
        //    bool isOn = checkbox.isOn;
        //    var args = new BoolToolParameterChangedArgs { toolName = currentToolName, parameterName = checkbox.gameObject.name, value = isOn };
        //    EventHandler<BoolToolParameterChangedArgs> handler = OnBoolToolParameterChangedEvent;
        //    if (handler != null) { handler(this, args); }
        //}

        private void TogglePanel(string activePanelName)
        {
            string panelObjectName = activePanelName + "Panel";

            for (int i = 0; i < panelsParent.childCount; i++)
            {
                GameObject child = panelsParent.GetChild(i).gameObject;
                child.SetActive(panelObjectName == child.name);
            }
            
            /*
            for (int i = 0; i < canvas3D.childCount; i++)
            {
                GameObject child = canvas3D.GetChild(i).gameObject;
                child.SetActive(activePanelName == child.name);
            }
            */

            //if (proxy3D != null)
            //{
            //    for (int i = 0; i < proxy3D.childCount; i++)
            //    {
            //        GameObject child = proxy3D.GetChild(i).gameObject;
            //        child.SetActive(activePanelName == child.name);
            //    }
            //}

            // TODO: show which TAB is active.
            //       with emissive in the shader??

            /*
            for (int i = 0; i < buttonsParent.childCount; i++)
            {
                GameObject child = buttonsParent.GetChild(i).gameObject;
                Image image = child.GetComponent<Image>();
                image.color = activePanelName == child.name ? Selection.SelectedColor : Selection.UnselectedColor;
            }
            */
        }

        //public bool isOverUI()
        //{
        //    return false;
        //    //return pointer.activeSelf;
        //}

        public void ToggleMenu()
        {
            EnableMenu(palette.transform.localScale == Vector3.zero);
        }

        public void EnableMenu(bool value)
        {
            palette.transform.localScale = value ? paletteScale : Vector3.zero;
            //EnableProxy3D(value);
        }

        public void SetForceShowPalette(bool value)
        {
            EnableMenu(value);
            if (value)
            {
                TogglePanel(forceShowPanel);
            }
        }

        //public void EnableProxy3D(bool value)
        //{
        //    if (proxy3D != null)
        //    {
        //        proxy3D.gameObject.SetActive(value);
        //    }
        //}

        //public void StoreProxy3DState()
        //{
        //    proxy3DState = IsProxy3DEnabled();
        //}

        //public bool IsProxy3DEnabled()
        //{
        //    return proxy3D != null ? proxy3D.gameObject.activeSelf : false;
        //}

        //public void RestoreProxy3DState()
        //{
        //    EnableProxy3D(proxy3DState);
        //}
    }
}