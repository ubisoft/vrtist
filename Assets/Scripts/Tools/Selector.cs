using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VRtist
{
    public class Selector : ToolBase
    {
        [Header("Selector Parameters")]
        [SerializeField] protected Transform selectorBrush;
        [SerializeField] protected Material selectionMaterial;
        [SerializeField] private float deadZoneDistance = 0.005f;

        static protected bool displayGizmos = true;
        public UICheckbox displayGizmosCheckbox = null;

        float selectorRadius;
        protected Color selectionColor = new Color(0f, 167f/255f, 1f);
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

        int groupId = 0;

        protected GameObject triggerTooltip;
        protected GameObject gripTooltip;
        protected GameObject joystickTooltip;

        void Start()
        {
            Init();
            CreateTooltips();
        }

        protected void CreateTooltips()
        {
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Primary, "Duplicate");
            Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Secondary, "Switch Tool");
            triggerTooltip = Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Trigger, "Select");
            gripTooltip = Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Grip, "Select & Move");
            joystickTooltip = Tooltips.CreateTooltip(transform.Find("right_controller").gameObject, Tooltips.Anchors.Joystick, "Scale");
            Tooltips.SetTooltipVisibility(triggerTooltip, false);
            Tooltips.SetTooltipVisibility(gripTooltip, false);
            Tooltips.SetTooltipVisibility(joystickTooltip, false);
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

        public void SetDisplayGizmos(bool value)
        {
            displayGizmos = value;
            ShowHideControllersGizmos(FindObjectsOfType<LightController>() as LightController[], value);
            ShowHideControllersGizmos(FindObjectsOfType<CameraController>() as CameraController[], value);
        }

        private void ShowHideControllersGizmos(ParametersController[] controllers, bool value)
        {
            foreach(var controller in controllers)
            {
                MeshFilter[] meshFilters = controller.gameObject.GetComponentsInChildren<MeshFilter>(true);
                foreach(MeshFilter meshFilter in meshFilters)
                {
                    meshFilter.gameObject.SetActive(value);
                }
            }
        }

        public bool DisplayGizmos()
        {
            return displayGizmos;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if( null != displayGizmosCheckbox)
                displayGizmosCheckbox.Checked = displayGizmos;
            OnSelectMode();
        }

        protected void Init()
        {
            if (null != displayGizmosCheckbox)
                displayGizmosCheckbox.Checked = displayGizmos;

            selectorRadius = selectorBrush.localScale.x;
            selectorBrush.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", selectionColor);

            updateButtonsColor();

            Selection.selectionMaterial = selectionMaterial;
            Selection.OnSelectionChanged += OnSelectionChanged;


        }

        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            //if (uiTools.isOverUI()) { return; }

            if (VRInput.GetValue(VRInput.rightController, CommonUsages.grip) <= deadZone)
            {
                // Change selector size
                Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                if (val != Vector2.zero)
                {
                    if (val.y > deadZone) { selectorRadius *= scaleFactor; }//+= 0.001f; }
                    if (val.y < -deadZone) { selectorRadius /= scaleFactor; }//-= 0.001f; }
                    selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                    selectorBrush.localScale = new Vector3(selectorRadius, selectorRadius, selectorRadius);
                }
            }

            switch (mode)
            {
                case SelectorModes.Select: UpdateSelect(position, rotation); break;
                case SelectorModes.Eraser: UpdateEraser(position, rotation); break;
            }
        }

        protected override void ShowTool(bool show)
        {
            Transform sphere = gameObject.transform.Find("Sphere");
            if (sphere != null)
            {
                sphere.gameObject.SetActive(show);
            }

            Transform rightController = gameObject.transform.Find("right_controller");
            if (rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        float scale = 1f;        

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
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                initParentMatrix[data.Value] = data.Value.transform.parent.localToWorldMatrix;
                initPositions[data.Value] = data.Value.transform.localPosition;
                initRotations[data.Value] = data.Value.transform.localRotation;
                initScales[data.Value] = data.Value.transform.localScale;
            }
            scale = 1f;
        }

        bool outOfDeadZone = false;
        protected bool gripped = false;
        protected CommandGroup undoGroup = null;

        protected void OnStartGrip()
        {
            if (GlobalState.isGrippingWorld)
                return;

            //if (!IsHandleSelected())
            {
                undoGroup = new CommandGroup();
            }

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

            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                GameObject gObject = data.Value;
                if (initPositions[gObject] == gObject.transform.localPosition && initRotations[gObject] == gObject.transform.localRotation && initScales[gObject] == gObject.transform.localScale)
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
            if (GlobalState.isGrippingWorld)
                return;

            foreach (var item in Selection.selection)
            {
                LightController lightController = item.Value.GetComponentInChildren<LightController>();
                if (null == lightController)
                    continue;
                MeshFilter meshFilter = item.Value.GetComponentInChildren<MeshFilter>(true);
                if(null != meshFilter)
                    meshFilter.gameObject.SetActive(displayGizmos);
            }

            if (!Selection.IsHandleSelected())
            {
                ManageMoveObjectsUndo();
            }
                undoGroup.Submit();
                undoGroup = null;
            //}

            gripped = false;
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            InitControllerMatrix();
            InitTransforms();
            outOfDeadZone = false;

            Tooltips.SetTooltipVisibility(joystickTooltip, Selection.selection.Count > 0);
        }

        private void UpdateSelect(Vector3 position, Quaternion rotation)
        {
            // Move & Duplicate selection
            bool buttonAJustPressed = false;

            // get rightPrimaryState
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.primaryButton, 
                () =>
                {
                    buttonAJustPressed = true;
                });            

            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.grip, OnStartGrip, OnEndGrip);

            SetControllerVisible(!gripped || Selection.selection.Count == 0);

            if (gripped)
            {
                // Duplicate selection (except if it is a UI handle)
                if (buttonAJustPressed && !Selection.IsHandleSelected())
                {
                    DuplicateSelection();
                    InitControllerMatrix();
                    InitTransforms();
                    outOfDeadZone = true;
                }

                Vector3 p;
                Quaternion r;
                VRInput.GetControllerTransform(VRInput.rightController, out p, out r);

                if (!outOfDeadZone && Vector3.Distance(p, initControllerPosition) > deadZoneDistance)
                    outOfDeadZone = true;

                if (!outOfDeadZone)
                    return;

                // Joystick zoom only for non-handle objects
                if (!Selection.IsHandleSelected())
                {
                    Vector2 joystickAxis = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                    if (joystickAxis.y > deadZone)
                        scale *= scaleFactor;
                    if (joystickAxis.y < -deadZone)
                        scale /= scaleFactor;
                }

                Transform parent = transform.parent;
                Matrix4x4 controllerMatrix = parent.localToWorldMatrix * Matrix4x4.TRS(p, r, new Vector3(scale, scale, scale));

                TransformSelection(controllerMatrix);
            }
        }

        protected void TransformSelection(Matrix4x4 transformation)
        {
            transformation = transformation * initTransformation;

            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                var meshParentTransform = data.Value.transform.parent;
                Matrix4x4 meshParentMatrixInverse = new Matrix4x4();
                if (meshParentTransform)
                    meshParentMatrixInverse = meshParentTransform.worldToLocalMatrix;
                else
                    meshParentMatrixInverse = Matrix4x4.identity;
                Matrix4x4 transformed = meshParentMatrixInverse * transformation * initParentMatrix[data.Value] * Matrix4x4.TRS(initPositions[data.Value], initRotations[data.Value], initScales[data.Value]);

                if (data.Value.transform.localToWorldMatrix != transformed)
                {
                    if (data.Value.GetComponent<UIHandle>())
                    {
                        data.Value.transform.localPosition = new Vector3(transformed.GetColumn(3).x, transformed.GetColumn(3).y, transformed.GetColumn(3).z);
                        data.Value.transform.localRotation = Quaternion.LookRotation(transformed.GetColumn(2), transformed.GetColumn(1));
                        //data.Value.transform.localScale = new Vector3(transformed.GetColumn(0).magnitude, transformed.GetColumn(1).magnitude, transformed.GetColumn(2).magnitude);
                    }
                    else
                    {
                        SyncData.SetTransform(data.Value.name, transformed);
                        CommandManager.SendEvent(MessageType.Transform, data.Value.transform);
                    }
                }
            }
        }

        private void UpdateEraser(Vector3 position, Quaternion rotation)
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
            if (gObject.GetComponent<UIHandle>())
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
            foreach (GameObject gobj in objects)
            {
                if (Selection.IsSelected(gobj))
                    continue;
                if (AddToSelection(gobj))
                    objectsAddedToSelection.Add(gobj);
            }

            if (haptic && objectsAddedToSelection.Count > 0)
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
                VRInput.rightController.SendHapticImpulse(0, 1, 0.1f);
            }

            new CommandRemoveFromSelection(objectsRemovedFromSelection).Submit();
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

        public void DuplicateSelection()
        {
            ManageMoveObjectsUndo();

            List<GameObject> clones = new List<GameObject>();

            Dictionary<Transform, Transform> groups = new Dictionary<Transform, Transform>();

            GameObject[] selectedObjects = new GameObject[Selection.selection.Count];
            int i = 0;
            foreach (KeyValuePair<int, GameObject> data in Selection.selection)
            {
                // TODO: maybe move that up and not do any duplicate if we have any window handle selected.
                if (!data.Value.GetComponent<UIHandle>())
                {
                    selectedObjects[i] = data.Value;
                    ++i;
                }
            }

            ClearSelection();

            for(i = 0; i < selectedObjects.Length; i++)
            {
                Transform parent = selectedObjects[i].transform.parent;                
                if (parent.name.StartsWith("Group__"))
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

            foreach (GameObject clone in clones)
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
            return;
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
        }

        public void OnUnlinkAction()
        {
            // TODO
            return;
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

        }
    }
}