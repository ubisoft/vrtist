using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorBase : ToolBase
    {
        [Header("Selector Parameters")]
        [SerializeField] protected Transform world;
        [SerializeField] protected Transform selectorBrush;
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
        protected Matrix4x4 initTransformation;

        public enum SelectorModes { Select = 0, Eraser }
        public SelectorModes mode = SelectorModes.Select;

        const float deadZone = 0.3f;

        float scale = 1f;
        bool outOfDeadZone = false;
        protected bool gripped = false;
        protected CommandGroup undoGroup = null;

        protected bool gripPrevented = false;
        protected bool gripInterrupted = false;

        int groupId = 0;

        protected GameObject triggerTooltip;
        protected GameObject gripTooltip;
        protected GameObject joystickTooltip;

        public GameObject selectionVFXPrefab = null;
        protected Dopesheet dopesheet;

        protected SelectorTrigger selectorTrigger;

        private CommandSetValue<float> cameraFocalCommand = null;

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
        List<ControllerDamping> damping = new List<ControllerDamping>();
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
            Selection.OnSelectionChanged += OnSelectionChanged;

        }

        protected override void OnDisable()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
            if (gripped)
                OnEndGrip();
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
            Tooltips.SetTooltipVisibility(triggerTooltip, true);
            Tooltips.SetTooltipVisibility(gripTooltip, true);
        }

        public virtual void OnSelectorTriggerExit(Collider other)
        {
            Tooltips.SetTooltipVisibility(triggerTooltip, false);
            Tooltips.SetTooltipVisibility(gripTooltip, false);
        }

        protected override void Init()
        {
            base.Init();

            CreateTooltips();

            selectorRadius = selectorBrush.localScale.x;
            selectorBrush.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            selectorTrigger = selectorBrush.GetComponent<SelectorTrigger>();

            updateButtonsColor();

            Selection.selectionMaterial = selectionMaterial;

            if (null == selectionVFXPrefab)
            {
                selectionVFXPrefab = Resources.Load<GameObject>("Prefabs/SelectionVFX");
            }

            dopesheet = GameObject.FindObjectOfType<Dopesheet>();
            UnityEngine.Assertions.Assert.IsNotNull(dopesheet);


        }

        protected void CreateTooltips()
        {
            GameObject controller = rightController.gameObject;
            Tooltips.CreateTooltip(controller, Tooltips.Anchors.Primary, "Duplicate");
            Tooltips.CreateTooltip(controller, Tooltips.Anchors.Secondary, "Switch Tool");
            triggerTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Trigger, "Select");
            gripTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Grip, "Select & Move");
            joystickTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Joystick, "Scale");
            Tooltips.SetTooltipVisibility(triggerTooltip, false);
            Tooltips.SetTooltipVisibility(gripTooltip, false);
            Tooltips.SetTooltipVisibility(joystickTooltip, false);
        }

        protected override void DoUpdate()
        {
            if (VRInput.GetValue(VRInput.rightController, CommonUsages.grip) <= deadZone)
            {
                if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                {
                    // Change selector size
                    Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                    if (val != Vector2.zero)
                    {
                        float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                        if (val.y > deadZone) { selectorRadius *= scaleFactor; }
                        if (val.y < -deadZone) { selectorRadius /= scaleFactor; }
                        selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                        selectorBrush.localScale = new Vector3(selectorRadius, selectorRadius, selectorRadius);
                    }
                }
            }

            switch (mode)
            {
                case SelectorModes.Select: UpdateSelect(); break;
                case SelectorModes.Eraser: UpdateEraser(); break;
            }
        }

        protected override void ShowTool(bool show)
        {
            ActivateMouthpiece(selectorBrush, show);

            if (rightController != null)
            {
                rightController.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        void SetControllerVisible(bool visible)
        {
            rightHandle.Find("right_controller").gameObject.SetActive(visible);

            // Mouth pieces have the selectorTrigger script attached to them which has to be always enabled
            // So don't deactivate mouth pieces, but scale them to 0 instead to hide them
            //rightHandle.Find("mouthpieces").gameObject.SetActive(visible);
            Transform mouthPieces = rightHandle.Find("mouthpieces");
            mouthPieces.localScale = visible ? Vector3.one : Vector3.zero;
        }

        protected void InitControllerMatrix()
        {
            VRInput.GetControllerTransform(VRInput.rightController, out initControllerPosition, out initControllerRotation);

            //initControllerPosition = rightControllerPosition;
            //initControllerRotation = rightControllerRotation;
            // compute rightMouthpiece local to world matrix with initial controller position/rotation
            //initTransformation = (rightHandle.parent.localToWorldMatrix * Matrix4x4.TRS(initControllerPosition, initControllerRotation, Vector3.one) * Matrix4x4.TRS(rightMouthpiece.localPosition, rightMouthpiece.localRotation, Vector3.one)).inverse;
            initTransformation = rightHandle.parent.localToWorldMatrix * Matrix4x4.TRS(initControllerPosition, initControllerRotation, Vector3.one) * Matrix4x4.TRS(rightMouthpiece.localPosition, rightMouthpiece.localRotation, Vector3.one);
            initTransformation = initTransformation.inverse;
        }

        protected void InitTransforms()
        {
            initParentMatrix.Clear();
            initPositions.Clear();
            initRotations.Clear();
            initScales.Clear();
            initFocals.Clear();
            foreach (GameObject obj in Selection.GetObjects())
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
            if (value)
            {
                if (!gripPrevented && gripped) // no need to interrupt if the grip was prevented
                {
                    OnEndGrip(); // prematurely end the grip action
                    gripInterrupted = true; // set bool to return immediately in the "real" OnEndGrip called when ungripping the controller.
                }
            }
        }

        protected virtual void OnStartGrip()
        {
            undoGroup = null;
            if (GlobalState.IsGrippingWorld)
            {
                gripPrevented = true;
                return;
            }

            Selection.SetGrippedObject(Selection.GetHoveredObject());

            undoGroup = new CommandGroup("Grip Selection");

            gripped = true;
            InitControllerMatrix();
            InitTransforms();
            outOfDeadZone = false;
        }

        protected virtual void OnEndGrip()
        {
            gripped = false;
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
            foreach (var obj in Selection.GetObjects())
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
                GlobalState.ShowHideControllersGizmos(controllers.ToArray(), GlobalState.Settings.displayGizmos);
            }

            if (!Selection.IsHandleSelected())
            {
                ManageMoveObjectsUndo();
                ManageCamerasFocalsUndo();
            }

            if (null != undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }
            Selection.SetGrippedObject(null);
        }


        protected void ManageMoveObjectsUndo()
        {
            List<string> objects = new List<string>();
            List<Vector3> beginPositions = new List<Vector3>();
            List<Quaternion> beginRotations = new List<Quaternion>();
            List<Vector3> beginScales = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();
            List<Quaternion> endRotations = new List<Quaternion>();
            List<Vector3> endScales = new List<Vector3>();

            foreach (GameObject obj in Selection.GetObjects())
            {
                if (!initPositions.ContainsKey(obj))
                    continue;
                if (initPositions[obj] == obj.transform.localPosition && initRotations[obj] == obj.transform.localRotation && initScales[obj] == obj.transform.localScale)
                    continue;
                objects.Add(obj.name);
                beginPositions.Add(initPositions[obj]);
                beginRotations.Add(initRotations[obj]);
                beginScales.Add(initScales[obj]);

                endPositions.Add(obj.transform.localPosition);
                endRotations.Add(obj.transform.localRotation);
                endScales.Add(obj.transform.localScale);
            }

            if (objects.Count > 0)
                new CommandMoveObjects(objects, beginPositions, beginRotations, beginScales, endPositions, endRotations, endScales).Submit();
        }

        protected void SendCameraFocal(CameraController cameraController)
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = cameraController.gameObject.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
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


        private ParametersController GetFirstController()
        {
            foreach (GameObject gObject in Selection.selection.Values)
            {
                ParametersController controller = gObject.GetComponent<ParametersController>();
                if (null != controller)
                    return controller;
            }
            return null;
        }

        private ParametersController GetFirstAnimation()
        {
            foreach (GameObject gObject in Selection.selection.Values)
            {
                ParametersController controller = gObject.GetComponent<ParametersController>();
                if (null == controller)
                    continue;
                if (controller.HasAnimation())
                    return controller;
            }
            return null;
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            outOfDeadZone = false;

            int numSelected = Selection.selection.Count;
            Tooltips.SetTooltipVisibility(joystickTooltip, numSelected > 0);
            ParametersController controller = GetFirstController();
            dopesheet.UpdateFromController(controller);

            // Update locked checkbox if anyone
            if (null != lockedCheckbox)
            {
                int numLocked = 0;
                int numUnlocked = 0;
                foreach (GameObject gobject in Selection.GetObjects())
                {
                    ParametersController parameters = gobject.GetComponent<ParametersController>();
                    if (null != parameters)
                    {
                        if (parameters.locked) { ++numLocked; }
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
            List<GameObject> objecs = Selection.GetObjects();
            if (objecs.Count != 1)
                return null;

            CameraController controller = null;
            foreach (GameObject gObject in objecs)
            {
                controller = gObject.GetComponent<CameraController>();
            }
            return controller;
        }

        protected bool HasDamping()
        {
            if (!gripped)
                return false;

            List<GameObject> objecs = Selection.GetObjects();
            if (objecs.Count != 1)
                return false;

            foreach (GameObject gObject in objecs)
            {
                if (null == gObject.GetComponent<CameraController>())
                    return false;
            }
            return true;
        }

        private void GetControllerPositionRotation()
        {
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

            if (!HasDamping())
            {
                damping.Clear();
                rightControllerPosition = position;
                rightControllerRotation = rotation;
                return;
            }

            // Compute damping
            float curretnTime = Time.time;
            ControllerDamping dampingElement = new ControllerDamping(curretnTime, position, rotation);
            damping.Add(dampingElement);

            // remove too old values
            ControllerDamping elem = damping[0];
            float dampingDuration = GlobalState.Settings.cameraDamping / 100f * 0.5f;
            while (curretnTime - elem.time > dampingDuration)
            {
                damping.RemoveAt(0);
                elem = damping[0];
            }

            Vector3 positionSum = Vector3.zero;
            Quaternion rotationSum = Quaternion.identity;
            rotationSum.w = 0f;
            float count = (float) damping.Count;
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

        // A ref for snapping controller
        //private Matrix4x4 SnapController(Matrix4x4 controllerMatrix)
        //{
        //    Matrix4x4 controllerMatrixInWorld = world.worldToLocalMatrix * controllerMatrix;
        //    Vector3 controllerTranslate, controllerScale;
        //    Quaternion controllerRotation;
        //    Maths.DecomposeMatrix(controllerMatrixInWorld, out controllerTranslate, out controllerRotation, out controllerScale);
        //    controllerTranslate = new Vector3((float)Math.Round(controllerTranslate.x, 0), (float)Math.Round(controllerTranslate.y, 0), (float)Math.Round(controllerTranslate.z, 0));

        //    float snapXAngle = 90;
        //    float snapYAngle = 90;
        //    float snapZAngle = 90;
        //    Vector3 eulerAngles = controllerRotation.eulerAngles;
        //    eulerAngles = new Vector3((float)Math.Round(eulerAngles.x / snapXAngle, 0) * snapXAngle, (float)Math.Round(eulerAngles.y / snapYAngle, 0) * snapYAngle, (float)Math.Round(eulerAngles.z / snapZAngle, 0) * snapZAngle);
        //    controllerRotation.eulerAngles = eulerAngles;

        //    return world.localToWorldMatrix * Matrix4x4.TRS(controllerTranslate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, controllerRotation, Vector3.one);
        //}

        private void UpdateSelect()
        {
            GetControllerPositionRotation();
            // Move & Duplicate selection

            // Duplicate
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.primaryButton,
                () => { },
                () =>
                {
                    if (!Selection.IsHandleSelected())
                    {
                        List<GameObject> objects = Selection.GetObjects();

                        if (objects.Count > 0)
                        {
                            DuplicateSelection();

                            InitControllerMatrix();
                            InitTransforms();
                            outOfDeadZone = true;
                        }
                    }
                }
            );

            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.grip, OnStartGrip, OnEndGrip);

            SetControllerVisible(!gripped || Selection.GetObjects().Count == 0);

            if (gripped)
            {
                Vector3 p = rightControllerPosition;
                Quaternion r = rightControllerRotation;

                if (!outOfDeadZone && Vector3.Distance(p, initControllerPosition) > deadZoneDistance)
                    outOfDeadZone = true;

                if (!outOfDeadZone)
                    return;

                // Joystick zoom only for non-handle objects
                if (!Selection.IsHandleSelected())
                {
                    if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                    {
                        Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                        float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000.0f;
                        if (joystickAxis.y > deadZone)
                            scale *= scaleFactor;
                        if (joystickAxis.y < -deadZone)
                            scale /= scaleFactor;
                    }
                }

                // compute rightMouthpiece local to world matrix with controller position/rotation
                Matrix4x4 controllerMatrix = rightHandle.parent.localToWorldMatrix * Matrix4x4.TRS(p, r, Vector3.one) *
                    Matrix4x4.TRS(rightMouthpiece.localPosition, rightMouthpiece.localRotation, new Vector3(scale, scale, scale));

                TransformSelection(controllerMatrix);
            }


            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);

                if (joystickAxis != Vector2.zero)
                {
                    CameraController cameraController = GetSingleSelectedCamera();
                    if (null != cameraController)
                    {
                        if (null == cameraFocalCommand && null == undoGroup)
                        {
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

                        cameraController.focal = currentFocal;
                        SendCameraFocal(cameraController);
                    }
                }
                else
                {
                    SubmitCameraFocalCommand();
                }
            }

        }

        protected void TransformSelection(Matrix4x4 transformation)
        {
            transformation = transformation * initTransformation;

            foreach (GameObject obj in Selection.GetObjects())
            {
                // Some objects may be locked, so check that
                ParametersController parameters = obj.GetComponent<ParametersController>();
                if (null != parameters && parameters.locked) { continue; }

                var meshParentTransform = obj.transform.parent;
                Matrix4x4 meshParentMatrixInverse = new Matrix4x4();
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
                        OnPreTransformSelection(obj.transform, ref mat);
                        // Set matrix
                        SyncData.SetTransform(obj.name, mat);
                        CommandManager.SendEvent(MessageType.Transform, obj.transform);
                    }
                }
            }
        }

        public virtual void OnPreTransformSelection(Transform transform, ref Matrix4x4 transformed) { }

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
            else if (!Selection.IsHandleSelected()) // Dont select things if we have a window selected.
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
                VRInput.SendHapticImpulse(VRInput.rightController, 0, 1, 0.1f);
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
                VRInput.SendHapticImpulse(VRInput.rightController, 0, 1, 0.1f);
            }
        }

        public void ClearSelection()
        {
            List<GameObject> objects = new List<GameObject>();
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                objects.Add(data.Value);
            }
            new CommandRemoveFromSelection(objects).Submit();

            Selection.ClearSelection();
        }

        public GameObject DuplicateObject(GameObject source, bool withVFX = true)
        {
            if (source.GetComponent<UIHandle>())
                return null;

            GameObject clone = SyncData.Duplicate(source);
            if (null == clone)
                return null;
            new CommandDuplicateGameObject(clone, source).Submit();

            // Add a selectionVFX instance on the duplicated objects
            if (withVFX)
            {
                GameObject vfxInstance = Instantiate(selectionVFXPrefab);
                vfxInstance.GetComponent<SelectionVFX>().SpawnDuplicateVFX(clone);
            }

            if (Selection.IsSelected(source))
            {
                Selection.RemoveFromSelection(source);
                new CommandRemoveFromSelection(source).Submit();
                Selection.AddToSelection(clone);
                new CommandAddToSelection(clone).Submit();
            }
            if (source == Selection.GetGrippedObject())
            {
                Selection.SetGrippedObject(clone);
            }
            return clone;
        }

        public void DuplicateSelection()
        {
            CommandGroup group = new CommandGroup("Duplicate Selection");
            try
            {
                ManageMoveObjectsUndo();

                List<GameObject> objectsToBeDuplicated = Selection.GetObjects();
                foreach (GameObject obj in objectsToBeDuplicated)
                    DuplicateObject(obj);
            }
            finally
            {
                group.Submit();
            }
        }

        public void OnSelectMode()
        {
            mode = SelectorModes.Select;
            selectorBrush.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);
            updateButtonsColor();
        }

        public void OnEraserMode()
        {
            mode = SelectorModes.Eraser;
            selectorBrush.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", eraseColor);
            updateButtonsColor();
        }

        public Color GetModeColor()
        {
            if (mode == SelectorModes.Select) { return selectionColor; }
            return eraseColor;
        }

        void updateButtonsColor()
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

        public void OnSetLocked(bool locked)
        {
            foreach (GameObject gobject in Selection.GetObjects())
            {
                ParametersController parameters = gobject.GetComponent<ParametersController>();
                if (null != parameters)
                {
                    parameters.locked = locked;
                }
            }
        }
    }
}
