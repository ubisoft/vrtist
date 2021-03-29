/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
        Transform grassPanel;
        
        UIButton tubeButton;
        UIButton ribbonButton;
        UIButton hullButton;
        UIButton volumeButton;
        UIButton grassButton;

        GameObject pencilCursor = null;
        GameObject flatCursor = null;
        GameObject convexCursor = null;
        GameObject volumeCursor = null;
        GameObject grassCursor = null;

        UIButton volumeCreateButton;
        UIButton volumeEditButton;

        // Paint tool
        Vector3 paintPrevPosition;
        GameObject currentPaint;
        float brushSize = 0.01f;
        enum PaintTools { Pencil = 0, FlatPencil, ConvexHull, Volume, Grass }
        PaintTools paintTool = PaintTools.Pencil;
        LineRenderer paintLineRenderer;
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

        // GRASS
        PaintGrassTool grassPainter;

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

            grassPainter = transform.Find("GrassPainter").GetComponent<PaintGrassTool>();
            grassPainter.SetPanel(grassPanel);

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
            // TODOGRASS: tooltips for Grass.
        }

        private void ConfigureSubPanels()
        {
            tubePanel = panel.Find("PaintTubePanel");
            ribbonPanel = panel.Find("PaintRibbonPanel");
            hullPanel = panel.Find("PaintHullPanel");
            volumePanel = panel.Find("PaintVolumePanel");
            grassPanel = panel.Find("PaintGrassPanel");

            tubePanel.gameObject.SetActive(true); // <---- Tube is default
            ribbonPanel.gameObject.SetActive(false);
            hullPanel.gameObject.SetActive(false);
            volumePanel.gameObject.SetActive(false);
            grassPanel.gameObject.SetActive(false);

            tubeButton = panel.Find("PaintTubeButton").GetComponent<UIButton>();
            tubeButton.Checked = true; // <---- Tube is default
            ribbonButton = panel.Find("PaintRibbonButton").GetComponent<UIButton>();
            hullButton = panel.Find("PaintHullButton").GetComponent<UIButton>();
            volumeButton = panel.Find("PaintVolumeButton").GetComponent<UIButton>();
            grassButton = panel.Find("PaintGrassButton").GetComponent<UIButton>();

            tubeButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.Pencil));
            ribbonButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.FlatPencil));
            hullButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.ConvexHull));
            volumeButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.Volume));
            grassButton.onReleaseEvent.AddListener(() => OnSelectPanel(PaintTools.Grass));

            // Sub - Volume
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
            grassCursor = mouthpiece.transform.Find("grass").gameObject;

            pencilCursor.SetActive(paintTool == PaintTools.Pencil);
            flatCursor.SetActive(paintTool == PaintTools.FlatPencil);
            convexCursor.SetActive(paintTool == PaintTools.ConvexHull);
            volumeCursor.SetActive(paintTool == PaintTools.Volume);
            grassCursor.SetActive(paintTool == PaintTools.Grass);
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
            // If changing tool TO or FROM volume, reset the volume generator.
            if (paintTool != tool && (paintTool == PaintTools.Volume || tool == PaintTools.Volume))
                ResetVolume();

            paintTool = tool;

            // CHECKED button
            tubeButton.Checked = tool == PaintTools.Pencil;
            ribbonButton.Checked = tool == PaintTools.FlatPencil;
            hullButton.Checked = tool == PaintTools.ConvexHull;
            volumeButton.Checked = tool == PaintTools.Volume;
            grassButton.Checked = tool == PaintTools.Grass;

            // ACTIVE panel
            tubePanel.gameObject.SetActive(tool == PaintTools.Pencil);
            ribbonPanel.gameObject.SetActive(tool == PaintTools.FlatPencil);
            hullPanel.gameObject.SetActive(tool == PaintTools.ConvexHull);
            volumePanel.gameObject.SetActive(tool == PaintTools.Volume);
            grassPanel.gameObject.SetActive(tool == PaintTools.Grass);

            // Mouthpiece
            pencilCursor.SetActive(tool == PaintTools.Pencil);
            flatCursor.SetActive(tool == PaintTools.FlatPencil);
            convexCursor.SetActive(tool == PaintTools.ConvexHull);
            volumeCursor.SetActive(tool == PaintTools.Volume);
            grassCursor.SetActive(tool == PaintTools.Grass);

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

                case PaintTools.Grass: 
                    grassPainter.OnSelectPanel();
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
            VRInput.GetControllerTransform(VRInput.primaryController, out Vector3 position, out Quaternion rotation);
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
                        currentPaint = Create(PaintTools.Pencil, GlobalState.CurrentColor);
                        freeDraw = new FreeDraw
                        {
                            matrix = currentPaint.transform.worldToLocalMatrix
                        };
                    }
                    break;

                case PaintTools.Volume:
                    if (volumeEditionMode == VolumeEditionMode.Create)
                    {
                        currentVolume = Create(PaintTools.Volume, GlobalState.CurrentColor);
                        currentVolume.transform.position = mouthpiece.position; // real-world position
                        volumeGenerator.Reset();
                        volumeGenerator.stepSize = stepSize / GlobalState.WorldScale; // viewer scale -> world scale.
                        volumeGenerator.toLocalMatrix = currentVolume.transform.worldToLocalMatrix;
                    }
                    else // volumeEditionMode == VolumeEditionMode.Edit
                    {
                        // nothing to do I guess.
                        volumeGenerator.toLocalMatrix = currentVolume.transform.worldToLocalMatrix;
                    }
                    break;

                case PaintTools.Grass:
                    Vector3 penPosition = mouthpiece.position;
                    Vector3 direction = transform.forward;
                    Vector3 startRay = penPosition + mouthpiece.lossyScale.x * direction;
                    grassPainter.SetRay(startRay, direction); // raycasts and updates linerenderer
                    grassPainter.BeginPaint();
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

                            CommandAddGameObject command = new CommandAddGameObject(paintObject);
                            command.Submit();
                            GameObject paintInstance = command.newObject;

                            PaintController controller = paintInstance.GetComponent<PaintController>();
                            controller.color = GlobalState.CurrentColor;
                            controller.controlPoints = freeDraw.controlPoints;
                            controller.controlPointsRadius = freeDraw.controlPointsRadius;
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

                                CommandAddGameObject command = new CommandAddGameObject(volumeObject);
                                command.Submit();
                                GameObject volumeInstance = command.newObject;

                                VolumeController controller = volumeInstance.GetComponent<VolumeController>();
                                controller.color = GlobalState.CurrentColor;
                                controller.origin = volumeGenerator.origin;
                                controller.bounds = volumeGenerator.bounds;
                                controller.field = volumeGenerator.field;
                                controller.resolution = volumeGenerator.resolution;
                                controller.stepSize = volumeGenerator.stepSize;

                            }
                            else // EDIT
                            {
                                // TODO: send an update mesh command. Which is it???
                            }
                        }
                    }
                    break;

                case PaintTools.Grass:
                    grassPainter.EndPaint();
                    break;
            }
        }

        private static GameObject Create(PaintTools what, Color color)
        {
            if (what == PaintTools.Grass)
                return null;

            GameObject rootObject = new GameObject();
            rootObject.transform.parent = SceneManager.RightHanded;
            rootObject.transform.localPosition = Vector3.zero;
            rootObject.transform.localRotation = Quaternion.identity;
            rootObject.transform.localScale = Vector3.one;

            GameObject gobject = new GameObject();
            gobject.transform.parent = rootObject.transform;
            gobject.name = Utils.CreateUniqueName(
                  what == PaintTools.Volume ? "Volume" 
                : what == PaintTools.Grass ? "Grass" 
                : "Paint");

            gobject.transform.localPosition = Vector3.zero;
            gobject.transform.localRotation = Quaternion.identity;
            gobject.transform.localScale = Vector3.one;
            gobject.tag = "PhysicObject";

            Mesh mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            MeshFilter meshFilter = gobject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer renderer = gobject.AddComponent<MeshRenderer>();
            MaterialID materialId = color.a == 1f ? MaterialID.ObjectOpaque : MaterialID.ObjectTransparent;
            Material paintMaterial = ResourceManager.GetMaterial(materialId);
            renderer.sharedMaterial = paintMaterial;
            renderer.material.SetColor("_BaseColor", color);
            renderer.material.SetFloat("_Opacity", color.a);

            // Update scene data for live sync
            SceneManager.AddMaterialParameters(Utils.GetMaterialName(gobject), materialId, color);

            gobject.AddComponent<MeshCollider>();

            if (what == PaintTools.Volume)
            {
                gobject.AddComponent<VolumeController>();
            }
            else if (what == PaintTools.Grass)
            {
                //gobject.AddComponent<GrassController>();
            }
            else
            {
                gobject.AddComponent<PaintController>();
            }

            return gobject;
        }

        private void UpdateToolPaint(Vector3 position, Quaternion _)
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

            //
            // BRUSH SIZE
            //
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 val = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (val != Vector2.zero)
                {
                    if (val.y > 0.3f) { brushSize += 0.001f; }
                    if (val.y < -0.3f) { brushSize -= 0.001f; }
                    brushSize = Mathf.Clamp(brushSize, 0.001f, 0.5f);
                    mouthpiece.localScale = new Vector3(brushSize, brushSize, brushSize);

                    switch (paintTool)
                    {
                        case PaintTools.Grass: grassPainter.SetBrushSize(brushSize); break;
                        default: break;
                    }
                }
            }

            //
            // LINE RENDERER
            //
            paintLineRenderer.enabled = false;
            Vector3 penPosition = mouthpiece.position;
            switch(paintTool)
            {
                case PaintTools.Pencil:
                case PaintTools.FlatPencil:
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
                        bool hit = Physics.Raycast(startRay, direction, out RaycastHit hitInfo, Mathf.Infinity);
                        if (!hit)
                            return;
                        penPosition = hitInfo.point - 0.001f * direction;
                        paintLineRenderer.SetPosition(1, penPosition);
                    }
                    break;

                case PaintTools.ConvexHull: break;

                case PaintTools.Volume:
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
                    break;

                case PaintTools.Grass:
                    {
                        Vector3 direction = transform.forward;
                        Vector3 startRay = penPosition + mouthpiece.lossyScale.x * direction;
                        grassPainter.SetRay(startRay, direction); // raycasts and updates linerenderer
                    }
                    break;
            }

            //
            // DRAW (trigger PRESSED)
            //
            float deadZone = VRInput.deadZoneIn;
            if (triggerValue >= deadZone &&
                (
                  (position != paintPrevPosition && currentPaint != null) || currentVolume != null)
                )
            {
                float pressure = (triggerValue - deadZone) / (1f - deadZone);
                float value = brushSize / GlobalState.WorldScale * pressure;

                switch (paintTool)
                {
                    case PaintTools.Pencil: freeDraw.AddControlPoint(penPosition, 0.5f * value); break;
                    case PaintTools.FlatPencil: freeDraw.AddFlatLineControlPoint(penPosition, -transform.forward, 0.5f * value); break;
                    case PaintTools.ConvexHull: freeDraw.AddConvexHullPoint(penPosition); break;
                    case PaintTools.Volume: volumeGenerator.AddPoint(penPosition, 2.0f * value * strength); break;
                    case PaintTools.Grass: grassPainter.AddPoint(value); break;
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
                            meshCollider.sharedMesh = mesh;
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

                    case PaintTools.Grass:
                        // update to mesh is already done in PaintGrassTool.AddPoint()
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

        // VOLUME CALLBACKS

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
