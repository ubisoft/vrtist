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
        //[SerializeField] private GameObject pointer;
        //[SerializeField] private Transform buttonsParent;
        //[SerializeField] private Transform panelsParent;
        //[SerializeField] private Transform canvas3D;
        [SerializeField] private Transform palette;
        Transform proxy3D = null;

        public event EventHandler<ToolChangedArgs> OnToolChangedEvent;
        public event EventHandler<ToolParameterChangedArgs> OnToolParameterChangedEvent;
        public event EventHandler<BoolToolParameterChangedArgs> OnBoolToolParameterChangedEvent;

        private string currentToolName;
        private bool proxy3DState = false;

        private static ToolsUIManager instance;
        public static ToolsUIManager Instance
        { get { return instance;  } }

        private void CreateProxy3D()
        {
            /*
            GameObject gobject = GameObject.Instantiate<GameObject>(canvas3D.gameObject);
            proxy3D = gobject.transform;
            proxy3D.position = Vector3.zero;
            proxy3D.rotation = Quaternion.identity;
            proxy3D.localScale = Vector3.one;
           
            Component[] components = canvas3D.gameObject.GetComponentsInChildren<Component>(true);
            for (int i = components.Length - 1; i >= 0; i--)
            {
                Component c = components[i];
                if (c.GetType() != typeof(Transform))
                    Destroy(c);
            }
            */
        }

        public void UpdateProxy3D()
        {
            /*
            if (proxy3D)
            {
                proxy3D.position = canvas3D.position;
                proxy3D.rotation = canvas3D.rotation;
                proxy3D.localScale = canvas3D.localScale;
            }*/
        }

        void Awake()
        {
            ToolsUIManager.instance = this;
        }

        void Start()
        {
            currentToolName = ToolsManager.Instance.CurrentTool().name;

            TogglePanel(currentToolName);
            OnToolChangedEvent += ToolsManager.Instance.MainGameObject.OnChangeTool;
            OnToolParameterChangedEvent += ToolsManager.Instance.MainGameObject.OnChangeToolParameter;

            palette.transform.localScale = Vector3.zero;

            CreateProxy3D();
        }

        public void ChangeTool(string toolName)
        {
            currentToolName = toolName;
            var args = new ToolChangedArgs { toolName = currentToolName };
            EventHandler<ToolChangedArgs> handler = OnToolChangedEvent;
            if (handler != null)
            {
                handler(this, args);
            }
            TogglePanel(currentToolName);
        }

        public void OnTool()
        {
            ChangeTool(EventSystem.current.currentSelectedGameObject.name);
        }

        public void OnSliderChange()
        {
            /*
            if (!EventSystem.current || !EventSystem.current.currentSelectedGameObject)
                return;
            SliderComp slider = EventSystem.current.currentSelectedGameObject.GetComponentInParent<SliderComp>();
            if (!slider)
                return;
            float value = slider.Value;
            var args = new ToolParameterChangedArgs { toolName = currentToolName, parameterName = slider.gameObject.name, value = value };
            EventHandler<ToolParameterChangedArgs> handler = OnToolParameterChangedEvent;
            if (handler != null) { handler(this, args); }
            */
        }

        public void OnCheckboxChange()
        {
            Toggle checkbox = EventSystem.current.currentSelectedGameObject.GetComponentInParent<Toggle>();
            bool isOn = checkbox.isOn;
            var args = new BoolToolParameterChangedArgs { toolName = currentToolName, parameterName = checkbox.gameObject.name, value = isOn };
            EventHandler<BoolToolParameterChangedArgs> handler = OnBoolToolParameterChangedEvent;
            if (handler != null) { handler(this, args); }
        }

        private void TogglePanel(string activePanelName)
        {
            /*
            for (int i = 0; i < panelsParent.childCount; i++)
            {
                GameObject child = panelsParent.GetChild(i).gameObject;
                child.SetActive(activePanelName == child.name);
            }
            */

            /*
            for (int i = 0; i < canvas3D.childCount; i++)
            {
                GameObject child = canvas3D.GetChild(i).gameObject;
                child.SetActive(activePanelName == child.name);
            }
            */
            if (proxy3D != null)
            {
                for (int i = 0; i < proxy3D.childCount; i++)
                {
                    GameObject child = proxy3D.GetChild(i).gameObject;
                    child.SetActive(activePanelName == child.name);
                }
            }

            /*
            for (int i = 0; i < buttonsParent.childCount; i++)
            {
                GameObject child = buttonsParent.GetChild(i).gameObject;
                Image image = child.GetComponent<Image>();
                image.color = activePanelName == child.name ? Selection.SelectedColor : Selection.UnselectedColor;
            }
            */
        }

        public bool isOverUI()
        {
            return false;
            //return pointer.activeSelf;
        }

        public void ToggleMenu()
        {
            EnableMenu(palette.transform.localScale == Vector3.zero);
        }

        public void EnableMenu(bool value)
        {
            palette.transform.localScale = value ? Vector3.one : Vector3.zero;
            EnableProxy3D(value);
        }
        public void EnableProxy3D(bool value)
        {
            proxy3D.gameObject.SetActive(value);
        }

        public void StoreProxy3DState()
        {
            proxy3DState = IsProxy3DEnabled();
        }

        public bool IsProxy3DEnabled()
        {
            return proxy3D.gameObject.activeSelf;
        }

        public void RestoreProxy3DState()
        {
            EnableProxy3D(proxy3DState);
        }
    }
}