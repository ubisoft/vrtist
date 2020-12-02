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

        // Paint tool
        Vector3 paintPrevPosition;
        GameObject currentPaintLine;
        float brushSize = 0.01f;
        enum PaintTools { Pencil = 0, FlatPencil, ConvexHull, Volume }
        PaintTools paintTool = PaintTools.Pencil;
        LineRenderer paintLineRenderer;
        int paintId = 0;
        bool paintOnSurface = false;

        GameObject pencilCursor = null;
        GameObject flatCursor = null;
        GameObject convexCursor = null;
        GameObject volumeCursor = null;

        FreeDraw freeDraw; // used for pencil, flat pencil and hull
        CommandGroup undoGroup = null;




        VolumeMeshGenerator volumeGenerator; // used for volume
        GameObject currentVolume;

        // Start is called before the first frame update
        void Start()
        {
            Init();

            paintLineRenderer = transform.gameObject.GetComponent<LineRenderer>();
            if (paintLineRenderer == null) { Debug.LogWarning("Expected a line renderer on the paintItem game object."); }
            else { paintLineRenderer.startWidth = 0.005f; paintLineRenderer.endWidth = 0.005f; }

            freeDraw = new FreeDraw();
            UpdateButtonsColor();

            brushSize = mouthpiece.localScale.x;
            OnPaintColor(GlobalState.CurrentColor);

            pencilCursor = mouthpiece.transform.Find("curve").gameObject;
            flatCursor = mouthpiece.transform.Find("flat_curve").gameObject;
            convexCursor = mouthpiece.transform.Find("convex").gameObject;
            volumeCursor = mouthpiece.transform.Find("volume").gameObject;

            pencilCursor.SetActive(paintTool == PaintTools.Pencil);
            flatCursor.SetActive(paintTool == PaintTools.FlatPencil);
            convexCursor.SetActive(paintTool == PaintTools.ConvexHull);
            volumeCursor.SetActive(paintTool == PaintTools.Volume);

            // Create tooltips
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Trigger, "Draw");
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Secondary, "Switch To Selection");
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Joystick, "Brush Size");

            GlobalState.colorChangedEvent.AddListener(OnPaintColor);
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
        protected override void DoUpdate()
        {
            Vector3 position;
            Quaternion rotation;
            VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

            UpdateToolPaintPencil(position, rotation);
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

            currentPaintLine.transform.localPosition += center;

            MeshFilter meshFilter = currentPaintLine.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            for (int i = 0; i < freeDraw.vertices.Length; i++)
            {
                freeDraw.vertices[i] -= center;
            }
            mesh.vertices = freeDraw.vertices;
            mesh.RecalculateBounds();
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
                        if (currentPaintLine != null)
                        {
                            TranslatePaintToItsCenter();
                            PaintController controller = currentPaintLine.GetComponent<PaintController>();
                            controller.color = GlobalState.CurrentColor;
                            controller.controlPoints = freeDraw.controlPoints;
                            controller.controlPointsRadius = freeDraw.controlPointsRadius;
                            new CommandAddGameObject(currentPaintLine).Submit();
                            currentPaintLine = null;
                        }
                        break;
                    }

                case PaintTools.Volume:
                    {
                        if (currentVolume != null)
                        {
                            VolumeController controller = currentVolume.GetComponent<VolumeController>();
                            controller.origin = volumeGenerator.origin;
                            controller.bounds = volumeGenerator.bounds;
                            controller.field = volumeGenerator.field;
                            controller.resolution = volumeGenerator.resolution;
                            controller.stepSize = volumeGenerator.stepSize;
                            new CommandAddGameObject(currentVolume).Submit();
                            currentVolume = null;
                        }
                    }
                    break;
            }
        }

        private void UpdateToolPaintPencil(Vector3 position, Quaternion rotation)//, bool flat)
        {
            // Activate a new paint            
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
            {
                switch (paintTool)
                {
                    case PaintTools.Pencil:
                    case PaintTools.FlatPencil:
                    case PaintTools.ConvexHull:
                        // Create an empty game object with a mesh
                        currentPaintLine = SyncData.InstantiatePrefab(Utils.CreatePaint(SyncData.prefab, GlobalState.CurrentColor));
                        ++paintId;
                        freeDraw = new FreeDraw();
                        freeDraw.matrix = currentPaintLine.transform.worldToLocalMatrix;
                        break;

                    case PaintTools.Volume:
                        currentVolume = SyncData.InstantiatePrefab(Utils.CreateVolume(SyncData.prefab, GlobalState.CurrentColor));
                        currentVolume.transform.position = mouthpiece.position; // real-world position
                        volumeGenerator = new VolumeMeshGenerator(); // TODO: pass in the accuracy/stepSize
                        volumeGenerator.stepSize = 0.1f;
                        volumeGenerator.toLocalMatrix = currentVolume.transform.worldToLocalMatrix;
                        break;
                }

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

            float triggerValue = VRInput.GetValue(VRInput.rightController, CommonUsages.trigger);

            // Change brush size
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
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
                paintLineRenderer.enabled = true;
                paintLineRenderer.SetPosition(0, transform.position);
                paintLineRenderer.SetPosition(1, transform.position + transform.forward * 10f);
                RaycastHit hitInfo;
                Vector3 direction = transform.forward; // (paintItem.position - centerEye.position).normalized;
                bool hit = Physics.Raycast(transform.position, direction, out hitInfo, Mathf.Infinity);
                if (hit)
                {
                    penPosition = hitInfo.point - 0.001f * direction;
                    paintLineRenderer.SetPosition(1, penPosition);
                }
            }

            // Draw
            float deadZone = VRInput.deadZoneIn;
            if (triggerValue >= deadZone &&
                (
                  (position != paintPrevPosition && currentPaintLine != null) || currentVolume != null)
                )
            {
                // Add a point (the current world position) to the line renderer                        

                float ratio = 0.5f * (triggerValue - deadZone) / (1f - deadZone);

                switch (paintTool)
                {
                    case PaintTools.Pencil: freeDraw.AddControlPoint(penPosition, brushSize * ratio); break;
                    case PaintTools.FlatPencil: freeDraw.AddFlatLineControlPoint(penPosition, -transform.forward, brushSize * ratio); break;
                    case PaintTools.ConvexHull: freeDraw.AddConvexHullPoint(penPosition); break;
                    case PaintTools.Volume: volumeGenerator.AddPoint(penPosition, brushSize * ratio); break;
                }

                switch (paintTool)
                {
                    case PaintTools.Pencil:
                    case PaintTools.FlatPencil:
                    case PaintTools.ConvexHull:
                        {
                            // set mesh components
                            MeshFilter meshFilter = currentPaintLine.GetComponent<MeshFilter>();
                            Mesh mesh = meshFilter.mesh;
                            mesh.Clear();
                            mesh.vertices = freeDraw.vertices;
                            mesh.normals = freeDraw.normals;
                            mesh.triangles = freeDraw.triangles;
                            break;
                        }

                    case PaintTools.Volume:
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
                            break;
                        }
                }
            }

            paintPrevPosition = position;
        }

        public void OnPaintColor(Color color)
        {
            mouthpiece.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", color);
        }

        public void OnCheckPaintOnSurface(bool value)
        {
            paintOnSurface = value;
        }

        void UpdateButtonsColor()
        {
            if (!panel)
                return;

            for (int i = 0; i < panel.childCount; i++)
            {
                GameObject child = panel.GetChild(i).gameObject;
                UIButton button = child.GetComponent<UIButton>();
                if (button != null)
                {
                    button.Checked = false;

                    if (child.name == "PencilButton" && paintTool == PaintTools.Pencil)
                    {
                        button.Checked = true;
                    }
                    if (child.name == "FlatPencilButton" && paintTool == PaintTools.FlatPencil)
                    {
                        button.Checked = true;
                    }
                    if (child.name == "ConvexHullPencilButton" && paintTool == PaintTools.ConvexHull)
                    {
                        button.Checked = true;
                    }
                    if (child.name == "VolumePencilButton" && paintTool == PaintTools.Volume)
                    {
                        button.Checked = true;
                    }
                }
            }
        }

        public void PaintSelectPencil()
        {
            paintTool = PaintTools.Pencil;
            pencilCursor.SetActive(true);
            flatCursor.SetActive(false);
            convexCursor.SetActive(false);
            volumeCursor.SetActive(false);
            UpdateButtonsColor();
        }

        public void PaintSelectFlatPencil()
        {
            paintTool = PaintTools.FlatPencil;
            pencilCursor.SetActive(false);
            flatCursor.SetActive(true);
            convexCursor.SetActive(false);
            volumeCursor.SetActive(false);
            UpdateButtonsColor();
        }

        public void PaintSelectConvexHullPencil()
        {
            paintTool = PaintTools.ConvexHull;
            pencilCursor.SetActive(false);
            flatCursor.SetActive(false);
            convexCursor.SetActive(true);
            volumeCursor.SetActive(false);
            UpdateButtonsColor();
        }

        public void PaintSelectVolumePencil()
        {
            paintTool = PaintTools.Volume;
            pencilCursor.SetActive(false);
            flatCursor.SetActive(false);
            convexCursor.SetActive(false);
            volumeCursor.SetActive(true);
            UpdateButtonsColor();
        }
    }
}
