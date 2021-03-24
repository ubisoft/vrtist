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
using UnityEngine.Events;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorBase : ToolBase
    {
        [Header("Selector Parameters")]
        //[SerializeField] protected Transform world;
        [SerializeField] protected Material selectionMaterial;
        [SerializeField] private float deadZoneDistance = 0.005f;
        [SerializeField] private NavigationOptions navigation;
        [SerializeField] protected UICheckbox lockedCheckbox;

        float selectorRadius;
        protected Color selectionColor = new Color(0f, 167f / 255f, 1f);
        protected Color eraseColor = new Color(1f, 0f, 0f);

        protected Dictionary<GameObject, Matrix4x4> initParentMatrix = new Dictionary<GameObject, Matrix4x4>();
        protected Dictionary<GameObject, Vector3> initPositions = new Dictionary<GameObject, Vector3>();
        protected Dictionary<GameObject, Quaternion> initRotations = new Dictionary<GameObject, Quaternion>();
        protected Dictionary<GameObject, Vector3> initScales = new Dictionary<GameObject, Vector3>();
        protected Dictionary<GameObject, float> initFocals = new Dictionary<GameObject, float>();
        protected Vector3 initControllerPosition;
        protected Quaternion initControllerRotation;
        protected Matrix4x4 initMouthPieceWorldToLocal;

        public enum SelectorModes { Select = 0, Eraser }
        public SelectorModes mode = SelectorModes.Select;

        protected bool deforming = false;
        public bool Deforming
        {
            get { return deforming; }
        }

        protected CommandGroup clearSelectionUndoGroup;
        protected int selectionStateTimestamp = -1;

        const float deadZone = 0.3f;

        float scale = 1f;
        bool outOfDeadZone = false;
        private CommandGroup gripCmdGroup = null;
        public bool Gripping { get { return null != gripCmdGroup; } }

        protected bool gripPrevented = false;
        protected bool gripInterrupted = false;

        protected Dopesheet dopesheet;
        protected UIShotManager shotManager;

        protected SelectorTrigger selectorTrigger;

        private CommandSetValue<float> cameraFocalCommand = null;
        private bool joystickScaling = false;

        // snap parameters
        [Header("Snap Parameters")]
        protected Ray[] snapRays;
        static protected UnityEvent snapChangedEvent = new UnityEvent();
        static private bool isSnapping = true;
        static protected bool IsSnapping
        {
            get { return isSnapping; }
            set
            {
                isSnapping = value;
                snapChangedEvent.Invoke();
            }
        }
        static protected bool isSnappingToGround = false;
        private readonly float snapDistance = 0.03f;
        private readonly float epsilonDistance = 0.0001f;
        private readonly float snapVisibleRayFactor = 3f;
        private Transform[] planes;
        private LineRenderer[] planeLines;
        protected GameObject boundingBox;
        protected GameObject snapUIContainer;
        private Transform[] snapTargets;
        private readonly float cameraSpaceGap = 0.0001f;
        [CentimeterFloat] public float collidersThickness = 0.05f;
        private Vector3 minBound = Vector3.positiveInfinity;
        private Vector3 maxBound = Vector3.negativeInfinity;
        private bool hasBounds = false;
        private Vector3[] planePositions;
        private Matrix4x4 planeContainerMatrix;

        [SerializeField] private Gradient rayGradient;

        struct ControllerDamping
        {
            public ControllerDamping(float time, Vector3 position, Quaternion rotation)
            {
                this.time = time;
                this.position = position;
                this.rotation = rotation;
            }

            public float time;
            public Vector3 position;
            public Quaternion rotation;
        }
        readonly List<ControllerDamping> damping = new List<ControllerDamping>();
        protected Vector3 rightControllerPosition;
        protected Quaternion rightControllerRotation;

        void Start()
        {
            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnSelectMode();
            SetTooltips();
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
        }

        protected void ResetClearSelectionUndoGroup()
        {
            if (null != clearSelectionUndoGroup)
            {
                clearSelectionUndoGroup.Submit();
                clearSelectionUndoGroup = null;
            }
        }

        protected override void OnDisable()
        {
            Selection.onSelectionChanged.RemoveListener(OnSelectionChanged);
            if (Gripping)
                OnEndGrip();
            EndUndoGroup(); // secu
            ResetClearSelectionUndoGroup();
            SubmitCameraFocalCommand();
            base.OnDisable();
        }

        protected void SubmitCameraFocalCommand()
        {
            if (null != cameraFocalCommand)
            {
                cameraFocalCommand.Submit();
                cameraFocalCommand = null;
            }
        }

        public virtual void OnSelectorTriggerEnter(Collider other)
        {
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, true);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, true);
        }

        public virtual void OnSelectorTriggerExit(Collider other)
        {
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
        }

        private void InitRayGradient()
        {
            rayGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[4];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
            colorKeys[0].color = Color.red;
            colorKeys[0].time = 0;
            colorKeys[1].color = Color.red;
            colorKeys[1].time = 0.2f;
            colorKeys[2].color = Color.blue;
            colorKeys[2].time = 0.21f;
            colorKeys[3].color = Color.blue;
            colorKeys[3].time = 1f;
            alphaKeys[0].alpha = 1f;
            alphaKeys[0].time = 0;
            alphaKeys[1].alpha = 1f;
            alphaKeys[1].time = 0.2f;
            alphaKeys[2].alpha = 1f;
            alphaKeys[2].time = 0.21f;
            alphaKeys[3].alpha = 1f;
            alphaKeys[3].time = 1f;
            rayGradient.SetKeys(colorKeys, alphaKeys);
            rayGradient.mode = GradientMode.Fixed;
        }

        protected override void Init()
        {
            base.Init();

            SetTooltips();

            selectorRadius = mouthpiece.localScale.x;
            mouthpiece.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            selectorTrigger = mouthpiece.GetComponent<SelectorTrigger>();

            UpdateButtonsColor();

            dopesheet = GameObject.FindObjectOfType<Dopesheet>(true);
            UnityEngine.Assertions.Assert.IsNotNull(dopesheet);

            shotManager = GameObject.FindObjectOfType<UIShotManager>(true);
            UnityEngine.Assertions.Assert.IsNotNull(shotManager);

            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);

            // bounding box
            boundingBox = SceneManager.BoundingBox.gameObject;
            planes = new Transform[6];
            planes[0] = boundingBox.transform.Find("Top");
            planes[1] = boundingBox.transform.Find("Bottom");
            planes[2] = boundingBox.transform.Find("Left");
            planes[3] = boundingBox.transform.Find("Right");
            planes[4] = boundingBox.transform.Find("Front");
            planes[5] = boundingBox.transform.Find("Back");

            InitRayGradient();
            snapTargets = new Transform[6];
            planeLines = new LineRenderer[6];
            snapUIContainer = Utils.FindRootGameObject("UIUtils").transform.Find("SnapUI").gameObject;
            for (int i = 0; i < 6; i++)
            {
                snapTargets[i] = snapUIContainer.transform.GetChild(i);
                planeLines[i] = snapTargets[i].GetComponent<LineRenderer>();
                planeLines[i].material = Resources.Load<Material>("Materials/SnapRayMaterial");
            }
        }
        private void OnAnimationStateChanged(AnimationState state)
        {
            if (state == AnimationState.AnimationRecording)
            {
                Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Stop Record");
            }
            else
            {
                Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Duplicate");
            }
        }

        public override void SetTooltips()
        {
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Primary, Tooltips.Action.Push, "Duplicate");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Secondary, Tooltips.Action.Push, "Switch Tool");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Trigger, Tooltips.Action.Push, "Select");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Grip, Tooltips.Action.Push, "Move");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Joystick, Tooltips.Action.Push, "Scale");
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Joystick, false);
        }

        virtual protected void ClearSelectionOnVoidTrigger()
        {
            // Clear selection on trigger click on nothing
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.trigger, () =>
            {
                selectionStateTimestamp = Selection.SelectionStateTimestamp;
                clearSelectionUndoGroup = new CommandGroup("Clear Selector");
            },
            () =>
            {
                try
                {
                    if (selectionStateTimestamp == Selection.SelectionStateTimestamp && !VRInput.GetValue(VRInput.primaryController, CommonUsages.primaryButton) && !VRInput.GetValue(VRInput.primaryController, CommonUsages.gripButton))
                    {
                        if (mode == SelectorBase.SelectorModes.Select)
                            ClearSelection();
                    }
                }
                finally
                {
                    ResetClearSelectionUndoGroup();
                }
            });
        }

        protected override void DoUpdate()
        {
            ClearSelectionOnVoidTrigger();

            if (VRInput.GetValue(VRInput.primaryController, CommonUsages.grip) <= deadZone)
            {
                if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                {
                    // Change selector size
                    Vector2 val = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                    if (val != Vector2.zero)
                    {
                        float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                        if (val.y > deadZone) { selectorRadius *= scaleFactor; }
                        if (val.y < -deadZone) { selectorRadius /= scaleFactor; }
                        selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                        mouthpiece.localScale = new Vector3(selectorRadius, selectorRadius, selectorRadius);
                    }
                }
            }

            switch (mode)
            {
                case SelectorModes.Select: UpdateSelect(); break;
                case SelectorModes.Eraser: UpdateEraser(); break;
            }
        }

        // Tell whether the current selection contains a hierarchical object (mesh somewhere in children) or not.
        // Camera and lights are known hierarchical objects.
        // TODO: check for multiselection of a light and and simple primitive for example
        protected bool IsHierarchical(HashSet<GameObject> objects)
        {
            foreach (GameObject gObject in objects)
            {
                if (gObject.GetComponent<LightController>() != null || gObject.GetComponent<CameraController>() != null)
                {
                    return true;
                }
                MeshFilter meshFilter = gObject.GetComponentInChildren<MeshFilter>();
                if (meshFilter.gameObject != gObject)
                {
                    return true;
                }
            }
            return false;
        }

        private Mesh CreatePlaneMesh(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = v1;
            vertices[1] = v2;
            vertices[2] = v3;
            vertices[3] = v4;

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(0, 1);

            int[] indices = { 0, 1, 2, 0, 2, 3 };
            Mesh mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                triangles = indices
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
        private void SetPlaneCollider(Transform plane, Vector3 center, Vector3 size)
        {
            var collider = plane.GetComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
        }

        protected void InitTransforms()
        {
            initParentMatrix.Clear();
            initPositions.Clear();
            initRotations.Clear();
            initScales.Clear();
            initFocals.Clear();
            foreach (GameObject obj in Selection.ActiveObjects)
            {
                initParentMatrix[obj] = obj.transform.parent.localToWorldMatrix;
                initPositions[obj] = obj.transform.localPosition;
                initRotations[obj] = obj.transform.localRotation;
                initScales[obj] = obj.transform.localScale;

                CameraController cameraController = obj.GetComponent<CameraController>();
                if (null != cameraController)
                    initFocals[obj] = cameraController.focal;
            }
            scale = 1f;
        }

        public void OnGripWorld(bool value)
        {
            if (!gameObject.activeSelf)
                return;

            if (value)
            {
                if (!gripPrevented && Gripping) // no need to interrupt if the grip was prevented
                {
                    OnEndGrip(); // prematurely end the grip action
                    gripInterrupted = true; // set bool to return immediately in the "real" OnEndGrip called when ungripping the controller.
                }
            }
            else
            {
                gripInterrupted = false;
                gripPrevented = false;
            }
        }

        protected void EndUndoGroup()
        {
            if (Gripping)
            {
                gripCmdGroup.Submit();
                gripCmdGroup = null;
                GlobalState.Instance.selectionGripped = false;
            }
        }

        public static bool IsHandleSelected()
        {
            bool handleSelected = false;

            if (Selection.ActiveObjects.Count == 1)
            {
                foreach (GameObject obj in Selection.ActiveObjects)
                {
                    if (obj.GetComponent<UIHandle>())
                        handleSelected = true;
                }
            }
            return handleSelected;
        }


        protected virtual void OnStartGrip()
        {
            EndUndoGroup(); // secu
            if (GlobalState.IsGrippingWorld)
            {
                gripPrevented = true;
                return;
            }

            enableToggleTool = false; // NO secondary button tool switch while gripping.

            Selection.AuxiliarySelection = Selection.HoveredObject;
            SetControllerVisible(Selection.ActiveObjects.Count == 0);

            ComputeSelectionBounds();
            InitControllerMatrix();
            InitSnap();
            InitTransforms();
            outOfDeadZone = false;

            gripCmdGroup = new CommandGroup("Grip Selection");
            GlobalState.Instance.selectionGripped = true;
        }

        protected virtual void OnEndGrip()
        {
            snapUIContainer.SetActive(false);
            boundingBox.SetActive(false);
            SetControllerVisible(true);
            enableToggleTool = true; // TODO: put back the original value, not always true (atm all tools have it to true).

            selectorTrigger.OnEndGrip();

            if (gripPrevented)
            {
                gripPrevented = false;
                return;
            }

            if (gripInterrupted)
            {
                gripInterrupted = false;
                return;
            }

            List<ParametersController> controllers = new List<ParametersController>();
            foreach (var obj in Selection.ActiveObjects)
            {
                LightController lightController = obj.GetComponentInChildren<LightController>();
                if (null != lightController)
                {
                    controllers.Add(lightController);
                    continue;
                }
                CameraController cameraController = obj.GetComponentInChildren<CameraController>();
                if (null != cameraController)
                {
                    controllers.Add(cameraController);
                    continue;
                }
            }
            if (controllers.Count > 0)
            {
                GlobalState.SetGizmosVisible(controllers.ToArray(), GlobalState.Settings.DisplayGizmos);
            }

            if (!IsHandleSelected())
            {
                ManageMoveObjectsUndo();
                ManageCamerasFocalsUndo();
                ManageAutoKeyframe();
            }

            EndUndoGroup();
            Selection.AuxiliarySelection = null;
        }

        protected void ManageAutoKeyframe()
        {
            if (!GlobalState.Animation.autoKeyEnabled)
                return;
            foreach (GameObject obj in Selection.ActiveObjects)
            {
                if (!initPositions.ContainsKey(obj))
                    continue;
                if (initPositions[obj] == obj.transform.localPosition && initRotations[obj] == obj.transform.localRotation && initScales[obj] == obj.transform.localScale)
                    continue;
                new CommandAddKeyframes(obj).Submit();
            }
        }

        protected void ManageMoveObjectsUndo()
        {
            List<Vector3> beginPositions = new List<Vector3>();
            List<Quaternion> beginRotations = new List<Quaternion>();
            List<Vector3> beginScales = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();
            List<Quaternion> endRotations = new List<Quaternion>();
            List<Vector3> endScales = new List<Vector3>();

            List<GameObject> objects = new List<GameObject>();
            foreach (GameObject obj in Selection.ActiveObjects)
            {
                if (!initPositions.ContainsKey(obj))
                    continue;
                if (initPositions[obj] == obj.transform.localPosition && initRotations[obj] == obj.transform.localRotation && initScales[obj] == obj.transform.localScale)
                    continue;
                objects.Add(obj);
                beginPositions.Add(initPositions[obj]);
                beginRotations.Add(initRotations[obj]);
                beginScales.Add(initScales[obj]);

                endPositions.Add(obj.transform.localPosition);
                endRotations.Add(obj.transform.localRotation);
                endScales.Add(obj.transform.localScale);
            }

            // A unique command at the end of the move operation
            if (objects.Count > 0)
                new CommandMoveObjects(objects, beginPositions, beginRotations, beginScales, endPositions, endRotations, endScales).Submit();
        }

        protected void SendCameraFocal(CameraController cameraController)
        {
            SceneManager.SendCameraInfo(cameraController.gameObject.transform);
        }

        protected void ManageCamerasFocalsUndo()
        {
            foreach (GameObject obj in initFocals.Keys)
            {
                float oldValue = initFocals[obj];
                CameraController cameraController = obj.GetComponent<CameraController>();
                if (oldValue != cameraController.focal)
                {
                    new CommandSetValue<float>(obj, "Camera Focal", "/CameraController/focal", oldValue).Submit();
                    SendCameraFocal(cameraController);
                }
            }

        }

        protected virtual void OnSelectionChanged(HashSet<GameObject> previousSelection, HashSet<GameObject> currentSelection)
        {
            outOfDeadZone = false;

            int numSelected = Selection.SelectedObjects.Count;
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Joystick, numSelected > 0);

            // Update locked checkbox if anyone
            if (null != lockedCheckbox)
            {
                int numLocked = 0;
                int numUnlocked = 0;
                foreach (GameObject gobject in Selection.SelectedObjects)
                {
                    ParametersController parameters = gobject.GetComponent<ParametersController>();
                    if (null != parameters)
                    {
                        if (parameters.Lock) { ++numLocked; }
                        else { ++numUnlocked; }
                    }
                }
                lockedCheckbox.Disabled = false;
                if (numLocked > 0 && numUnlocked == 0)
                {
                    lockedCheckbox.Checked = true;
                }
                else if (numUnlocked > 0 && numLocked == 0)
                {
                    lockedCheckbox.Checked = false;
                }
                else
                {
                    lockedCheckbox.Disabled = true;
                }
            }
        }

        protected CameraController GetSingleSelectedCamera()
        {
            if (Selection.ActiveObjects.Count != 1)
                return null;

            CameraController controller = null;
            foreach (GameObject gObject in Selection.ActiveObjects)
            {
                controller = gObject.GetComponent<CameraController>();
            }
            return controller;
        }

        protected bool HasDamping()
        {
            if (!Gripping)
                return false;

            if (Selection.ActiveObjects.Count != 1)
                return false;

            foreach (GameObject gObject in Selection.ActiveObjects)
            {
                if (null == gObject.GetComponent<CameraController>())
                    return false;
            }
            return true;
        }

        private void GetControllerPositionRotation()
        {
            VRInput.GetControllerTransform(VRInput.primaryController, out Vector3 position, out Quaternion rotation);

            if (!HasDamping())
            {
                damping.Clear();
                rightControllerPosition = position;
                rightControllerRotation = rotation;
                return;
            }

            // Compute damping
            float currentTime = Time.time;
            ControllerDamping dampingElement = new ControllerDamping(currentTime, position, rotation);
            damping.Add(dampingElement);

            // remove too old values
            ControllerDamping elem = damping[0];
            float dampingDuration = GlobalState.Settings.cameraDamping / 100f * 0.5f;
            while (currentTime - elem.time > dampingDuration)
            {
                damping.RemoveAt(0);
                elem = damping[0];
            }

            Vector3 positionSum = Vector3.zero;
            Quaternion rotationSum = Quaternion.identity;
            rotationSum.w = 0f;
            float count = damping.Count;
            float factor = 1f / count;
            foreach (ControllerDamping dampingElem in damping)
            {
                positionSum += factor * dampingElem.position;
                rotationSum.x += factor * dampingElem.rotation.x;
                rotationSum.y += factor * dampingElem.rotation.y;
                rotationSum.z += factor * dampingElem.rotation.z;
                rotationSum.w += factor * dampingElem.rotation.w;
            }
            rightControllerPosition = positionSum;
            rightControllerRotation = rotationSum.normalized;
        }

        private void UpdateSelect()
        {
            GetControllerPositionRotation();
            // Move & Duplicate selection

            // Duplicate / Stop Record
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.primaryButton,
                () => { },
                () =>
                {
                    if (GlobalState.Animation.animationState == AnimationState.AnimationRecording || GlobalState.Animation.animationState == AnimationState.Preroll)
                    {
                        GlobalState.Animation.Pause();
                        return;
                    }

                    if (!IsHandleSelected())
                    {
                        if (Selection.ActiveObjects.Count > 0)
                        {
                            DuplicateSelection();

                            InitControllerMatrix();
                            InitSnap();
                            InitTransforms();
                            outOfDeadZone = true;
                        }
                    }
                }
            );

            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.grip, OnStartGrip, OnEndGrip);

            if (Gripping)
            {
                Vector3 p = rightControllerPosition;
                Quaternion r = rightControllerRotation;

                if (!outOfDeadZone && Vector3.Distance(p, initControllerPosition) > deadZoneDistance)
                    outOfDeadZone = true;

                if (!outOfDeadZone)
                    return;

                // Joystick zoom only for non-handle objects
                if (!IsHandleSelected())
                {
                    if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                    {
                        Vector2 joystickAxis = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                        float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                        if (joystickAxis.y > deadZone)
                            scale *= scaleFactor;
                        if (joystickAxis.y < -deadZone)
                            scale /= scaleFactor;
                    }
                }

                // right controller filtered matrix
                Matrix4x4 mouthPieceLocalToWorld = GlobalState.Instance.toolsController.parent.localToWorldMatrix * Matrix4x4.TRS(p, r, Vector3.one) *
                    Matrix4x4.TRS(mouthpieces.localPosition, mouthpieces.localRotation, Vector3.one * scale);

                Snap(ref mouthPieceLocalToWorld);
                TransformSelection(mouthPieceLocalToWorld * initMouthPieceWorldToLocal);
                ComputeSelectionBounds();
            }


            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 joystickAxis = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);

                if (joystickAxis != Vector2.zero)
                {
                    CameraController cameraController = GetSingleSelectedCamera();
                    if (null != cameraController)
                    {
                        if (null == cameraFocalCommand && !Gripping)
                        {
                            // allow camera focal change when not gripped
                            cameraFocalCommand = new CommandSetValue<float>(cameraController.gameObject, "Camera Focal", "/CameraController/focal");
                        }

                        float currentFocal = cameraController.focal;
                        float focalFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;

                        if (joystickAxis.x > deadZone)
                            currentFocal *= focalFactor;
                        if (joystickAxis.x < -deadZone)
                            currentFocal /= focalFactor;

                        if (currentFocal < 10f)
                            currentFocal = 10f;
                        if (currentFocal > 300f)
                            currentFocal = 300f;

                        // Don't RoundToInt here since currentFocal may stick to the lowest value 10
                        cameraController.focal = currentFocal;
                        SendCameraFocal(cameraController);
                    }

                    foreach (GameObject obj in Selection.SelectedObjects)
                    {
                        ParametersController controller = obj.GetComponent<ParametersController>();
                        if (null == controller)
                        {
                            Vector3 currentScale = obj.transform.localScale;
                            float newScale = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                            if (joystickAxis.x > deadZone)
                                currentScale *= newScale;
                            if (joystickAxis.x < -deadZone)
                                currentScale /= newScale;

                            if (currentScale.x < 0.0001f)
                                continue;
                            if (currentScale.x > 1000f)
                                continue;

                            obj.transform.localScale = currentScale;
                        }

                        if (!joystickScaling && !Gripping)
                        {
                            InitTransforms();
                            joystickScaling = true;
                        }
                    }
                }
                else
                {
                    SubmitCameraFocalCommand();
                    if (joystickScaling)
                    {
                        joystickScaling = false;
                        ManageMoveObjectsUndo();
                        ManageAutoKeyframe();
                    }
                }
            }
        }

        public void ComputeSelectionBounds()
        {
            // Get bounds
            minBound = Vector3.positiveInfinity;
            maxBound = Vector3.negativeInfinity;
            hasBounds = false;
            int selectionCount = Selection.ActiveObjects.Count;

            bool foundHierarchicalObject = false;
            if (selectionCount == 1)
            {
                foundHierarchicalObject = IsHierarchical(Selection.ActiveObjects);
            }

            GameObject firstSelectedObject = null;
            foreach (GameObject obj in Selection.ActiveObjects)
            {
                if (null == firstSelectedObject)
                    firstSelectedObject = obj;
                MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();
                if (null != meshFilter)
                {
                    Matrix4x4 transformMatrix;
                    if (selectionCount > 1 || foundHierarchicalObject)
                    {
                        if (meshFilter.gameObject != obj)
                        {
                            transformMatrix = SceneManager.RightHanded.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                        }
                        else
                        {
                            transformMatrix = SceneManager.RightHanded.worldToLocalMatrix * obj.transform.localToWorldMatrix;
                        }
                    }
                    else
                    {
                        transformMatrix = Matrix4x4.identity;
                    }

                    Mesh mesh = meshFilter.mesh;
                    // Get vertices
                    Vector3[] vertices = new Vector3[8];
                    vertices[0] = new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.min.z);
                    vertices[1] = new Vector3(mesh.bounds.min.x, mesh.bounds.min.y, mesh.bounds.max.z);
                    vertices[2] = new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.min.z);
                    vertices[3] = new Vector3(mesh.bounds.min.x, mesh.bounds.max.y, mesh.bounds.max.z);
                    vertices[4] = new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.min.z);
                    vertices[5] = new Vector3(mesh.bounds.max.x, mesh.bounds.min.y, mesh.bounds.max.z);
                    vertices[6] = new Vector3(mesh.bounds.max.x, mesh.bounds.max.y, mesh.bounds.min.z);
                    vertices[7] = new Vector3(mesh.bounds.max.x, mesh.bounds.max.y, mesh.bounds.max.z);

                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = transformMatrix.MultiplyPoint(vertices[i]);
                        //  Compute min and max bounds
                        if (vertices[i].x < minBound.x) { minBound.x = vertices[i].x; }
                        if (vertices[i].y < minBound.y) { minBound.y = vertices[i].y; }
                        if (vertices[i].z < minBound.z) { minBound.z = vertices[i].z; }

                        if (vertices[i].x > maxBound.x) { maxBound.x = vertices[i].x; }
                        if (vertices[i].y > maxBound.y) { maxBound.y = vertices[i].y; }
                        if (vertices[i].z > maxBound.z) { maxBound.z = vertices[i].z; }
                    }
                    hasBounds = true;
                }
            }
            if (hasBounds)
            {
                planePositions = new Vector3[6];
                planePositions[0] = new Vector3((maxBound.x + minBound.x) * 0.5f, maxBound.y, (maxBound.z + minBound.z) * 0.5f);
                planePositions[1] = new Vector3((maxBound.x + minBound.x) * 0.5f, minBound.y, (maxBound.z + minBound.z) * 0.5f);
                planePositions[2] = new Vector3(minBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
                planePositions[3] = new Vector3(maxBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
                planePositions[4] = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, minBound.z);
                planePositions[5] = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, maxBound.z);
            }

            if (selectionCount == 1 && !foundHierarchicalObject)
            {
                Transform transform = firstSelectedObject.GetComponentInChildren<MeshFilter>().transform;
                planeContainerMatrix = SceneManager.RightHanded.worldToLocalMatrix * transform.localToWorldMatrix;
            }
            else
            {
                planeContainerMatrix = Matrix4x4.identity;
            }

            UpdateSelectionPlanes();
        }

        public void UpdateSelectionPlanes()
        {
            Maths.DecomposeMatrix(planeContainerMatrix, out Vector3 planePosition, out Quaternion planeRotation, out Vector3 planeScale);
            boundingBox.transform.localPosition = planePosition;
            boundingBox.transform.localRotation = planeRotation;
            boundingBox.transform.localScale = planeScale;

            if (!hasBounds)
            {
                snapUIContainer.SetActive(false);
                boundingBox.SetActive(false);
                return;
            }

            Vector3 bs = boundingBox.transform.localScale; // boundsScale

            // Collider Scale
            Vector3 cs = new Vector3(
                collidersThickness * (1.0f / bs.x),
                collidersThickness * (1.0f / bs.y),
                collidersThickness * (1.0f / bs.z)
            );

            // GAP: fixed in camera space. Scales with world and objet scales, inverse.
            Vector3 g = new Vector3(
                cameraSpaceGap * (1.0f / bs.x),
                cameraSpaceGap * (1.0f / bs.y),
                cameraSpaceGap * (1.0f / bs.z)
            );

            Vector3 minGapBound = minBound - new Vector3(g.x, g.y, g.z);
            Vector3 maxGapBound = maxBound + new Vector3(g.x, g.y, g.z);

            Vector3 delta = (maxGapBound - minGapBound) * 0.5f;

            // Set planes (depending on their initial rotation)
            // Top
            planes[0].transform.localPosition = planePositions[0];
            planes[0].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, g.y, -delta.z), new Vector3(-delta.x, g.y, delta.z), new Vector3(delta.x, g.y, delta.z), new Vector3(delta.x, g.y, -delta.z));
            SetPlaneCollider(planes[0], new Vector3(0, g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Bottom
            planes[1].transform.localPosition = planePositions[1];
            planes[1].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -g.y, -delta.z), new Vector3(delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, -delta.z));
            SetPlaneCollider(planes[1], new Vector3(0, -g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Left
            planes[2].transform.localPosition = planePositions[2];
            planes[2].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-g.x, -delta.y, -delta.z), new Vector3(-g.x, -delta.y, delta.z), new Vector3(-g.x, delta.y, delta.z), new Vector3(-g.x, delta.y, -delta.z));
            SetPlaneCollider(planes[2], new Vector3(-g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Right
            planes[3].transform.localPosition = planePositions[3];
            planes[3].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(g.x, delta.y, -delta.z), new Vector3(g.x, delta.y, delta.z), new Vector3(g.x, -delta.y, delta.z), new Vector3(g.x, -delta.y, -delta.z));
            SetPlaneCollider(planes[3], new Vector3(g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Front
            planes[4].transform.localPosition = planePositions[4];
            planes[4].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, -delta.y, -g.z), new Vector3(-delta.x, delta.y, -g.z), new Vector3(delta.x, delta.y, -g.z), new Vector3(delta.x, -delta.y, -g.z));
            SetPlaneCollider(planes[4], new Vector3(0, 0, -g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            // Back
            planes[5].transform.localPosition = planePositions[5];
            planes[5].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -delta.y, g.z), new Vector3(delta.x, delta.y, g.z), new Vector3(-delta.x, delta.y, g.z), new Vector3(-delta.x, -delta.y, g.z));
            SetPlaneCollider(planes[5], new Vector3(0, 0, g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));


        }

        protected void InitControllerMatrix()
        {
            initMouthPieceWorldToLocal = mouthpieces.worldToLocalMatrix;
        }

        protected void InitSnap()
        {
            if (!hasBounds)
                return;

            snapRays = new Ray[6];
            Vector3 worldPlanePosition;

            worldPlanePosition = boundingBox.transform.TransformPoint(planePositions[0]);
            snapRays[0] = new Ray(mouthpieces.transform.InverseTransformPoint(worldPlanePosition), mouthpieces.transform.InverseTransformDirection(boundingBox.transform.up));
            worldPlanePosition = boundingBox.transform.TransformPoint(planePositions[1]);
            snapRays[1] = new Ray(mouthpieces.transform.InverseTransformPoint(worldPlanePosition), mouthpieces.transform.InverseTransformDirection(-boundingBox.transform.up));
            worldPlanePosition = boundingBox.transform.TransformPoint(planePositions[2]);
            snapRays[2] = new Ray(mouthpieces.transform.InverseTransformPoint(worldPlanePosition), mouthpieces.transform.InverseTransformDirection(boundingBox.transform.right));
            worldPlanePosition = boundingBox.transform.TransformPoint(planePositions[3]);
            snapRays[3] = new Ray(mouthpieces.transform.InverseTransformPoint(worldPlanePosition), mouthpieces.transform.InverseTransformDirection(-boundingBox.transform.right));
            worldPlanePosition = boundingBox.transform.TransformPoint(planePositions[4]);
            snapRays[4] = new Ray(mouthpieces.transform.InverseTransformPoint(worldPlanePosition), mouthpieces.transform.InverseTransformDirection(-boundingBox.transform.forward));
            worldPlanePosition = boundingBox.transform.TransformPoint(planePositions[5]);
            snapRays[5] = new Ray(mouthpieces.transform.InverseTransformPoint(worldPlanePosition), mouthpieces.transform.InverseTransformDirection(boundingBox.transform.forward));
        }

        protected void Snap(ref Matrix4x4 currentMouthPieceLocalToWorld)
        {
            foreach (Transform snapUI in snapTargets)
                snapUI.gameObject.SetActive(false);

            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.secondaryButton, () =>
            {
            },
            () =>
            {
                IsSnapping = !IsSnapping;
            });

            if (!IsSnapping || !IsSelectionSnappable())
            {
                boundingBox.SetActive(false);
                return;
            }

            if (isSnappingToGround)
            {
                int bestIndex = 0;
                float bestY = 0;
                for (int i = 0; i < 6; i++)
                {
                    Vector3 normalDirection = currentMouthPieceLocalToWorld.MultiplyVector(snapRays[i].direction).normalized;
                    if (normalDirection.y < bestY)
                    {
                        bestY = normalDirection.y;
                        bestIndex = i;
                    }
                }
                SnapPlane(ref currentMouthPieceLocalToWorld, bestIndex);
            }
            else
            {
                int i = 0;
                int notSnappedCount = 0;
                while (notSnappedCount < 6 && i < 18)
                {
                    if (!SnapPlane(ref currentMouthPieceLocalToWorld, i % 6))
                        notSnappedCount++;
                    else
                        notSnappedCount = 1;
                    i++;
                }
            }
        }

        protected int GetOppositePlaneIndex(int planeIndex)
        {
            switch (planeIndex)
            {
                case 0: return 1;
                case 1: return 0;
                case 2: return 3;
                case 3: return 2;
                case 4: return 5;
                case 5: return 4;
                default:
                    break;
            }
            return -1;
        }

        protected bool SnapPlane(ref Matrix4x4 currentMouthPieceLocalToWorld, int planeIndex)
        {
            if (!hasBounds)
                return false;

            int layersMask = LayerMask.GetMask(new string[] { "Default" });

            Vector3 origin = currentMouthPieceLocalToWorld.MultiplyPoint(snapRays[planeIndex].origin);
            Vector3 originOppositePlane = currentMouthPieceLocalToWorld.MultiplyPoint(snapRays[GetOppositePlaneIndex(planeIndex)].origin);

            Ray ray = new Ray(originOppositePlane, currentMouthPieceLocalToWorld.MultiplyVector(snapRays[planeIndex].direction).normalized);

            LineRenderer line = planeLines[planeIndex];
            Transform snapTarget = snapTargets[planeIndex];
            bool enableSnapUI = false;

            float plansDistance = Vector3.Distance(originOppositePlane, origin);
            if (Physics.Raycast(ray, out RaycastHit hit, plansDistance + snapVisibleRayFactor * snapDistance / GlobalState.WorldScale, layersMask))
            {
                float hitDistance = hit.distance - plansDistance;
                snapUIContainer.SetActive(hitDistance > 0);
                boundingBox.SetActive(true);
                enableSnapUI = true;
                line.positionCount = 2;
                line.SetPosition(0, origin);
                line.SetPosition(1, hit.point);
                line.material.SetFloat("_Threshold", 1f - (snapDistance / GlobalState.WorldScale / hitDistance));
                line.endWidth = line.startWidth = 0.001f / GlobalState.WorldScale;

                snapTarget.localScale = Vector3.one * 0.03f / GlobalState.WorldScale;
                snapTarget.LookAt(hit.point - 1000f * hit.normal);
                snapTarget.position = hit.point + hit.normal * 0.001f / GlobalState.WorldScale;

                if (Mathf.Abs(hitDistance) <= snapDistance / GlobalState.WorldScale)
                {
                    snapTarget.gameObject.SetActive(false);
                    if (Mathf.Abs(hitDistance) > epsilonDistance / GlobalState.WorldScale)
                    {
                        Vector3 hitPoint = currentMouthPieceLocalToWorld.MultiplyPoint(currentMouthPieceLocalToWorld.inverse.MultiplyPoint(hit.point) - snapRays[planeIndex].origin);
                        // set position to hit point
                        currentMouthPieceLocalToWorld.SetColumn(3, new Vector4(hitPoint.x, hitPoint.y, hitPoint.z, 1));

                        // compute rotation to align up vector to hit normal
                        Matrix4x4 T = Matrix4x4.Translate(-hit.point);
                        Matrix4x4 R = Matrix4x4.TRS(Vector3.zero, Quaternion.FromToRotation(-currentMouthPieceLocalToWorld.MultiplyVector(snapRays[planeIndex].direction).normalized, hit.normal), Vector3.one);

                        currentMouthPieceLocalToWorld = T.inverse * R * T * currentMouthPieceLocalToWorld;
                        return true;
                    }
                }

            }

            snapTarget.gameObject.SetActive(enableSnapUI);
            return false;
        }

        private bool IsSelectionSnappable()
        {
            int layersMask = LayerMask.NameToLayer("HoverCameraHidden");
            foreach (GameObject obj in Selection.ActiveObjects)
            {
                if (obj.layer == layersMask)
                    return false;
                ParametersController controller = obj.GetComponent<ParametersController>();
                if (controller != null && !controller.IsSnappable())
                    return false;
            }
            return true;
        }

        protected void TransformSelection(Matrix4x4 transformation)
        {
            foreach (GameObject obj in Selection.ActiveObjects)
            {
                if (!initParentMatrix.ContainsKey(obj)) { continue; }

                // Check constraints
                if (ConstraintManager.IsLocked(obj)) { continue; }

                var meshParentTransform = obj.transform.parent;
                Matrix4x4 meshParentMatrixInverse;
                if (meshParentTransform)
                    meshParentMatrixInverse = meshParentTransform.worldToLocalMatrix;
                else
                    meshParentMatrixInverse = Matrix4x4.identity;
                Matrix4x4 transformed = meshParentMatrixInverse * transformation * initParentMatrix[obj] * Matrix4x4.TRS(initPositions[obj], initRotations[obj], initScales[obj]);

                if (obj.transform.localToWorldMatrix != transformed)
                {
                    // UI objects
                    if (obj.GetComponent<UIHandle>())
                    {
                        obj.transform.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                        obj.transform.localRotation = Quaternion.LookRotation(transformed.GetColumn(2), transformed.GetColumn(1));
                        //obj.transform.localScale = new Vector3(transformed.GetColumn(0).magnitude, transformed.GetColumn(1).magnitude, transformed.GetColumn(2).magnitude);
                    }
                    // Standard game objects
                    else
                    {
                        Matrix4x4 mat = transformed;  // copy

                        // Constraints and locked properties may change the final transform
                        // TODO

                        OnPreTransformSelection(obj.transform, ref mat);
                        SceneManager.SetObjectMatrix(obj, mat);
                    }
                }
            }
        }

        public void OnPreTransformSelection(Transform transform, ref Matrix4x4 transformed)
        {
            // Constrain movement
            bool lockPosition = false;
            bool lockRotation = false;
            bool lockScale = false;
            ParametersController parametersController = transform.gameObject.GetComponent<ParametersController>();
            if (null != parametersController)
            {
                lockPosition = parametersController.lockPosition;
                lockRotation = parametersController.lockRotation;
                lockScale = parametersController.lockScale;
            }

            Maths.DecomposeMatrix(transformed, out Vector3 newPosition, out Quaternion newRotation, out Vector3 newScale);

            // Translate
            if (lockPosition)
            {
                newPosition = transform.localPosition;
            }

            if (lockRotation)
            {
                newRotation = transform.localRotation;
            }

            if (lockScale)
            {
                newScale = transform.localScale;
            }
            // transformation matrix (local)
            transformed = Matrix4x4.TRS(newPosition, newRotation, newScale);

        }

        private void UpdateEraser()
        {
            // Nothing for now
        }

        private List<GameObject> GetGroupSiblings(GameObject gObject)
        {
            List<GameObject> objects = new List<GameObject>();
            Transform parent = gObject.transform.parent;
            if (parent.name.StartsWith("Group__"))
            {
                for (int i = 0; i < parent.childCount; i++)
                    objects.Add(parent.GetChild(i).gameObject);
            }
            else
            {
                objects.Add(gObject);
            }
            return objects;
        }

        public bool AddToSelection(GameObject gObject)
        {
            // Selection is EXCLUSIVE between windows and objects.
            if (gObject.GetComponent<UIHandle>()) // if we select a UI handle, deselect all other objects first.
            {
                ClearSelection();
                bool res = Selection.AddToSelection(gObject);
                new CommandAddToSelection(gObject).Submit();
                return res;
            }
            else if (!IsHandleSelected()) // Dont select things if we have a window selected.
            {
                bool res = Selection.AddToSelection(gObject);
                new CommandAddToSelection(gObject).Submit();
                return res;
            }

            return false;
        }

        public void AddSiblingsToSelection(GameObject gObject, bool haptic = true)
        {
            List<GameObject> objects = GetGroupSiblings(gObject);
            List<GameObject> objectsAddedToSelection = new List<GameObject>();
            foreach (GameObject gobj in objects)
            {
                if (Selection.IsSelected(gobj))
                    continue;
                if (AddToSelection(gobj))
                    objectsAddedToSelection.Add(gobj);
            }

            if (haptic && objectsAddedToSelection.Count > 0)
            {
                VRInput.SendHapticImpulse(VRInput.primaryController, 0, 1, 0.1f);
            }
        }

        public bool RemoveFromSelection(GameObject gObject)
        {
            new CommandRemoveFromSelection(gObject).Submit();
            return Selection.RemoveFromSelection(gObject);
        }

        public void RemoveSiblingsFromSelection(GameObject gObject, bool haptic = true)
        {
            List<GameObject> objects = GetGroupSiblings(gObject);

            List<GameObject> objectsRemovedFromSelection = new List<GameObject>();
            foreach (GameObject gobj in objects)
            {
                if (!Selection.IsSelected(gobj))
                    continue;

                if (RemoveFromSelection(gobj))
                {
                    objectsRemovedFromSelection.Add(gobj);
                }
            }

            if (haptic && objectsRemovedFromSelection.Count > 0)
            {
                VRInput.SendHapticImpulse(VRInput.primaryController, 0, 1, 0.1f);
            }
        }

        public void ClearSelection()
        {
            new CommandRemoveFromSelection(new List<GameObject>(Selection.SelectedObjects)).Submit();
            Selection.ClearSelection();
        }

        public GameObject DuplicateObject(GameObject source, bool withVFX = true)
        {
            if (source.GetComponent<UIHandle>())
                return null;

            GameObject clone = SceneManager.DuplicateObject(source);
            if (null == clone)
                return null;
            new CommandDuplicateGameObject(clone).Submit();

            // Add a selectionVFX instance on the duplicated objects
            if (withVFX)
            {
                ToolsUIManager.Instance.SpawnCreateInstanceVFX(clone);
            }

            if (Selection.IsSelected(source))
            {
                Selection.RemoveFromSelection(source);
                new CommandRemoveFromSelection(source).Submit();
                Selection.AddToSelection(clone);
                new CommandAddToSelection(clone).Submit();
            }
            if (source == Selection.AuxiliarySelection)
            {
                Selection.AuxiliarySelection = clone;
            }
            if (source == Selection.HoveredObject)
            {
                Selection.HoveredObject = clone;
            }

            selectorTrigger.ClearCollidedObjects();
            return clone;
        }

        public void DuplicateSelection()
        {
            CommandGroup group = new CommandGroup("Duplicate Selection");
            try
            {
                HashSet<GameObject> activeObjectsCopy = new HashSet<GameObject>(Selection.ActiveObjects);
                foreach (GameObject obj in activeObjectsCopy)
                    DuplicateObject(obj);
                ManageMoveObjectsUndo();
            }
            finally
            {
                group.Submit();
            }
        }

        public void EraseSelection()
        {
            CommandGroup group = new CommandGroup("Erase Selection");
            try
            {
                foreach (GameObject o in Selection.ActiveObjects)
                {
                    //RemoveCollidedObject(o);
                    RemoveSiblingsFromSelection(o, false);
                    new CommandRemoveGameObject(o).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        public void OnSelectMode()
        {
            mode = SelectorModes.Select;
            mouthpiece.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            UpdateButtonsColor();
        }

        public void OnEraserMode()
        {
            mode = SelectorModes.Eraser;
            mouthpiece.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", eraseColor);
            UpdateButtonsColor();
        }

        public Color GetModeColor()
        {
            if (mode == SelectorModes.Select) { return selectionColor; }
            return eraseColor;
        }

        void UpdateButtonsColor()
        {
            if (!panel)
                return;

            // NOTE: currently the SelectorPanel has 4 children of type UIButton
            //       which have on Canvas children, with in turn has one Image and one Text children.
            // TODO: do a proper Radio Button Group
            for (int i = 0; i < panel.childCount; i++)
            {
                GameObject child = panel.GetChild(i).gameObject;
                UIButton button = child.GetComponent<UIButton>();
                if (button != null)
                {
                    button.Checked = false;

                    if (child.name == "Select" && mode == SelectorModes.Select)
                    {
                        button.Checked = true;
                    }
                    if (child.name == "Eraser" && mode == SelectorModes.Eraser)
                    {
                        button.Checked = true;
                    }
                }
            }
        }

        public void OnLinkAction()
        {
            // TODO
            /*
            if (Selection.selection.Count <= 1)
                return;

            GameObject container = new GameObject("Group__" + groupId.ToString());
            groupId++;

            SortedSet<Transform> groups = new SortedSet<Transform>();

            bool groupHasParent = false;
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                Transform parent = gobject.transform.parent;
                if (parent && parent.name.StartsWith("Group__"))
                {
                    if (!groupHasParent)
                    {
                        container.transform.parent = parent.parent;
                        container.transform.localPosition = Vector3.zero;
                        container.transform.localRotation = Quaternion.identity;
                        container.transform.localScale = Vector3.one;
                        groupHasParent = true;
                    }

                    // reparent
                    groups.Add(gobject.transform.parent);
                    gobject.transform.parent = container.transform;
                }
                else
                {
                    if (!groupHasParent)
                    {
                        container.transform.parent = data.Value.transform.parent;
                        groupHasParent = true;
                    }
                    data.Value.transform.parent = container.transform;
                }
            }

            foreach (Transform group in groups)
            {
                if (group.childCount == 0)
                    Destroy(group.gameObject);
            }
            */
        }

        public void OnUnlinkAction()
        {
            // TODO
            /*
            SortedSet<Transform> groups = new SortedSet<Transform>();
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                Transform parent = gobject.transform.parent;
                if (parent && parent.name.StartsWith("Group__"))
                {
                    groups.Add(gobject.transform.parent);
                }
            }

            foreach (Transform group in groups)
            {
                for (int i = group.childCount - 1; i >= 0; i--)
                {
                    group.GetChild(i).parent = group.parent;
                }
                Destroy(group.gameObject);
            }
            */
        }

    }
}
