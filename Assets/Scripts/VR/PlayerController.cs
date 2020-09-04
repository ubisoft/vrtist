using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Base Parameters")]
        [SerializeField] private Transform world = null;
        [SerializeField] private Transform buttonsContainer = null;
        [SerializeField] private Transform navigationParametersContainer = null;
        [SerializeField] private Transform leftHandle = null;
        [SerializeField] private Transform rightHandle = null;
        [SerializeField] private Transform pivot = null;
        [SerializeField] private Transform vrCamera = null;
        [Tooltip("Player can be xxx times bigger than the world")]
        public float maxPlayerScale = 2000.0f;// world min scale = 0.0005f;
        [Tooltip("Player can be xxx times smaller than the world")]
        public float minPlayerScale = 50.0f; // world scale = 50.0f;

        [Header("Navigation Options")]
        public NavigationOptions options;

        [Header("BiManual Navigation Mode")]
        public StretchUI lineUI = null;

        [Header("Orbit Navigation")]
        public StraightRay ray = null;

        [Header("Teleport Navigation")]
        public TeleportUI teleport = null;
        public TrajectoryParams trajectoryParams = new TrajectoryParams();

        //private NavigationMode currentNavigationMode = null;

        private const float deadZone = 0.3f; // for palette pop

        private Vector3 initCameraPosition; // for reset
        private Quaternion initCameraRotation; // for reset

        private GameObject tooltipPalette = null;
        private GameObject tooltipUndo = null;
        private GameObject tooltipRedo = null;
        private GameObject tooltipReset = null;

        private Vector3 previousPosition;
        private Vector3 previousForward;
        private Transform rightHanded;

        void Start()
        {
            if (!VRInput.TryGetDevices())
            {
                Debug.LogWarning("PlayerController cannot VRInput.TryGetDevices().");
            }

            if (vrCamera == null) { Debug.LogWarning("Cannot find 'VRCamera' game object"); }
            if (leftHandle == null) { Debug.LogWarning("Cannot find 'LeftHandle' game object"); }
            if (rightHandle == null) { Debug.LogWarning("Cannot find 'RightHandle' game object"); }
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

            rightHanded = world.Find("Avatars");

            StartCoroutine(SendPlayerTransform());
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // NAVIGATION
                HandleNavigation();

                // RESET/FIT -- Left Joystick Click
                if (IsCompatibleWithReset(options.currentNavigationMode))
                {
                    HandleReset();
                }

                // RESET SCALE -- Right Joystick Click
                if (IsCompatibleWithResetScale(options.currentNavigationMode))
                {
                    HandleResetScale();
                }

                // PALETTE POP -- Left Trigger
                if (IsCompatibleWithPalette(options.currentNavigationMode))
                {
                    HandlePalette();
                }

                // UNDO/REDO -- Left A/B
                if (IsCompatibleWithUndoRedo(options.currentNavigationMode))
                {
                    HandleUndoRedo();
                }                
            }
        }

        IEnumerator SendPlayerTransform()
        {
            while (true)
            {
                // Send position and orientation
                Vector3 forward = vrCamera.forward;
                if (vrCamera.position != previousPosition || previousForward != forward)
                {
                    previousPosition = vrCamera.position;
                    previousForward = forward;

                    Vector3 upRight = vrCamera.position + vrCamera.forward + vrCamera.up + vrCamera.right;
                    Vector3 upLeft = vrCamera.position + vrCamera.forward + vrCamera.up - vrCamera.right;
                    Vector3 bottomRight = vrCamera.position + vrCamera.forward - vrCamera.up + vrCamera.right;
                    Vector3 bottomLeft = vrCamera.position + vrCamera.forward - vrCamera.up - vrCamera.right;
                    Vector3 target = vrCamera.position + vrCamera.forward * 2f;

                    GlobalState.networkUser.eye = rightHanded.InverseTransformPoint(vrCamera.position);
                    GlobalState.networkUser.target = rightHanded.InverseTransformPoint(target);
                    GlobalState.networkUser.corners[0] = rightHanded.InverseTransformPoint(upLeft);
                    GlobalState.networkUser.corners[1] = rightHanded.InverseTransformPoint(upRight);
                    GlobalState.networkUser.corners[2] = rightHanded.InverseTransformPoint(bottomRight);
                    GlobalState.networkUser.corners[3] = rightHanded.InverseTransformPoint(bottomLeft);

                    // TODO: fix this. Instance is sometimes null, even if retrieved in Awake.
                    if (NetworkClient.GetInstance())
                    {
                        NetworkClient.GetInstance().SendPlayerTransform(GlobalState.networkUser);
                    }
                }
                yield return new WaitForSeconds(1f / 15f);
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

            if (null != options.currentNavigationMode)
                options.currentNavigationMode.Update();
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

            center /= (float) bboxCount;

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
            if (options.currentNavigationMode == null)
                return;

            if (IsCompatibleWithReset(options.currentNavigationMode))
            {
                Tooltips.SetTooltipVisibility(tooltipReset, true);
                Tooltips.SetTooltipText(tooltipReset, "Reset");
            }
            else
            {
                Tooltips.SetTooltipVisibility(tooltipReset, false);
            }

            if (IsCompatibleWithPalette(options.currentNavigationMode))
            {
                Tooltips.SetTooltipVisibility(tooltipPalette, true);
                Tooltips.SetTooltipText(tooltipPalette, "Display Palette");
            }
            else
            {
                Tooltips.SetTooltipVisibility(tooltipPalette, false);
            }

            if (IsCompatibleWithUndoRedo(options.currentNavigationMode))
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

            if (options.currentNavigationMode != null)
                options.currentNavigationMode.DeInit();

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

            // TODO Scriptable: trouver qui accede a ca
            //GlobalState.currentNavigationMode = currentNavigationMode;
        }

        public void OnNavMode_BiManual()
        {
            // TODO: Scriptable NEW ou CreateInstance(SO), ou avoir une liste de SO dans PlayerController.
            options.currentNavigationMode = new NavigationMode_BiManual(lineUI, minPlayerScale, maxPlayerScale);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Teleport()
        {
            options.currentNavigationMode = new NavigationMode_Teleport(teleport, trajectoryParams);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Orbit()
        {
            options.currentNavigationMode = new NavigationMode_Orbit(ray, options.orbitRotationalSpeed, options.orbitScaleSpeed, options.orbitMoveSpeed, minPlayerScale, maxPlayerScale);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Fps()
        {
            options.currentNavigationMode = new NavigationMode_FPS();
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Drone()
        {
            options.currentNavigationMode = new NavigationMode_Drone();
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
        }

        public void OnNavMode_Fly()
        {
            options.currentNavigationMode = new NavigationMode_Fly(options.flySpeed, minPlayerScale, maxPlayerScale);
            options.currentNavigationMode.options = options;
            options.currentNavigationMode.Init(transform, world, leftHandle, rightHandle, pivot, vrCamera, navigationParametersContainer);
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

        #region On Navigation Options Change

        public void OnFlightSpeedChange(float value)
        {
            options.flightSpeed = value;
        }

        public void OnFlightRotationSpeedChange(float value)
        {
            options.flightRotationSpeed = value;
        }

        public void OnFlightDampingChange(float value)
        {
            options.flightDamping = value;
        }

        public void OnFPSSpeedChange(float value)
        {
            options.fpsSpeed = value;
        }

        public void OnFPSRotationSpeedChange(float value)
        {
            options.fpsRotationSpeed = value;
        }

        public void OnFPSDampingChange(float value)
        {
            options.fpsDamping = value;
        }

        public void OnOrbitScaleSpeedChange(float value)
        {
            options.orbitScaleSpeed = value / 100.0f;
        }

        public void OnOrbitMoveSpeedChange(float value)
        {
            options.orbitMoveSpeed = value / 100.0f;
        }

        public void OnOrbitRotationalSpeedChange(float value)
        {
            options.orbitRotationalSpeed = value;
        }

        public void OnTeleportLockHeightChange(bool value)
        {
            options.lockHeight = value;
        }

        #endregion

        private bool IsCompatibleWithPalette(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_TRIGGER); }
        private bool IsCompatibleWithUndoRedo(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_PRIMARY | NavigationMode.UsedControls.LEFT_SECONDARY); }
        private bool IsCompatibleWithReset(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.LEFT_JOYSTICK_CLICK); }
        private bool IsCompatibleWithResetScale(NavigationMode n) { return options.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK_CLICK); }
    }
}
