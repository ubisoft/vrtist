using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{
    public class Paint : ToolBase
    {
        [Header("Paint Parameters")]
        [SerializeField] private Transform paintContainer;
        [SerializeField] private Transform paintBrush;
        [SerializeField] private Material paintMaterial;

        // Paint tool
        Vector3 paintPrevPosition;
        GameObject currentPaintLine;
        float brushSize = 0.01f;
        enum PaintTools { Pencil = 0, FlatPencil }
        PaintTools paintTool = PaintTools.Pencil;
        LineRenderer paintLineRenderer;
        int paintId = 0;
        bool paintOnSurface = false;

        FreeDraw freeDraw;

        // Start is called before the first frame update
        void Start()
        {
            Init();

            paintLineRenderer = transform.gameObject.GetComponent<LineRenderer>();
            if (paintLineRenderer == null) { Debug.LogWarning("Expected a line renderer on the paintItem game object."); }
            else { paintLineRenderer.startWidth = 0.005f; paintLineRenderer.endWidth = 0.005f; }

            freeDraw = new FreeDraw();
            updateButtonsColor();

            brushSize = paintBrush.localScale.x;
            OnPaintColor(GlobalState.CurrentColor);
            
            // Create tooltips
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Trigger, "Draw");
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Secondary, "Switch To Selection");
            Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Joystick, "Brush Size");

            GlobalState.colorChangedEvent.AddListener(OnPaintColor);
        }

        protected override void OnDisable()
        {
            EndCurrentPaint();
            base.OnDisable();
        }
        protected override void DoUpdate(Vector3 position, Quaternion rotation)
        {
            switch (paintTool)
            {
                case PaintTools.Pencil: UpdateToolPaintPencil(position, rotation, false); break;
                case PaintTools.FlatPencil: UpdateToolPaintPencil(position, rotation, true); break;
                //case PaintTools.Eraser: UpdateToolPaintEraser(position, rotation); break;
            }
        }

        protected override void ShowTool(bool show)
        {
            ActivateMouthpiece(paintBrush, show);

            if (rightController != null)
            {
                rightController.gameObject.transform.localScale = show ? Vector3.one : Vector3.zero;
            }
        }

        private void TranslatePaintToItsCenter()
        {
            // determine center
            PaintParameters paintParameters = currentPaintLine.GetComponent<PaintController>().GetParameters() as PaintParameters;
            Vector3 min = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            Vector3 max = new Vector3(-Mathf.Infinity, -Mathf.Infinity, -Mathf.Infinity);
            foreach(Vector3 pos in freeDraw.controlPoints)
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
            // Bake line renderer into a mesh so we can raycast on it
            if (currentPaintLine != null)
            {
                TranslatePaintToItsCenter();
                PaintParameters paintParameters = currentPaintLine.GetComponent<PaintController>().GetParameters() as PaintParameters;
                paintParameters.color = GlobalState.CurrentColor;
                paintParameters.controlPoints = freeDraw.controlPoints;
                paintParameters.controlPointsRadius = freeDraw.controlPointsRadius;
                new CommandAddGameObject(currentPaintLine).Submit();
                currentPaintLine = null;
            }

        }

        private void UpdateToolPaintPencil(Vector3 position, Quaternion rotation, bool flat)
        {
            // Activate a new paint            
            VRInput.ButtonEvent(VRInput.rightController, CommonUsages.trigger, () =>
            {
                // Create an empty game object with a mesh
                currentPaintLine = SyncData.InstantiatePrefab(Utils.CreatePaint(SyncData.prefab, GlobalState.CurrentColor));
                ++paintId;
                paintPrevPosition = Vector3.zero;
                freeDraw = new FreeDraw();
                freeDraw.matrix = currentPaintLine.transform.worldToLocalMatrix;

                /*
                freeDraw.matrix = Matrix4x4.identity;
                freeDraw.AddControlPoint(new Vector3(0, 0, 20), 1f);
                freeDraw.AddControlPoint(new Vector3(10, 0, 20), 1f);
                freeDraw.AddControlPoint(new Vector3(10, 10, 20), 1f);                
                freeDraw.AddControlPoint(new Vector3(20, 10, 20), 1f);              
                freeDraw.AddControlPoint(new Vector3(20, 0, 20), 1f);
                */
            },            
            () =>
            {
                EndCurrentPaint();
            });

            float triggerValue = VRInput.GetValue(VRInput.rightController, CommonUsages.trigger);

            // Change brush size
            if (GlobalState.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 val = VRInput.GetValue(VRInput.rightController, CommonUsages.primary2DAxis);
                if (val != Vector2.zero)
                {
                    if (val.y > 0.3f) { brushSize += 0.001f; }
                    if (val.y < -0.3f) { brushSize -= 0.001f; }
                    brushSize = Mathf.Clamp(brushSize, 0.001f, 0.5f);
                    paintBrush.localScale = new Vector3(brushSize, brushSize, brushSize);
                }
            }

            paintLineRenderer.enabled = false;
            Vector3 penPosition = paintBrush.position;
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
            if (triggerValue >= deadZone && position != paintPrevPosition && currentPaintLine != null)
            {
                // Add a point (the current world position) to the line renderer                        

                float ratio = 0.5f * (triggerValue - deadZone) / (1f - deadZone);

                // make some vibration feedback
                //OVRInput.SetControllerVibration(1, ratio, OVRInput.Controller.RTouch);
                //VRInput.rightController.SendHapticImpulse(0, ratio, 1f);                       

                if (flat)
                    freeDraw.AddFlatLineControlPoint(penPosition, -transform.forward, brushSize * ratio);
                else
                    freeDraw.AddControlPoint(penPosition, brushSize * ratio);
                // set mesh components
                MeshFilter meshFilter = currentPaintLine.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;
                mesh.Clear();
                mesh.vertices = freeDraw.vertices;
                mesh.normals = freeDraw.normals;
                mesh.triangles = freeDraw.triangles;
            }

            paintPrevPosition = position;
        }        

        public void OnPaintColor(Color color)
        {
            paintBrush.gameObject.GetComponent<MeshRenderer>().material.SetColor("_BaseColor", color);
        }

        public void OnCheckPaintOnSurface(bool value)
        {
            paintOnSurface = value;
        }

        void updateButtonsColor()
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
                }
            }
        }

        public void PaintSelectPencil()
        {
            paintTool = PaintTools.Pencil;
            updateButtonsColor();
        }

        public void PaintSelectFlatPencil()
        {
            paintTool = PaintTools.FlatPencil;
            updateButtonsColor();
        }

        // DEBUG

        public void GenerateRandomBrushStroke()
        {
            //
            // START
            //
            currentPaintLine = SyncData.InstantiatePrefab(Utils.CreatePaint(SyncData.prefab, GlobalState.CurrentColor));
            ++paintId;
            paintPrevPosition = Vector3.zero;
            freeDraw = new FreeDraw();
            //freeDraw.matrix = currentPaintLine.transform.worldToLocalMatrix;
            freeDraw.matrix = Matrix4x4.identity;

            //
            // SPIRAL
            //
            int nbSteps = 200;
            float radius = 1.0f;
            for (int i = 0; i < nbSteps; ++i)
            {
                float t = (float)i / (float)(nbSteps - 1);
                float growingRadius = (0.1f + radius * t * t);
                Vector3 pos = new Vector3(
                    growingRadius * Mathf.Cos(t * 5.0f * 2.0f * Mathf.PI),
                    growingRadius * Mathf.Sin(t * 5.0f * 2.0f * Mathf.PI),
                    t * 1.0f);
                freeDraw.AddControlPoint(pos, 0.01f);
            }
            

            // set mesh components
            MeshFilter meshFilter = currentPaintLine.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            mesh.Clear();
            mesh.vertices = freeDraw.vertices;
            mesh.normals = freeDraw.normals;
            mesh.triangles = freeDraw.triangles;

            //
            // END
            //
            if (currentPaintLine != null)
            {
                MeshCollider collider = currentPaintLine.AddComponent<MeshCollider>();
                PaintParameters paintParameters = currentPaintLine.GetComponent<PaintController>().GetParameters() as PaintParameters;
                paintParameters.color = GlobalState.CurrentColor;
                paintParameters.controlPoints = freeDraw.controlPoints;
                paintParameters.controlPointsRadius = freeDraw.controlPointsRadius;
                new CommandAddGameObject(currentPaintLine).Submit();
                currentPaintLine = null;
            }
        }
    }
}
