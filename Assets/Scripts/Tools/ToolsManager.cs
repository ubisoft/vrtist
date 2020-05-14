using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class ToolsManager : MonoBehaviour
    {
        private static ToolsManager instance = null;
        private static ToolsManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<ToolsManager>();
                }
                return instance;
            }
        }
        // Must be done before Start of ToolsUIManager
        void Awake()
        {
            _ = Instance;
            SetCurrentTool(defaultTool);
        }

        private Dictionary<string, GameObject> tools = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> Tools
        {
            get
            {
                return tools;
            }
        }

        public GameObject defaultTool = null;
        public GameObject altTool = null;
        private GameObject currentToolRef = null;
        private GameObject previousTool = null;

        public static void RegisterTool(GameObject tool)
        {
            Instance._RegisterTool(tool);
        }

        public void _RegisterTool(GameObject tool)
        {
            tools.Add(tool.name, tool);
            tool.SetActive(false);
        }

        public static void ToggleTool()
        {
            Instance._ToggleTool();
        }

        public void _ToggleTool()
        {
            if (currentToolRef.name == altTool.name && previousTool != null)
            {
                ToolsUIManager.Instance.ChangeTool(previousTool.name);
                ToolsUIManager.Instance.ChangeTab(previousTool.name); // TODO: forcement?
                previousTool = null;
            }
            // Go to selection mode
            else
            {
                if (currentToolRef.name != altTool.name)
                {
                    previousTool = currentToolRef;
                }
                ToolsUIManager.Instance.ChangeTool(altTool.name);
                ToolsUIManager.Instance.ChangeTab(altTool.name);
            }
        }

        public static GameObject CurrentTool()
        {
            return Instance._CurrentTool();
        }
        public GameObject _CurrentTool()
        {
            return currentToolRef;
        }

        public static void SetCurrentTool(GameObject tool)
        {
            Instance._SetCurrentTool(tool);
        }
        public void _SetCurrentTool(GameObject tool)
        {
            currentToolRef = tool;
        }

        public static void OnChangeTool(object sender, ToolChangedArgs args)
        {
            Instance.currentToolRef.SetActive(false);
            Instance.currentToolRef = Instance.Tools[args.toolName];
            Instance.currentToolRef.SetActive(true);
        }

        public static void OnChangeToolParameter(object sender, ToolParameterChangedArgs args)
        {
        }        
    }
}