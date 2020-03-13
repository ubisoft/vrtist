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
        [SerializeField] private float paletteScale = 0.5f;
        [SerializeField] private Color defaultColor = new Color(114f/ 255f, 114f / 255f, 114f / 255f);
        [SerializeField] private Color selectionColor = new Color(0f, 167f / 255f, 1f);
        [SerializeField] private AudioSource audioOpenPalette = null;
        [SerializeField] private AudioSource audioClosePalette = null;
        [SerializeField] private AudioSource audioOpenDopesheet = null;
        [SerializeField] private AudioSource audioCloseDopesheet = null;

        public event EventHandler<ToolChangedArgs> OnToolChangedEvent;
        public event EventHandler<ToolParameterChangedArgs> OnToolParameterChangedEvent;
        public event EventHandler<BoolToolParameterChangedArgs> OnBoolToolParameterChangedEvent;

        [Header("Debug tweaking")]
        public int palettePopNbFrames = 8;
        public AnimationCurve paletteOpenAnimXCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.5f, 1.5f, 0, 0),
            new Keyframe(0.75f, 0.8f, 0, 0),
            new Keyframe(1, 1, 0, 0)
        );
        public AnimationCurve paletteOpenAnimYCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.7f, 1.0f, 4.0f, 4.0f),
            new Keyframe(1, 1, 0, 0)
        );
        public AnimationCurve paletteOpenAnimZCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(1, 1, 0, 0)
        );
        public AnimationCurve paletteCloseAnimXCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.5f, 1.5f, 0, 0),
            new Keyframe(0.75f, 0.8f, 0, 0),
            new Keyframe(1, 1, 0, 0)
        );
        public AnimationCurve paletteCloseAnimYCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
            new Keyframe(0.7f, 1.0f, 4.0f, 4.0f),
            new Keyframe(1, 1, 0, 0)
        );
        public AnimationCurve paletteCloseAnimZCurve = new AnimationCurve(
            new Keyframe(0, 0, 0, 0),
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
            ShowPalette(!forceShowPalette);
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

            // TODO: pk on fait pas juste un Find ici?
            for (int i = 0; i < panelsParent.childCount; i++)
            {
                GameObject child = panelsParent.GetChild(i).gameObject;
                child.SetActive(panelObjectName == child.name);
            }
        }

        public void OpenWindow(Transform window, float scaleFactor)
        {
            Coroutine co = StartCoroutine(AnimateWindowOpen(window, paletteOpenAnimXCurve, paletteOpenAnimYCurve, paletteOpenAnimZCurve, scaleFactor, palettePopNbFrames, false));
            if (audioOpenPalette != null)
                audioOpenPalette.Play();
        }

        public void CloseWindow(Transform window, float scaleFactor)
        {
            Coroutine co = StartCoroutine(AnimateWindowOpen(window, paletteCloseAnimXCurve, paletteCloseAnimYCurve, paletteCloseAnimZCurve, scaleFactor, palettePopNbFrames, false));
            if (audioClosePalette != null)
                audioClosePalette.Play();
        }

        public void ShowPalette(bool value)
        {
            if (value != forceShowPalette)
            {
                forceShowPalette = value;

                if (value)
                {
                    OpenWindow(palette.transform, paletteScale);
                }
                else
                {
                    CloseWindow(palette.transform, paletteScale);
                }
            }
        }

        private IEnumerator AnimateWindowOpen(Transform window, AnimationCurve xCurve, AnimationCurve yCurve, AnimationCurve zCurve, float scaleFactor, int nbFrames, bool reverse = false)
        {
            for (int i = 0; i < nbFrames; i++)
            {
                float t = (float)i / (nbFrames - 1);
                if (reverse) t = 1.0f - t;
                float tx = scaleFactor * xCurve.Evaluate(t);
                float ty = scaleFactor * yCurve.Evaluate(t);
                float tz = scaleFactor * zCurve.Evaluate(t);
                Vector3 s = new Vector3(tx, ty, tz);
                window.localScale = s;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}