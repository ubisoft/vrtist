using System;
using System.Collections;
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
        [SerializeField] private Color defaultColor = new Color(114f/ 255f, 114f / 255f, 114f / 255f);
        [SerializeField] private Color selectionColor = new Color(0f, 167f / 255f, 1f);
        public AudioSource audioOpenPalette = null;
        public AudioSource audioClosePalette = null;
        public AudioSource audioOpenDopesheet = null;
        public AudioSource audioCloseDopesheet = null;

        public event EventHandler<ToolChangedArgs> OnToolChangedEvent;
        public event EventHandler<ToolParameterChangedArgs> OnToolParameterChangedEvent;
        public event EventHandler<BoolToolParameterChangedArgs> OnBoolToolParameterChangedEvent;

        // DEBUG
        public int palettePopNbFrames = 8;
        public AnimationCurve paletteXCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.5f, 1.5f, 0, 0),
            new Keyframe(0.75f, 0.8f, 0, 0),
            new Keyframe(1, 1, 0, 0)
        );
        public AnimationCurve paletteYCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.7f, 1.0f, 4.0f, 4.0f), // goes over
            new Keyframe(1, 1, 0, 0)
        );
        private bool forceShowPalette = false;
        private bool showTools = true;

        private string currentToolName;
        private Transform mainPanel;

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
            OnToolChangedEvent += ToolsManager.OnChangeTool;
            OnToolParameterChangedEvent += ToolsManager.OnChangeToolParameter;

            palette.transform.localScale = Vector3.zero;
            mainPanel = palette.transform.GetChild(0);

            ChangeTool(ToolsManager.CurrentTool().name);
        }

        // Show/Hide palette
        public void TogglePalette()
        {
            EnableMenu(!forceShowPalette);
        }

        public void ChangeTool(string toolName)
        {
            // Restore previous panel color
            SetToolButtonActive(currentToolName, false);

            currentToolName = toolName;
            SetToolButtonActive(currentToolName, true);
            TogglePanel(currentToolName);

            var args = new ToolChangedArgs { toolName = currentToolName };
            EventHandler<ToolChangedArgs> handler = OnToolChangedEvent;
            if (handler != null)
            {
                handler(this, args);
            }
            ShowCurrentTool(showTools);
        }

        public void ShowCurrentTool(bool doShowTool)
        {
            ToolBase tool = ToolsManager.CurrentTool().GetComponent<ToolBase>();
            if (tool != null)
            {
                tool.IsInGui = !doShowTool;
            }
        }
        
        public void ShowTools(bool doShowTools)
        {
            showTools = doShowTools;
            ShowCurrentTool(showTools);
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
            ToolBase tool = ToolsManager.CurrentTool().GetComponent<ToolBase>();
            if (tool != null)
            {
                tool.OnUIObjectEnter(gohash);
            }
        }

        public void OnUI3DObjectExit(int gohash)
        {
            ToolBase tool = ToolsManager.CurrentTool().GetComponent<ToolBase>();
            if (tool != null)
            {
                tool.OnUIObjectExit(gohash);
            }
        }

        public void SetToolButtonActive(string toolName, bool active)
        {
            if(toolName == null) { return; }
            string buttonName = toolName + "ToolButton";

            for (int i = 0; i < mainPanel.childCount; i++)
            {
                GameObject gobj = mainPanel.GetChild(i).gameObject;
                if (gobj.name == buttonName)
                {
                    UIButton buttonElement = gobj.GetComponent<UIButton>();
                    buttonElement.Checked = active;
                }
            }
        }

        public void TogglePanel(string activePanelName)
        {
            string panelObjectName = activePanelName + "Panel";

            for (int i = 0; i < panelsParent.childCount; i++)
            {
                GameObject child = panelsParent.GetChild(i).gameObject;
                child.SetActive(panelObjectName == child.name);
            }
        }

        public void EnableMenu(bool value)
        {
            if (value != forceShowPalette)
            {
                forceShowPalette = value;

                // TODO: interruption in middle
                Coroutine co = StartCoroutine(AnimatePalettePopup(value ? Vector3.zero : paletteScale, value ? paletteScale : Vector3.zero));
                if (value)
                {
                    if (audioOpenPalette != null)
                        audioOpenPalette.Play();
                }
                else
                {
                    if (audioClosePalette != null)
                        audioClosePalette.Play();
                }
            }
        }

        private IEnumerator AnimatePalettePopup(Vector3 startScale, Vector3 endScale)
        {
            int nbFrames = palettePopNbFrames;
            float t = 0.0f;
            for(int i = 0; i < nbFrames; i++)
            {
                t = (float)i / (nbFrames - 1);
                float tx = paletteXCurve.Evaluate(t);
                float ty = paletteYCurve.Evaluate(t);
                Vector3 tt = new Vector3(tx, ty, t);
                Vector3 s = startScale + Vector3.Scale(tt, (endScale - startScale));
                palette.transform.localScale = s;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}