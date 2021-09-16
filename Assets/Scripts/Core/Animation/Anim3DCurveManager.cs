/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 * &
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
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Display motion trails of animated objects.
    /// </summary>
    public class Anim3DCurveManager : MonoBehaviour
    {
        private bool displaySelectedCurves = true;
        public Transform curvesParent;
        public GameObject curvePrefab;

        private readonly float lineWidth = 0.001f;

        private bool isAnimationTool;
        private float currentCurveOffset;

        private readonly Dictionary<GameObject, GameObject> curves = new Dictionary<GameObject, GameObject>();
        private Dictionary<RigController, List<GameObject>> goalCurves = new Dictionary<RigController, List<GameObject>>();

        void Start()
        {
            ToolsUIManager.Instance.OnToolChangedEvent += OnToolChanged;
            Selection.onSelectionChanged.AddListener(OnSelectionChanged);
            GlobalState.Animation.onFrameEvent.AddListener(UpdateOffset);
            GlobalState.Animation.onChangeCurve.AddListener(OnCurveChanged);
            GlobalState.Animation.onAddAnimation.AddListener(OnAnimationAdded);
            GlobalState.Animation.onRemoveAnimation.AddListener(OnAnimationRemoved);
            GlobalState.ObjectMovingEvent.AddListener(OnObjectMoved);
        }

        private void OnObjectMoved(GameObject gObject)
        {
            if (gObject.TryGetComponent(out RigController rigController) && goalCurves.ContainsKey(rigController)) UpdateFromSelection();
        }

        void Update()
        {
            if (displaySelectedCurves != GlobalState.Settings.Display3DCurves)
            {
                displaySelectedCurves = GlobalState.Settings.Display3DCurves;
                if (displaySelectedCurves)
                    UpdateFromSelection();
                else
                    ClearCurves();
            }
            if (currentCurveOffset != GlobalState.Settings.CurveForwardOffset)
            {
                currentCurveOffset = GlobalState.Settings.CurveForwardOffset;
                UpdateOffsetValue();
            }
            UpdateCurvesWidth();
        }

        void UpdateCurvesWidth()
        {
            foreach (GameObject curve in curves.Values)
            {
                LineRenderer line = curve.GetComponent<LineRenderer>();
                line.startWidth = lineWidth / GlobalState.WorldScale;
                line.endWidth = line.startWidth;
            }
        }

        void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> selectedObjects)
        {
            if (GlobalState.Settings.Display3DCurves)
                UpdateFromSelection();
        }

        void UpdateFromSelection()
        {
            ClearCurves();
            foreach (GameObject gObject in Selection.SelectedObjects)
            {
                AddCurve(gObject);
                if (gObject.TryGetComponent(out RigController skinController))
                {
                    if ((ToolsManager.CurrentToolName() == "Animation")) AddHumanCurve(gObject, skinController);
                    else
                    {
                        //Only display curve for Rig's root
                        RigGoalController goalController = skinController.RootObject.GetComponent<RigGoalController>();
                        AddGoalCurve(goalController, skinController);
                    }
                }
            }
        }

        void OnCurveChanged(GameObject gObject, AnimatableProperty property)
        {
            RigGoalController[] controllers = gObject.GetComponentsInChildren<RigGoalController>();
            if (controllers.Length > 0)
            {
                if ((ToolsManager.CurrentToolName() == "Animation"))
                {
                    //update all goals' curves
                    UpdateGoalCurve(controllers);
                }
                else
                {
                    //only update rig's root curve
                    UpdateGoalCurve(new RigGoalController[] { controllers[0] });
                }
            }
            if (property != AnimatableProperty.PositionX && property != AnimatableProperty.PositionY && property != AnimatableProperty.PositionZ)
                return;

            if (!Selection.IsSelected(gObject))
                return;

            UpdateCurve(gObject);
        }


        void OnAnimationAdded(GameObject gObject)
        {
            if (!Selection.IsSelected(gObject))
                return;
            UpdateCurve(gObject);
        }

        void OnAnimationRemoved(GameObject gObject)
        {
            if (gObject.TryGetComponent<RigController>(out RigController controller))
            {
                RecursiveDeleteCurve(gObject.transform);
                if (goalCurves.ContainsKey(controller)) goalCurves.Remove(controller);
            }
            else
            {
                DeleteCurve(gObject);
            }
        }

        void OnToolChanged(object sender, ToolChangedArgs args)
        {
            bool switchToAnim = args.toolName == "Animation";
            if (switchToAnim && !isAnimationTool)
            {
                UpdateFromSelection();
            }
            if (!switchToAnim && isAnimationTool)
            {
                DeleteGoalCurves();
            }
            isAnimationTool = switchToAnim;
        }

        void ClearCurves()
        {
            foreach (GameObject curve in curves.Values)
                Destroy(curve);
            curves.Clear();
            goalCurves.Clear();
        }

        void DeleteCurve(GameObject gObject)
        {
            if (curves.ContainsKey(gObject))
            {
                Destroy(curves[gObject]);
                curves.Remove(gObject);
            }
        }

        void RecursiveDeleteCurve(Transform target)
        {
            DeleteCurve(target.gameObject);
            foreach (Transform child in target)
            {
                RecursiveDeleteCurve(child);
            }
        }

        void DeleteGoalCurves()
        {
            List<GameObject> removedCurves = new List<GameObject>();
            foreach (KeyValuePair<GameObject, GameObject> curve in curves)
            {
                if (curve.Key.TryGetComponent<RigGoalController>(out RigGoalController controller) && controller.PathToRoot[0] != controller.transform)
                {
                    Destroy(curve.Value);
                    removedCurves.Add(curve.Key);
                }
            }
            goalCurves.Clear();
            removedCurves.ForEach(x => curves.Remove(x));
        }

        void UpdateCurve(GameObject gObject)
        {
            AddCurve(gObject);
        }

        private void UpdateGoalCurve(RigGoalController[] controllers)
        {
            if (goalCurves.ContainsKey(controllers[0].RootController)) goalCurves.Remove(controllers[0].RootController);
            for (int i = 0; i < controllers.Length; i++)
            {
                DeleteCurve(controllers[i].gameObject);
                AddGoalCurve(controllers[i], controllers[i].RootController);
            }
        }


        void AddCurve(GameObject gObject)
        {
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet)
            {
                return;
            }

            Curve positionX = animationSet.GetCurve(AnimatableProperty.PositionX);
            Curve positionY = animationSet.GetCurve(AnimatableProperty.PositionY);
            Curve positionZ = animationSet.GetCurve(AnimatableProperty.PositionZ);

            if (null == positionX || null == positionY || null == positionZ)
                return;

            if (positionX.keys.Count == 0)
                return;

            if (positionX.keys.Count != positionY.keys.Count || positionX.keys.Count != positionZ.keys.Count)
                return;

            int frameStart = Mathf.Clamp(positionX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            int frameEnd = Mathf.Clamp(positionX.keys[positionX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            Matrix4x4 matrix = curvesParent.worldToLocalMatrix * gObject.transform.parent.localToWorldMatrix;

            List<Vector3> positions = new List<Vector3>();
            for (int i = frameStart; i <= frameEnd; i++)
            {
                positionX.Evaluate(i, out float x);
                positionY.Evaluate(i, out float y);
                positionZ.Evaluate(i, out float z);
                Vector3 position = new Vector3(x, y, z);
                position = matrix.MultiplyPoint(position);

                positions.Add(position);
            }

            int count = positions.Count;
            GameObject curve3D = curves.TryGetValue(gObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

            LineRenderer line = curve3D.GetComponent<LineRenderer>();
            line.positionCount = count;
            for (int index = 0; index < count; index++)
            {
                line.SetPosition(index, positions[index]);
            }
            line.startWidth = lineWidth / GlobalState.WorldScale;
            line.endWidth = line.startWidth;

            MeshCollider collider = curve3D.GetComponent<MeshCollider>();
            Mesh lineMesh = new Mesh();
            line.BakeMesh(lineMesh);
            collider.sharedMesh = lineMesh;

            curves[gObject] = curve3D;
        }

        private void AddHumanCurve(GameObject gObject, RigController controller)
        {
            RigGoalController goalController = controller.RootObject.GetComponent<RigGoalController>();
            RigGoalController[] controllers = goalController.GetComponentsInChildren<RigGoalController>();
            foreach (RigGoalController ctrl in controllers)
            {
                AddGoalCurve(ctrl, controller);
            }
        }

        private void AddGoalCurve(RigGoalController goalController, RigController skinController)
        {
            if (!goalController.ShowCurve) return;

            AnimationSet goalAnimation = GlobalState.Animation.GetObjectAnimation(goalController.gameObject);
            if (null == goalAnimation) return;

            Curve rotationX = goalAnimation.GetCurve(AnimatableProperty.RotationX);
            if (rotationX.keys.Count == 0) return;

            int frameStart = Mathf.Clamp(rotationX.keys[0].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
            int frameEnd = Mathf.Clamp(rotationX.keys[rotationX.keys.Count - 1].frame, GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);

            List<Vector3> positions = new List<Vector3>();
            GameObject curve3D = curves.TryGetValue(goalController.gameObject, out GameObject current) ? current : Instantiate(curvePrefab, curvesParent);

            Vector3 forwardOffset = (skinController.transform.forward * skinController.transform.localScale.x) * currentCurveOffset;

            goalController.CheckAnimations();
            for (int i = frameStart; i <= frameEnd; i++)
            {
                Vector3 position = curve3D.transform.InverseTransformDirection(goalController.FramePosition(i) - (forwardOffset * i));
                positions.Add(position);
            }
            LineRenderer line = curve3D.GetComponent<LineRenderer>();
            line.positionCount = positions.Count;
            line.SetPositions(positions.ToArray());

            line.startWidth = lineWidth / GlobalState.WorldScale;
            line.endWidth = line.startWidth;

            curve3D.transform.position = forwardOffset * GlobalState.Animation.CurrentFrame;

            MeshCollider collider = curve3D.GetComponent<MeshCollider>();
            Mesh lineMesh = new Mesh();
            line.BakeMesh(lineMesh);
            collider.sharedMesh = lineMesh;
            curves[goalController.gameObject] = curve3D;
            if (goalCurves.ContainsKey(skinController))
            {
                goalCurves[skinController].Add(curve3D);
            }
            else
            {
                goalCurves[skinController] = new List<GameObject>();
                goalCurves[skinController].Add(curve3D);
            }
        }

        public GameObject GetObjectFromCurve(GameObject curve)
        {
            foreach (KeyValuePair<GameObject, GameObject> pair in curves)
            {
                if (pair.Value == curve) return pair.Key;
            }
            return null;
        }

        public bool TryGetLine(GameObject gobject, out LineRenderer line)
        {
            if (!curves.TryGetValue(gobject, out GameObject value))
            {
                line = null;
                return false;
            }
            return (value.TryGetComponent<LineRenderer>(out line));
        }

        private void UpdateOffsetValue()
        {
            foreach (KeyValuePair<RigController, List<GameObject>> curves in goalCurves)
            {
                RigGoalController[] controllers = curves.Key.GetComponentsInChildren<RigGoalController>();
                UpdateGoalCurve(controllers);
            }
        }

        private void UpdateOffset(int frame)
        {
            foreach (KeyValuePair<RigController, List<GameObject>> curves in goalCurves)
            {
                Vector3 forwardVector = (curves.Key.transform.forward * curves.Key.transform.localScale.x) * currentCurveOffset;
                curves.Value.ForEach(x =>
                {
                    x.transform.position = forwardVector * frame;
                });
            }
        }
    }
}
