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

        private void OnEnable()
        {
        }

        void Start()
        {
            Init();
            switchToSelectionEnabled = false;

            picker = panel.Find("ColorPicker");
            intensitySlider = panel.Find("Intensity");
            rangeSlider = panel.Find("Range");
            //innerAngleSlider = panel.Find("InnerAngle");
            outerAngleSlider = panel.Find("Angle");
            castShadowsCheckbox = panel.Find("CastShadows");
            enableCheckbox = panel.Find("Enable");

            DisableUI();

            Selection.OnSelectionChanged += OnSelectionChanged;
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
                    light = Utils.CreateInstance(sunPrefab, parentContainer);
                    break;
                case "Spot":
                    light = Utils.CreateInstance(spotPrefab, parentContainer);
                    break;
                case "Point":
                    light = Utils.CreateInstance(pointPrefab, parentContainer);
                    break;
            }

            if (light)
            {
                new CommandAddGameObject(light).Submit();
                Matrix4x4 matrix = parentContainer.worldToLocalMatrix * transform.localToWorldMatrix;
                light.transform.localPosition = matrix.GetColumn(3);
                light.transform.localRotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                light.transform.localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

                ClearSelection();
                AddToSelection(light);
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

        void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            // update lighting panel from selection
            ////////////////////////////////////////

            int sunCount = 0;
            int pointCount = 0;
            int spotCount = 0;

            Dictionary<int, GameObject> selectedLights = new Dictionary<int, GameObject>();
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;
                LightParameters lightParameters = lightController.GetParameters() as LightParameters;
                if(lightParameters != null)
                {
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

            /*
            if (sunCount > 0)
            {
                intensitySlider.GetComponent<SliderComp>().MinValue = 0f;
                intensitySlider.GetComponent<SliderComp>().MaxValue = 10f;
            }
            else
            {
                intensitySlider.GetComponent<SliderComp>().MinValue = 0f;
                intensitySlider.GetComponent<SliderComp>().MaxValue = 10000f;
            }
            */

            foreach (KeyValuePair<int, GameObject> data in selectedLights)
            {
                GameObject gobject = data.Value;
                LightParameters lightingParameters = gobject.GetComponent<LightController>().GetParameters() as LightParameters;

                /*
                ColorPickerControl pickerControl = picker.GetComponent<ColorPickerControl>();
                pickerControl.blockSignals = true;
                pickerControl.CurrentColor = lightingParameters.color;
                pickerControl.blockSignals = false;
                */

                // TODO: put min/max for each slider in LightParameters

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

        public void OnLightColor(Color color)
        {
            // update selection light color from UI
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                LightParameters lightingParameters = gobject.GetComponent<LightController>().GetParameters() as LightParameters;
                if (lightingParameters != null)
                    lightingParameters.color = color;
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
                LightParameters lightingParameters = gobject.GetComponent<LightController>().GetParameters() as LightParameters;
                if (lightingParameters != null)
                {
                    if (param == "CastShadows")
                    {
                        lightingParameters.castShadows = value;
                    }

                    if (param == "Enable")
                    {
                        Light light = gobject.transform.GetComponentInChildren<Light>(true);
                        light.gameObject.SetActive(value);
                    }
                }
            }
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
                LightParameters lightingParameters = gobject.GetComponent<LightController>().GetParameters() as LightParameters;
                if (lightingParameters != null)
                {
                    if (param == "Intensity")
                        lightingParameters.intensity = value;
                    if (param == "Range")
                        lightingParameters.SetRange(value);
                    if (param == "OuterAngle")
                        lightingParameters.SetOuterAngle(value);
                    if (param == "InnerAngle")
                        lightingParameters.SetInnerAngle(value);
                }
            }
        }
    }
}
