using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Lighting : SelectorBase
    {
        [Header("Lighting Parameters")]
        [SerializeField] private Transform parentContainer;
        [SerializeField] private GameObject sunPrefab;
        [SerializeField] private GameObject pointPrefab;
        [SerializeField] private GameObject spotPrefab;

        enum LightTools { None = 0, Sun, Spot, Point }

        private Transform globalCastShadows;
        private Transform intensitySlider;
        private Transform rangeSlider;
        //private Transform innerAngleSlider;
        private Transform outerAngleSlider;
        private Transform castShadowsCheckbox;
        private Transform enableCheckbox;

        private GameObject UIObject = null;

        public UIDynamicList lightList;
        private GameObject lightItemPrefab;

        void DisableUI()
        {
            intensitySlider.gameObject.SetActive(false);
            rangeSlider.gameObject.SetActive(false);
            //innerAngleSlider.gameObject.SetActive(false);
            outerAngleSlider.gameObject.SetActive(false);
            castShadowsCheckbox.gameObject.SetActive(false);
            enableCheckbox.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnSelectionChanged(null, null);
        }

        protected override void Awake()
        {
            base.Awake();

            intensitySlider = panel.Find("Intensity");
            rangeSlider = panel.Find("Range");
            //innerAngleSlider = panel.Find("InnerAngle");
            outerAngleSlider = panel.Find("Angle");
            castShadowsCheckbox = panel.Find("CastShadows");
            enableCheckbox = panel.Find("Enable");

            DisableUI();

            Init();
            CreateTooltips();

            // Camera list
            GlobalState.ObjectAddedEvent.AddListener(OnLightAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnLightRemoved);
            GlobalState.ObjectRenamedEvent.AddListener(OnLightRenamed);
            if (null != lightList) { lightList.ItemClickedEvent += OnSelectLightItem; }
            lightItemPrefab = Resources.Load<GameObject>("Prefabs/UI/LightItem");

            globalCastShadows = panel.Find("GlobalCastShadows");
            SetCheckboxValue(globalCastShadows, GlobalState.Instance.settings.castShadows);
        }

        private void Start()
        {
            GlobalState.colorChangedEvent.AddListener(OnLightColor);
            GlobalState.colorClickedEvent.AddListener(OnColorPickerPressed);
            GlobalState.colorReleasedEvent.AddListener((Color color) => OnReleased());
        }

        public override void OnUIObjectEnter(int gohash)
        {
            UIObject = ToolsUIManager.Instance.GetUI3DObject(gohash);
        }

        public override void OnUIObjectExit(int gohash)
        {
            UIObject = null;
        }

        public void CreateLight(string lightType)
        {
            GameObject light = null;

            switch (lightType)
            {
                case "Sun":
                    light = sunPrefab;
                    break;
                case "Spot":
                    light = spotPrefab;
                    break;
                case "Point":
                    light = pointPrefab;
                    break;
            }

            if (light)
            {
                Matrix4x4 matrix = parentContainer.worldToLocalMatrix * mouthpiece.localToWorldMatrix * Matrix4x4.Scale(new Vector3(10f, 10f, 10f));
                GameObject instance = SyncData.InstantiateUnityPrefab(light, matrix);

                CommandGroup undoGroup = new CommandGroup("Instantiate Light");
                try
                {
                    ClearSelection();
                    new CommandAddGameObject(instance).Submit();
                    AddToSelection(instance);
                    Selection.SetHoveredObject(instance);
                }
                finally
                {
                    undoGroup.Submit();
                }
            }
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    CreateLight(UIObject.name);
                    UIObject = null;
                }
                OnStartGrip();
            }, OnEndGrip);
        }

        private void SetSliderValues(Transform slider, float value, float minValue, float maxValue)
        {
            UISlider sliderComp = slider.GetComponent<UISlider>();
            if (sliderComp != null)
            {
                sliderComp.minValue = minValue;
                sliderComp.maxValue = maxValue;
                sliderComp.Value = value;
            }
        }

        private void SetCheckboxValue(Transform checkbox, bool value)
        {
            UICheckbox checkboxComp = checkbox.GetComponent<UICheckbox>();
            if (checkboxComp != null)
            {
                checkboxComp.Checked = value;
            }
        }

        protected override void UpdateUI()
        {
            foreach (var item in Selection.selection)
            {
                GameObject gobject = item.Value;
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;

                GlobalState.CurrentColor = lightController.color;

                SetSliderValues(intensitySlider, lightController.intensity, lightController.minIntensity, lightController.maxIntensity);
                SetSliderValues(rangeSlider, lightController.range, lightController.minRange, lightController.maxRange);
                //SetSliderValue(innerAngleSlider, lightingParameters.GetInnerAngle());
                SetSliderValues(outerAngleSlider, lightController.outerAngle, 0f, 180f);

                SetCheckboxValue(castShadowsCheckbox, lightController.castShadows);
                SetCheckboxValue(enableCheckbox, gobject.activeSelf);

                // Only the first light sets its parameters to the widgets
                break;
            }
        }

        protected override void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            base.OnSelectionChanged(sender, args);
            // update lighting panel from selection
            ////////////////////////////////////////

            int sunCount = 0;
            int pointCount = 0;
            int spotCount = 0;

            List<GameObject> selectedLights = new List<GameObject>();
            foreach (GameObject gobject in Selection.GetGrippedOrSelection())
            {
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;

                selectedLights.Add(gobject);
                switch (lightController.lightType)
                {
                    case LightType.Directional:
                        sunCount++;
                        break;
                    case LightType.Point:
                        pointCount++;
                        break;
                    case LightType.Spot:
                        spotCount++;
                        break;
                }
            }

            if (selectedLights.Count == 0)
            {
                DisableUI();
                return;
            }

            intensitySlider.gameObject.SetActive(true);
            rangeSlider.gameObject.SetActive(sunCount == 0);

            //innerAngleSlider.gameObject.SetActive(sunCount == 0 && pointCount == 0);
            outerAngleSlider.gameObject.SetActive(sunCount == 0 && pointCount == 0);

            castShadowsCheckbox.gameObject.SetActive(true);
            enableCheckbox.gameObject.SetActive(true);

            UpdateUI();
        }

        public void SendLightParams(GameObject light)
        {
            LightInfo lightInfo = new LightInfo { transform = light.transform };
            CommandManager.SendEvent(MessageType.Light, lightInfo);
        }

        public void OnLightColor(Color color)
        {
            if (!gameObject.activeSelf) { return; }

            // update selection light color from UI
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;
                lightingController.color = color;
                SendLightParams(gobject);
            }
        }

        public void OnCheckEnable(bool value)
        {
            OnBoolChangeParameter("Enable", value);
        }

        public void OnCheckCastShadows(bool value)
        {
            OnBoolChangeParameter("CastShadows", value);
        }

        private void OnBoolChangeParameter(string param, bool value)
        {
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;
                if (param == "CastShadows")
                {
                    lightingController.castShadows = value;
                }

                if (param == "Enable")
                {
                    Light light = gobject.transform.GetComponentInChildren<Light>(true);
                    light.gameObject.SetActive(value);
                }

                SendLightParams(gobject);
            }
        }

        public void OnGlobalCheckCastShadows(bool value)
        {
            // Set the cast shadows parameter to all lights
            LightController[] lightControllers = FindObjectsOfType<LightController>() as LightController[];
            foreach (LightController lightController in lightControllers)
            {
                lightController.castShadows = value;
                SendLightParams(lightController.gameObject);
            }
        }

        public void OnIntensitySliderPressed()
        {
            OnSliderPressed("Light Intensity", "/LightController/intensity");
        }
        public void OnRangeSliderPressed()
        {
            OnSliderPressed("Light Range", "/LightController/range");
        }
        public void OnAngleSliderPressed()
        {
            OnSliderPressed("Light Angle", "/LightController/outerAngle");
        }

        public void OnCastShadowCheckboxPressed()
        {
            OnCheckboxPressed("Light Cast Shadows", "/LightController/castShadows");
        }

        public void OnGlobalCastShadowCheckboxPressed()
        {
            // Get all lights
            LightController[] lightControllers = FindObjectsOfType<LightController>() as LightController[];
            List<GameObject> lights = new List<GameObject>();
            foreach (LightController lightController in lightControllers)
            {
                lights.Add(lightController.gameObject);
            }

            // Create command for all the lights (not only selected ones)
            parameterCommand = new CommandSetValue<bool>(lights, "Light Cast Shadows", "/LightController/castShadows");
        }

        public void OnColorPickerPressed()
        {
            if (!gameObject.activeSelf) { return; }
            OnColorPressed("Light Color", "/LightController/color");
        }


        public void OnChangeIntensity(float value)
        {
            OnFloatChangeParameter("Intensity", value);
        }

        public void OnChangeRange(float value)
        {
            OnFloatChangeParameter("Range", value);
        }

        public void OnChangeOuterAngle(float value)
        {
            OnFloatChangeParameter("OuterAngle", value);
        }

        public void OnChangeInnerAngle(float value)
        {
            OnFloatChangeParameter("InnerAngle", value);
        }

        private void OnFloatChangeParameter(string param, float value)
        {
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;

                if (param == "Intensity")
                    lightingController.intensity = value;
                if (param == "Range")
                    lightingController.range = value;
                if (param == "OuterAngle")
                    lightingController.outerAngle = value;
                if (param == "InnerAngle")
                    lightingController.innerAngle = value;
                SendLightParams(gobject);
            }
        }

        public void OnLightAdded(GameObject gObject)
        {
            LightController controller = gObject.GetComponent<LightController>();
            if (null == controller)
                return;
            GameObject lightItemObject = Instantiate(lightItemPrefab);
            LightItem lightItem = lightItemObject.GetComponentInChildren<LightItem>();
            lightItem.SetLightObject(gObject, controller);
            UIDynamicListItem item = lightList.AddItem(lightItem.transform);
            item.UseColliderForUI = true;
        }

        public void OnLightRemoved(GameObject gObject)
        {
            LightController controller = gObject.GetComponent<LightController>();
            if (null == controller)
                return;
            foreach (var item in lightList.GetItems())
            {
                LightItem lightItem = item.Content.GetComponent<LightItem>();
                if (lightItem.lightObject == gObject)
                {
                    lightList.RemoveItem(item);
                    return;
                }
            }
        }

        public void OnLightRenamed(GameObject gObject)
        {
            LightController controller = gObject.GetComponent<LightController>();
            if (null == controller)
                return;
            foreach (UIDynamicListItem item in lightList.GetItems())
            {
                LightItem lightItem = item.Content.gameObject.GetComponent<LightItem>();
                if (lightItem.lightObject == gObject)
                {
                    lightItem.SetItemName(gObject.name);
                }
            }
        }

        public void OnSelectLightItem(object sender, IndexedGameObjectArgs args)
        {
            GameObject item = args.gobject;
            LightItem lightItem = item.GetComponent<LightItem>();

            // Select light in scene
            CommandGroup command = new CommandGroup("Select Light");
            try
            {
                ClearSelection();
                AddToSelection(lightItem.lightObject);
            }
            finally
            {
                command.Submit();
            }
        }
    }
}
