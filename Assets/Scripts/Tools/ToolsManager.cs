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

using UnityEngine;

namespace VRtist
{
    // TODO: create a real singleton instead of a MonoBehaviour
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
        public GameObject windowTool = null;

        private GameObject currentToolRef = null;
        private GameObject previousTool = null;

        private bool isInWindowTool = false;
        private GameObject pushedTool = null;

        public static void PushWindowTool()
        {
            if (!Instance.isInWindowTool && Instance.currentToolRef != Instance.windowTool) // protect against multiple PUSH
            {
                Instance.isInWindowTool = true;
                Instance.pushedTool = Instance.currentToolRef;

                Instance.currentToolRef.SetActive(false);
                Instance.currentToolRef = Instance.windowTool;
                Instance.currentToolRef.SetActive(true);
            }
        }

        public static void PopWindowTool()
        {
            if (Instance.isInWindowTool && Instance.pushedTool != null) // protect against multiple POP
            {
                Instance.isInWindowTool = false;
                Instance.currentToolRef.SetActive(false);
                Instance.currentToolRef = Instance.pushedTool;
                Instance.currentToolRef.SetActive(true);
                Instance.pushedTool = null;
            }
        }

        public static bool CurrentToolIsGripping()
        {
            if (Instance.currentToolRef == null)
                return false;

            SelectorBase t = Instance.currentToolRef.GetComponent<SelectorBase>();
            if (t == null)
                return false;

            return t.Gripping;
        }

        public static void RegisterTool(GameObject tool)
        {
            Instance.tools.Add(tool.name, tool);
            tool.SetActive(tool == Instance.defaultTool);
            tool.GetComponent<ToolBase>().ActivateMouthpiece(tool == Instance.defaultTool);
        }

        public static void ToggleTool()
        {
            // Toggle to alt tool
            if (Instance.currentToolRef.name != Instance.altTool.name)
            {
                Instance.previousTool = Instance.currentToolRef;
                ToolsUIManager.Instance.ChangeTool(Instance.altTool.name);
                ToolsUIManager.Instance.ChangeTab(Instance.altTool.name);
            }
            // Toggle to previous tool or sub toggle alt tool
            else if (Instance.currentToolRef.name == Instance.altTool.name)
            {
                ToolBase toolBase = Instance.altTool.GetComponent<ToolBase>();
                if (!toolBase.SubToggleTool())
                {
                    // Toggle to previous tool
                    if (null != Instance.previousTool)
                    {
                        ToolsUIManager.Instance.ChangeTool(Instance.previousTool.name);
                        ToolsUIManager.Instance.ChangeTab(Instance.previousTool.name);
                        Instance.previousTool = null;
                    }
                }
            }
        }

        public static GameObject CurrentToolGameObject()
        {
            return Instance.currentToolRef;
        }

        public static string CurrentToolName()
        {
            return Instance.currentToolRef.name;
        }

        public static ToolBase CurrentTool()
        {
            if (null != Instance.currentToolRef) { return Instance.currentToolRef.GetComponent<ToolBase>(); }
            return null;
        }

        public static void SetCurrentTool(GameObject tool)
        {
            Instance.currentToolRef = tool;
        }

        public static GameObject GetTool(string name)
        {
            return Instance.Tools[name];
        }

        public static void ActivateCurrentTool(bool value)
        {
            // Activate/deactivate the tool and its mouthpiece
            Instance.currentToolRef.SetActive(value);
            Instance.currentToolRef.GetComponent<ToolBase>().ActivateMouthpiece(value);
        }

        public static void ChangeTool(string toolName)
        {
            Instance.currentToolRef.SetActive(false);
            Instance.currentToolRef = Instance.Tools[toolName];
            Instance.currentToolRef.SetActive(true);
        }

        public static void OnChangeTool(object sender, ToolChangedArgs args)
        {
            ChangeTool(args.toolName);
        }

        public static void OnChangeToolParameter(object sender, ToolParameterChangedArgs args)
        {
        }
    }
}