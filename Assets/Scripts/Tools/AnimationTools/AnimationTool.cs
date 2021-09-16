/* MIT License
 *
 * Université de Rennes 1 / Invictus Project
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

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace VRtist
{

    public class AnimationTool : ToolBase
    {
        [SerializeField] private NavigationOptions navigation;

        public Anim3DCurveManager CurveManager;
        public Material GhostMaterial;
        private GameObject ghost;
        private float deadzone = 0.3f;

        private float scaleIndice;

        private Transform AddKeyModeButton;
        private Transform ZoneModeButton;
        private Transform SegmentModeButton;
        private Transform TangentModeButton;
        private Transform ZoneSlider;
        private Transform CurveModeButton;
        private Transform PoseModeButton;
        private Transform FKModeButton;
        private Transform IKModeButton;
        private Transform ContSlider;
        private Transform SkeletonDisplay;
        private Transform OffsetLabel;

        private int zoneSize;
        private float tanCont;

        private LineRenderer lastLine;
        private Texture2D lastTexture;

        public Color DefaultColor;
        public Color ZoneColor;

        private CurveManipulation curveManip;
        private PoseManipulation poseManip;

        public AnimGhostManager ghostManager;

        public GoalGizmo goalGizmo;
        public RigGoalController selectedGoal;

        public enum EditMode { Curve, Pose }
        private EditMode editMode = EditMode.Pose;
        public EditMode Mode
        {
            get { return editMode; }
            set
            {
                GetModeButton(editMode).Checked = false;
                editMode = value;
                GetModeButton(editMode).Checked = true;
            }
        }

        public enum CurveEditMode { AddKeyframe, Zone, Segment, Tangents }
        private CurveEditMode curveMode;
        public CurveEditMode CurveMode
        {
            set
            {
                GetCurveModeButton(curveMode).Checked = false;
                curveMode = value;
                GetCurveModeButton(curveMode).Checked = true;
            }
            get { return curveMode; }
        }

        public enum PoseEditMode { FK, IK, AC }
        private PoseEditMode poseMode;


        public PoseEditMode PoseMode
        {
            get { return poseMode; }
            set
            {
                GetPoseModeButton(poseMode).Checked = false;
                poseMode = value;
                GetPoseModeButton(poseMode).Checked = true;
            }
        }

        private float offsetValue;
        public float Offsetvalue
        {
            get { return offsetValue; }
            set
            {
                offsetValue = value;
                GlobalState.Settings.curveForwardOffset = offsetValue;
                OffsetLabel.GetComponent<UILabel>().Text = "Offset : " + offsetValue;
            }
        }

        #region ButtonEvents

        public void AddOffset()
        {
            Offsetvalue += 0.5f;
        }
        public void RemoveOffset()
        {
            Offsetvalue -= 0.5f;
        }

        public void SetCurveMode()
        {
            Mode = EditMode.Curve;
        }
        public void SetPoseMode()
        {
            Mode = EditMode.Pose;
        }

        public void SetAddKeyMode()
        {
            CurveMode = CurveEditMode.AddKeyframe;
        }
        public void SetZoneMode()
        {
            CurveMode = CurveEditMode.Zone;
        }

        public void SetSegmentMode()
        {
            CurveMode = CurveEditMode.Segment;
        }

        public void SetTangentMode()
        {
            CurveMode = CurveEditMode.Tangents;
        }

        public void SetFKMode()
        {
            PoseMode = PoseEditMode.FK;
        }

        public void SetIKMode()
        {
            PoseMode = PoseEditMode.IK;
        }

        public void SetZoneSize(float value)
        {
            zoneSize = Mathf.RoundToInt(value);
            ZoneSlider.GetComponent<UISlider>().Value = zoneSize;
        }

        public void SetTanCont(float value)
        {
            tanCont = value;
            ContSlider.GetComponent<UISlider>().Value = tanCont;
        }

        public void SetSkeleton(bool value)
        {
            GlobalState.Settings.DisplaySkeletons = value;
        }
        #endregion

        protected override void Awake()
        {
            base.Awake();

            AddKeyModeButton = panel.Find("AddKey");
            ZoneModeButton = panel.Find("Zone");
            SegmentModeButton = panel.Find("Segment");
            TangentModeButton = panel.Find("Tangent");
            ZoneSlider = panel.Find("ZoneSize");
            CurveModeButton = panel.Find("Curve");
            PoseModeButton = panel.Find("Pose");
            FKModeButton = panel.Find("FK");
            IKModeButton = panel.Find("IK");
            ContSlider = panel.Find("Tangents");
            SkeletonDisplay = panel.Find("Skeleton");
            OffsetLabel = panel.Find("OffsetValue");

            CurveMode = CurveEditMode.AddKeyframe;
            Mode = EditMode.Pose;
            PoseMode = PoseEditMode.FK;
            zoneSize = Mathf.RoundToInt(ZoneSlider.GetComponent<UISlider>().Value);
            Offsetvalue = GlobalState.Settings.CurveForwardOffset;
            SkeletonDisplay.GetComponent<UICheckbox>().Checked = GlobalState.Settings.DisplaySkeletons;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (GameObject select in Selection.SelectedObjects)
            {
                if (select.TryGetComponent<RigController>(out RigController controller))
                {
                    RigGoalController[] GoalController = controller.GetComponentsInChildren<RigGoalController>();
                    for (int i = 0; i < GoalController.Length; i++)
                    {
                        GoalController[i].UseGoal(true);
                    }
                }
            }
            GlobalState.Instance.onGripWorldEvent.AddListener(OnGripWorld);
            UnHideActuator();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (GameObject select in Selection.SelectedObjects)
            {
                if (select.TryGetComponent<RigController>(out RigController controller))
                {
                    RigGoalController[] GoalController = controller.GetComponentsInChildren<RigGoalController>();
                    for (int i = 0; i < GoalController.Length; i++)
                    {
                        GoalController[i].UseGoal(false);
                    }
                }
            }
            HideActuator();
        }


        private UIButton GetCurveModeButton(CurveEditMode mode)
        {
            switch (mode)
            {
                case CurveEditMode.AddKeyframe: return AddKeyModeButton.GetComponent<UIButton>();
                case CurveEditMode.Zone: return ZoneModeButton.GetComponent<UIButton>();
                case CurveEditMode.Segment: return SegmentModeButton.GetComponent<UIButton>();
                case CurveEditMode.Tangents: return TangentModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }

        private UIButton GetModeButton(EditMode mode)
        {
            switch (mode)
            {
                case EditMode.Curve: return CurveModeButton.GetComponent<UIButton>();
                case EditMode.Pose: return PoseModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }

        private UIButton GetPoseModeButton(PoseEditMode mode)
        {
            switch (mode)
            {
                case PoseEditMode.FK: return FKModeButton.GetComponent<UIButton>();
                case PoseEditMode.IK: return IKModeButton.GetComponent<UIButton>();
                default: return null;
            }
        }


        protected override void DoUpdate()
        {
            if (navigation.CanUseControls(NavigationMode.UsedControls.RIGHT_JOYSTICK))
            {
                Vector2 AxisValue = VRInput.GetValue(VRInput.primaryController, CommonUsages.primary2DAxis);
                if (AxisValue != Vector2.zero)
                {
                    float scaleFactor = 1f + GlobalState.Settings.scaleSpeed / 1000f;
                    if (null == curveManip)
                    {
                        float selectorRadius = mouthpiece.localScale.x;
                        if (AxisValue.y > deadzone) selectorRadius *= scaleFactor;
                        if (AxisValue.y < deadzone) selectorRadius /= scaleFactor;
                        selectorRadius = Mathf.Clamp(selectorRadius, 0.001f, 0.5f);
                        mouthpiece.localScale = Vector3.one * selectorRadius;
                    }
                    else
                    {
                        if (AxisValue.y > deadzone) scaleIndice *= scaleFactor;
                        if (AxisValue.y < deadzone) scaleIndice /= scaleFactor;
                        scaleIndice = Mathf.Clamp(scaleIndice, 0.001f, 100f);
                    }
                }
            }
        }
        public void OnGripWorld(bool state)
        {
            if (state && null != poseManip) EndPose();
            if (state && null != curveManip) ReleaseCurve();
            if (state && movedObjects.Count > 0) EndDragObject();
        }

        #region Ghost&Curve
        public void HoverLine(GameObject curveObject, Vector3 point)
        {
            LineRenderer line = curveObject.GetComponent<LineRenderer>();
            GameObject gobject = CurveManager.GetObjectFromCurve(curveObject);
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gobject);

            int frame = GetFrameFromPoint(line, point, animationSet);
            DrawCurveGhost(gobject, frame);
            if (CurveMode == CurveEditMode.Zone || CurveMode == CurveEditMode.Segment) DrawZone(line, frame - zoneSize, frame + zoneSize);
            if (CurveMode == CurveEditMode.Tangents)
            {
                Curve curve = animationSet.GetCurve(AnimatableProperty.PositionX);
                int prev = curve.GetPreviousKeyFrame(frame);
                int next = curve.GetNextKeyFrame(frame);
                DrawZone(line, prev, next);

            }
        }

        private void DrawCurveGhost(GameObject gobject, int frame)
        {
            if (gobject == null) return;
            if (gobject.TryGetComponent<RigGoalController>(out RigGoalController goalController))
            {
                ghostManager.CreateHoverGhost(goalController.RootController, frame);
                return;
            }
            if (gobject.TryGetComponent<MeshFilter>(out MeshFilter objectMesh))
            {
                if (null == ghost) CreateGhost();
                ShowGhost(true);
                MeshFilter ghostFilter = ghost.GetComponent<MeshFilter>();
                ghostFilter.mesh = objectMesh.mesh;
                AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gobject);
                animationSet.EvaluateTransform(frame, ghost.transform);
            }
        }

        private void DrawCurveGhost()
        {
            DrawCurveGhost(curveManip.Target, curveManip.Frame);
            if (CurveMode == CurveEditMode.Zone || CurveMode == CurveEditMode.Segment || CurveMode == CurveEditMode.Tangents) DrawZoneDrag();
        }
        private void ShowGhost(bool state)
        {
            if (null == ghost) return;
            ghost.SetActive(state);
            foreach (Transform child in mouthpiece)
            {
                child.gameObject.SetActive(!state);
            }
        }

        public void StopHovering()
        {
            ShowGhost(false);
            ghostManager.HideGhost();
            ResetColor();
        }

        private void CreateGhost()
        {
            ghost = new GameObject();
            ghost.name = "AnimationGhost";
            ghost.transform.parent = CurveManager.curvesParent;
            ghost.AddComponent<MeshRenderer>();
            ghost.AddComponent<MeshFilter>();

            ghost.GetComponent<MeshRenderer>().material = GhostMaterial;
        }

        public void DrawZone(LineRenderer line, int start, int end)
        {
            lastTexture = (Texture2D)line.material.mainTexture;
            if (null == lastTexture)
            {
                lastTexture = new Texture2D(line.positionCount, 1, TextureFormat.RGBA32, false);
                line.material.mainTexture = lastTexture;
            }

            ApplyTexture(start, end);
            lastLine = line;
        }
        public void DrawZoneDrag()
        {
            if (CurveManager.TryGetLine(curveManip.Target, out LineRenderer line))
            {
                line.material.mainTexture = lastTexture;
            }
        }

        private void ApplyTexture(int start, int end)
        {
            NativeArray<Color32> colors = lastTexture.GetRawTextureData<Color32>();
            for (int i = 0; i < colors.Length; i++)
            {
                if (i < start || i > end)
                {
                    colors[i] = DefaultColor;
                }
                else
                {
                    colors[i] = ZoneColor;
                }
            }
            lastTexture.Apply();
        }

        public void ResetColor()
        {
            if (null == lastLine) return;
            lastLine.material.mainTexture = null;
        }
        #endregion

        #region PoseMode

        public void StartPose(RigGoalController controller, Transform mouthpiece)
        {
            poseManip = new PoseManipulation(controller.transform, controller.PathToRoot, mouthpiece, controller.RootController, PoseMode);
        }
        public bool DragPose(Transform mouthpiece)
        {
            if (null == poseManip) return false;
            poseManip.SetDestination(mouthpiece);
            poseManip.TrySolver();
            if (null != goalGizmo && goalGizmo.gameObject.activeSelf) goalGizmo.ResetPosition(selectedGoal.gameObject);
            return true;
        }

        public void EndPose()
        {
            poseManip.GetCommand().Submit();
            if (GlobalState.Animation.autoKeyEnabled) new CommandAddKeyframes(poseManip.MeshController.gameObject, false).Submit();
            poseManip = null;
        }

        #endregion

        #region CurveMode
        public void StartDrag(GameObject gameObject, Transform mouthpiece)
        {
            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            GameObject target = CurveManager.GetObjectFromCurve(gameObject);
            AnimationSet anim = GlobalState.Animation.GetObjectAnimation(target);
            int frame = GetFrameFromPoint(line, mouthpiece.position, anim);

            if (target.TryGetComponent<RigGoalController>(out RigGoalController controller))
            {
                //temporary solution for hips joint
                if (target.name.Contains("Hips")) curveManip = new CurveManipulation(target, frame, mouthpiece, CurveMode, zoneSize, (double)tanCont);
                else curveManip = new CurveManipulation(target, controller, frame, mouthpiece, CurveMode, zoneSize, (double)tanCont);
            }
            else
            {
                curveManip = new CurveManipulation(target, frame, mouthpiece, CurveMode, zoneSize, (double)tanCont);
            }
        }

        internal bool DragCurve(Transform mouthpiece)
        {
            if (null == curveManip) return false;
            scaleIndice = 1f;
            curveManip.DragCurve(mouthpiece, scaleIndice);
            DrawCurveGhost();
            return true;
        }

        public void ReleaseCurve()
        {
            curveManip.ReleaseCurve();
            curveManip = null;
        }



        private int GetFrameFromPoint(LineRenderer line, Vector3 pointPosition, AnimationSet anim)
        {
            Vector3 localPointPosition = line.transform.InverseTransformPoint(pointPosition);
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            int closestPoint = 0;
            float closestDistance = Vector3.Distance(positions[0], localPointPosition);
            for (int i = 1; i < line.positionCount; i++)
            {
                float dist = Vector3.Distance(positions[i], localPointPosition);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestPoint = i;
                }
            }

            int firstAnimFrame = anim.GetCurve(AnimatableProperty.PositionX).keys[0].frame + GlobalState.Animation.StartFrame - 1;
            return closestPoint + firstAnimFrame;
        }


        #endregion

        #region DragObject

        Matrix4x4 initMouthPieceWorldToLocal;
        List<GameObject> movedObjects = new List<GameObject>();
        Dictionary<GameObject, Matrix4x4> initialParentMatrixLtW = new Dictionary<GameObject, Matrix4x4>();
        Dictionary<GameObject, Matrix4x4> initialParentMatrixWtL = new Dictionary<GameObject, Matrix4x4>();
        Dictionary<GameObject, Vector3> initialPositions = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Quaternion> initialRotation = new Dictionary<GameObject, Quaternion>();
        Dictionary<GameObject, Vector3> initialScale = new Dictionary<GameObject, Vector3>();

        public void StartDragObject(GameObject gobject, Transform mouthpiece)
        {
            initMouthPieceWorldToLocal = mouthpiece.worldToLocalMatrix;

            initialParentMatrixLtW[gobject] = gobject.transform.parent.localToWorldMatrix;
            initialParentMatrixWtL[gobject] = gobject.transform.parent.worldToLocalMatrix;
            initialPositions[gobject] = gobject.transform.localPosition;
            initialRotation[gobject] = gobject.transform.localRotation;
            initialScale[gobject] = gobject.transform.localScale;
            movedObjects.Add(gobject);
        }

        public void DragObject(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initMouthPieceWorldToLocal;
            movedObjects.ForEach(x =>
            {
                Matrix4x4 transformed = initialParentMatrixWtL[x] * transformation * initialParentMatrixLtW[x] * Matrix4x4.TRS(initialPositions[x], initialRotation[x], initialScale[x]);
                Maths.DecomposeMatrix(transformed, out Vector3 pos, out Quaternion rot, out Vector3 scale);
                x.transform.localPosition = pos;
                x.transform.localRotation = rot;
                x.transform.localScale = scale;
            });
        }

        public void EndDragObject()
        {

            List<Vector3> beginPositions = new List<Vector3>();
            List<Vector3> endPositions = new List<Vector3>();
            List<Quaternion> beginRotations = new List<Quaternion>();
            List<Quaternion> endRotations = new List<Quaternion>();
            List<Vector3> beginScales = new List<Vector3>();
            List<Vector3> endScales = new List<Vector3>();

            foreach (GameObject gObject in movedObjects)
            {
                beginPositions.Add(initialPositions[gObject]);
                beginRotations.Add(initialRotation[gObject]);
                beginScales.Add(initialScale[gObject]);
                endPositions.Add(gObject.transform.localPosition);
                endRotations.Add(gObject.transform.localRotation);
                endScales.Add(gObject.transform.localScale);
                if (GlobalState.Animation.autoKeyEnabled) new CommandAddKeyframes(gObject, false).Submit();

                initialParentMatrixLtW.Remove(gObject);
                initialParentMatrixWtL.Remove(gObject);
                initialPositions.Remove(gObject);
                initialRotation.Remove(gObject);
                initialScale.Remove(gObject);
            }

            new CommandMoveObjects(movedObjects, beginPositions, beginRotations, beginScales, endPositions, endRotations, endScales).Submit();
            movedObjects.Clear();
        }
        #endregion


        #region GoalSelection


        public void HideActuator()
        {
            if (null != goalGizmo) goalGizmo.gameObject.SetActive(false);
        }
        public void UnHideActuator()
        {
            if (null != selectedGoal) goalGizmo.gameObject.SetActive(true);
        }

        public void SelectGoal(RigGoalController controller)
        {
            if (selectedGoal != null) UnSelectGoal(selectedGoal);
            goalGizmo.gameObject.SetActive(true);
            selectedGoal = controller;
            controller.gameObject.layer = 20;
            goalGizmo.Init(controller);
        }

        public void SelectEmpty()
        {
            if (null != selectedGoal) UnSelectGoal(selectedGoal);
        }

        private void UnSelectGoal(RigGoalController controller)
        {
            goalGizmo.gameObject.SetActive(false);
            controller.gameObject.layer = 21;
            selectedGoal = null;
        }

        public void StartAcutator(GameObject actuator, Transform mouthpiece)
        {
            PoseManipulation.RotationAxis axis = PoseManipulation.RotationAxis.X;
            if (actuator == goalGizmo.xCurve) axis = PoseManipulation.RotationAxis.X;
            if (actuator == goalGizmo.yCurve) axis = PoseManipulation.RotationAxis.Y;
            if (actuator == goalGizmo.zCurve) axis = PoseManipulation.RotationAxis.Z;
            poseManip = new PoseManipulation(goalGizmo.Controller.transform, goalGizmo.Controller.RootController, mouthpiece, axis);
        }

        #endregion

    }

}