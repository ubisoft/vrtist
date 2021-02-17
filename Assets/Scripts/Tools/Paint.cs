using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Paint : ToolBase
    {
        [Header("Paint Parameters")]
        [SerializeField] private Transform paintContainer;
        [SerializeField] private Material paintMaterial;
        [SerializeField] private NavigationOptions navigation;

        Transform tubePanel;
        Transform ribbonPanel;
        Transform hullPanel;
        Transform volumePanel;

        UIButton tubeButton;
        UIButton ribbonButton;
        UIButton hullButton;
        UIButton volumeButton;

        GameObject pencilCursor = null;
        GameObject flatCursor = null;
        GameObject convexCursor = null;
        GameObject volumeCursor = null;

        UIButton volumeCreateButton;
        UIButton volumeEditButton;

        // Paint tool
        Vector3 paintPrevPosition;
        GameObject currentPaint;
        float brushSize = 0.01f;
        enum PaintTools { Pencil = 0, FlatPencil, ConvexHull, Volume }
        PaintTools paintTool = PaintTools.Pencil;
        LineRenderer paintLineRenderer;
        int paintId = 0;
        bool paintOnSurface = false;

        FreeDraw freeDraw; // used for pencil, flat pencil and hull
        CommandGroup undoGroup = null;

        // VOLUME
        enum VolumeEditionMode { Create, Edit };
        VolumeEditionMode volumeEditionMode = VolumeEditionMode.Create;
        VolumeMeshGenerator volumeGenerator; // used for volume
        GameObject currentVolume;
        private float stepSize = 0.01f; // size in viewer's space
        private float strength = 0.5f;

        // Start is called before the first frame update
        void Start()
        {
            Init();

            paintTool = PaintTools.Pencil; // <---- Tube is default

            ConfigureSubPanels();

            ConfigureCursors();

            paintLineRenderer = transform.gameObject.GetComponent<LineRenderer>();
            if (paintLineRenderer == null) { Debug.LogWarning("Expected a line renderer on the paintItem game object."); }
            else { paintLineRenderer.startWidth = 0.005f; paintLineRenderer.endWidth = 0.005f; }

            freeDraw = new FreeDraw();
            volumeGenerator = new VolumeMeshGenerator();

            brushSize = mouthpiece.localScale.x;
            OnPaintColor(GlobalState.CurrentColor);

            SetTooltips();

            GlobalState.colorChangedEvent.AddListener(OnPaintColor);
        }

        public override void SetTooltips()
        {
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Trigger, Tooltips.Action.HoldPush, "Draw");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Secondary, Tooltips.Action.Push, "Switch Tool");
            Tooltips.SetText(VRDevice.PrimaryController, Tooltips.Location.Joystick, Tooltips.Action.HoldHorizontal, "Brush Size");
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Primary, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
        }

        private void ConfigureSubPanels()
        {
            tubePanel = panel.Find("PaintTubePanel");
            ribbonPanel = panel.Find("PaintRibbonPanel");
            hullPanel = panel.Find("PaintHullPanel");
            volumePanel = panel.Find("PaintVolumePanel");

            tubePanel.gameObject.SetActive(true); // <---- Tube is default
            ribbonPanel.gameObject.SetActive(false);
            hullPanel.gameObject.SetActive(false);
            volumePanel.gameObject.SetActive(false);

            tubeButton = panel.Find("PaintTubeButton").GetComponent<UIButton>();
            tubeButton.Checked = true; // <---- Tube is default
            ribbonButton = panel.Find("PaintRibbonButton").GetComponent<UIButton>();
            hullButton = panel.Find("PaintHullButton").GetComponent<UIButton>();
            volumeButton = panel.Find("PaintVolumeButton").GetComponent<UIButton>();

            tubeButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.Pencil));
            ribbonButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.FlatPencil));
            hullButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.ConvexHull));
            volumeButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.Volume));

            // Sub
            volumeCreateButton = volumePanel.Find("ModeCreateButton").GetComponent<UIButton>(); // <---- Create is default.
            volumeCreateButton.Checked = true;
            volumeEditButton = volumePanel.Find("ModeEditButton").GetComponent<UIButton>();

            volumeCreateButton.onReleaseEvent.AddListener(() => OnVolumeCreatePressed());
            volumeEditButton.onReleaseEvent.AddListener(() => OnVolumeEditPressed());
        }

        private void ConfigureCursors()
        {
            pencilCursor = mouthpiece.transform.Find("curve").gameObject;
            flatCursor = mouthpiece.transform.Find("flat_curve").gameObject;
            convexCursor = mouthpiece.transform.Find("convex").gameObject;
            volumeCursor = mouthpiece.transform.Find("volume").gameObject;

            pencilCursor.SetActive(paintTool == PaintTools.Pencil);
            flatCursor.SetActive(paintTool == PaintTools.FlatPencil);
            convexCursor.SetActive(paintTool == PaintTools.ConvexHull);
            volumeCursor.SetActive(paintTool == PaintTools.Volume);
        }

        protected override void OnDisable()
        {
            if (null != undoGroup)
            {
                undoGroup.Submit();
                undoGroup = null;
            }

            EndCurrentPaint();
            base.OnDisable();
        }

        void OnSelectPanel(PaintTools tool)
        {
            // If changing tool TO of FROM volume, reset the volume generator.
            if (paintTool != tool && (paintTool == PaintTools.Volume || tool == PaintTools.Volume))
                ResetVolume();

            paintTool = tool;

            // CHECKED button
            tubeButton.Checked = tool == PaintTools.Pencil;
            ribbonButton.Checked = tool == PaintTools.FlatPencil;
            hullButton.Checked = tool == PaintTools.ConvexHull;
            volumeButton.Checked = tool == PaintTools.Volume;

            // ACTIVE panel
            tubePanel.gameObject.SetActive(tool == PaintTools.Pencil);
            ribbonPanel.gameObject.SetActive(tool == PaintTools.FlatPencil);
            hullPanel.gameObject.SetActive(tool == PaintTools.ConvexHull);
            volumePanel.gameObject.SetActive(tool == PaintTools.Volume);

            // Mouthpiece
            pencilCursor.SetActive(tool == PaintTools.Pencil);
            flatCursor.SetActive(tool == PaintTools.FlatPencil);
            convexCursor.SetActive(tool == PaintTools.ConvexHull);
            volumeCursor.SetActive(tool == PaintTools.Volume);

            // Sub-Elements (put in its own function?)
            switch (tool)
            {
                case PaintTools.Volume:
                    {
                        volumeCreateButton.Checked = true;
                        volumeEditButton.Checked = false;
                        // TODO: default values for sliders?
                        // ...
                    }
                    break;

                default: break;
            }
        }

        protected override void DoUpdateGui()
        {
            base.DoUpdateGui();
            paintLineRenderer.enabled = false;
        }

        protected override void DoUpdate()
        {
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.primaryController, out position, out rotation);

            UpdateToolPaint(position, rotation);
        }

        private void TranslatePaintToItsCenter()
        {
            // determine center
            Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 max = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);
            foreach (Vector3 pos in freeDraw.controlPoints)
            {
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
                if (pos.z < min.z) min.z = pos.z;
                if (pos.x > max.x) max.x = pos.x;
                if (pos.y > max.y) max.y = pos.y;
                if (pos.z > max.z) max.z = pos.z;
            }
            Vector3 center = (max + min) * 0.5f;

            currentPaint.transform.localPosition += center;

            MeshFilter meshFilter = currentPaint.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            for (int i = 0; i < freeDraw.vertices.Length; i++)
            {
                freeDraw.vertices[i] -= center;
            }
            mesh.vertices = freeDraw.vertices;
            mesh.RecalculateBounds();
        }

        private void BeginPaint()
        {
            switch (paintTool)
            {
                case PaintTools.Pencil:
                case PaintTools.FlatPencil:
                case PaintTools.ConvexHull:
                    {
                        // Create an empty game object with a mesh
                        currentPaint = Utils.CreatePaint(GlobalState.CurrentColor);
                        ++paintId;
                        freeDraw = new FreeDraw();
                        freeDraw.matrix = currentPaint.transform.worldToLocalMatrix;
                    }
                    break;

                case PaintTools.Volume:
                    if (volumeEditionMode == VolumeEditionMode.Create)
                    {
                        currentVolume = Utils.CreateVolume(GlobalState.CurrentColor);
                        currentVolume.transform.position = mouthpiece.position; // real-world position
                        volumeGenerator.Reset();
                        volumeGenerator.stepSize = stepSize / GlobalState.WorldScale; // viewer scale -> world scale.
                        volumeGenerator.toLocalMatrix = currentVolume.transform.worldToLocalMatrix;
                    }
                    else // volumeEditionMode == VolumeEditionMode.Edit
                    {
                        // nothing to do I guess.
                    }
                    break;
            }
        }

        private void EndCurrentPaint()
        {
            switch (paintTool)
            {
                case PaintTools.Pencil:
                case PaintTools.FlatPencil:
                case PaintTools.ConvexHull:
                    {
                        // Bake line renderer into a mesh so we can raycast on it
                        if (currentPaint != null)
                        {
                            TranslatePaintToItsCenter();

                            GameObject paintObject = SceneManager.InstantiateUnityPrefab(currentPaint);
                            GameObject.Destroy(currentPaint.transform.parent.gameObject);
                            currentPaint = null;

                            GameObject paintInstance = SceneManager.AddObject(paintObject);
                            PaintController controller = paintInstance.GetComponent<PaintController>();
                            controller.color = GlobalState.CurrentColor;
                            controller.controlPoints = freeDraw.controlPoints;
                            controller.controlPointsRadius = freeDraw.controlPointsRadius;
                            new CommandAddGameObject(paintInstance).Submit();
                        }
                        break;
                    }

                case PaintTools.Volume:
                    {
                        if (currentVolume != null)
                        {
                            if (volumeEditionMode == VolumeEditionMode.Create)
                            {
                                GameObject volumeObject = SceneManager.InstantiateUnityPrefab(currentVolume);
                                GameObject.Destroy(currentVolume.transform.parent.gameObject);
                                currentVolume = null;

                                GameObject volumeInstance = SceneManager.AddObject(volumeObject);

                                VolumeController controller = volumeInstance.GetComponent<VolumeController>();
                                controller.color = GlobalState.CurrentColor;
                                controller.origin = volumeGenerator.origin;
                                controller.bounds = volumeGenerator.bounds;
                                controller.field = volumeGenerator.field;
                                controller.resolution = volumeGenerator.resolution;
                                controller.stepSize = volumeGenerator.stepSize;
                                new CommandAddGameObject(volumeInstance).Submit();
                                currentVolume = null;
                            }
                            else // EDIT
                            {
                                // TODO: send an update mesh command. Which is it???
                            }
                        }
                    }
                    break;
            }
        }

        private void UpdateToolPaint(Vector3 position, Quaternion rotation)
        {
            // ON TRIGGER
            VRInput.ButtonEvent(VRInput.primaryController, CommonUsages.trigger, () =>
            {
                BeginPaint();

                paintPrevPosition = Vector3.zero;
                undoGroup = new CommandGroup("Paint");
            },
            () =>
            {
                try
                {
                    EndCurrentPaint();
                }
                finally
                {
                    if (null != undoGroup)
                    {
                        undoGroup.Submit();
                        undoGroup = null;
                    }
                }

            });

            float triggerValue = VRInput.GetValue(VRInput.primaryController, CommonUsages.trigger);

            // Change brush size
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 val = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (val != Vector2.zero)
                {
                    if (val.y > 0.3f) { brushSize += 0.001f; }
                    if (val.y < -0.3f) { brushSize -= 0.001f; }
                    brushSize = Mathf.Clamp(brushSize, 0.001f, 0.5f);
                    mouthpiece.localScale = new Vector3(brushSize, brushSize, brushSize);
                }
            }

            paintLineRenderer.enabled = false;
            Vector3 penPosition = mouthpiece.position;
            if (paintOnSurface)
            {
                Vector3 direction = transform.forward; // (paintItem.position - centerEye.position).normalized;
                Vector3 startRay = penPosition + mouthpiece.lossyScale.x * direction;
                Vector3 endRay = startRay + 1000f * direction;
                paintLineRenderer.enabled = true;
                paintLineRenderer.positionCount = 2;
                paintLineRenderer.SetPosition(0, startRay);
                paintLineRenderer.SetPosition(1, endRay);
                paintLineRenderer.startWidth = 0.005f / GlobalState.WorldScale;
                paintLineRenderer.endWidth = paintLineRenderer.startWidth;
                RaycastHit hitInfo;
                bool hit = Physics.Raycast(startRay, direction, out hitInfo, Mathf.Infinity);
                if (!hit)
                    return;
                penPosition = hitInfo.point - 0.001f * direction;
                paintLineRenderer.SetPosition(1, penPosition);
            }
            else if (paintTool == PaintTools.Volume)
            {
                if (currentVolume)
                {
                    VolumeController controller = currentVolume.GetComponent<VolumeController>();
                    if (null != controller)
                    {
                        paintLineRenderer.enabled = true;

                        Vector3 C = controller.bounds.center;
                        Vector3 E = controller.bounds.extents;

                        Vector3 tlf = controller.transform.TransformPoint(C + new Vector3(-E.x, E.y, -E.z));
                        Vector3 trf = controller.transform.TransformPoint(C + new Vector3(E.x, E.y, -E.z));
                        Vector3 blf = controller.transform.TransformPoint(C + new Vector3(-E.x, -E.y, -E.z));
                        Vector3 brf = controller.transform.TransformPoint(C + new Vector3(E.x, -E.y, -E.z));
                        Vector3 tlb = controller.transform.TransformPoint(C + new Vector3(-E.x, E.y, E.z));
                        Vector3 trb = controller.transform.TransformPoint(C + new Vector3(E.x, E.y, E.z));
                        Vector3 blb = controller.transform.TransformPoint(C + new Vector3(-E.x, -E.y, E.z));
                        Vector3 brb = controller.transform.TransformPoint(C + new Vector3(E.x, -E.y, E.z));

                        paintLineRenderer.positionCount = 16;
                        paintLineRenderer.SetPositions(new Vector3[] {
                            blf, tlf, brf, trf, brb, trb, blb,
                            blf, brf, brb, blb,
                            tlb, tlf, trf, trb, tlb
                        });
                        paintLineRenderer.startWidth = 0.001f / GlobalState.WorldScale;
                        paintLineRenderer.endWidth = 0.001f / GlobalState.WorldScale;
                    }
                }
            }

            // Draw
            float deadZone = VRInput.deadZoneIn;
            if (triggerValue >= deadZone &&
                (
                  (position != paintPrevPosition && currentPaint != null) || currentVolume != null)
                )
            {
                // Add a point (the current world position) to the line renderer

                float pressure = (triggerValue - deadZone) / (1f - deadZone);
                float value = brushSize / GlobalState.WorldScale * pressure;

                switch (paintTool)
                {
                    case PaintTools.Pencil: freeDraw.AddControlPoint(penPosition, 0.5f * value); break;
                    case PaintTools.FlatPencil: freeDraw.AddFlatLineControlPoint(penPosition, -transform.forward, 0.5f * value); break;
                    case PaintTools.ConvexHull: freeDraw.AddConvexHullPoint(penPosition); break;
                    case PaintTools.Volume: volumeGenerator.AddPoint(penPosition, 2.0f * value * strength); break;
                }

                switch (paintTool)
                {
                    case PaintTools.Pencil:
                    case PaintTools.FlatPencil:
                    case PaintTools.ConvexHull:
                        {
                            // set mesh components
                            MeshFilter meshFilter = currentPaint.GetComponent<MeshFilter>();
                            Mesh mesh = meshFilter.mesh;
                            mesh.Clear();
                            mesh.vertices = freeDraw.vertices;
                            mesh.normals = freeDraw.normals;
                            mesh.triangles = freeDraw.triangles;
                            break;
                        }

                    case PaintTools.Volume:
                        if (null != currentVolume)
                        {
                            MeshFilter meshFilter = currentVolume.GetComponent<MeshFilter>();
                            Mesh mesh = meshFilter.mesh;
                            mesh.Clear();
                            mesh.vertices = volumeGenerator.vertices;
                            mesh.triangles = volumeGenerator.triangles;
                            mesh.RecalculateNormals();

                            // Recompute collider
                            MeshCollider meshCollider = currentVolume.GetComponent<MeshCollider>();
                            if (meshCollider.sharedMesh == null)
                            {
                                meshCollider.sharedMesh = mesh;
                            }
                            // force update
                            meshCollider.enabled = false;
                            meshCollider.enabled = true;

                            VolumeController controller = currentVolume.GetComponent<VolumeController>();
                            controller.origin = volumeGenerator.origin;
                            controller.bounds = volumeGenerator.bounds; // TODO: dont duplicate data? use volumeparameters in volumegenerator?
                            controller.field = volumeGenerator.field;
                            controller.resolution = volumeGenerator.resolution;
                            controller.stepSize = volumeGenerator.stepSize;
                            //controller.UpdateBoundsRenderer();
                        }
                        break;
                }
            }

            paintPrevPosition = position;
        }

        private void ResetVolume()
        {
            // TODO: put an end to any EDIT that could still be happening (send command).
            // ...

            volumeGenerator.Reset();
        }

        private void InitVolumeFromSelection()
        {
            GameObject selected = null;
            foreach (GameObject o in Selection.SelectedObjects)
            {
                selected = o;
                break;
            }
            if (null != selected)
            {
                VolumeController controller = selected.GetComponent<VolumeController>();
                if (null != controller)
                {
                    currentVolume = selected;
                    volumeGenerator.InitFromController(controller);

                    GlobalState.CurrentColor = controller.color;

                    return;
                }
            }

            ResetVolume();
            currentVolume = null;
        }

        public void OnPaintColor(Color color)
        {
            mouthpiece.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", color);
        }

        public void OnCheckPaintOnSurface(bool value)
        {
            paintOnSurface = value;
        }

        public void OnVolumeCreatePressed()
        {
            volumeEditionMode = VolumeEditionMode.Create;

            volumeCreateButton.Checked = true;
            volumeEditButton.Checked = false;

            ResetVolume();
        }

        public void OnVolumeEditPressed()
        {
            volumeEditionMode = VolumeEditionMode.Edit;

            volumeCreateButton.Checked = false;
            volumeEditButton.Checked = true;

            InitVolumeFromSelection();
        }

        public void OnVolumeStrengthChanged(float value)
        {
            strength = value;
        }

        public void OnVolumeCellSizeChanged(float value)
        {
            stepSize = value / 1000.0f; // millimeters to meters
        }
    }
}
