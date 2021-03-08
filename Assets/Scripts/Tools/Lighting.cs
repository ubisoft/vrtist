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
using UnityEngine.XR;

namespace VRtist
{
    public class Lighting : SelectorBase
    {
        [Header("Lighting Parameters")]
        [SerializeField] private Transform parentContainer;

        enum LightTools { None = 0, Sun, Spot, Point }

        private Transform globalCastShadows;
        private Transform intensitySlider;
        private Transform rangeSlider;
        private Transform sharpnessSlider;
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
            sharpnessSlider.gameObject.SetActive(false);
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
            sharpnessSlider = panel.Find("Sharpness");
            outerAngleSlider = panel.Find("Angle");
            castShadowsCheckbox = panel.Find("CastShadows");
            enableCheckbox = panel.Find("Enable");

            DisableUI();

            Init();
            SetTooltips();

            // Camera list
            GlobalState.ObjectAddedEvent.AddListener(OnLightAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnLightRemoved);
            GlobalState.ObjectRenamedEvent.AddListener(OnLightRenamed);
            SceneManager.clearSceneEvent.AddListener(OnClearScene);
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
            ToolsUIManager.Instance.onPaletteOpened.AddListener(OnPaletteOpened);
        }
        void OnPaletteOpened()
        {
            lightList.NeedsRebuild = true;
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
                    light = ResourceManager.GetPrefab(PrefabID.SunLight);
                    break;
                case "Spot":
                    light = ResourceManager.GetPrefab(PrefabID.SpotLight);
                    break;
                case "Point":
                    light = ResourceManager.GetPrefab(PrefabID.PointLight);
                    break;
            }

            if (light)
            {
                Matrix4x4 matrix = parentContainer.worldToLocalMatrix * mouthpiece.localToWorldMatrix * Matrix4x4.Scale(new Vector3(10f, 10f, 10f));

                GameObject instance = SceneManager.InstantiateUnityPrefab(light);
                Vector3 position = matrix.GetColumn(3);
                Quaternion rotation = Quaternion.AngleAxis(180, Vector3.forward) * Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                Vector3 scale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

                CommandGroup undoGroup = new CommandGroup("Instantiate Light");
                try
                {
                    ClearSelection();
                    CommandAddGameObject command = new CommandAddGameObject(instance);
                    command.Submit();
                    instance = command.newObject;
                    AddToSelection(instance);
                    SceneManager.SetObjectTransform(instance, position, rotation, scale);
                    Selection.HoveredObject = instance;
                }
                finally
                {
                    undoGroup.Submit();
                }
            }
        }

        protected override void DoUpdateGui()
        {
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.gripButton, () =>
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

        protected override void DoUpdate()
        {
            UpdateUI();
            base.DoUpdate();
        }

        protected override void UpdateUI()
        {
            foreach (var gobject in Selection.SelectedObjects)
            {
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;

                GlobalState.CurrentColor = lightController.Color;

                SetSliderValues(intensitySlider, lightController.Intensity, lightController.minIntensity, lightController.maxIntensity);
                SetSliderValues(rangeSlider, lightController.Range, lightController.minRange, lightController.maxRange);
                SetSliderValues(sharpnessSlider, lightController.Sharpness, 0f, 100f);
                SetSliderValues(outerAngleSlider, lightController.OuterAngle, 0f, 180f);

                SetCheckboxValue(castShadowsCheckbox, lightController.CastShadows);
                SetCheckboxValue(enableCheckbox, gobject.activeSelf);

                // Only the first light sets its parameters to the widgets
                break;
            }
        }

        protected override void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            base.OnSelectionChanged(previousSelection, currentSelection);
            // update lighting panel from selection
            ////////////////////////////////////////

            int sunCount = 0;
            int pointCount = 0;
            int spotCount = 0;

            List<GameObject> selectedLights = new List<GameObject>();
            foreach (GameObject gobject in Selection.ActiveObjects)
            {
                LightController lightController = gobject.GetComponent<LightController>();
                if (null == lightController)
                    continue;

                selectedLights.Add(gobject);
                switch (lightController.Type)
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

            sharpnessSlider.gameObject.SetActive(sunCount == 0 && pointCount == 0);
            outerAngleSlider.gameObject.SetActive(sunCount == 0 && pointCount == 0);

            castShadowsCheckbox.gameObject.SetActive(true);
            enableCheckbox.gameObject.SetActive(true);

            UpdateUI();
        }

        public void SendLightParams(GameObject light)
        {
            SceneManager.SendLightInfo(light.transform);
        }

        public void OnLightColor(Color color)
        {
            if (!gameObject.activeSelf) { return; }

            // update selection light color from UI
            foreach (GameObject gobject in Selection.SelectedObjects)
            {
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;
                lightingController.Color = color;
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
            foreach (GameObject gobject in Selection.SelectedObjects)
            {
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;
                if (param == "CastShadows")
                {
                    lightingController.CastShadows = value;
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
                lightController.CastShadows = value;
                SendLightParams(lightController.gameObject);
            }
        }

        public void OnIntensitySliderPressed()
        {
            OnSliderPressed("Light Intensity", "/LightController/Intensity");
        }
        public void OnRangeSliderPressed()
        {
            OnSliderPressed("Light Range", "/LightController/Range");
        }
        public void OnAngleSliderPressed()
        {
            OnSliderPressed("Light Angle", "/LightController/OuterAngle");
        }
        public void OnSharpnessSliderPressed()
        {
            OnSliderPressed("Light Sharpness", "/LightController/Sharpness");
        }

        public void OnCastShadowCheckboxPressed()
        {
            OnCheckboxPressed("Light Cast Shadows", "/LightController/CastShadows");
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
            parameterCommand = new CommandSetValue<bool>(lights, "Light Cast Shadows", "/LightController/CastShadows");
        }

        public void OnColorPickerPressed()
        {
            if (!gameObject.activeSelf) { return; }
            OnColorPressed("Light Color", "/LightController/Color");
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

        public void OnChangeSharpness(float value)
        {
            OnFloatChangeParameter("Sharpness", value);
        }

        private void OnFloatChangeParameter(string param, float value)
        {
            foreach (GameObject gobject in Selection.SelectedObjects)
            {
                LightController lightingController = gobject.GetComponent<LightController>();
                if (null == lightingController)
                    continue;

                if (param == "Intensity")
                    lightingController.Intensity = value;
                if (param == "Range")
                    lightingController.Range = value;
                if (param == "OuterAngle")
                    lightingController.OuterAngle = value;
                if (param == "Sharpness")
                    lightingController.Sharpness = value;
                SendLightParams(gobject);
            }
        }

        private void OnLightAdded(GameObject gObject)
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

        private void OnLightRemoved(GameObject gObject)
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

        private void OnLightRenamed(GameObject gObject)
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

        private void OnClearScene()
        {
            lightList.Clear();
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
