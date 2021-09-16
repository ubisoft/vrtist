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
using UnityEngine;
namespace VRtist
{

    public class CurveManipulation
    {
        public bool isHuman;
        public GameObject Target;
        public int Frame;

        public struct ObjectData
        {
            public AnimationSet Animation;
            public Matrix4x4 InitialParentMatrix;
            public Matrix4x4 InitialParentMatrixWorldToLocal;
            public Matrix4x4 InitialTRS;
            public float ScaleIndice;
            public TangentSimpleSolver Solver;

            public Vector3 lastPosition;
            public Vector3 lastRotation;
            public Quaternion lastQRotation;
            public Vector3 lastScale;
        }
        private ObjectData objectData;

        public struct HumanData
        {
            public RigGoalController Controller;
            public List<AnimationSet> Animations;
            public AnimationSet ObjectAnimation;
            public Matrix4x4 InitFrameMatrix;
            public TangentRigSolver Solver;
        }
        private HumanData humanData;

        private Matrix4x4 initialMouthMatrix;

        private int startFrame;
        private int endFrame;
        private double continuity;

        private AnimationTool.CurveEditMode manipulationMode;

        /// <summary>
        /// Initialize Curve manipulation for Rigs
        /// </summary>
        public CurveManipulation(GameObject target, RigGoalController controller, int frame, Transform mouthpiece, AnimationTool.CurveEditMode manipMode, int zoneSize, double tangentContinuity)
        {
            isHuman = true;
            manipulationMode = manipMode;
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            Frame = frame;
            Target = target;
            continuity = tangentContinuity;

            List<AnimationSet> previousSets = new List<AnimationSet>();
            controller.AnimToRoot.ForEach(x =>
            {
                if (null != x) previousSets.Add(new AnimationSet(x));
            });
            humanData = new HumanData()
            {
                Animations = previousSets,
                Controller = controller,
                ObjectAnimation = new AnimationSet(controller.Animation),
                InitFrameMatrix = controller.MatrixAtFrame(frame)
            };
            if (manipulationMode == AnimationTool.CurveEditMode.Segment)
            {
                startFrame = frame - zoneSize;
                endFrame = frame + zoneSize;
                AddSegmentHierarchy(controller, frame);
                AddSegmentKeyframes(frame, controller.Animation);
            }
            if (manipulationMode == AnimationTool.CurveEditMode.Tangents)
            {
                startFrame = humanData.ObjectAnimation.GetCurve(AnimatableProperty.RotationX).GetPreviousKeyFrame(frame);
                endFrame = humanData.ObjectAnimation.GetCurve(AnimatableProperty.RotationX).GetNextKeyFrame(frame);
            }
        }

        /// <summary>
        /// Initialize Curve Manipulation for objects
        /// </summary>
        public CurveManipulation(GameObject target, int frame, Transform mouthpiece, AnimationTool.CurveEditMode manipMode, int zoneSize, double tanCont)
        {
            isHuman = false;
            manipulationMode = manipMode;
            AnimationSet previousSet = GlobalState.Animation.GetObjectAnimation(target);
            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            Target = target;
            Frame = frame;
            continuity = tanCont;


            if (!previousSet.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = target.transform.localPosition.x;
            if (!previousSet.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = target.transform.localPosition.y;
            if (!previousSet.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = target.transform.localPosition.z;
            if (!previousSet.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = target.transform.localEulerAngles.x;
            if (!previousSet.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = target.transform.localEulerAngles.y;
            if (!previousSet.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = target.transform.localEulerAngles.z;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = target.transform.localScale.x;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = target.transform.localScale.y;
            if (!previousSet.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = target.transform.localScale.z;

            Vector3 initialPosition = new Vector3(posx, posy, posz);
            Quaternion initialRotation = Quaternion.Euler(rotx, roty, rotz);
            Vector3 initialScale = new Vector3(scax, scay, scaz);

            objectData = new ObjectData()
            {
                Animation = new AnimationSet(previousSet),
                InitialParentMatrix = target.transform.parent.localToWorldMatrix,
                InitialParentMatrixWorldToLocal = target.transform.parent.worldToLocalMatrix,
                InitialTRS = Matrix4x4.TRS(initialPosition, initialRotation, initialScale),
                ScaleIndice = 1f
            };
            if (manipulationMode == AnimationTool.CurveEditMode.Zone)
            {
                startFrame = frame - zoneSize;
                endFrame = frame + zoneSize;
            }
            if (manipulationMode == AnimationTool.CurveEditMode.Segment)
            {
                startFrame = frame - zoneSize;
                endFrame = frame + zoneSize;
                AddSegmentKeyframes(frame, previousSet);
            }
            if (manipMode == AnimationTool.CurveEditMode.Tangents)
            {
                startFrame = objectData.Animation.GetCurve(AnimatableProperty.PositionX).GetPreviousKeyFrame(frame);
                endFrame = objectData.Animation.GetCurve(AnimatableProperty.PositionX).GetNextKeyFrame(frame);
            }
        }

        public void DragCurve(Transform mouthpiece, float scaleIndice)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            if (isHuman) DragHuman(transformation);
            else DragObject(transformation, scaleIndice);
        }

        private void DragHuman(Matrix4x4 transformation)
        {
            if (humanData.Solver == null)
                CreateSolver(transformation);
            else if (!humanData.Solver.NextStep())
                CreateSolver(transformation);
        }

        private void CreateSolver(Matrix4x4 transformation)
        {
            Matrix4x4 target = transformation * humanData.InitFrameMatrix;
            target = humanData.Controller.RootController.transform.worldToLocalMatrix * target;
            Maths.DecomposeMatrix(target, out Vector3 targetPos, out Quaternion targetRot, out Vector3 targetScale);

            TangentRigSolver solver = new TangentRigSolver(targetPos, targetRot, humanData.Controller.Animation, humanData.Controller.AnimToRoot, Frame, startFrame, endFrame, continuity);
            humanData.Solver = solver;

            solver.NextStep();
            GlobalState.Animation.onChangeCurve.Invoke(humanData.Controller.RootController.gameObject, AnimatableProperty.PositionX);
        }

        private void DragObject(Matrix4x4 transformation, float scaleIndice)
        {
            Matrix4x4 transformed = objectData.InitialParentMatrixWorldToLocal *
                transformation * objectData.InitialParentMatrix *
                objectData.InitialTRS;

            Maths.DecomposeMatrix(transformed, out objectData.lastPosition, out objectData.lastQRotation, out objectData.lastScale);
            objectData.lastRotation = objectData.lastQRotation.eulerAngles;
            objectData.lastScale *= scaleIndice;

            Interpolation interpolation = GlobalState.Settings.interpolation;
            AnimationKey posX = new AnimationKey(Frame, objectData.lastPosition.x, interpolation);
            AnimationKey posY = new AnimationKey(Frame, objectData.lastPosition.y, interpolation);
            AnimationKey posZ = new AnimationKey(Frame, objectData.lastPosition.z, interpolation);
            AnimationKey rotX = new AnimationKey(Frame, objectData.lastRotation.x, interpolation);
            AnimationKey rotY = new AnimationKey(Frame, objectData.lastRotation.y, interpolation);
            AnimationKey rotZ = new AnimationKey(Frame, objectData.lastRotation.z, interpolation);
            AnimationKey scalex = new AnimationKey(Frame, objectData.lastScale.z, interpolation);
            AnimationKey scaley = new AnimationKey(Frame, objectData.lastScale.z, interpolation);
            AnimationKey scalez = new AnimationKey(Frame, objectData.lastScale.z, interpolation);

            switch (manipulationMode)
            {
                case AnimationTool.CurveEditMode.AddKeyframe:
                    AddFilteredKeyframe(Target, posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
                    break;
                case AnimationTool.CurveEditMode.Zone:
                    AddFilteredKeyframeZone(Target, posX, posY, posZ, rotX, rotY, rotZ, scalex, scaley, scalez);
                    break;
                case AnimationTool.CurveEditMode.Segment:
                    objectData.Solver = new TangentSimpleSolver(objectData.lastPosition, objectData.lastQRotation, GlobalState.Animation.GetObjectAnimation(Target), Frame, startFrame, endFrame, continuity);
                    objectData.Solver.TrySolver();
                    GlobalState.Animation.onChangeCurve.Invoke(Target, AnimatableProperty.PositionX);
                    break;
                case AnimationTool.CurveEditMode.Tangents:
                    objectData.Solver = new TangentSimpleSolver(objectData.lastPosition, objectData.lastQRotation, GlobalState.Animation.GetObjectAnimation(Target), Frame, startFrame, endFrame, continuity);
                    objectData.Solver.TrySolver();
                    GlobalState.Animation.onChangeCurve.Invoke(Target, AnimatableProperty.PositionX);
                    break;
            }
        }

        public void ReleaseCurve()
        {
            if (isHuman) ReleaseHuman();
            else ReleaseObject();
        }

        private void ReleaseObject()
        {
            GlobalState.Animation.SetObjectAnimations(Target, objectData.Animation);
            CommandGroup group = new CommandGroup("Add Keyframe");
            switch (manipulationMode)
            {
                case AnimationTool.CurveEditMode.AddKeyframe:
                    new CommandAddKeyframes(Target, Frame, objectData.lastPosition, objectData.lastRotation, objectData.lastScale).Submit();
                    break;
                case AnimationTool.CurveEditMode.Zone:
                    new CommandAddKeyframes(Target, Frame, startFrame, endFrame, objectData.lastPosition, objectData.lastRotation, objectData.lastScale).Submit();
                    break;

                case AnimationTool.CurveEditMode.Segment:
                    Dictionary<AnimatableProperty, List<AnimationKey>> keyframeList = new Dictionary<AnimatableProperty, List<AnimationKey>>();

                    for (int prop = 0; prop < 6; prop++)
                    {
                        AnimatableProperty property = (AnimatableProperty)prop;
                        keyframeList.Add(property, new List<AnimationKey>());
                        int firstKey = Mathf.Max(0, objectData.Solver.RequiredKeyframeIndices[0] - 1);
                        int lastKey = Mathf.Min(objectData.Solver.ObjectAnimation.GetCurve(property).keys.Count - 1, objectData.Solver.RequiredKeyframeIndices[objectData.Solver.RequiredKeyframeIndices.Count - 1] + 1);
                        for (int i = firstKey; i <= lastKey; i++)
                        {
                            keyframeList[property].Add(objectData.Solver.ObjectAnimation.GetCurve(property).keys[i]);
                        }
                    }
                    new CommandAddKeyframes(Target, Frame, startFrame, endFrame, keyframeList).Submit();
                    break;

                case AnimationTool.CurveEditMode.Tangents:
                    Dictionary<AnimatableProperty, List<AnimationKey>> keyList = new Dictionary<AnimatableProperty, List<AnimationKey>>();
                    for (int prop = 0; prop < 6; prop++)
                    {
                        AnimatableProperty property = (AnimatableProperty)prop;
                        keyList.Add(property, new List<AnimationKey>());
                        int firstKey = objectData.Solver.RequiredKeyframeIndices[0];
                        int lastKey = objectData.Solver.RequiredKeyframeIndices[1];
                        for (int i = firstKey; i <= lastKey; i++)
                        {
                            keyList[property].Add(objectData.Solver.ObjectAnimation.GetCurve(property).keys[i]);
                        }
                    }
                    new CommandAddKeyframes(Target, Frame, startFrame, endFrame, keyList).Submit();
                    break;
            }
            group.Submit();
        }

        private void ReleaseHuman()
        {
            List<GameObject> objectList = new List<GameObject>();
            List<Dictionary<AnimatableProperty, List<AnimationKey>>> keyframesLists = new List<Dictionary<AnimatableProperty, List<AnimationKey>>>();

            while (humanData.Solver.NextStep()) { }
            humanData.Solver.ClearJob();

            int index = 0;
            for (int i = 0; i < humanData.Controller.PathToRoot.Count; i++)
            {
                if (null == humanData.Controller.AnimToRoot[i]) continue;
                keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
                for (int pIndex = 0; pIndex < 6; pIndex++)
                {
                    AnimatableProperty property = (AnimatableProperty)pIndex;
                    List<AnimationKey> keys = new List<AnimationKey>();
                    Curve curve = humanData.Controller.AnimToRoot[i].GetCurve(property);
                    for (int k = 0; k < humanData.Solver.requiredKeyframe.Count; k++)
                    {
                        curve.GetKeyIndex(humanData.Solver.requiredKeyframe[k], out int keyIndex);
                        keys.Add(curve.keys[keyIndex]);
                    }
                    keyframesLists[keyframesLists.Count - 1].Add(property, keys);
                }
                GlobalState.Animation.SetObjectAnimations(humanData.Animations[index].transform.gameObject, humanData.Animations[index]);
                objectList.Add(humanData.Animations[index].transform.gameObject);
                index++;
            }

            keyframesLists.Add(new Dictionary<AnimatableProperty, List<AnimationKey>>());
            for (int prop = 0; prop < 6; prop++)
            {
                AnimatableProperty property = (AnimatableProperty)prop;
                List<AnimationKey> keys = new List<AnimationKey>();
                Curve curve = humanData.Controller.Animation.GetCurve(property);

                curve.GetKeyIndex(humanData.Solver.requiredKeyframe[0], out int beforKey);
                for (int k = 0; k < humanData.Solver.requiredKeyframe.Count; k++)
                {
                    curve.GetKeyIndex(humanData.Solver.requiredKeyframe[k], out int keyIndex);
                    keys.Add(curve.keys[keyIndex]);
                }
                curve.GetKeyIndex(humanData.Solver.requiredKeyframe[humanData.Solver.requiredKeyframe.Count - 1], out int afterKey);
                keyframesLists[keyframesLists.Count - 1].Add(property, keys);
            }
            GlobalState.Animation.SetObjectAnimations(Target, humanData.ObjectAnimation);
            objectList.Add(Target);

            GlobalState.Animation.onChangeCurve.Invoke(humanData.Animations[0].transform.gameObject, AnimatableProperty.PositionX);
            CommandGroup group = new CommandGroup("Add Keyframe");
            new CommandAddKeyframes(humanData.Controller.RootController.gameObject, objectList, Frame, startFrame, endFrame, keyframesLists).Submit();
            group.Submit();
        }

        private void AddSegmentKeyframes(int frame, AnimationSet animation)
        {
            if (!animation.GetCurve(AnimatableProperty.PositionX).Evaluate(frame, out float posx)) posx = animation.transform.localPosition.x;
            if (!animation.GetCurve(AnimatableProperty.PositionY).Evaluate(frame, out float posy)) posy = animation.transform.localPosition.y;
            if (!animation.GetCurve(AnimatableProperty.PositionZ).Evaluate(frame, out float posz)) posz = animation.transform.localPosition.z;
            if (!animation.GetCurve(AnimatableProperty.RotationX).Evaluate(frame, out float rotx)) rotx = animation.transform.localEulerAngles.x;
            if (!animation.GetCurve(AnimatableProperty.RotationY).Evaluate(frame, out float roty)) roty = animation.transform.localEulerAngles.y;
            if (!animation.GetCurve(AnimatableProperty.RotationZ).Evaluate(frame, out float rotz)) rotz = animation.transform.localEulerAngles.z;
            if (!animation.GetCurve(AnimatableProperty.ScaleX).Evaluate(frame, out float scax)) scax = animation.transform.localScale.x;
            if (!animation.GetCurve(AnimatableProperty.ScaleY).Evaluate(frame, out float scay)) scay = animation.transform.localScale.y;
            if (!animation.GetCurve(AnimatableProperty.ScaleZ).Evaluate(frame, out float scaz)) scaz = animation.transform.localScale.z;

            AddFilteredKeyframeTangent(animation.transform.gameObject,
                new AnimationKey(frame, posx),
                new AnimationKey(frame, posy),
                new AnimationKey(frame, posz),
                new AnimationKey(frame, rotx),
                new AnimationKey(frame, roty),
                new AnimationKey(frame, rotz),
                new AnimationKey(frame, scax),
                new AnimationKey(frame, scay),
                new AnimationKey(frame, scaz));
        }

        private void AddSegmentHierarchy(RigGoalController controller, int frame)
        {
            for (int i = 0; i < controller.AnimToRoot.Count; i++)
            {
                AnimationSet anim = controller.AnimToRoot[i];
                if (null != anim)
                    AddSegmentKeyframes(frame, anim);
            }
        }


        private void AddFilteredKeyframeTangent(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scaleX, AnimationKey scaleY, AnimationKey scaleZ)
        {
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationX, rotX, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationY, rotY, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.RotationZ, rotZ, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleX, scaleX, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleY, scaleY, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.ScaleZ, scaleZ, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionX, posX, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionY, posY, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeTangent(target, AnimatableProperty.PositionZ, posZ, startFrame, endFrame, false);
        }

        private void AddFilteredKeyframe(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.RotationX, rotX, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.RotationY, rotY, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.RotationZ, rotZ, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.ScaleX, scalex, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.ScaleY, scaley, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.ScaleZ, scalez, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.PositionY, posY, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.PositionZ, posZ, false);
            GlobalState.Animation.AddFilteredKeyframe(target, AnimatableProperty.PositionX, posX);
        }

        private void AddFilteredKeyframeZone(GameObject target, AnimationKey posX, AnimationKey posY, AnimationKey posZ, AnimationKey rotX, AnimationKey rotY, AnimationKey rotZ, AnimationKey scalex, AnimationKey scaley, AnimationKey scalez)
        {
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.RotationX, rotX, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.RotationY, rotY, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.RotationZ, rotZ, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.ScaleX, scalex, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.ScaleY, scaley, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.ScaleZ, scalez, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.PositionX, posX, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.PositionY, posY, startFrame, endFrame, false);
            GlobalState.Animation.AddFilteredKeyframeZone(target, AnimatableProperty.PositionZ, posZ, startFrame, endFrame);
        }
    }
}