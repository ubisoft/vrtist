using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [RequireComponent(typeof(MeshFilter)), // DEBUG
     RequireComponent(typeof(MeshRenderer)), // DEBUG
     RequireComponent(typeof(LineRenderer))]
    public class PaintGrassTool : MonoBehaviour
    {
        enum GrassEditionMode { CreateNew, EditExisting };
        GrassEditionMode grassEditionMode = GrassEditionMode.CreateNew;

        enum GrassAction { Add, Remove, Edit };
        GrassAction grassAction = GrassAction.Add;

        GameObject grass;
        float grassBrushSize = 1.0f;
        float grassDensity = 1.0f;

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

        private LineRenderer line;
        // ADD an object (from UIUtils) to symbolize the brush.

        // DEBUG visualization
        private Mesh DEBUG_mesh;
        private MeshFilter DEBUG_filter;
        private MeshRenderer DEBUG_render;
        private List<Vector3> DEBUG_positions = new List<Vector3>();
        private List<Color> DEBUG_colors = new List<Color>();
        private List<int> DEBUG_indices = new List<int>();
        private List<Vector3> DEBUG_normals = new List<Vector3>();



        // ----------

        List<Vector2> grassSizeMultipliers = new List<Vector2>();

        // Grass Limit
        public int grassLimit = 100000;
        public int currentGrassAmount = 0;

        // Paint Status
        public bool painting;
        public bool removing;
        public bool editing;
        public int toolbarInt = 0;

        // Brush Settings
        public LayerMask hitMask = 1;
        public LayerMask paintMask = 1;
        //public float brushSize = 1f;
        //public float density = 2f;
        [Range(0.0f, 1.0f)]
        public float normalLimit = 0.5f;

        // Grass Size
        public float widthMultiplier = 1f;
        public float heightMultiplier = 1f;

        // Color
        public Color adjustedColor = Color.white;
        public float rangeR, rangeG, rangeB;

        // Stored Values
        public Vector3 hitPosGizmo;
        public Vector3 hitNormal;

        private Vector3 lastPosition = Vector3.zero;

        int[] indi;

        // ----------

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

        public void SetBrushSize(float s)
        {
            Debug.Log("GRASS set brush size: " + s);

            grassBrushSize = s;
        }

        public void SetRay(Vector3 startRay, Vector3 dir)
        {
            line.SetPosition(0, startRay);
            line.SetPosition(1, startRay + 1.0f * dir);
        
            Ray ray = new Ray(startRay, dir);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, hitMask.value))
            {
                hitPosGizmo = hit.point;
                line.SetPosition(1, hitPosGizmo);
            }

            line.startWidth = 0.005f / GlobalState.WorldScale;
            line.endWidth = line.startWidth;
        }

        public GameObject Create()
        {
            Debug.Log("GRASS CREATE");

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

            gobject.AddComponent<BoxCollider>(); // LOL, how are we going to handle this??? A BoxCollider?
            GrassController controller = gobject.AddComponent<GrassController>();
            controller.cameraXf = vrCamera;
            controller.interactorXf = paletteController;
            // material
            // compute shader


            // DEBUG - add a mesh/filter/renderer to debug the pointcloud creation
            DEBUG_mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            DEBUG_filter = GetComponent<MeshFilter>();
            DEBUG_filter.mesh = DEBUG_mesh;
            DEBUG_render = GetComponent<MeshRenderer>();
            Material debugMaterial = ResourceManager.GetMaterial(MaterialID.ObjectOpaque);
            DEBUG_render.sharedMaterial = debugMaterial;
            DEBUG_render.material.SetColor("_BaseColor", Color.red);
            DEBUG_render.material.SetFloat("_Opacity", 1.0f);

            return gobject;
        }

        public void AddPoint(float pressure)
        {
            Debug.Log("GRASS ADD POINT");









            //MeshFilter meshFilter = currentVolume.GetComponent<MeshFilter>();
            //Mesh mesh = meshFilter.mesh;
            //mesh.Clear();
            //mesh.vertices = volumeGenerator.vertices;
            //mesh.triangles = volumeGenerator.triangles;
            //mesh.RecalculateNormals();

            //// Recompute collider
            //MeshCollider meshCollider = currentVolume.GetComponent<MeshCollider>();
            //meshCollider.sharedMesh = mesh;
            //// force update
            //meshCollider.enabled = false;
            //meshCollider.enabled = true;
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

                GameObject grassObject = SceneManager.InstantiateUnityPrefab(grass);
                GameObject.Destroy(grass.transform.parent.gameObject); // what are we doing here??
                grass = null;

                CommandAddGameObject command = new CommandAddGameObject(grassObject);
                command.Submit();
                GameObject grassInstance = command.newObject;

                GrassController controller = grassInstance.GetComponent<GrassController>();
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
            Debug.Log("GRASS On BrushSize changed");

            grassBrushSize = value;
        }

        public void OnGrassDensityChanged(float value)
        {
            Debug.Log("GRASS On Density changed");

            grassDensity = value;
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
