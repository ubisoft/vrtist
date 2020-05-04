using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Base Parameters")]
        [SerializeField] private Transform world = null;
        [SerializeField] private Transform buttonsContainer = null;
        [SerializeField] private Transform navigationParametersContainer= null;
        [SerializeField] private Transform leftHandle = null;
        [SerializeField] private Transform pivot = null;
        [SerializeField] private Transform vrCamera = null;
        [Tooltip("Player can be xxx times bigger than the world")]
        public float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        [Tooltip("Player can be xxx times smaller than the world")]
        public float minPlayerScale = 50.0f; // world scale = 50.0f;

        [Header("BiManual Navigation Mode")]
        public StretchUI lineUI = null;

        [Header("Fly Navigation")]
        [Tooltip("Speed in m/s")]
        public float flySpeed = 0.2f;

        [Header("Orbit Navigation")]
        public StraightRay ray = null;
        public float moveSpeed = 0.05f;
        [Tooltip("Speed in degrees/s")]
        public float rotationalSpeed = 3.0f;
        public float scaleSpeed = 0.02f;

        [Header("Teleport Navigation")]
        public TeleportUI teleport = null;
        public TrajectoryParams trajectoryParams = new TrajectoryParams();

        private NavigationMode currentNavigationMode = null;

        private const float deadZone = 0.3f; // for palette pop

        private Vector3 initCameraPosition; // for reset
        private Quaternion initCameraRotation; // for reset

        private GameObject tooltipPalette = null;
        private GameObject tooltipUndo = null;
        private GameObject tooltipRedo = null;
        private GameObject tooltipReset = null;

        void Start()
        {
            if (!VRInput.TryGetDevices())
            {
                Debug.LogWarning("PlayerController cannot VRInput.TryGetDevices().");
            }

            if (vrCamera == null) { Debug.LogWarning("Cannot find 'VRCamera' game object"); }
            if (leftHandle == null) { Debug.LogWarning("Cannot find 'LeftHandle' game object"); }
            if (pivot == null) { Debug.LogWarning("Cannot find 'Pivot' game object"); }

            if (ray != null)
                ray.gameObject.SetActive(false);

            if (teleport != null)
                teleport.gameObject.SetActive(false);

            tooltipPalette = Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Trigger, "Display Palette");
            tooltipUndo = Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Primary, "Undo");
            tooltipRedo = Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.Secondary, "Redo");
            tooltipReset = Tooltips.CreateTooltip(leftHandle.Find("left_controller").gameObject, Tooltips.Anchors.JoystickClick, "Reset");

            OnChangeNavigationMode("BiManual");

            initCameraPosition = transform.position; // for reset
            initCameraRotation = transform.rotation; // for reset
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // NAVIGATION
                HandleNavigation();

                // RESET/FIT -- Left Joystick Click
                if (currentNavigationMode == null || IsCompatibleWithReset(currentNavigationMode))
                {
                    HandleReset();
                }

                // RESET SCALE -- Right Joystick Click
                if (currentNavigationMode == null || IsCompatibleWithResetScale(currentNavigationMode))
                {
                    HandleResetScale();
                }

                // PALETTE POP -- Left Trigger
                if (currentNavigationMode == null || IsCompatibleWithPalette(currentNavigationMode))
                {
                    HandlePalette();
                }

                // UNDO/REDO -- Left A/B
                if (currentNavigationMode == null || IsCompatibleWithUndoRedo(currentNavigationMode))
                {
                    HandleUndoRedo();
                }
            }
        }

        private void HandleNavigation()
        {
            if (!leftHandle.gameObject.activeSelf)
            {
                leftHandle.gameObject.SetActive(true);
            }

            // Update left controller transform
            VRInput.UpdateTransformFromVRDevice(leftHandle, VRInput.leftController);

            if(null != currentNavigationMode)
                currentNavigationMode.Update();
        }

        private void FitToSelection()
        {
            Transform cam = vrCamera.transform;
            
            Vector3 bmin = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 bmax = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);

            Vector3[] points = new Vector3[8];
            Vector3 center = Vector3.zero;
            int bboxCount = 0;

            // parse selection
            foreach (var item in Selection.selection)
            {
                // get all meshes of selected object
                GameObject gobj = item.Value;
                MeshFilter[] meshFilters = gobj.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter meshFilter in meshFilters)
                {
                    Mesh mesh = meshFilter.mesh;
                    if (null == mesh)
                        continue;

                    Bounds bounds = mesh.bounds;
                    Vector3 omin = bounds.min;
                    Vector3 omax = bounds.max;
                    bboxCount++;

                    Matrix4x4 meshMatrix = meshFilter.transform.localToWorldMatrix;
                    // Get absolute bbox center
                    Vector3 globalBoundCenter = meshMatrix.MultiplyPoint(bounds.center);
                    center += globalBoundCenter;

                    // Get all points of the bounding box
                    points[0] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omin.y, omin.z));
                    points[1] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omin.y, omax.z));
                    points[2] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omax.y, omin.z));
                    points[3] = meshMatrix.MultiplyPoint(new Vector3(omin.x, omax.y, omax.z));
                    points[4] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omin.y, omin.z));
                    points[5] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omin.y, omax.z));
                    points[6] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omax.y, omin.z));
                    points[7] = meshMatrix.MultiplyPoint(new Vector3(omax.x, omax.y, omax.z));

                    // check min/max
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 p = points[i];
                        if (p.x < bmin.x) bmin.x = p.x;
                        if (p.y < bmin.y) bmin.y = p.y;
                        if (p.z < bmin.z) bmin.z = p.z;
                        if (p.x > bmax.x) bmax.x = p.x;
                        if (p.y > bmax.y) bmax.y = p.y;
                        if (p.z > bmax.z) bmax.z = p.z;
                    }
                }
            }

            center /= (float)bboxCount;

            // compute distance to camera;
            float max = Mathf.Max(bmax.x - bmin.x, bmax.y - bmin.y);
            max = Mathf.Max(max, bmax.z - bmin.z);
            float newDistance = 3 * max;

            // clamp distance with near/far camera planes
            float near = cam.GetComponent<Camera>().nearClipPlane * 1.2f;
            float far = cam.GetComponent<Camera>().farClipPlane * 0.8f;
            if (newDistance < near)
                newDistance = near;
            if (newDistance > far)
                newDistance = far;

            // compute new camera position
            Vector3 cameraGlobalForward = cam.forward;
            Vector3 newCameraPosition = center - cameraGlobalForward * newDistance;
            
            // do not apply the position to the camera but invert it and apply to world
            Vector3 deltaCamera = newCameraPosition - cam.position;
            world.position -= deltaCamera;
        }

        private void HandleReset()
        {
            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.primary2DAxisClick,
            () =>
            {
                if (Selection.selection.Count == 0)
                {
                    world.localPosition = Vector3.zero;
                    world.localRotation = Quaternion.identity;
                    world.localScale = Vector3.one;
                    GlobalState.worldScale = 1f;

                    transform.position = initCameraPosition;
                    transform.rotation = initCameraRotation;
                }
                else
                {
                    FitToSelection();
                }
            });
        }

        private void HandleResetScale()
        {
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.primary2DAxisClick,
            () =>
            {
                Transform cam = vrCamera.transform;

                Vector3 initCameraInWorldPosition = world.worldToLocalMatrix.MultiplyPoint(cam.position);
                world.localScale = Vector3.one;
                GlobalState.worldScale = 1f;
                Vector3 initGlobalCamera = world.localToWorldMatrix.MultiplyPoint(initCameraInWorldPosition);
                world.position += cam.position - initGlobalCamera;

                // reset world up vector as well
                world.up = Vector3.up;
            });
        }

        private void HandlePalette()
        {
            if (null == ToolsUIManager.Instance)
                return;

            if (VRInput.GetValue(VRInput.leftController, CommonUsages.trigger) > deadZone)
            {
                ToolsUIManager.Instance.PopUpPalette(true);
            }
            else
            {
                ToolsUIManager.Instance.PopUpPalette(false);
            }
        }

        private void HandleUndoRedo()
        {
            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.primaryButton, () => { },
            () =>
            {
                CommandManager.Undo();
            });
            VRInput.ButtonEvent(VRInput.leftController, CommonUsages.secondaryButton, () => { },
            () =>
            {
                CommandManager.Redo();
            });
        }

        private void HandleCommonTooltipsVisibility()
        {
            if (currentNavigationMode == null)
                return;

            if (IsCompatibleWithReset(currentNavigationMode))
            {
                Tooltips.SetTooltipVisibility(tooltipReset, true);
                Tooltips.SetTooltipText(tooltipReset, "Reset");
            }
            else
            {
                Tooltips.SetTooltipVisibility(tooltipReset, false);
            }

            if (IsCompatibleWithPalette(currentNavigationMode))
            {
                Tooltips.SetTooltipVisibility(tooltipPalette, true);
                Tooltips.SetTooltipText(tooltipPalette, "Display Palette");
            }
            else
            {
                Tooltips.SetTooltipVisibility(tooltipPalette, false);
            }

            if (IsCompatibleWithUndoRedo(currentNavigationMode))
            {
                Tooltips.SetTooltipVisibility(tooltipUndo, true);
                Tooltips.SetTooltipText(tooltipUndo, "Undo");

                Tooltips.SetTooltipVisibility(tooltipRedo, true);
                Tooltips.SetTooltipText(tooltipRedo, "Redo");
            }
            else
            {
                Tooltips.SetTooltipVisibility(tooltipUndo, false);
                Tooltips.SetTooltipVisibility(tooltipRedo, false);
            }
        }

        #region OnNavMode

        // Callback for the NavigationMode buttons.
        public void OnChangeNavigationMode(string buttonName)
        {
            UpdateRadioButtons(buttonName);

            Tooltips.HideAllTooltips(leftHandle.Find("left_controller").gameObject);

            if (currentNavigationMode != null)
                currentNavigationMode.DeInit();

            switch (buttonName)
            {
                case "BiManual": OnNavMode_BiManual(); break;
                case "Teleport": OnNavMode_Teleport(); break;
                case "Orbit": OnNavMode_Orbit(); break;
                case "Fps": OnNavMode_Fps(); break;
                case "Drone": OnNavMode_Drone(); break;
                case "Fly": OnNavMode_Fly(); break;
                default: Debug.LogError("Unknown navigation mode button name was passed."); break;
            }
            HandleCommonTooltipsVisibility();

            GlobalState.currentNavigationMode = currentNavigationMode;
        }

        public void OnNavMode_BiManual()
        {
            currentNavigationMode = new NavigationMode_BiManual(lineUI, minPlayerScale, maxPlayerScale);
            currentNavigationMode.Init(transform, world, leftHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Teleport()
        {
            currentNavigationMode = new NavigationMode_Teleport(teleport, trajectoryParams);
            currentNavigationMode.Init(transform, world, leftHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Orbit()
        {
            currentNavigationMode = new NavigationMode_Orbit(ray, rotationalSpeed, scaleSpeed, moveSpeed, minPlayerScale, maxPlayerScale);
            currentNavigationMode.Init(transform, world, leftHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Fps()
        {
            currentNavigationMode = new NavigationMode_FPS();
            currentNavigationMode.Init(transform, world, leftHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Drone()
        {
            currentNavigationMode = new NavigationMode_Drone();
            currentNavigationMode.Init(transform, world, leftHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Fly()
        {
            currentNavigationMode = new NavigationMode_Fly(flySpeed, minPlayerScale, maxPlayerScale);
            currentNavigationMode.Init(transform, world, leftHandle, pivot, vrCamera, navigationParametersContainer);
        }

        private void UpdateRadioButtons(string activeButtonName)
        {
            if (buttonsContainer != null)
            {
                for (int i = 0; i < buttonsContainer.transform.childCount; ++i)
                {
                    Transform t = buttonsContainer.GetChild(i);
                    UIButton button = t.GetComponent<UIButton>();
                    if (button != null)
                    {
                        button.Checked = button.name == activeButtonName;
                    }
                }
            }
        }

        #endregion

        private bool IsCompatibleWithPalette(NavigationMode n) { return !n.usedControls.HasFlag(NavigationMode.UsedControls.LEFT_TRIGGER); }
        private bool IsCompatibleWithUndoRedo(NavigationMode n) { return !n.usedControls.HasFlag(NavigationMode.UsedControls.LEFT_PRIMARY | NavigationMode.UsedControls.LEFT_SECONDARY); }
        private bool IsCompatibleWithReset(NavigationMode n) { return !n.usedControls.HasFlag(NavigationMode.UsedControls.LEFT_JOYSTICK_CLICK); }
        private bool IsCompatibleWithResetScale(NavigationMode n) { return !n.usedControls.HasFlag(NavigationMode.UsedControls.RIGHT_JOYSTICK_CLICK); }
    }
}
