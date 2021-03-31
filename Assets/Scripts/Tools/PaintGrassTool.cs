using UnityEngine;

namespace VRtist
{
    [RequireComponent(typeof(LineRenderer))]
    public class PaintGrassTool : MonoBehaviour
    {
        enum GrassEditionMode { CreateNew, EditExisting };
        GrassEditionMode grassEditionMode = GrassEditionMode.CreateNew;

        enum GrassAction { Add, Remove, Edit };
        GrassAction grassAction = GrassAction.Add;

        // Grass Limit
        public int grassLimit = 100000;
        public int currentGrassAmount = 0;

        // Brush Settings
        public LayerMask hitMask = 1;
        public LayerMask paintMask = 1;
        public float brushSize = 0.1f; // 10cm
        public float density = 5f;
        [Range(0.0f, 1.0f)]
        public float normalLimit = 0.5f;

        // Grass Size
        public float widthMultiplier = 1f; // TODO: expose
        public float heightMultiplier = 1f; // TODO: expose

        // Color
        public Color adjustedColor = Color.white;
        public float rangeR, rangeG, rangeB;

        // ----------------------------------------------------------

        // Current Grass object being worked on.
        private GameObject grass;

        // UI
        private Transform panel;
        private UIButton grassCreateNewButton;
        private UIButton grassEditExistingButton;
        private UIButton grassAddButton;
        private UIButton grassRemoveButton;
        private UIButton grassEditButton;
        private UISlider grassBrushSizeSlider;
        private UISlider grassDensitySlider;

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
        }

        public bool IsInGUI { set { if (line != null) line.enabled = !value; } }

        public void SetPanel(Transform subPanel)
        {
            panel = subPanel;

            grassCreateNewButton = panel.Find("CreateNewButton").GetComponent<UIButton>(); // <---- CreateNew is default.
            grassCreateNewButton.Checked = true;
            grassEditExistingButton = panel.Find("EditExistingButton").GetComponent<UIButton>();

            grassAddButton = panel.Find("ModeAddButton").GetComponent<UIButton>(); // <---- Add is default.
            grassAddButton.Checked = true;
            grassRemoveButton = panel.Find("ModeRemoveButton").GetComponent<UIButton>();
            grassEditButton = panel.Find("ModeEditButton").GetComponent<UIButton>();

            grassBrushSizeSlider = panel.Find("GrassBrushSize").GetComponent<UISlider>();
            grassDensitySlider = panel.Find("GrassDensity").GetComponent<UISlider>();

            grassCreateNewButton.onReleaseEvent.AddListener(() => OnGrassCreateNewPressed());
            grassEditExistingButton.onReleaseEvent.AddListener(() => OnGrassEditExistingPressed());

            grassAddButton.onReleaseEvent.AddListener(() => OnGrassAddPressed());
            grassRemoveButton.onReleaseEvent.AddListener(() => OnGrassRemovePressed());
            grassEditButton.onReleaseEvent.AddListener(() => OnGrassEditPressed());

            grassBrushSizeSlider.onSlideEvent.AddListener((float value) => OnGrassBrushSizeChanged(value));
            grassDensitySlider.onSlideEvent.AddListener((float value) => OnGrassDensityChanged(value));
        }

        // Called by the generic Paint tool, when touching the vertical joystick
        public void SetBrushSize(float s)
        {
            Debug.Log("GRASS set brush size: " + s);
            brushSize = s;
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

        public GameObject Create()
        {
            GameObject rootObject = new GameObject();
            rootObject.transform.parent = SceneManager.RightHanded;
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            GameObject gobject = new GameObject();
            gobject.transform.parent = rootObject.transform;
            gobject.name = Utils.CreateUniqueName("Grass");

            gobject.transform.localPosition = Vector3.zero;
            gobject.transform.localRotation = Quaternion.identity;
            gobject.transform.localScale = Vector3.one;
            gobject.tag = "PhysicObject";

            //gobject.AddComponent<BoxCollider>(); // LOL, how are we going to handle this??? A BoxCollider?

            GrassController controller = gobject.AddComponent<GrassController>();
            controller.cameraXf = vrCamera;
            controller.interactorXf = paletteController;
            controller.material = Resources.Load<Material>("Grass/Grass");
            controller.computeShader = Resources.Load<ComputeShader>("Grass/GrassComputeShader");
            controller.overrideMaterial = true;
            controller.castShadow = true;
            controller.Clear();
            // DEBUG
            controller.InitDebugData();

            return gobject;
        }

        public void Paint(float pressure)
        {
            Debug.Log("GRASS Paint");

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

            GrassController controller = grass.GetComponent<GrassController>(); // TODO: not for every point, store it.
            if (controller == null)
                return;

            // place based on density
            for (int k = 0; k < density; k++)
            {
                // brush range
                float t = 2f * Mathf.PI * Random.Range(0f, brushSize);
                float u = Random.Range(0f, brushSize) + Random.Range(0f, brushSize);
                float r = (u > 1 ? 2 - u : u);

                // Create rays in a circle around the firstRay, in controller space.
                Ray ray = firstRay;
                Vector3 rayOriginDelta = Vector3.zero;
                if (k != 0) // except for the first point, at the center of the circle).
                {
                    rayOriginDelta.x += r * Mathf.Cos(t);
                    rayOriginDelta.y += r * Mathf.Sin(t);
                    ray.origin = toolControllerXf.TransformPoint(toolControllerXf.InverseTransformPoint(firstRay.origin) + rayOriginDelta);
                }

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
                            Color newGrassColor = new Color( // add random color variations
                                adjustedColor.r + (Random.Range(0, 1.0f) * rangeR),
                                adjustedColor.g + (Random.Range(0, 1.0f) * rangeG),
                                adjustedColor.b + (Random.Range(0, 1.0f) * rangeB), 1);

                            // ADD POINT to the controller.
                            controller.AddPoint(newGrassPosition, newGrassNormal, newGrassUV, newGrassColor);

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

        public void BeginPaint()
        {
            Debug.Log("GRASS BEGIN");

            if (grassEditionMode == GrassEditionMode.CreateNew)
            {
                grass = Create();
                grass.transform.position = hitPosGizmo;
            }
            else // EDIT
            {
                // TODO: ????
            }
        }

        public void EndPaint()
        {
            Debug.Log("GRASS END");

            if (grassEditionMode == GrassEditionMode.CreateNew)
            {
                // END happens even if no BEGIN. Find out why.
                if (grass == null)
                    return;

                //GameObject grassObject = SceneManager.InstantiateUnityPrefab(grass);
                //GameObject.Destroy(grass.transform.parent.gameObject); // what are we doing here??
                grass = null;

                //CommandAddGameObject command = new CommandAddGameObject(grassObject);
                //command.Submit();
                //GameObject grassInstance = command.newObject;

                //GrassController controller = grassInstance.GetComponent<GrassController>();
                // Store stuff in controller (stuff probably known only by the painter class at this point).
                // e.g.:
                //    controller.origin = volumeGenerator.origin;
                //    controller.bounds = volumeGenerator.bounds;
                //    controller.field = volumeGenerator.field;
                //    controller.resolution = volumeGenerator.resolution;
                //    controller.stepSize = volumeGenerator.stepSize;
            }
            else // EDIT
            { 
                // TODO: ????
            }
        }

        public void ResetPanel()
        {
            Debug.Log("GRASS Reset Panel");

            grassCreateNewButton.Checked = true;
            grassEditExistingButton.Checked = false;

            grassAddButton.Checked = true;
            grassRemoveButton.Checked = false;
            grassEditButton.Checked = false;
        }

        public void OnGrassCreateNewPressed()
        {
            Debug.Log("GRASS On Create New");

            grassCreateNewButton.Checked = true;
            grassEditExistingButton.Checked = false;

            grassAddButton.Checked = true;
            grassRemoveButton.Checked = false;
            grassEditButton.Checked = false;
        }

        public void OnGrassEditExistingPressed()
        {
            Debug.Log("GRASS On Edit Existing");

            grassCreateNewButton.Checked = false;
            grassEditExistingButton.Checked = true;

            grassAddButton.Checked = true;
            grassRemoveButton.Checked = false;
            grassEditButton.Checked = false;

            InitFromSelection();
        }

        public void OnGrassAddPressed()
        {
            Debug.Log("GRASS On Add");

            grassAction = GrassAction.Add;

            grassAddButton.Checked = true;
            grassRemoveButton.Checked = false;
            grassEditButton.Checked = false;
        }

        public void OnGrassRemovePressed()
        {
            Debug.Log("GRASS On Remove");

            grassAction = GrassAction.Remove;

            grassAddButton.Checked = false;
            grassRemoveButton.Checked = true;
            grassEditButton.Checked = false;
        }

        public void OnGrassEditPressed()
        {
            Debug.Log("GRASS On Edit");

            grassAction = GrassAction.Edit;

            grassAddButton.Checked = false;
            grassRemoveButton.Checked = false;
            grassEditButton.Checked = true;
        }

        public void OnGrassBrushSizeChanged(float value)
        {
            Debug.Log($"GRASS On BrushSize changed: {value}");
            brushSize = value;
        }

        public void OnGrassDensityChanged(float value)
        {
            Debug.Log($"GRASS OnDensityChanged: {value}");
            density = value;
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
                    grass = selected;

                    // DEBUG - rebuild debug arrays and mesh from controller
                    // ...

                    return;
                }
            }

            grass = null;
        }
    }
}
