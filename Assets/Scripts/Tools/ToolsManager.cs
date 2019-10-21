using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class ToolsManagerSingleton
    {
        private Dictionary<string, GameObject> tools = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> Tools
        {
            get
            {
                return tools;
            }
        }

        private ToolsManager mainGameObject = null;
        public ToolsManager MainGameObject
        {
            get
            {
                return mainGameObject;
            }
            set
            {
                mainGameObject = value;
            }
        }

        public GameObject currentToolRef = null;

        public void registerTool(GameObject tool, bool activate = false)
        {
            tools.Add(tool.name, tool);
            tool.SetActive(activate);
            if (activate)
                currentToolRef = tool;
        }
        public GameObject CurrentTool()
        {
            return currentToolRef;
        }
    }

    public class ToolsManager : MonoBehaviour
    {
        static ToolsManagerSingleton _instance = null;
        public static ToolsManagerSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ToolsManagerSingleton();
                }
                return _instance;
            }
        }

        // Must be done before Start of ToolsUIManager
        void Awake()
        {
            ToolsManager.Instance.MainGameObject = this;
        }

        /*
        public void OnChangeTool(object sender, ToolChangedArgs args)
        {
            Instance.currentToolRef.SetActive(false);
            Instance.currentToolRef = Instance.Tools[args.toolName];
            Instance.currentToolRef.SetActive(true);

            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);
            Instance.currentToolRef.transform.localPosition = position;
            Instance.currentToolRef.transform.localRotation = rotation;
        }

        public void OnChangeToolParameter(object sender, ToolParameterChangedArgs args)
        {
        }
        */
    }
}