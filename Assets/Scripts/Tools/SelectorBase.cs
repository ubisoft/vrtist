using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class SelectorBase : ToolBase
    {
        [Header("Selector Parameters")]
        [SerializeField] protected Transform selectorBrush;
        [SerializeField] protected Material selectionMaterial;
        [SerializeField] private float deadZoneDistance = 0.005f;

        float selectorRadius;
        protected Color selectionColor = new Color(0f, 167f / 255f, 1f);
        protected Color eraseColor = new Color(1f, 0f, 0f);

        Dictionary<GameObject, Matrix4x4> initParentMatrix = new Dictionary<GameObject, Matrix4x4>();
        Dictionary<GameObject, Vector3> initPositions = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Quaternion> initRotations = new Dictionary<GameObject, Quaternion>();
        Dictionary<GameObject, Vector3> initScales = new Dictionary<GameObject, Vector3>();
        protected Vector3 initControllerPosition;
        protected Quaternion initControllerRotation;

        protected Matrix4x4 initTransformation;

        public enum SelectorModes { Select = 0, Eraser }
        public SelectorModes mode = SelectorModes.Select;

        const float deadZone = 0.3f;
        const float scaleFactor = 1.1f;

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
        protected GameObject displayTooltip;

        public GameObject selectionVFXPrefab = null;
        private Dopesheet dopesheet;

        void Start()
        {
            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnSelectMode();
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

        protected virtual void Init()
        {
            CreateTooltips();

            selectorRadius = selectorBrush.localScale.x;
            selectorBrush.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);

            updateButtonsColor();

            Selection.selectionMaterial = selectionMaterial;
            Selection.OnSelectionChanged += OnSelectionChanged;

            if(null == selectionVFXPrefab)
            {
                selectionVFXPrefab = Resources.Load<GameObject>("Prefabs/SelectionVFX");
            }

            dopesheet = GameObject.FindObjectOfType<Dopesheet>();
            UnityEngine.Assertions.Assert.IsNotNull(dopesheet);
        }

        protected void CreateTooltips()
        {
            GameObject controller = transform.Find("right_controller").gameObject;
            Tooltips.CreateTooltip(controller, Tooltips.Anchors.Primary, "Duplicate");
            Tooltips.CreateTooltip(controller, Tooltips.Anchors.Secondary, "Switch Tool");
            triggerTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Trigger, "Select");
            gripTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Grip, "Select & Move");
            joystickTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Joystick, "Scale");
            displayTooltip = Tooltips.CreateTooltip(controller, Tooltips.Anchors.Info, "0\nselected");
            Tooltips.SetTooltipVisibility(triggerTooltip, false);
            Tooltips.SetTooltipVisibility(gripTooltip, false);
            Tooltips.SetTooltipVisibility(joystickTooltip, false);
            Tooltips.SetTooltipVisibility(displayTooltip, false);
        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            if(VRInput.GetValue(VRInput.rightController, CommonUsages.grip) <= deadZone)
            {
                if(GlobalState.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                {
                    // Change selector size
                    Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                    if(val != Vector2.zero)
                    {
                        if(val.y > deadZone) { selectorRadius *= scaleFactor; }//+= 0.001f; }
                        if(val.y < -deadZone) { selectorRadius /= scaleFactor; }//-= 0.001f; }
                        selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                        selectorBrush.localScale = new Vector3(selectorRadius, selectorRadius, selectorRadius);
                    }
                }
            }

            switch(mode)
            {
                case SelectorModes.Select: UpdateSelect(position, rotation); break;
                case SelectorModes.Eraser: UpdateEraser(position, rotation); break;
            }
        }

        protected override void ShowTool(bool show)
        {
            Transform sphere = gameObject.transform.Find("Sphere");
            if(sphere != null)
            {
                sphere.gameObject.SetActive(show);
            }

            Transform rightController = gameObject.transform.Find("right_controller");
            if(rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        void SetControllerVisible(bool visible)
        {
            transform.localScale = visible ? Vector3.one : Vector3.zero;
        }

        protected void InitControllerMatrix()
        {
            VRInput.GetControllerTransform(VRInput.rightController, out initControllerPosition, out initControllerRotation);
            initTransformation = (transform.parent.localToWorldMatrix * Matrix4x4.TRS(initControllerPosition, initControllerRotation, Vector3.one)).inverse;
        }

        protected void InitTransforms()
        {
            initParentMatrix.Clear();
            initPositions.Clear();
            initRotations.Clear();
            initScales.Clear();
            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                initParentMatrix[data.Value] = data.Value.transform.parent.localToWorldMatrix;
                initPositions[data.Value] = data.Value.transform.localPosition;
                initRotations[data.Value] = data.Value.transform.localRotation;
                initScales[data.Value] = data.Value.transform.localScale;
            }
            scale = 1f;
        }

        public void OnGripWorld(bool value)
        {
            if(value)
            {
                if(!gripPrevented && gripped) // no need to interrupt if the grip was prevented
                {
                    OnEndGrip(); // prematurely end the grip action
                    gripInterrupted = true; // set bool to return immediately in the "real" OnEndGrip called when ungripping the controller.
                }
            }
        }

        protected void OnStartGrip()
        {
            if(GlobalState.IsGrippingWorld)
            {
                gripPrevented = true;
                return;
            }

            undoGroup = new CommandGroup();

            InitControllerMatrix();
            InitTransforms();
            outOfDeadZone = false;
            gripped = true;
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

            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gObject = data.Value;
                if(initPositions[gObject] == gObject.transform.localPosition && initRotations[gObject] == gObject.transform.localRotation && initScales[gObject] == gObject.transform.localScale)
                    continue;
                objects.Add(gObject.name);
                beginPositions.Add(initPositions[gObject]);
                beginRotations.Add(initRotations[gObject]);
                beginScales.Add(initScales[gObject]);

                endPositions.Add(gObject.transform.localPosition);
                endRotations.Add(gObject.transform.localRotation);
                endScales.Add(gObject.transform.localScale);
            }

            if(objects.Count > 0)
                new CommandMoveObjects(objects, beginPositions, beginRotations, beginScales, endPositions, endRotations, endScales).Submit();
        }

        protected void OnEndGrip()
        {
            if(gripPrevented)
            {
                gripPrevented = false;
                return;
            }

            if(gripInterrupted)
            {
                gripInterrupted = false;
                return;
            }

            List<ParametersController> controllers = new List<ParametersController>();
            foreach(var item in Selection.selection)
            {
                LightController lightController = item.Value.GetComponentInChildren<LightController>();
                if(null != lightController)
                {
                    controllers.Add(lightController);
                    continue;
                }
                CameraController cameraController = item.Value.GetComponentInChildren<CameraController>();
                if(null != cameraController)
                {
                    controllers.Add(cameraController);
                    continue;
                }
            }
            if(controllers.Count > 0)
            {
                GlobalState.ShowHideControllersGizmos(controllers.ToArray(), GlobalState.displayGizmos);
            }

            if(!Selection.IsHandleSelected())
            {
                ManageMoveObjectsUndo();
            }
            undoGroup.Submit();
            undoGroup = null;

            gripped = false;
        }

        private ParametersController GetFirstAnimation()
        {
            foreach(GameObject gObject in Selection.selection.Values)
            {
                ParametersController controller = gObject.GetComponent<ParametersController>();
                if (null == controller)
                    continue;
                if (controller.HasAnimation())
                    return controller;
            }
            return null;
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            InitControllerMatrix();
            InitTransforms();
            outOfDeadZone = false;

            int numSelected = Selection.selection.Count;
            Tooltips.SetTooltipVisibility(joystickTooltip, numSelected > 0);
            Tooltips.SetTooltipVisibility(displayTooltip, numSelected > 0);
            if(numSelected > 0)
            {
                Tooltips.SetTooltipText(displayTooltip, $"{numSelected}\nselected");

                ParametersController controller = GetFirstAnimation();
                if (null != controller)
                {
                    dopesheet.UpdateFromController(controller);
                }
            }
        }

        private void UpdateSelect(Vector3 position, Quaternion rotation)
        {
            // Move & Duplicate selection

            // Duplicate
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.primaryButton,
                () => { },
                () => {
                    if(!Selection.IsHandleSelected() && Selection.selection.Count > 0)
                    {
                        DuplicateSelection();

                        // Add a selectionVFX instance on the duplicated objects
                        foreach(GameObject gobj in Selection.selection.Values)
                        {
                            GameObject vfxInstance = Instantiate(selectionVFXPrefab);
                            vfxInstance.GetComponent<SelectionVFX>().SpawnDuplicateVFX(gobj);
                        }

                        InitControllerMatrix();
                        InitTransforms();
                        outOfDeadZone = true;
                    }
                }
            );

            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.grip, OnStartGrip, OnEndGrip);

            SetControllerVisible(!gripped || Selection.selection.Count == 0);

            if(gripped)
            {
                Vector3 p;
                Quaternion r;
                VRInput.GetControllerTransform(VRInput.rightController, out p, out r);

                if(!outOfDeadZone && Vector3.Distance(p, initControllerPosition) > deadZoneDistance)
                    outOfDeadZone = true;

                if(!outOfDeadZone)
                    return;

                // Joystick zoom only for non-handle objects
                if(!Selection.IsHandleSelected())
                {
                    if(GlobalState.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
                    {
                        Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                        if(joystickAxis.y > deadZone)
                            scale *= scaleFactor;
                        if(joystickAxis.y < -deadZone)
                            scale /= scaleFactor;
                    }
                }

                Transform parent = transform.parent;
                Matrix4x4 controllerMatrix = parent.localToWorldMatrix * Matrix4x4.TRS(p, r, new Vector3(scale, scale, scale));

                TransformSelection(controllerMatrix);
            }
        }

        protected void TransformSelection(Matrix4x4 transformation)
        {
            transformation = transformation * initTransformation;

            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                var meshParentTransform = data.Value.transform.parent;
                Matrix4x4 meshParentMatrixInverse = new Matrix4x4();
                if(meshParentTransform)
                    meshParentMatrixInverse = meshParentTransform.worldToLocalMatrix;
                else
                    meshParentMatrixInverse = Matrix4x4.identity;
                Matrix4x4 transformed = meshParentMatrixInverse * transformation * initParentMatrix[data.Value] * Matrix4x4.TRS(initPositions[data.Value], initRotations[data.Value], initScales[data.Value]);

                if(data.Value.transform.localToWorldMatrix != transformed)
                {
                    // UI objects
                    if(data.Value.GetComponent<UIHandle>())
                    {
                        data.Value.transform.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                        data.Value.transform.localRotation = Quaternion.LookRotation(transformed.GetColumn(2), transformed.GetColumn(1));
                        //data.Value.transform.localScale = new Vector3(transformed.GetColumn(0).magnitude, transformed.GetColumn(1).magnitude, transformed.GetColumn(2).magnitude);
                    }
                    // Standard game objects
                    else
                    {
                        Matrix4x4 currentMat = meshParentMatrixInverse * initParentMatrix[data.Value] * Matrix4x4.TRS(initPositions[data.Value], initRotations[data.Value], initScales[data.Value]);
                        OnPreTransformSelection(currentMat, ref transformed);

                        // Set matrix
                        SyncData.SetTransform(data.Value.name, transformed);
                        CommandManager.SendEvent(MessageType.Transform, data.Value.transform);
                    }
                }
            }
        }

        public virtual void OnPreTransformSelection(Matrix4x4 current, ref Matrix4x4 transformed) { }

        private void UpdateEraser(Vector3 position, Quaternion rotation)
        {
            // Nothing for now
        }

        private List<GameObject> GetGroupSiblings(GameObject gObject)
        {
            List<GameObject> objects = new List<GameObject>();
            Transform parent = gObject.transform.parent;
            if(parent.name.StartsWith("Group__"))
            {
                for(int i = 0; i < parent.childCount; i++)
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
            if(gObject.GetComponent<UIHandle>())
            {
                // if we select a UI handle, deselect all other objects first.
                ClearSelection();
            }
            return Selection.AddToSelection(gObject);
        }

        public void AddSiblingsToSelection(GameObject gObject, bool haptic = true)
        {
            List<GameObject> objects = GetGroupSiblings(gObject);
            List<GameObject> objectsAddedToSelection = new List<GameObject>();
            foreach(GameObject gobj in objects)
            {
                if(Selection.IsSelected(gobj))
                    continue;
                if(AddToSelection(gobj))
                    objectsAddedToSelection.Add(gobj);
            }

            if(haptic && objectsAddedToSelection.Count > 0)
            {
                VRInput.rightController.SendHapticImpulse(0, 1, 0.1f);
            }

            new CommandAddToSelection(objectsAddedToSelection).Submit();
        }

        public bool RemoveFromSelection(GameObject gObject)
        {
            return Selection.RemoveFromSelection(gObject);
        }

        public void RemoveSiblingsFromSelection(GameObject gObject, bool haptic = true)
        {
            List<GameObject> objects = GetGroupSiblings(gObject);

            List<GameObject> objectsRemovedFromSelection = new List<GameObject>();
            foreach(GameObject gobj in objects)
            {
                if(!Selection.IsSelected(gobj))
                    continue;

                if(RemoveFromSelection(gobj))
                {
                    objectsRemovedFromSelection.Add(gobj);
                }
            }

            if(haptic && objectsRemovedFromSelection.Count > 0)
            {
                VRInput.rightController.SendHapticImpulse(0, 1, 0.1f);
            }

            new CommandRemoveFromSelection(objectsRemovedFromSelection).Submit();
        }

        public void ClearSelection()
        {
            List<GameObject> objects = new List<GameObject>();
            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                objects.Add(data.Value);
            }
            new CommandRemoveFromSelection(objects).Submit();

            Selection.ClearSelection();
        }

        public void DuplicateSelection()
        {
            ManageMoveObjectsUndo();

            List<GameObject> clones = new List<GameObject>();

            Dictionary<Transform, Transform> groups = new Dictionary<Transform, Transform>();

            GameObject[] selectedObjects = new GameObject[Selection.selection.Count];
            int i = 0;
            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                // TODO: maybe move that up and not do any duplicate if we have any window handle selected.
                if(!data.Value.GetComponent<UIHandle>())
                {
                    selectedObjects[i] = data.Value;
                    ++i;
                }
            }

            ClearSelection();

            for(i = 0; i < selectedObjects.Length; i++)
            {
                Transform parent = selectedObjects[i].transform.parent;
                if(parent.name.StartsWith("Group__"))
                {/*
                    if (!groups.ContainsKey(parent))
                    {
                        GameObject newGroup = new GameObject("Group__" + groupId.ToString());
                        groupId++;
                        groups[parent] = newGroup.transform;
                        newGroup.transform.parent = parent.parent;
                        newGroup.transform.localPosition = parent.localPosition;
                        newGroup.transform.localRotation = parent.localRotation;
                        newGroup.transform.localScale = parent.localScale;
                    }

                    GameObject clone = Utils.CreateInstance(selectedObjects[i], groups[parent]);
                    clones.Add(clone);

                    new CommandDuplicateGameObject(clone, selectedObjects[i]).Submit();
                    */
                }
                else
                {
                    GameObject clone = SyncData.Duplicate(selectedObjects[i]);
                    clones.Add(clone);

                    new CommandDuplicateGameObject(clone, selectedObjects[i]).Submit();
                }
            }

            foreach(GameObject clone in clones)
            {
                AddToSelection(clone);
            }
            new CommandAddToSelection(clones).Submit();
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
            if(mode == SelectorModes.Select) { return selectionColor; }
            return eraseColor;
        }

        void updateButtonsColor()
        {
            if(!panel)
                return;

            // NOTE: currently the SelectorPanel has 4 children of type UIButton
            //       which have on Canvas children, with in turn has one Image and one Text children.
            // TODO: do a proper Radio Button Group
            for(int i = 0; i < panel.childCount; i++)
            {
                GameObject child = panel.GetChild(i).gameObject;
                UIButton button = child.GetComponent<UIButton>();
                if(button != null)
                {
                    button.Checked = false;

                    if(child.name == "Select" && mode == SelectorModes.Select)
                    {
                        button.Checked = true;
                    }
                    if(child.name == "Eraser" && mode == SelectorModes.Eraser)
                    {
                        button.Checked = true;
                    }
                }
            }
        }

        public void OnLinkAction()
        {
            // TODO
            return;
            if(Selection.selection.Count <= 1)
                return;

            GameObject container = new GameObject("Group__" + groupId.ToString());
            groupId++;

            SortedSet<Transform> groups = new SortedSet<Transform>();

            bool groupHasParent = false;
            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                Transform parent = gobject.transform.parent;
                if(parent && parent.name.StartsWith("Group__"))
                {
                    if(!groupHasParent)
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
                    if(!groupHasParent)
                    {
                        container.transform.parent = data.Value.transform.parent;
                        groupHasParent = true;
                    }
                    data.Value.transform.parent = container.transform;
                }
            }

            foreach(Transform group in groups)
            {
                if(group.childCount == 0)
                    Destroy(group.gameObject);
            }
        }

        public void OnUnlinkAction()
        {
            // TODO
            return;
            SortedSet<Transform> groups = new SortedSet<Transform>();
            foreach(KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gobject = data.Value;
                Transform parent = gobject.transform.parent;
                if(parent && parent.name.StartsWith("Group__"))
                {
                    groups.Add(gobject.transform.parent);
                }
            }

            foreach(Transform group in groups)
            {
                for(int i = group.childCount - 1; i >= 0; i--)
                {
                    group.GetChild(i).parent = group.parent;
                }
                Destroy(group.gameObject);
            }
        }
    }
}
