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

        }

        // Show/Hide palette
        public void TogglePalette()
        {
            forceShowPalette = !forceShowPalette;
            EnableMenu(forceShowPalette);
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

        public void TogglePanel(string activePanelName)
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
            // TODO: interruption in middle
            Coroutine co = StartCoroutine(AnimatePalettePopup(value ? Vector3.zero : paletteScale, value ? paletteScale : Vector3.zero));
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