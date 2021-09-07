using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR;

namespace VRtist
{
    public class VRControllerManager : MonoBehaviour
    {
        [System.Serializable]
        public class VRController
        {
            public Transform controllerTransform;
            public GameObject rootObject;
            public TextMeshProUGUI controllerDisplay;
            public Transform gripDisplay;
            public Transform triggerDisplay;
            public Transform primaryButtonDisplay;
            public Transform secondaryButtonDisplay;
            public Transform joystickDisplay;
            public Transform mouthpieceHolder;
            public Transform laserHolder;
            public Transform upAxis;
            public Transform paletteHolder;
            public Transform helperHolder;
            public Transform frontAnchor;

            public VRController(string ControllerPath, string rootPath, Transform toolsPalette)
            {
                controllerTransform = toolsPalette.Find(ControllerPath);
                rootObject = toolsPalette.Find(rootPath).gameObject;
                rootObject.SetActive(true);
                controllerDisplay = controllerTransform.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
                mouthpieceHolder = controllerTransform.Find("MouthpieceHolder");
                laserHolder = controllerTransform.Find("LaserHolder");
                upAxis = controllerTransform.Find("UpAxis");
                paletteHolder = controllerTransform.Find("PaletteHolder");
                helperHolder = controllerTransform.Find("HelperHolder");
                frontAnchor = controllerTransform.Find("FrontAnchor");
                GetControllerTooltips();
            }

            private void GetControllerTooltips()
            {
                gripDisplay = controllerTransform.Find("GripButtonAnchor/Tooltip");
                triggerDisplay = controllerTransform.Find("TriggerButtonAnchor/Tooltip");
                primaryButtonDisplay = controllerTransform.Find("PrimaryButtonAnchor/Tooltip");
                secondaryButtonDisplay = controllerTransform.Find("SecondaryButtonAnchor/Tooltip");
                joystickDisplay = controllerTransform.Find("JoystickBaseAnchor/Tooltip");
            }

            public void SetActive(bool isActive)
            {
                rootObject.SetActive(isActive);
                controllerTransform.gameObject.SetActive(isActive);
            }
        }

        public ControllerPath ControllerPaths;

        private VRController rightController;
        private VRController inverseRightController;
        private VRController leftController;
        private VRController inverseLeftController;

        public enum ControllerModel { Index, Quest, Quest2 }


        internal void InitializeControllers(string name)
        {
            switch (name)
            {
                case "Index Controller OpenXR": InitializeControllersFromModel(ControllerModel.Index); break;
                case "Oculus Touch Controller OpenXR": InitializeOculusController(GetHMD()); break;
                default: InitializeControllersFromModel(ControllerModel.Quest); break;
            }
        }

        internal void InitializeOculusController(string name)
        {
            switch (name)
            {
                case "Oculus Rift S": InitializeControllersFromModel(ControllerModel.Quest); break;
                case "Oculus Quest": InitializeControllersFromModel(ControllerModel.Quest); break;
                case "Oculus Quest2": InitializeControllersFromModel(ControllerModel.Quest2); break;
                default: InitializeControllersFromModel(ControllerModel.Quest); break;
            }
        }

        public void InitializeControllersFromModel(ControllerModel model)
        {
            if (null != rightController) rightController.SetActive(false);
            if (null != leftController) leftController.SetActive(false);
            if (null != inverseRightController) inverseRightController.SetActive(false);
            if (null != inverseLeftController) inverseLeftController.SetActive(false);
            GetControllerValues(model);
            SetRightHanded(GlobalState.Settings.rightHanded);
        }

        private static string GetHMD()
        {
            string exe = "VRtistOpenXRHMD.exe";
#if UNITY_EDITOR
            string path = "./Data/bin";
#else
            string path = "./VRtist_Data/bin";
#endif
            string fullpath = System.IO.Path.GetFullPath(path) + "/" + exe;
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(fullpath);
            info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute = false;
            System.Diagnostics.Process process = System.Diagnostics.Process.Start(info);
            process.WaitForExit();
            return process.StandardOutput.ReadLine();
        }

        public void SetRightHanded(bool value)
        {
            inverseRightController.controllerTransform.gameObject.SetActive(!value);
            inverseLeftController.controllerTransform.gameObject.SetActive(!value);
            rightController.controllerTransform.gameObject.SetActive(value);
            leftController.controllerTransform.gameObject.SetActive(value);

            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;


            // Update controller's displays
            rightController.controllerDisplay.text = "";
            inverseRightController.controllerDisplay.text = "";
            leftController.controllerDisplay.text = "";
            inverseLeftController.controllerDisplay.text = "";

            // Update tooltips
            Tooltips.HideAll(VRDevice.PrimaryController);
            Tooltips.HideAll(VRDevice.SecondaryController);
            ToolBase tool = ToolsManager.CurrentTool();
            if (null != tool)
            {
                tool.SetTooltips();
            }
            GlobalState.Instance.playerController.HandleCommonTooltipsVisibility();

            Transform palette = GlobalState.Instance.paletteController.Find("PaletteHandle");
            if (value)
            {
                SetHolders(leftController, rightController, toolsController, paletteController, palette);
            }
            else
            {
                SetHolders(inverseRightController, inverseLeftController, toolsController, paletteController, palette);
            }
        }

        private void SetHolders(VRController primary, VRController secondary, Transform toolsController, Transform paletteController, Transform palette)
        {
            if (null != palette && !GlobalState.Instance.settings.pinnedPalette)
                palette.SetPositionAndRotation(primary.paletteHolder.TransformPoint(GlobalState.Instance.settings.palettePositionOffset), primary.paletteHolder.rotation * GlobalState.Instance.settings.paletteRotationOffset);
            toolsController.Find("mouthpieces").SetPositionAndRotation(secondary.mouthpieceHolder.position, secondary.mouthpieceHolder.rotation);
            toolsController.Find("SelectionHelper").SetPositionAndRotation(secondary.helperHolder.position, secondary.helperHolder.rotation);
            paletteController.Find("SceneHelper").SetPositionAndRotation(primary.helperHolder.position, primary.helperHolder.rotation);
        }

        public void SetPaletteHolder(Transform palette)
        {
            if (GlobalState.Settings.rightHanded)
                palette.SetPositionAndRotation(leftController.paletteHolder.position, leftController.paletteHolder.rotation);
            else
                palette.SetPositionAndRotation(inverseRightController.paletteHolder.position, inverseRightController.paletteHolder.rotation);
        }

        private void GetControllerValues(ControllerModel model)
        {
            Transform toolsController = GlobalState.Instance.toolsController;
            Transform paletteController = GlobalState.Instance.paletteController;

            rightController = new VRController(ControllerPaths.GetControllerPath(true, model), ControllerPaths.GetControllerRoot(model), toolsController);
            rightController.controllerTransform.gameObject.SetActive(true);
            toolsController.Find("mouthpieces").position = rightController.mouthpieceHolder.position;

            inverseRightController = new VRController(ControllerPaths.GetControllerPath(true, model), ControllerPaths.GetControllerRoot(model), paletteController);

            leftController = new VRController(ControllerPaths.GetControllerPath(false, model), ControllerPaths.GetControllerRoot(model), paletteController);
            leftController.controllerTransform.gameObject.SetActive(true);

            inverseLeftController = new VRController(ControllerPaths.GetControllerPath(false, model), ControllerPaths.GetControllerRoot(model), toolsController);
        }

        public Transform GetPrimaryControllerTransform()
        {
            if (null == rightController || null == inverseLeftController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return rightController.controllerTransform;
            else return inverseLeftController.controllerTransform;
        }
        public Transform GetSecondaryControllerTransform()
        {
            if (null == leftController || null == inverseRightController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return leftController.controllerTransform;
            else return inverseRightController.controllerTransform;
        }

        public Vector3 GetPrimaryControllerUp()
        {
            if (null == rightController || null == inverseLeftController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return rightController.upAxis.up;
            else return inverseLeftController.upAxis.up;
        }

        public Vector3 GetSecondaryControllerUp()
        {
            if (null == leftController || null == inverseRightController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return leftController.upAxis.up;
            else return inverseRightController.upAxis.up;
        }

        internal Transform GetPrimaryTooltipTransform(Tooltips.Location location)
        {
            if (null == rightController || null == inverseLeftController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return GetTooltipTransform(rightController, location);
            else return GetTooltipTransform(inverseLeftController, location);
        }


        internal Transform GetSecondaryTooltipTransform(Tooltips.Location location)
        {
            if (null == leftController || null == inverseRightController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return GetTooltipTransform(leftController, location);
            else return GetTooltipTransform(inverseRightController, location);
        }

        public TextMeshProUGUI GetPrimaryDisplay()
        {
            if (null == rightController || null == inverseLeftController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return rightController.controllerDisplay;
            else return inverseLeftController.controllerDisplay;
        }
        public TextMeshProUGUI GetSecondaryDisplay()
        {
            if (null == leftController || null == inverseRightController) InitializeControllersFromModel(ControllerModel.Quest);
            if (GlobalState.Settings.rightHanded) return leftController.controllerDisplay;
            else return inverseRightController.controllerDisplay;
        }

        private Transform GetTooltipTransform(VRController controller, Tooltips.Location location)
        {
            switch (location)
            {
                case Tooltips.Location.Grip: return controller.gripDisplay;
                case Tooltips.Location.Joystick: return controller.joystickDisplay;
                case Tooltips.Location.Primary: return controller.primaryButtonDisplay;
                case Tooltips.Location.Secondary: return controller.secondaryButtonDisplay;
                case Tooltips.Location.Trigger: return controller.triggerDisplay;
                default: return null;
            }
        }

        public Transform GetLaserTransform()
        {
            if (GlobalState.Settings.rightHanded) return rightController.laserHolder;
            else return inverseLeftController.laserHolder;
        }

        public Transform GetPaletteTransform()
        {
            if (GlobalState.Settings.rightHanded) return leftController.paletteHolder;
            else return inverseRightController.paletteHolder;
        }

        public Transform GetFrontAnchor()
        {
            if (GlobalState.Settings.rightHanded) return leftController.frontAnchor;
            else return inverseRightController.frontAnchor;
        }

    }
}
