using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class InputManagerSingleton
    {
        private Dictionary<string, GameObject> inputs = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> Inputs
        {
            get
            {
                return inputs;
            }
        }

        private InputManager mainGameObject = null;
        public InputManager MainGameObject
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

        public GameObject currentInputRef = null;

        public void registerTool(GameObject tool, bool activate = false)
        {
            inputs.Add(tool.name, tool);
            tool.SetActive(activate);
            if (activate)
                currentInputRef = tool;
        }
        public GameObject CurrentTool()
        {
            return currentInputRef;
        }
    }

    public class InputManager : MonoBehaviour
    {
        static InputManagerSingleton _instance = null;
        public static InputManagerSingleton Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new InputManagerSingleton();
                }
                return _instance;
            }
        }
        
        void Awake()
        {
            InputManager.Instance.MainGameObject = this;
        }

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }
    }

}