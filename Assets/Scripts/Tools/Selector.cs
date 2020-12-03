using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR;

namespace VRtist
{
    public class Selector : SelectorBase
    {
        // Snap
        [Header("Movement & Snapping parameters")]
        public UICheckbox snapToGridCheckbox = null;
        public UISlider snapGridSizeSlider = null;
        public UIButton moveOnAllButton = null;
        public UICheckbox moveOnXCheckbox = null;
        public UICheckbox moveOnYCheckbox = null;
        public UICheckbox moveOnZCheckbox = null;

        public UICheckbox snapRotationCheckbox = null;
        public UISlider snapAngleSlider = null;
        public UIButton turnAroundAllButton = null;
        public UICheckbox turnAroundXCheckbox = null;
        public UICheckbox turnAroundYCheckbox = null;
        public UICheckbox turnAroundZCheckbox = null;

        // Panels
        GameObject selectPanel;
        GameObject snapPanel;
        GameObject inspectorPanel;

        UIButton selectPanelButton;
        UIButton snapPanelButton;
        UIButton inspectorPanelButton;

        UILabel selectedObjectNameLabel;

        // Transform
        UISpinner posXSpinner;
        UISpinner posYSpinner;
        UISpinner posZSpinner;

        UIButton posLockButton;
        UIButton posXLockButton;
        UIButton posYLockButton;
        UIButton posZLockButton;

        UISpinner rotXSpinner;
        UISpinner rotYSpinner;
        UISpinner rotZSpinner;

        UIButton rotLockButton;
        UIButton rotXLockButton;
        UIButton rotYLockButton;
        UIButton rotZLockButton;

        UISpinner scaleXSpinner;
        UISpinner scaleYSpinner;
        UISpinner scaleZSpinner;

        UIButton scaleLockButton;
        UIButton scaleXLockButton;
        UIButton scaleYLockButton;
        UIButton scaleZLockButton;

        // Constraints
        UIButton enableParentButton;
        UILabel parentTargetLabel;
        UIButton selectParentButton;
        UIButton deleteParentButton;

        UIButton enableLookAtButton;
        UILabel lookAtTargetLabel;
        UIButton selectLookAtButton;
        UIButton deleteLookAtButton;

        public GridVFX grid = null;

        protected bool snapToGrid = false;
        protected float snapPrecision = 0.05f;    // grid size 5 centimeters (old = 1 meter)
        protected float snapGap = 0.3f; //0.05f;       // relative? percentage?
        protected bool moveOnX = true;
        protected bool moveOnY = true;
        protected bool moveOnZ = true;

        protected bool snapRotation = false;
        protected float snapAngle = 45f;       // in degrees
        protected float snapAngleGap = 0.25f;   // percentage
        protected bool turnAroundAll = true;
        protected bool turnAroundX = true;
        protected bool turnAroundY = true;
        protected bool turnAroundZ = true;

        protected bool scaleOnX = true;
        protected bool scaleOnY = true;
        protected bool scaleOnZ = true;

        [Header("Deformer Parameters")]
        public Transform container;
        public Transform[] planes;
        public GameObject planesContainer;
        [CentimeterFloat] public float cameraSpaceGap = 0.01f;
        [CentimeterFloat] public float collidersThickness = 0.05f;
        public UICheckbox uniformScaleCheckbox = null;
        public bool uniformScale = false;

        private Matrix4x4 initPlaneContainerMatrix;
        private Matrix4x4 initInversePlaneContainerMatrix;
        private Matrix4x4 initOppositeMatrix;

        private Vector3 minBound = Vector3.positiveInfinity;
        private Vector3 maxBound = Vector3.negativeInfinity;

        private DeformerPlane activePlane = null;
        private bool deforming = false;
        private float initMagnitude;
        private Vector3 planeControllerDelta;

        private bool deformEnabled = false;

        private Vector3 initControllerPositionRelativeToHead;
        private Matrix4x4 inverseHeadMatrix;

        private CommandGroup undoGroup = null;

        void Start()
        {
            ToggleMouthpiece(mouthpiece, true);

            // Sub panels
            selectPanel = panel.Find("SelectPanel").gameObject;
            snapPanel = panel.Find("SnapPanel").gameObject;
            inspectorPanel = panel.Find("ObjectPropertiesPanel").gameObject;

            selectPanel.SetActive(true);
            snapPanel.SetActive(false);
            inspectorPanel.SetActive(false);

            selectPanelButton = panel.Find("SelectPanelButton").GetComponent<UIButton>();
            selectPanelButton.Checked = true;
            snapPanelButton = panel.Find("SnapPanelButton").GetComponent<UIButton>();
            inspectorPanelButton = panel.Find("ObjectPropertiesPanelButton").GetComponent<UIButton>();

            selectPanelButton.onReleaseEvent.AddListener(() => OnSelectPanel(selectPanelButton));
            snapPanelButton.onReleaseEvent.AddListener(() => OnSelectPanel(snapPanelButton));
            inspectorPanelButton.onReleaseEvent.AddListener(() => OnSelectPanel(inspectorPanelButton));

            selectedObjectNameLabel = inspectorPanel.transform.Find("Object Name").GetComponent<UILabel>();

            // Constraints
            enableParentButton = inspectorPanel.transform.Find("Constraints/Parent/Active Button").GetComponent<UIButton>();
            parentTargetLabel = inspectorPanel.transform.Find("Constraints/Parent/Target Label").GetComponent<UILabel>();
            selectParentButton = inspectorPanel.transform.Find("Constraints/Parent/Select Button").GetComponent<UIButton>();
            deleteParentButton = inspectorPanel.transform.Find("Constraints/Parent/Delete Button").GetComponent<UIButton>();

            enableParentButton.onReleaseEvent.AddListener(OnToggleParentConstraint);
            selectParentButton.onReleaseEvent.AddListener(() => { selectParentButton.Checked = true; });
            deleteParentButton.onReleaseEvent.AddListener(RemoveParentConstraint);

            enableLookAtButton = inspectorPanel.transform.Find("Constraints/Look At/Active Button").GetComponent<UIButton>();
            lookAtTargetLabel = inspectorPanel.transform.Find("Constraints/Look At/Target Label").GetComponent<UILabel>();
            selectLookAtButton = inspectorPanel.transform.Find("Constraints/Look At/Select Button").GetComponent<UIButton>();
            deleteLookAtButton = inspectorPanel.transform.Find("Constraints/Look At/Delete Button").GetComponent<UIButton>();

            enableLookAtButton.onReleaseEvent.AddListener(OnToggleLookAtConstraint);
            selectLookAtButton.onReleaseEvent.AddListener(() => { selectLookAtButton.Checked = true; });
            deleteLookAtButton.onReleaseEvent.AddListener(RemoveLookAtConstraint);

            // Transforms
            posXSpinner = inspectorPanel.transform.Find("Transform/Position/X/Value").GetComponent<UISpinner>();
            posYSpinner = inspectorPanel.transform.Find("Transform/Position/Y/Value").GetComponent<UISpinner>();
            posZSpinner = inspectorPanel.transform.Find("Transform/Position/Z/Value").GetComponent<UISpinner>();

            posLockButton = inspectorPanel.transform.Find("Transform/Position/Global Lock").GetComponent<UIButton>();
            posXLockButton = inspectorPanel.transform.Find("Transform/Position/X/Lock").GetComponent<UIButton>();
            posYLockButton = inspectorPanel.transform.Find("Transform/Position/Y/Lock").GetComponent<UIButton>();
            posZLockButton = inspectorPanel.transform.Find("Transform/Position/Z/Lock").GetComponent<UIButton>();

            rotXSpinner = inspectorPanel.transform.Find("Transform/Rotation/X/Value").GetComponent<UISpinner>();
            rotYSpinner = inspectorPanel.transform.Find("Transform/Rotation/Y/Value").GetComponent<UISpinner>();
            rotZSpinner = inspectorPanel.transform.Find("Transform/Rotation/Z/Value").GetComponent<UISpinner>();

            rotLockButton = inspectorPanel.transform.Find("Transform/Rotation/Global Lock").GetComponent<UIButton>();
            rotXLockButton = inspectorPanel.transform.Find("Transform/Rotation/X/Lock").GetComponent<UIButton>();
            rotYLockButton = inspectorPanel.transform.Find("Transform/Rotation/Y/Lock").GetComponent<UIButton>();
            rotZLockButton = inspectorPanel.transform.Find("Transform/Rotation/Z/Lock").GetComponent<UIButton>();

            scaleXSpinner = inspectorPanel.transform.Find("Transform/Scale/X/Value").GetComponent<UISpinner>();
            scaleYSpinner = inspectorPanel.transform.Find("Transform/Scale/Y/Value").GetComponent<UISpinner>();
            scaleZSpinner = inspectorPanel.transform.Find("Transform/Scale/Z/Value").GetComponent<UISpinner>();

            scaleLockButton = inspectorPanel.transform.Find("Transform/Scale/Global Lock").GetComponent<UIButton>();
            scaleXLockButton = inspectorPanel.transform.Find("Transform/Scale/X/Lock").GetComponent<UIButton>();
            scaleYLockButton = inspectorPanel.transform.Find("Transform/Scale/Y/Lock").GetComponent<UIButton>();
            scaleZLockButton = inspectorPanel.transform.Find("Transform/Scale/Z/Lock").GetComponent<UIButton>();

            posXSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(posXSpinner.FloatValue, "px"));
            posYSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(posYSpinner.FloatValue, "py"));
            posZSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(posZSpinner.FloatValue, "pz"));

            rotXSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(rotXSpinner.FloatValue, "rx"));
            rotYSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(rotYSpinner.FloatValue, "ry"));
            rotZSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(rotZSpinner.FloatValue, "rz"));

            scaleXSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(scaleXSpinner.FloatValue, "sx"));
            scaleYSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(scaleYSpinner.FloatValue, "sy"));
            scaleZSpinner.onReleaseEvent.AddListener(() => OnStartEditTransform(scaleZSpinner.FloatValue, "sz"));

            posLockButton.onCheckEvent.AddListener(SetLockPosition);
            posXLockButton.onCheckEvent.AddListener((bool value) => SetMoveOnX(!value));
            posYLockButton.onCheckEvent.AddListener((bool value) => SetMoveOnY(!value));
            posZLockButton.onCheckEvent.AddListener((bool value) => SetMoveOnZ(!value));

            rotLockButton.onCheckEvent.AddListener(SetLockRotation);
            rotXLockButton.onCheckEvent.AddListener((bool value) => SetTurnAroundX(!value));
            rotYLockButton.onCheckEvent.AddListener((bool value) => SetTurnAroundY(!value));
            rotZLockButton.onCheckEvent.AddListener((bool value) => SetTurnAroundZ(!value));

            scaleLockButton.onCheckEvent.AddListener(SetLockScale);
            scaleXLockButton.onCheckEvent.AddListener((bool value) => SetScaleOnX(!value));
            scaleYLockButton.onCheckEvent.AddListener((bool value) => SetScaleOnY(!value));
            scaleZLockButton.onCheckEvent.AddListener((bool value) => SetScaleOnZ(!value));

            // Global events bindings
            GlobalState.ObjectMovingEvent.AddListener(UpdateTransformUI);
            GlobalState.ObjectConstraintEvent.AddListener((GameObject gobject) => UpdateUIOnSelectionChanged(null, null));
            Selection.OnSelectionChanged += SetConstraintTargetOnSelectionChanged;
            Selection.OnSelectionChanged += UpdateUIOnSelectionChanged;

            Init();
        }

        void OnStartEditTransform(float currentValue, string attr)
        {
            if (Selection.IsEmpty()) { return; }
            ToolsUIManager.Instance.OpenNumericKeyboard((float value) => OnEndEditTransform(value, attr), panel, currentValue);
        }

        void OnEndEditTransform(float value, string attr)
        {
            // Expecting attr to be one of px, py, pz, rx, ry, rz, sx, sy, sz
            CommandMoveObjects command = new CommandMoveObjects();

            GameObject firstSelected = null;
            foreach (var selected in Selection.GetSelectedObjects())
            {
                if (null == firstSelected) { firstSelected = selected; }
                Vector3 position = selected.transform.localPosition;
                Vector3 rotation = selected.transform.localEulerAngles;
                Vector3 scale = selected.transform.localScale;

                if (attr[0] == 'p')
                {                    
                    switch (attr[1])
                    {
                        case 'x': position.x = value; break;
                        case 'y': position.y = value; break;
                        case 'z': position.z = value; break;
                    }                                        
                }
                else if (attr[0] == 'r')
                {                    
                    switch (attr[1])
                    {
                        case 'x': rotation.x = value; break;
                        case 'y': rotation.y = value; break;
                        case 'z': rotation.z = value; break;
                    }
                    
                }
                else if (attr[0] == 's')
                {                    
                    switch (attr[1])
                    {
                        case 'x': scale.x = value; break;
                        case 'y': scale.y = value; break;
                        case 'z': scale.z = value; break;
                    }
                    
                }

                command.AddObject(selected, position, Quaternion.Euler(rotation), scale);

                selected.transform.localPosition = position;
                selected.transform.localEulerAngles = rotation;
                selected.transform.localScale = scale;
            }

            if (null != firstSelected)
            {
                command.Submit();
                UpdateTransformUI(firstSelected);
            }
        }

        void OnToggleParentConstraint()
        {
            foreach (var selected in Selection.GetSelectedObjects())
            {
                ParentConstraint constraint = selected.GetComponent<ParentConstraint>();
                if (null != constraint)
                {
                    constraint.constraintActive = !constraint.constraintActive;
                }
            }
        }

        void SetParentConstraint()
        {
            GameObject hovered = Selection.GetHoveredObject();
            if (null == hovered) { return; }
            UIHandle uiHandle = hovered.GetComponent<UIHandle>();
            if (null != uiHandle) { return; }

            CommandGroup commandGroup = new CommandGroup("Add Parent Constraint");
            foreach (var selected in Selection.GetSelectedObjects())
            {
                CommandAddConstraint command = new CommandAddConstraint(ConstraintType.Parent, selected, hovered);
                command.Submit();
            }
            commandGroup.Submit();
        }

        void RemoveParentConstraint()
        {
            CommandGroup commandGroup = new CommandGroup();
            foreach (var selected in Selection.GetSelectedObjects())
            {
                CommandRemoveConstraint command = new CommandRemoveConstraint(ConstraintType.Parent, selected);
                command.Submit();
            }
            commandGroup.Submit();
        }

        void OnToggleLookAtConstraint()
        {
            foreach (var selected in Selection.GetSelectedObjects())
            {
                LookAtConstraint constraint = selected.GetComponent<LookAtConstraint>();
                if (null != constraint)
                {
                    constraint.constraintActive = !constraint.constraintActive;
                }
            }
        }

        void SetLookAtConstraint()
        {
            GameObject hovered = Selection.GetHoveredObject();
            if (null == hovered) { return; }
            UIHandle uiHandle = hovered.GetComponent<UIHandle>();
            if (null != uiHandle) { return; }

            CommandGroup commandGroup = new CommandGroup();
            foreach (var selected in Selection.GetSelectedObjects())
            {
                CommandAddConstraint command = new CommandAddConstraint(ConstraintType.LookAt, selected, hovered);
                command.Submit();
            }
            commandGroup.Submit();
        }

        void RemoveLookAtConstraint()
        {
            CommandGroup commandGroup = new CommandGroup();
            foreach (var selected in Selection.GetSelectedObjects())
            {
                CommandRemoveConstraint command = new CommandRemoveConstraint(ConstraintType.LookAt, selected);
                command.Submit();
            }
            commandGroup.Submit();
        }

        void UpdateUIOnSelectionChanged(object _, SelectionChangedArgs args)
        {
            if (null == selectedObjectNameLabel)
                return;
            // Clear
            selectedObjectNameLabel.Text = "";

            enableParentButton.Checked = false;
            parentTargetLabel.Text = "";

            enableLookAtButton.Checked = false;
            lookAtTargetLabel.Text = "";

            posLockButton.Checked = false;
            rotLockButton.Checked = false;
            scaleLockButton.Checked = false;

            GameObject selected = Selection.GetFirstSelectedObject();
            if (null == selected)
            {
                return;
            }

            // Selected label
            if (Selection.Count() > 1) { selectedObjectNameLabel.Text = $"{Selection.Count()} objects selected"; }
            else { selectedObjectNameLabel.Text = selected.name; }
            selectedObjectNameLabel.Text = "<color=#0079FF>" + selectedObjectNameLabel.Text + "</color>";

            // Transform
            UpdateTransformUI(selected);

            // Constraints
            ParentConstraint parentConstraint = selected.GetComponent<ParentConstraint>();
            if (null != parentConstraint && parentConstraint.sourceCount > 0)
            {
                enableParentButton.Checked = parentConstraint.constraintActive;
                parentTargetLabel.Text = parentConstraint.GetSource(0).sourceTransform.name;
            }

            LookAtConstraint lookAtConstraint = selected.GetComponent<LookAtConstraint>();
            if (null != lookAtConstraint && lookAtConstraint.sourceCount > 0)
            {
                enableLookAtButton.Checked = lookAtConstraint.constraintActive;
                lookAtTargetLabel.Text = lookAtConstraint.GetSource(0).sourceTransform.name;
            }
        }

        void SetConstraintTargetOnSelectionChanged(object _, SelectionChangedArgs args)
        {
            // Manage constraints target selection
            if (selectParentButton.Checked)
            {
                SetParentConstraint();
                selectParentButton.Checked = false;
            }
            if (selectLookAtButton.Checked)
            {
                SetLookAtConstraint();
                selectLookAtButton.Checked = false;
            }
        }

        void UpdateTransformUI(GameObject gobject)
        {
            GameObject selected = Selection.GetFirstSelectedObject();
            if (null == selected || gobject != selected) { return; }

            // Transform
            Vector3 localPosition = selected.transform.localPosition;
            posXSpinner.FloatValue = localPosition.x;
            posYSpinner.FloatValue = localPosition.y;
            posZSpinner.FloatValue = localPosition.z;

            Vector3 localRotation = selected.transform.localEulerAngles;
            rotXSpinner.FloatValue = localRotation.x;
            rotYSpinner.FloatValue = localRotation.y;
            rotZSpinner.FloatValue = localRotation.z;

            Vector3 localScale = selected.transform.localScale;
            scaleXSpinner.FloatValue = localScale.x;
            scaleYSpinner.FloatValue = localScale.y;
            scaleZSpinner.FloatValue = localScale.z;

            ParametersController parametersController = selected.GetComponent<ParametersController>();
            if (null != parametersController)
            {
                posLockButton.Checked = parametersController.lockPosition;
                SetLockPosition(parametersController.lockPosition);
                rotLockButton.Checked = parametersController.lockRotation;
                SetLockRotation(parametersController.lockRotation);
                scaleLockButton.Checked = parametersController.lockScale;
                SetLockScale(parametersController.lockScale);
            }
        }

        void OnSelectPanel(UIButton button)
        {
            selectPanelButton.Checked = button == selectPanelButton;
            snapPanelButton.Checked = button == snapPanelButton;
            inspectorPanelButton.Checked = button == inspectorPanelButton;

            selectPanel.SetActive(button == selectPanelButton);
            snapPanel.SetActive(button == snapPanelButton);
            inspectorPanel.SetActive(button == inspectorPanelButton);
        }

        protected override void Init()
        {
            base.Init();
            InitUIPanel();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitUIPanel();
            UpdateGrid();
            Selection.OnSelectionChanged += UpdateGridFromSelection;
            if (null != planesContainer) { planesContainer.SetActive(false); }
            UpdateUIOnSelectionChanged(null, null);
        }

        protected override void OnDisable()
        {
            if (null != undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }

            base.OnDisable();
            Selection.OnSelectionChanged -= UpdateGridFromSelection;
            if (null != grid) { grid.gameObject.SetActive(false); }
            if (null != planesContainer) { planesContainer.SetActive(false); }
        }

        public void OnDeleteSelection()
        {
            List<GameObject> allSelected = Selection.GetSelectedObjects(SelectionType.Selection);
            if (allSelected.Count == 0) { return; }

            CommandGroup group = new CommandGroup("Delete All Selection");
            try
            {
                foreach (GameObject selected in allSelected)
                {
                    ToolsUIManager.Instance.SpawnDeleteInstanceVFX(selected);
                    new CommandRemoveGameObject(selected).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        protected void UpdateGrid()
        {
            List<GameObject> objects = Selection.GetGrippedOrSelection();
            int numSelected = objects.Count;
            bool showGrid = numSelected > 0 && snapToGrid;
            if (grid != null)
            {
                grid.gameObject.SetActive(showGrid);
                if (showGrid)
                {
                    grid.SetStepSize(snapPrecision);

                    grid.SetAxis(moveOnX, moveOnZ, moveOnY); // right handed

                    foreach (GameObject gobj in objects)
                    {
                        // Snap VFX position in (world object) local space.
                        Vector3 targetPositionInWorldObject = world.InverseTransformPoint(gobj.transform.position);
                        float snappedX = moveOnX ? Mathf.Round(targetPositionInWorldObject.x / snapPrecision) * snapPrecision : targetPositionInWorldObject.x;
                        float snappedY = moveOnZ ? Mathf.Round(targetPositionInWorldObject.y / snapPrecision) * snapPrecision : targetPositionInWorldObject.y; // NOTE: right handed.
                        float snappedZ = moveOnY ? Mathf.Round(targetPositionInWorldObject.z / snapPrecision) * snapPrecision : targetPositionInWorldObject.z;
                        Vector3 snappedPosition = new Vector3(snappedX, snappedY, snappedZ);
                        grid.transform.localPosition = snappedPosition; // position in world-object space.
                        grid.SetTargetPosition(gobj.transform.position); // world space position of target object.

                        break;
                    }
                }
            }
        }

        public void SetSnapToGrid(bool value)
        {
            snapToGrid = value;
            UpdateGrid();
            if (null != snapGridSizeSlider) { snapGridSizeSlider.Disabled = !snapToGrid; }
            if (!value)  // reset all constraints
            {
                OnMoveOnAll();
            }
        }

        public void OnChangeSnapGridSize(float value)
        {
            snapPrecision = value / 100.0f; // centimeters-to-meters
            grid.SetStepSize(snapPrecision);
            grid.Restart();
        }

        public void OnMoveOnAll()
        {
            moveOnX = moveOnY = moveOnZ = true;
            grid.SetAxis(moveOnX, moveOnZ, moveOnY); // right handed
            grid.Restart(); // re-start the vfx to single-burst a new set of particles with the new axis configuration.
            InitUIPanel();
        }

        public void SetLockPosition(bool value)
        {
            foreach(GameObject gobject in Selection.GetSelectedObjects())
            {
                if (value)
                {
                    ParametersController controller = gobject.GetComponent<ParametersController>();
                    if (null == controller)
                    {
                        controller = gobject.AddComponent<ParametersController>();
                    }
                    controller.lockPosition = value;
                }
                else
                {
                    ParametersController controller = gobject.GetComponent<ParametersController>();
                    if (null != controller)
                    {
                        controller.lockPosition = value;
                    }
                }
            }
        }

        public void SetMoveOnX(bool value)
        {
            moveOnX = value;
            grid.SetAxis(moveOnX, moveOnZ, moveOnY); // right handed
            grid.Restart();
        }

        public void SetMoveOnY(bool value)
        {
            moveOnY = value;
            grid.SetAxis(moveOnX, moveOnZ, moveOnY); // right handed
            grid.Restart();
        }

        public void SetMoveOnZ(bool value)
        {
            moveOnZ = value;
            grid.SetAxis(moveOnX, moveOnZ, moveOnY); // right handed
            grid.Restart();
        }

        public void SetSnapRotation(bool value)
        {
            snapRotation = value;
            InitUIPanel();
            if (!value)  // reset all constraints
            {
                OnTurnAroundAll();
            }
        }

        public void OnChangeSnapAngle(float value)
        {
            snapAngle = value;
        }

        public void OnTurnAroundAll()
        {
            turnAroundAll = true;
            turnAroundX = turnAroundY = turnAroundZ = true;
            InitUIPanel();
        }

        public void SetLockRotation(bool value)
        {
            foreach (GameObject gobject in Selection.GetSelectedObjects())
            {
                if (value)
                {
                    ParametersController controller = gobject.GetComponent<ParametersController>();
                    if (null == controller)
                    {
                        controller = gobject.AddComponent<ParametersController>();
                    }
                    controller.lockRotation = value;
                }
                else
                {
                    ParametersController controller = gobject.GetComponent<ParametersController>();
                    if (null != controller)
                    {
                        controller.lockRotation = value;
                    }
                }
            }
        }

        public void SetTurnAroundX(bool value)
        {
            if (value || !value && turnAroundAll)  // as a radio button
            {
                turnAroundAll = false;
                turnAroundX = true;
                turnAroundY = false;
                turnAroundZ = false;
            }
            InitUIPanel();
        }

        public void SetTurnAroundY(bool value)
        {
            if (value || !value && turnAroundAll)  // as a radio button
            {
                turnAroundAll = false;
                turnAroundX = false;
                turnAroundY = true;
                turnAroundZ = false;
            }
            InitUIPanel();
        }

        public void SetTurnAroundZ(bool value)
        {
            if (value || !value && turnAroundAll)  // as a radio button
            {
                turnAroundAll = false;
                turnAroundX = false;
                turnAroundY = false;
                turnAroundZ = true;
            }
            InitUIPanel();
        }

        public void SetLockScale(bool value)
        {
            foreach (GameObject gobject in Selection.GetSelectedObjects())
            {
                if (value)
                {
                    ParametersController controller = gobject.GetComponent<ParametersController>();
                    if (null == controller)
                    {
                        controller = gobject.AddComponent<ParametersController>();
                    }
                    controller.lockScale = value;
                }
                else
                {
                    ParametersController controller = gobject.GetComponent<ParametersController>();
                    if (null != controller)
                    {
                        controller.lockScale = value;
                    }
                }
            }
        }

        public void SetScaleOnX(bool value)
        {
            if (value != scaleOnX)
            {
                scaleOnX = true;
                InitUIPanel();
            }
        }

        public void SetScaleOnY(bool value)
        {
            if (value != scaleOnY)
            {
                scaleOnY = true;
                InitUIPanel();
            }
        }
        public void SetScaleOnZ(bool value)
        {
            if (value != scaleOnZ)
            {
                scaleOnZ = true;
                InitUIPanel();
            }
        }
        public void EnableDeformMode(bool enabled)
        {
            deformEnabled = enabled;
            if (!enabled)
            {
                planesContainer.SetActive(false);
            }
        }

        public void SetUniformScale(bool value)
        {
            uniformScale = value;
        }

        protected virtual void InitUIPanel()
        {
            if (null != snapToGridCheckbox) { snapToGridCheckbox.Checked = snapToGrid; }
            if (null != snapGridSizeSlider)
            {
                snapGridSizeSlider.Value = snapPrecision * 100.0f; // meters-to-centimeters
                snapGridSizeSlider.Disabled = !snapToGrid;
            }
            if (null != moveOnXCheckbox) { moveOnXCheckbox.Checked = moveOnX; }
            if (null != moveOnYCheckbox) { moveOnYCheckbox.Checked = moveOnY; }
            if (null != moveOnZCheckbox) { moveOnZCheckbox.Checked = moveOnZ; }

            if (null != posXLockButton) { posXLockButton.Checked = !moveOnX; }
            if (null != posYLockButton) { posYLockButton.Checked = !moveOnY; }
            if (null != posZLockButton) { posZLockButton.Checked = !moveOnZ; }

            if (null != posLockButton)
            {
                if (!moveOnX && !moveOnY && !moveOnZ)
                    posLockButton.Checked = true;
                else
                    posLockButton.Checked = false;
            }

            if (null != rotXLockButton) { rotXLockButton.Checked = !turnAroundX; }
            if (null != rotYLockButton) { rotYLockButton.Checked = !turnAroundY; }
            if (null != rotZLockButton) { rotZLockButton.Checked = !turnAroundZ; }

            if (null != rotLockButton)
            {
                if (!turnAroundX && !turnAroundY && !turnAroundZ)
                    rotLockButton.Checked = true;
                else
                    rotLockButton.Checked = false;
            }

            if (null != scaleXLockButton) { scaleXLockButton.Checked = !scaleOnX; }
            if (null != scaleYLockButton) { scaleYLockButton.Checked = !scaleOnY; }
            if (null != scaleZLockButton) { scaleZLockButton.Checked = !scaleOnZ; }

            if (null != scaleLockButton)
            {
                if (!scaleOnX && !scaleOnY && !scaleOnZ)
                    scaleLockButton.Checked = true;
                else
                    scaleLockButton.Checked = false;
            }


            if (null != snapRotationCheckbox) { snapRotationCheckbox.Checked = snapRotation; }
            if (null != snapAngleSlider)
            {
                snapAngleSlider.Value = snapAngle;
                snapAngleSlider.Disabled = !snapRotation;
            }
            if (null != turnAroundXCheckbox) { turnAroundXCheckbox.Checked = turnAroundX; }
            if (null != turnAroundYCheckbox) { turnAroundYCheckbox.Checked = turnAroundY; }
            if (null != turnAroundZCheckbox) { turnAroundZCheckbox.Checked = turnAroundZ; }
        }

        protected override void OnStartGrip()
        {
            base.OnStartGrip();

            // Get head position
            Vector3 HeadPosition;
            Quaternion headRotation;
            VRInput.GetControllerTransform(VRInput.head, out HeadPosition, out headRotation);
            inverseHeadMatrix = Matrix4x4.TRS(HeadPosition, headRotation, Vector3.one).inverse;

            initControllerPositionRelativeToHead = inverseHeadMatrix.MultiplyPoint(initControllerPosition);
        }

        /*
        public override void OnPreTransformSelection(Transform transform, ref Matrix4x4 transformed)
        {
            // Constrain movement
            if (turnAroundAll)
            {
                // Translate
                if (!moveOnX || !moveOnY || !moveOnZ || snapToGrid)
                {
                    Vector4 column = transformed.GetColumn(3);

                    float absWorldScale = Mathf.Abs(GlobalState.WorldScale);
                    Vector3 position = new Vector3(column.x, column.y, column.z);
                    Vector3 roundedPosition = new Vector3(
                        Mathf.Round(column.x / snapPrecision) * snapPrecision,
                        Mathf.Round(column.y / snapPrecision) * snapPrecision,
                        Mathf.Round(column.z / snapPrecision) * snapPrecision
                    );

                    float snapThreshold = (snapGap * snapPrecision) / absWorldScale;

                    if (!moveOnX) { column.x = transform.localPosition.x; }
                    else if (snapToGrid && Mathf.Abs(position.x - roundedPosition.x) <= snapThreshold)
                    {
                        column.x = roundedPosition.x;
                    }

                    if (!moveOnY) { column.y = transform.localPosition.y; }
                    else if (snapToGrid && Mathf.Abs(position.y - roundedPosition.y) <= snapThreshold)
                    {
                        column.y = roundedPosition.y;
                    }

                    if (!moveOnZ) { column.z = transform.localPosition.z; }
                    else if (snapToGrid && Mathf.Abs(position.z - roundedPosition.z) <= snapThreshold)
                    {
                        column.z = roundedPosition.z;
                    }

                    // Set new translation and lock rotation
                    transformed.SetTRS(column, initRotations[transform.gameObject], initScales[transform.gameObject]);
                }
            }
            else  // Rotate & lock translation
            {
                Vector4 column = new Vector4(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z, 1f);
                transformed.SetColumn(3, column);

                // compute rotation angle from start controller position to current controller position
                // in X axis (left/right) local to initial head direction
                Vector3 controllerPosition = rightControllerPosition;
                Quaternion controllerRotation = rightControllerRotation;
                controllerPosition = inverseHeadMatrix.MultiplyPoint(controllerPosition);
                float angle = (controllerPosition.x - initControllerPositionRelativeToHead.x) * 1000f;

                Quaternion newQuaternion = new Quaternion();
                if (snapRotation)
                {
                    float roundedAngle = Mathf.Round(angle / snapAngle) * snapAngle;
                    Vector3 initAngles = world.worldToLocalMatrix * initParentMatrix[transform.gameObject] * initRotations[transform.gameObject].eulerAngles;
                    newQuaternion.eulerAngles = new Vector3(
                        turnAroundX ? roundedAngle - initAngles.x : 0,
                        turnAroundZ ? roundedAngle - initAngles.y : 0,
                        turnAroundY ? -(roundedAngle - initAngles.z) : 0
                    );
                    //newQuaternion.eulerAngles = new Vector3(
                    //    turnAroundX ? Mathf.Round(angle / snapAngle) * snapAngle : 0,
                    //    turnAroundY ? Mathf.Round(angle / snapAngle) * snapAngle : 0,
                    //    turnAroundZ ? Mathf.Round(angle / snapAngle) * snapAngle : 0
                    //);
                }
                else
                {
                    newQuaternion.eulerAngles = new Vector3(
                        turnAroundX ? angle : 0,
                        turnAroundZ ? angle : 0,
                        turnAroundY ? -angle : 0
                    );
                }

                Matrix4x4 objectInitGlobalMatrix = initParentMatrix[transform.gameObject] * Matrix4x4.TRS(initPositions[transform.gameObject], initRotations[transform.gameObject], initScales[transform.gameObject]);
                Matrix4x4 objectInitWorldMatrix = world.worldToLocalMatrix * objectInitGlobalMatrix;
                Matrix4x4 globalRotation = world.localToWorldMatrix * Matrix4x4.TRS(Vector3.zero, newQuaternion, Vector3.one);

                Matrix4x4 globalRotatedObject = globalRotation * objectInitWorldMatrix;
                Matrix4x4 localRotatedObject = initParentMatrix[transform.gameObject].inverse * globalRotatedObject;

                // transformation matrix (local)
                transformed = Matrix4x4.TRS(initPositions[transform.gameObject], Maths.GetRotationFromMatrix(localRotatedObject), initScales[transform.gameObject]);
            }
        }
        */

        public override void OnSelectorTriggerEnter(Collider other)
        {
            Tooltips.SetTooltipVisibility(gripTooltip, true);
        }

        public override void OnSelectorTriggerExit(Collider other)
        {
            Tooltips.SetTooltipVisibility(gripTooltip, false);
        }

        protected void OnStartDeform()
        {
            deforming = true;

            if (null != undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }
            undoGroup = new CommandGroup("Deform");
        }

        protected void OnEndDeform()
        {
            if (null != undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }
            deforming = false;
            SetActivePLane(null);

            ManageMoveObjectsUndo();
        }

        private void UpdateGridFromSelection(object sender, SelectionChangedArgs args)
        {
            UpdateGrid();
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
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = indices;
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

        // Tell whether the current selection contains a hierarchical object (mesh somewhere in children) or not.
        // Camera and lights are known hierarchical objects.
        // TODO: check for multiselection of a light and and simple primitive for example
        private bool IsHierarchical()
        {
            foreach (KeyValuePair<int, GameObject> item in Selection.selection)
            {
                GameObject gObject = item.Value;
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

        public void ComputeSelectionBounds()
        {
            planesContainer.SetActive(gameObject.activeSelf && Selection.selection.Count > 0);
            if (Selection.selection.Count == 0 || Selection.IsHandleSelected())
            {
                planesContainer.SetActive(false);
                return;
            }

            // Get bounds
            minBound = Vector3.positiveInfinity;
            maxBound = Vector3.negativeInfinity;
            bool foundBounds = false;
            int selectionCount = Selection.selection.Count;

            bool foundHierarchicalObject = false;
            if (selectionCount == 1)
            {
                foundHierarchicalObject = IsHierarchical();
            }

            if (selectionCount == 1 && !foundHierarchicalObject)
            {
                // NOTE: pourquoi un foreach si on a un seul element?
                foreach (KeyValuePair<int, GameObject> item in Selection.selection)
                {
                    Transform transform = item.Value.GetComponentInChildren<MeshFilter>().transform;
                    planesContainer.transform.parent = transform.parent;
                    planesContainer.transform.localPosition = transform.localPosition;
                    planesContainer.transform.localRotation = transform.localRotation;
                    planesContainer.transform.localScale = transform.localScale;
                }
            }
            else
            {
                planesContainer.transform.parent = container;
                planesContainer.transform.localPosition = Vector3.zero;
                planesContainer.transform.localRotation = Quaternion.identity;
                planesContainer.transform.localScale = Vector3.one;
            }

            foreach (KeyValuePair<int, GameObject> item in Selection.selection)
            {
                MeshFilter meshFilter = item.Value.GetComponentInChildren<MeshFilter>();
                if (null != meshFilter)
                {
                    Matrix4x4 transform;
                    if (selectionCount > 1 || foundHierarchicalObject)
                    {
                        if (meshFilter.gameObject != item.Value)
                        {
                            transform = container.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                        }
                        else
                        {
                            transform = container.worldToLocalMatrix * item.Value.transform.localToWorldMatrix;
                        }
                    }
                    else
                    {
                        transform = Matrix4x4.identity;
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
                        vertices[i] = transform.MultiplyPoint(vertices[i]);
                        //  Compute min and max bounds
                        if (vertices[i].x < minBound.x) { minBound.x = vertices[i].x; }
                        if (vertices[i].y < minBound.y) { minBound.y = vertices[i].y; }
                        if (vertices[i].z < minBound.z) { minBound.z = vertices[i].z; }

                        if (vertices[i].x > maxBound.x) { maxBound.x = vertices[i].x; }
                        if (vertices[i].y > maxBound.y) { maxBound.y = vertices[i].y; }
                        if (vertices[i].z > maxBound.z) { maxBound.z = vertices[i].z; }
                    }
                    foundBounds = true;
                }
            }

            if (!foundBounds)
            {
                planesContainer.SetActive(false);
                return;
            }

            Vector3 bs = planesContainer.transform.localScale; // boundsScale

            // Collider Scale
            Vector3 cs = new Vector3(
                collidersThickness * (1.0f / world.localScale.x) * (1.0f / bs.x),
                collidersThickness * (1.0f / world.localScale.y) * (1.0f / bs.y),
                collidersThickness * (1.0f / world.localScale.z) * (1.0f / bs.z)
            );

            // GAP: fixed in camera space. Scales with world and objet scales, inverse.
            Vector3 g = new Vector3(
                cameraSpaceGap * (1.0f / world.localScale.x) * (1.0f / bs.x),
                cameraSpaceGap * (1.0f / world.localScale.y) * (1.0f / bs.y),
                cameraSpaceGap * (1.0f / world.localScale.z) * (1.0f / bs.z)
            );

            Vector3 minGapBound = minBound - new Vector3(g.x, g.y, g.z);
            Vector3 maxGapBound = maxBound + new Vector3(g.x, g.y, g.z);

            Vector3 delta = (maxGapBound - minGapBound) * 0.5f;

            // Set planes (depending on their initial rotation)
            // Top
            planes[0].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, maxBound.y, (maxBound.z + minBound.z) * 0.5f);
            planes[0].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, g.y, -delta.z), new Vector3(-delta.x, g.y, delta.z), new Vector3(delta.x, g.y, delta.z), new Vector3(delta.x, g.y, -delta.z));
            SetPlaneCollider(planes[0], new Vector3(0, g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Bottom
            planes[1].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, minBound.y, (maxBound.z + minBound.z) * 0.5f);
            planes[1].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -g.y, -delta.z), new Vector3(delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, delta.z), new Vector3(-delta.x, -g.y, -delta.z));
            SetPlaneCollider(planes[1], new Vector3(0, -g.y, 0), new Vector3(delta.x * 2f, cs.y, delta.z * 2f));

            // Left
            planes[2].transform.localPosition = new Vector3(minBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
            planes[2].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-g.x, -delta.y, -delta.z), new Vector3(-g.x, -delta.y, delta.z), new Vector3(-g.x, delta.y, delta.z), new Vector3(-g.x, delta.y, -delta.z));
            SetPlaneCollider(planes[2], new Vector3(-g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Right
            planes[3].transform.localPosition = new Vector3(maxBound.x, (maxBound.y + minBound.y) * 0.5f, (maxBound.z + minBound.z) * 0.5f);
            planes[3].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(g.x, delta.y, -delta.z), new Vector3(g.x, delta.y, delta.z), new Vector3(g.x, -delta.y, delta.z), new Vector3(g.x, -delta.y, -delta.z));
            SetPlaneCollider(planes[3], new Vector3(g.x, 0, 0), new Vector3(cs.x, delta.y * 2f, delta.z * 2f));

            // Front
            planes[4].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, minBound.z);
            planes[4].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(-delta.x, -delta.y, -g.z), new Vector3(-delta.x, delta.y, -g.z), new Vector3(delta.x, delta.y, -g.z), new Vector3(delta.x, -delta.y, -g.z));
            SetPlaneCollider(planes[4], new Vector3(0, 0, -g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            // Back
            planes[5].transform.localPosition = new Vector3((maxBound.x + minBound.x) * 0.5f, (maxBound.y + minBound.y) * 0.5f, maxBound.z);
            planes[5].GetComponent<MeshFilter>().mesh = CreatePlaneMesh(new Vector3(delta.x, -delta.y, g.z), new Vector3(delta.x, delta.y, g.z), new Vector3(-delta.x, delta.y, g.z), new Vector3(-delta.x, -delta.y, g.z));
            SetPlaneCollider(planes[5], new Vector3(0, 0, g.z), new Vector3(delta.x * 2f, delta.y * 2f, cs.z));

            planesContainer.SetActive(true);
        }

        protected Vector3 FilterControllerDirection()
        {
            Vector3 controllerPosition = rightControllerPosition;
            Quaternion controllerRotation = rightControllerRotation;
            controllerPosition = rightHandle.parent.TransformPoint(controllerPosition); // controller in absolute coordinates

            controllerPosition = initInversePlaneContainerMatrix.MultiplyPoint(controllerPosition);     //controller in planesContainer coordinates
            controllerPosition = Vector3.Scale(controllerPosition, activePlane.direction);              // apply direction (local to planeContainer)
            controllerPosition = initPlaneContainerMatrix.MultiplyPoint(controllerPosition);            // back to absolute coordinates
            return controllerPosition;
        }

        protected override void DoUpdate()
        {
            // Base selection update
            base.DoUpdate();

            // Deform
            if (deformEnabled && activePlane != null)
            {
                VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
                {
                    InitDeformerMatrix();
                    InitTransforms();

                    planeControllerDelta = FilterControllerDirection() - activePlane.transform.position; // in absolute coordinates

                    Vector3 initDelta = activePlane.transform.position - activePlane.opposite.position;
                    initMagnitude = initDelta.magnitude; // initial scale value

                    OnStartDeform();
                }, () =>
                {
                    OnEndDeform();
                });

            }

            if (deformEnabled && deforming)
            {
                Vector3 controllerPosition = FilterControllerDirection();
                controllerPosition -= planeControllerDelta;

                Vector3 delta = controllerPosition - activePlane.opposite.position;
                float magnitude = delta.magnitude;

                float scaleFactor = magnitude / initMagnitude;

                Vector3 scale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

                int selectionCount = Selection.selection.Count;
                bool foundLightOrCamera = false;
                if (selectionCount == 1)
                {
                    foundLightOrCamera = IsHierarchical();
                }

                bool scaleAll = Selection.selection.Count != 1 || foundLightOrCamera || uniformScale;
                if (!scaleAll)
                {
                    scale = new Vector3(
                        activePlane.direction.x == 0f ? 1f : scale.x,
                        activePlane.direction.y == 0f ? 1f : scale.y,
                        activePlane.direction.z == 0f ? 1f : scale.z
                    );
                }

                Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
                Matrix4x4 transformationMatrix = initOppositeMatrix * scaleMatrix;

                TransformSelection(transformationMatrix);
            }

            // Bounds
            if (deformEnabled)
                ComputeSelectionBounds();

            // Move grid with object(s), enable/disable it.
            UpdateGrid();
        }

        protected override void ShowTool(bool show)
        {
            base.ShowTool(show);
            UpdateGrid();
        }

        private void InitDeformerMatrix()
        {
            initTransformation = activePlane.opposite.worldToLocalMatrix;
            initPlaneContainerMatrix = planesContainer.transform.localToWorldMatrix;
            initInversePlaneContainerMatrix = planesContainer.transform.worldToLocalMatrix;
            initOppositeMatrix = activePlane.opposite.localToWorldMatrix;
        }

        public DeformerPlane ActivePlane()
        {
            return activePlane;
        }
        public void SetActivePLane(DeformerPlane plane)
        {
            if (!deforming)
            {
                if (activePlane)
                    activePlane.gameObject.GetComponent<MeshRenderer>().material.SetColor("_PlaneColor", new Color(128f / 255f, 128f / 255f, 128f / 255f, 0.2f));

                activePlane = plane;
                if (plane != null)
                {
                    Color selectColor = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.2f);
                    activePlane.gameObject.GetComponent<MeshRenderer>().material.SetColor("_PlaneColor", selectColor);

                    Tooltips.SetTooltipVisibility(triggerTooltip, true);
                }
                else
                {
                    Tooltips.SetTooltipVisibility(triggerTooltip, false);
                }

                selectorTrigger.enabled = (plane == null);
            }
        }

        public override bool SubToggleTool()
        {
            // From selection to eraser
            if (mode == SelectorModes.Select)
            {
                OnEraserMode();
                return true;  // we toggled
            }

            // From eraser to selection
            OnSelectMode();
            return false;  // not a cyclic toggle, we reach the end
        }
    }
}
