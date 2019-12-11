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

        static int sunId = 0;
        static int spotId = 0;
        static int pointId = 0;

        enum LightTools { None = 0, Sun, Spot, Point }
        LightTools lightTool = LightTools.None;

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
            outerAngleSlider = panel.Find("OuterAngle");
            castShadowsCheckbox = panel.Find("CastShadows");
            enableCheckbox = panel.Find("Enable");

            DisableUI();

            Selection.OnSelectionChanged += OnSelectionChanged;
        }


        public override void OnUIObjectEnter(GameObject gObject)
        {
            UIObject = gObject;
        }
        public override void OnUIObjectExit(GameObject gObject)
        {
            UIObject = null;
        }

        // Update is called once per frame
        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.gripButton, () =>
            {
                if (UIObject)
                {
                    GameObject light = null;
                    string lightName = "";

                    // Create an empty game object with a mesh
                    switch (UIObject.name)
                    {
                        case "Sun":
                            light = Utils.CreateInstance(sunPrefab, parentContainer);
                            lightName = "Sun" + sunId.ToString();
                            sunId++;
                            break;
                        case "Spot":
                            light = Utils.CreateInstance(spotPrefab, parentContainer);
                            lightName = "Spot" + spotId.ToString();
                            spotId++;
                            break;
                        case "Point":
                            light = Utils.CreateInstance(pointPrefab, parentContainer);
                            lightName = "Point" + pointId.ToString();
                            pointId++;
                            break;
                    }

                    if (light)
                    {
                        light.name = lightName;
                        light.transform.position = transform.position;
                        light.transform.rotation = transform.rotation;
                        light.transform.localScale = Vector3.one * 0.1f;

                        Selection.ClearSelection();
                        Selection.AddToSelection(light);
                    }
                }
                OnStartGrip();
            }, OnEndGrip);

            base.DoUpdate(position, rotation);
        }


        private void SetSliderValue(Transform slider, float value)
        {
            // TODO : Re-enable
            /*
            UISlider sliderComp = slider.GetComponent<UISlider>();
            if(sliderComp != null)
            {
                sliderComp.Value = value;
            }
            */
        }

        private void SetCheckboxValue(Transform checkbox, bool value)
        {
            // TODO : Re-enable
            /*
            UICheckbox checkboxComp = checkbox.GetComponent<UICheckbox>();
            if (checkboxComp!= null)
            {
                checkboxComp.Checked = value;
            }*/
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
                LightParameters lightParameters = gobject.GetComponent<LightController>().GetParameters() as LightParameters;
                if(lightParameters != null)
                {
                    selectedLights[data.Key] = data.Value;
                    switch(lightParameters.lightType)
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

                SetSliderValue(intensitySlider, lightingParameters.intensity);
                SetSliderValue(rangeSlider, lightingParameters.GetRange());
                //SetSliderValue(innerAngleSlider, lightingParameters.GetInnerAngle());
                SetSliderValue(outerAngleSlider, lightingParameters.GetOuterAngle());

                SetCheckboxValue(castShadowsCheckbox, lightingParameters.castShadows);
                SetCheckboxValue(enableCheckbox, gobject.activeSelf);

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
                        gobject.transform.GetChild(1).gameObject.SetActive(value);
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
