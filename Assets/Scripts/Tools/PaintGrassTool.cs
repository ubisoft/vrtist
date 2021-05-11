
using UnityEngine;

namespace VRtist
{
    [RequireComponent(typeof(LineRenderer))]
    public class PaintGrassTool : MonoBehaviour
    {
        enum GrassAction { Add, Remove, Edit };
        GrassAction grassAction = GrassAction.Add;

        // Grass Limit
        public int grassLimit = 100000;
        public int currentGrassAmount = 0;

        // Brush Settings
        public LayerMask hitMask = 1;
        public LayerMask paintMask = 1;
        public float brushSize = 0.1f;
        public int density = 5; // number of raycasts, of tufts of grass created per stroke.
        [Range(0.0f, 1.0f)]
        public float normalLimit = 1.0f; // 1.0f = ALL orientation. 0.5f = top half hemisphere. 0.0f = only strictly top/horizontal

        // Modifiers for the EDIT mode.
        public float widthMultiplier = 1f;
        public float heightMultiplier = 1f;
        public Color adjustedColor = new Color(0.5f, 0.5f, 0.5f);

        // ----------------------------------------------------------

        // Current Grass object being worked on.
        private GrassController grass;

        // UI
        #region UI Variables

        //private Transform panel;

        private UIButton uiNewButton;
        private UIButton uiPickButton;
        private UIButton uiActiveButton;

        private UIButton uiAddButton;
        private UIButton uiRemoveButton;
        private UIButton uiEditButton;

        private UIButton uiPaintParamsButton;
        private UIButton uiRenderParamsButton;

        private UIPanel uiPaintParamsPanel;
        private UIPanel uiRenderParamsPanel;

        private UISlider uiBrushSizeSlider;
        private UISlider uiDensitySlider;
        private UISlider uiWidthMultiplierSlider;
        private UISlider uiHeightMultiplierSlider;
        private UISlider uiHueShiftSlider;
        private UISlider uiSaturationShiftSlider;
        private UISlider uiValueShiftSlider;

        private UISlider uiGrassWidthSlider;
        private UISlider uiGrassHeightSlider;
        private UISlider uiGrassBendingSlider;
        private UIButton uiTopColorButton;
        private UIButton uiBottomColorButton;

        public UIDynamicList grassList;
        private GameObject grassItemPrefab;

        #endregion

        // 
        private Transform vrCamera;
        private Transform paletteController;
        private Transform toolControllerXf;

        private LineRenderer line;
        // TODO: ADD an object (from UIUtils) to symbolize the brush.

        // Stored Values
        private Vector3 hitPosGizmo;
        private Vector3 hitNormalGizmo;

        private Ray firstRay;
        private Vector3 lastPosition = Vector3.zero;

        // ----------------------------------------------------------

        private void OnEnable()
        {
            ResetPanel();
        }

        public void Start()
        {
            vrCamera = Utils.FindRootGameObject("Camera Rig").transform.Find("Pivot/VRCamera");
            paletteController = Utils.FindRootGameObject("Camera Rig").transform.Find("Pivot/PaletteController");

            line = GetComponent<LineRenderer>();

            GlobalState.ObjectAddedEvent.AddListener(OnGrassAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnGrassRemoved);
            SceneManager.clearSceneEvent.AddListener(OnClearScene);
            ToolsUIManager.Instance.onPaletteOpened.AddListener(OnPaletteOpened);

            if (null != grassList) { grassList.ItemClickedEvent += OnSelectGrassItem; }
            grassItemPrefab = Resources.Load<GameObject>("Prefabs/UI/GrassItem");
        }

        public bool IsInGUI { set { if (line != null) line.enabled = !value; } }

        public void SetPanel(Transform subPanel)
        {
            Transform P = subPanel;

            #region UI Setup

            uiNewButton = P.Find("CreateNewButton").GetComponent<UIButton>();
            uiNewButton.onReleaseEvent.AddListener(() => OnGrassCreateNewPressed());

            uiPickButton = P.Find("PickExistingButton").GetComponent<UIButton>();
            uiPickButton.onReleaseEvent.AddListener(() => OnGrassPickExistingPressed());

            uiActiveButton = P.Find("ActivePaintButton").GetComponent<UIButton>();
            uiActiveButton.onReleaseEvent.AddListener(() => OnStopPaintPressed());

            uiAddButton = P.Find("ModeAddButton").GetComponent<UIButton>(); // <---- Add is default.
            uiAddButton.onReleaseEvent.AddListener(() => OnGrassAddPressed());
            uiAddButton.Checked = true;

            uiRemoveButton = P.Find("ModeRemoveButton").GetComponent<UIButton>();
            uiRemoveButton.onReleaseEvent.AddListener(() => OnGrassRemovePressed());

            uiEditButton = P.Find("ModeEditButton").GetComponent<UIButton>();
            uiEditButton.onReleaseEvent.AddListener(() => OnGrassEditPressed());

            uiPaintParamsButton = P.Find("PaintParametersPanelButton").GetComponent<UIButton>(); // <---- Paint is default.
            uiPaintParamsButton.onReleaseEvent.AddListener(() => OnPaintParamsPressed());
            uiPaintParamsButton.Checked = true;

            uiRenderParamsButton = P.Find("RenderParametersPanelButton").GetComponent<UIButton>();
            uiRenderParamsButton.onReleaseEvent.AddListener(() => OnRenderParamsPressed());

            uiPaintParamsPanel = P.Find("PaintParametersPanel").GetComponent<UIPanel>(); // <---- PaintParams is default.
            uiPaintParamsPanel.gameObject.SetActive(true);

            uiRenderParamsPanel = P.Find("RenderParametersPanel").GetComponent<UIPanel>();
            uiRenderParamsPanel.gameObject.SetActive(false);

            // PAINT PARAMS
            P = uiPaintParamsPanel.transform;

            uiBrushSizeSlider = P.Find("GrassBrushSize").GetComponent<UISlider>();
            uiBrushSizeSlider.onSlideEvent.AddListener((float value) => OnBrushSizeChanged(value));

            uiDensitySlider = P.Find("GrassDensity").GetComponent<UISlider>();
            uiDensitySlider.onSlideEventInt.AddListener((int value) => OnDensityChanged(value));

            uiWidthMultiplierSlider = P.Find("WidthMultiplier").GetComponent<UISlider>();
            uiWidthMultiplierSlider.onSlideEvent.AddListener((float value) => OnWidthMultiplierChanged(value));

            uiHeightMultiplierSlider = P.Find("HeightMultiplier").GetComponent<UISlider>();
            uiHeightMultiplierSlider.onSlideEvent.AddListener((float value) => OnHeightMultiplierChanged(value));

            uiHueShiftSlider = P.Find("HueShift").GetComponent<UISlider>();
            uiHueShiftSlider.onSlideEvent.AddListener((float value) => OnHueShiftChanged(value));

            uiSaturationShiftSlider = P.Find("SaturationShift").GetComponent<UISlider>();
            uiSaturationShiftSlider.onSlideEvent.AddListener((float value) => OnSaturationShiftChanged(value));

            uiValueShiftSlider = P.Find("ValueShift").GetComponent<UISlider>();
            uiValueShiftSlider.onSlideEvent.AddListener((float value) => OnValueShiftChanged(value));

            // RENDER PARAMS
            P = uiRenderParamsPanel.transform;

            uiGrassWidthSlider = P.Find("GrassWidth").GetComponent<UISlider>();
            uiGrassWidthSlider.onSlideEvent.AddListener((float value) => OnGrassWidthChanged(value));

            uiGrassHeightSlider = P.Find("GrassHeight").GetComponent<UISlider>();
            uiGrassHeightSlider.onSlideEvent.AddListener((float value) => OnGrassHeightChanged(value));

            uiGrassBendingSlider = P.Find("GrassBending").GetComponent<UISlider>();
            uiGrassBendingSlider.onSlideEvent.AddListener((float value) => OnGrassBendingChanged(value));

            uiTopColorButton = P.Find("TopColorButton").GetComponent<UIButton>();
            uiTopColorButton.onReleaseEvent.AddListener(() => OnTopColorPressed());

            uiBottomColorButton = P.Find("BottomColorButton").GetComponent<UIButton>();
            uiBottomColorButton.onReleaseEvent.AddListener(() => OnBottomColorPressed());

            #endregion
        }

        void OnPaletteOpened()
        {
            grassList.NeedsRebuild = true;
        }

        private void OnClearScene()
        {
            grassList.Clear();
        }

        private void OnGrassAdded(GameObject gObject)
        {
            GrassController grassController = gObject.GetComponent<GrassController>();
            if (null == grassController)
                return;

            // Grass is added two times. Dont duplicate.
            bool grassFound = false;
            foreach(var it in grassList.GetItems())
            {
                GrassItem gIt = it.Content.GetComponentInChildren<GrassItem>();
                if (gIt != null)
                {
                    if (gIt.grassObject.name == gObject.name)
                    {
                        grassFound = true;
                    }
                }
            }

            if (!grassFound)
            {
                GameObject grassItemObject = Instantiate(grassItemPrefab);
                GrassItem grassItem = grassItemObject.GetComponentInChildren<GrassItem>();
                grassItem.SetGrassObject(gObject);
                Transform t = grassItem.transform;
                UIDynamicListItem item = grassList.AddItem(t);
                item.UseColliderForUI = true;
            }
            else
            {
                // TODO: Update image of the item, now that the painting is done.
            }
        }

        private void OnGrassRemoved(GameObject gObject)
        {
            GrassController grassController = gObject.GetComponent<GrassController>();
            if (null == grassController)
                return;
            foreach (var item in grassList.GetItems())
            {
                GrassItem grassItem = item.Content.GetComponent<GrassItem>();
                if (grassItem.grassObject == gObject)
                {
                    grassList.RemoveItem(item);
                    return;
                }
            }
        }

        public void OnSelectGrassItem(object sender, IndexedGameObjectArgs args)
        {
            GameObject item = args.gobject;
            GrassItem grassItem = item.GetComponent<GrassItem>();

            CommandGroup command = new CommandGroup("Select Grass");
            try
            {
                SelectorBase.ClearSelection();
                SelectorBase.AddToSelection(grassItem.grassObject);
            }
            finally
            {
                command.Submit();
            }
        }

        public void UpdateControllerInfo(Transform controllerXf, Transform mouthpieceXf)
        {
            toolControllerXf = controllerXf;

            Vector3 direction = transform.forward;
            Vector3 startRay = mouthpieceXf.position + mouthpieceXf.lossyScale.x * direction;

            line.SetPosition(0, startRay);
            line.SetPosition(1, startRay + 1.0f * direction);

            // First raycast to update the LineRenderer/Gizmo, done every frame even if not painting.
            firstRay.origin = startRay;
            firstRay.direction = direction;
            RaycastHit hit;
            if (Physics.Raycast(firstRay, out hit, 100f, hitMask.value))
            {
                hitPosGizmo = hit.point; // store for gizmo display
                hitNormalGizmo = hit.normal;
                line.SetPosition(1, hitPosGizmo);
            }

            line.startWidth = 0.005f / GlobalState.WorldScale;
            line.endWidth = line.startWidth;

            // TODO: add a subobject with a gizmo, like the teleport tool.
        }

        public GrassController Create()
        {
            GameObject gobject = new GameObject();
            gobject.name = Utils.CreateUniqueName("Grass");
            gobject.transform.localPosition = Vector3.zero;
            gobject.transform.localRotation = Quaternion.identity;
            gobject.transform.localScale = Vector3.one;
            gobject.tag = "PhysicObject";

            GrassController controller = gobject.AddComponent<GrassController>();
            controller.cameraXf = vrCamera;
            controller.interactorXf = paletteController;
            controller.material = Resources.Load<Material>("Grass/Grass");
            controller.computeShader = Resources.Load<ComputeShader>("Grass/GrassComputeShader");
            controller.overrideMaterial = true;
            controller.castShadow = true;
            controller.Clear();
            controller.InitDebugData(); // DEBUG

            SceneManager.AddObject(gobject); // add to right handed and fire object added.

            return controller;
        }

        public void Paint(float pressure)
        {
            // TODO: use pressure to increase add-density or remove-radius???

            switch(grassAction)
            {
                case GrassAction.Add:
                    Add();
                    break;

                case GrassAction.Remove:
                    Remove();
                    break;

                case GrassAction.Edit:
                    Edit();
                    break;
            }
        }

        private void Add()
        {
            if (grass == null)
                return;

            // DEBUG
            //line.positionCount = 2 * density;
            
            // place based on density
            for (int k = 0; k < density; k++)
            {
                float worldBrushSize = brushSize * GlobalState.WorldScale;

                // Random on the brush disc
                Vector2 delta = worldBrushSize * Random.insideUnitCircle;

                // Create rays in a circle around the firstRay, in controller space.
                Ray ray = firstRay;
                Vector3 rayOriginDelta = Vector3.zero;
                if (k != 0) // except for the first point, at the center of the circle).
                {
                    rayOriginDelta.x = delta.x;
                    rayOriginDelta.y = delta.y;
                    ray.origin = 
                        toolControllerXf.TransformPoint(
                            toolControllerXf.InverseTransformPoint(firstRay.origin) + rayOriginDelta);
                }

                //line.SetPosition(2 * k + 0, ray.origin);
                //line.SetPosition(2 * k + 1, ray.origin + ray.direction);

                // if the ray hits something thats on the layer mask,
                // within the grass limit and within the y normal limit
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 200f, hitMask.value) &&
                    currentGrassAmount < grassLimit &&
                    hit.normal.y >= (2.0f * normalLimit - 1.0f))
                {
                    if ((paintMask.value & (1 << hit.transform.gameObject.layer)) > 0)
                    {
                        Vector3 hitPos = hit.point;
                        Vector3 hitNormal = hit.normal;
                        if (k != 0 || (Vector3.Distance(hit.point, lastPosition) > brushSize))
                        {
                            Vector3 newGrassPosition = grass.transform.InverseTransformPoint(hitPos); // to Local mesh position
                            Vector3 newGrassNormal = grass.transform.InverseTransformDirection(hit.normal);
                            Vector2 newGrassUV = new Vector2(widthMultiplier, heightMultiplier);
                            Color newGrassColor = adjustedColor;

                            // ADD POINT to the controller.
                            grass.AddPoint(newGrassPosition, newGrassNormal, newGrassUV, newGrassColor);

                            currentGrassAmount++;

                            if (rayOriginDelta == Vector3.zero)
                            {
                                lastPosition = hitPos;
                            }
                        }
                    }
                }
            }
        }

        private void Remove()
        {

        }

        private void Edit()
        {

        }

        private void StartPainting()
        {
            if (grass != null)
            {
                StopPainting(); // NOTE: sets grass to null
            }

            // Create the object holding a set of grass
            grass = Create();
            grass.transform.position = hitPosGizmo;

            // switch ON the record button
            uiActiveButton.Checked = true;
        }

        private void StopPainting()
        {
            if (grass == null)
            {
                return;
            }

            // Unparent from RIGHT_HANDED so that CommandAddGameObject can RE-ADD.
            grass.transform.SetParent(null, false);
            CommandAddGameObject command = new CommandAddGameObject(grass.gameObject);
            command.Submit();

            grass = null;

            uiActiveButton.Checked = false; // may be useless, StopPainting come from clicking on that button.
        }

        public void BeginPaint()
        {
            // new CommandModifyGrass(grass)
        }

        public void EndPaint()
        {
            // command.Submit();
        }

        private void InitFromSelection()
        {
            GameObject selected = null;
            foreach (GameObject o in Selection.SelectedObjects)
            {
                selected = o;
                break;
            }
            if (null != selected)
            {
                GrassController controller = selected.GetComponent<GrassController>();
                if (null != controller)
                {
                    grass = controller;

                    // DEBUG - rebuild debug arrays and mesh from controller
                    // ...

                    return;
                }
            }

            grass = null;
        }

        #region UI Callbacks

        public void SetDefaultUIState()
        {
            uiAddButton.Checked = true; // <--- ADD default
            uiRemoveButton.Checked = false;
            uiEditButton.Checked = false;

            uiPaintParamsButton.Checked = true; // <--- PAINT PARAMS default
            uiRenderParamsButton.Checked = false;

            uiPaintParamsPanel.gameObject.SetActive(true); // <--- PAINT PARAMS panel default
            uiRenderParamsPanel.gameObject.SetActive(false);
        }

        public void ResetPanel()
        {
            uiPickButton.Checked = false; // no active object picking
            uiActiveButton.Checked = false; // no active paint

            SetDefaultUIState();
        }

        public void OnGrassCreateNewPressed()
        {
            StartPainting();

            // TODO: cancel picking.
            uiPickButton.Checked = false;

            SetDefaultUIState();
        }

        public void OnGrassPickExistingPressed()
        {
            StopPainting();
            
            // TODO: Picking!!

            uiPickButton.Checked = true;

            SetDefaultUIState();

            //InitFromSelection();
        }

        public void OnStopPaintPressed()
        {
            StopPainting();

            uiPickButton.Checked = false;
            uiActiveButton.Checked = false;

            SetDefaultUIState();
        }

        public void OnGrassAddPressed()
        {
            grassAction = GrassAction.Add;

            uiAddButton.Checked = true;
            uiRemoveButton.Checked = false;
            uiEditButton.Checked = false;
        }

        public void OnGrassRemovePressed()
        {
            grassAction = GrassAction.Remove;

            uiAddButton.Checked = false;
            uiRemoveButton.Checked = true;
            uiEditButton.Checked = false;
        }

        public void OnGrassEditPressed()
        {
            grassAction = GrassAction.Edit;

            uiAddButton.Checked = false;
            uiRemoveButton.Checked = false;
            uiEditButton.Checked = true;
        }

        public void OnPaintParamsPressed()
        {
            uiPaintParamsButton.Checked = true;
            uiRenderParamsButton.Checked = false;

            uiPaintParamsPanel.gameObject.SetActive(true);
            uiRenderParamsPanel.gameObject.SetActive(false);
        }

        public void OnRenderParamsPressed()
        {
            uiPaintParamsButton.Checked = false;
            uiRenderParamsButton.Checked = true;

            uiPaintParamsPanel.gameObject.SetActive(false);
            uiRenderParamsPanel.gameObject.SetActive(true);
        }

        // Called by the generic Paint tool, when touching the vertical joystick
        public void SetBrushSize(float s)
        {
            brushSize = s;
        }

        public void OnBrushSizeChanged(float value)
        {
            brushSize = value;
        }

        public void OnDensityChanged(int value)
        {
            density = value;
        }

        public void OnWidthMultiplierChanged(float value)
        {
            widthMultiplier = value;
        }

        public void OnHeightMultiplierChanged(float value)
        {
            heightMultiplier = value;
        }

        public void OnHueShiftChanged(float value)
        {
            adjustedColor.r = 0.5f * (value + 1); // -1..1 to 0..1
        }

        public void OnSaturationShiftChanged(float value)
        {
            adjustedColor.g = 0.5f * (value + 1); // -1..1 to 0..1
        }

        public void OnValueShiftChanged(float value)
        {
            adjustedColor.b = 0.5f * (value + 1); // -1..1 to 0..1
        }

        public void OnGrassWidthChanged(float value)
        {
            if (grass != null)
            {
                grass.grassWidth = value;
            }
        }

        public void OnGrassHeightChanged(float value)
        {
            if (grass != null)
            {
                grass.grassHeight = value;
            }
        }

        public void OnGrassBendingChanged(float value)
        {
            if (grass != null)
            {
                grass.bladeForwardAmount = value; // TODO: rename grassBending
            }
        }

        public void OnTopColorPressed()
        {
            Debug.Log($"GRASS OnTopColorPressed");
            // TODO
        }

        public void OnBottomColorPressed()
        {
            Debug.Log($"GRASS OnBottomColorPressed");
            // TODO
        }

        #endregion
    }
}
