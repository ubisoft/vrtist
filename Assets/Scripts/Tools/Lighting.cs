using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.UI.Extensions.ColorPicker;
using UnityEngine.XR;

namespace VRtist
{
    public class Lighting : Selector
    {
        [Header("Lighting Parameters")]
        [SerializeField] private Transform parentContainer;
        [SerializeField] private GameObject sunPrefab;
        [SerializeField] private GameObject pointPrefab;
        [SerializeField] private GameObject spotPrefab;

        enum LightTools { None = 0, Sun, Spot, Point }

        private Transform picker;
        private Transform intensitySlider;
        private Transform rangeSlider;
        //private Transform innerAngleSlider;
        private Transform outerAngleSlider;
        private Transform castShadowsCheckbox;
        private Transform enableCheckbox;

        private GameObject UIObject = null;

        private UIColorPicker colorPicker = null;

        void DisableUI()
        {
            picker.gameObject.SetActive(false);
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

            picker = panel.Find("ColorPicker");
            colorPicker = picker.GetComponent<UIColorPicker>();
            intensitySlider = panel.Find("Intensity");
            rangeSlider = panel.Find("Range");
            //innerAngleSlider = panel.Find("InnerAngle");
            outerAngleSlider = panel.Find("Angle");
            castShadowsCheckbox = panel.Find("CastShadows");
            enableCheckbox = panel.Find("Enable");

            DisableUI();

            Init();
            enableToggleTool = false;
            Selection.OnSelectionChanged += OnSelectionChanged;
            CreateTooltips();
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
                Matrix4x4 matrix = parentContainer.worldToLocalMatrix * transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(0.1f, 0.1f, 0.1f));
                GameObject instance = SyncData.InstantiateUnityPrefab(light, matrix);

                CommandGroup undoGroup = new CommandGroup();
                new CommandAddGameObject(instance).Submit();                
                ClearSelection();
                AddToSelection(instance);
                undoGroup.Submit();
            }
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    CreateLight(UIObject.name);
                }
                OnStartGrip();
            }, OnEndGrip);
        }

        // Update is called once per frame
        //protected override void DoUpdate(Vector3 position, Quaternion rotation)
        //{
        //    VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
        //    {
        //        OnStartGrip();
        //    }, OnEndGrip);

        //    base.DoUpdate(position, rotation);
        //}

        protected override void ShowTool(bool show)
        {
            Transform sphere = gameObject.transform.Find("Sphere");
            if (sphere != null)
            {
                sphere.gameObject.SetActive(show);
            }

            Transform rightController = gameObject.transform.Find("right_controller");
            if (rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        private void SetSliderValues(Transform slider, float value, float minValue, float maxValue)
        {
            UISlider sliderComp = slider.GetComponent<UISlider>();
            if(sliderComp != null)
            {
                sliderComp.minValue = minValue;
                sliderComp.maxValue = maxValue;
                sliderComp.Value = value;
            }
        }

        private void SetCheckboxValue(Transform checkbox, bool value)
        {
            UICheckbox checkboxComp = checkbox.GetComponent<UICheckbox>();
            if (checkboxComp!= null)
            {
                checkboxComp.Checked = value;
            }
        }

        protected override void UpdateUI()
        {
            foreach(var item in Selection.selection)
            {
                GameObject gobject = item.Value;
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;
                LightParameters lightingParameters = lightController.GetParameters() as LightParameters;
                if (null == lightingParameters)
                    continue;

                colorPicker.CurrentColor = lightingParameters.color;

                SetSliderValues(intensitySlider, lightingParameters.intensity, lightingParameters.minIntensity, lightingParameters.maxIntensity);
                SetSliderValues(rangeSlider, lightingParameters.GetRange(), lightingParameters.GetMinRange(), lightingParameters.GetMaxRange());
                //SetSliderValue(innerAngleSlider, lightingParameters.GetInnerAngle());
                SetSliderValues(outerAngleSlider, lightingParameters.GetOuterAngle(), lightingParameters.GetMinOuterAngle(), lightingParameters.GetMaxOuterAngle());

                SetCheckboxValue(castShadowsCheckbox, lightingParameters.castShadows);
                SetCheckboxValue(enableCheckbox, gobject.activeSelf);

                // Only the first light sets its parameters to the widgets
                break;
            }
        }

        void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            // update lighting panel from selection
            ////////////////////////////////////////

            int sunCount = 0;
            int pointCount = 0;
            int spotCount = 0;

            ClearListeners();

            Dictionary<int, GameObject> selectedLights = new Dictionary<int, GameObject>();
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;
                LightParameters lightParameters = lightController.GetParameters() as LightParameters;
                if (null == lightParameters)
                    continue;

                AddListener(lightController);

                selectedLights[data.Key] = data.Value;
                switch(lightParameters.GetLightType())
                {
                    case LightParameters.LightType.Sun:
                        sunCount++;
                        break;
                    case LightParameters.LightType.Point:
                        pointCount++;
                        break;
                    case LightParameters.LightType.Spot:
                        spotCount++;
                        break;
                }
            }

            if (selectedLights.Count == 0)
            {
                DisableUI();
                return;
            }

            picker.gameObject.SetActive(true);
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
            LightInfo lightInfo = new LightInfo();
            lightInfo.transform = light.transform;
            CommandManager.SendEvent(MessageType.Light, lightInfo);
        }

        public void OnLightColor(Color color)
        {
            // update selection light color from UI
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;
                LightParameters lightingParameters  = lightingController.GetParameters() as LightParameters;
                if (null == lightingParameters)
                    continue;
                lightingParameters.color = color;
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
                LightParameters lightingParameters = lightingController.GetParameters() as LightParameters;
                if (null == lightingParameters)
                    continue;
                if (param == "CastShadows")
                {
                    lightingParameters.castShadows = value;
                }

                if (param == "Enable")
                {
                    Light light = gobject.transform.GetComponentInChildren<Light>(true);
                    light.gameObject.SetActive(value);
                }
                
                SendLightParams(gobject);
            }
        }

        public void OnIntensitySliderPressed()
        {
            OnSliderPressed("Light Intensity", "/LightController/parameters.intensity");
        }
        public void OnRangeSliderPressed()
        {
            OnSliderPressed("Light Range", "/LightController/parameters.range");
        }
        public void OnAngleSliderPressed()
        {
            OnSliderPressed("Light Angle", "/LightController/parameters.outerAngle");
        }       

        public void OnCastShadowCheckboxPressed()
        {
            OnCheckboxPressed("Light Cast Shadows", "/LightController/parameters.castShadows");
        }

        public void OnColorPickerPressed()
        {
            OnColorPressed("Light Color", "/LightController/parameters.color");
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
                LightParameters lightingParameters = lightingController.GetParameters() as LightParameters;
                if (null == lightingParameters)
                    continue;

                if (param == "Intensity")
                    lightingParameters.intensity = value;
                if (param == "Range")
                    lightingParameters.SetRange(value);
                if (param == "OuterAngle")
                    lightingParameters.SetOuterAngle(value);
                if (param == "InnerAngle")
                    lightingParameters.SetInnerAngle(value);
                SendLightParams(gobject);
            }
        }
    }
}
