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

    public class PoseManipulation
    {
        public RigController MeshController;
        private Transform oTransform;
        private List<Transform> fullHierarchy;

        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private int hierarchySize;

        private Matrix4x4 InitialParentMatrixWorldToLocal;
        private Matrix4x4 InitialParentMatrix;
        private Matrix4x4 initialMouthMatrix;
        private Matrix4x4 initialTransformMatrix;
        private Matrix4x4 InitialTRS;
        private Vector3 fromRotation;
        private Quaternion initialRotation;

        private float rootScale;

        private Vector3 acAxis;
        private float previousAngle;
        private Vector3 initForward;
        private RotationAxis rAxis;
        public enum RotationAxis { X, Y, Z };


        private AnimationTool.PoseEditMode poseMode;

        private struct State
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        private State currentState;
        private State desiredState;


        private int p;
        private double[] theta;
        private double[,] Q_opt;
        private double[] b_opt;
        private double[] lowerBound, upperBound;
        private double[] delta_theta_0;
        private double[] s;
        private double[] delta_theta;

        private List<GameObject> movedObjects;
        private List<RigGoalController> controllers;
        private List<Vector3> startPositions;
        private List<Quaternion> startRotations;
        private List<Vector3> startScales;
        private List<Vector3> endPositions;
        private List<Quaternion> endRotations;
        private List<Vector3> endScales;

        public PoseManipulation(Transform objectTransform, List<Transform> objectHierarchy, Transform mouthpiece, RigController skinController, AnimationTool.PoseEditMode mode)
        {
            MeshController = skinController;
            poseMode = mode;
            oTransform = objectTransform;
            fullHierarchy = new List<Transform>(objectHierarchy);
            if (!fullHierarchy.Contains(objectTransform))
            {
                fullHierarchy.Add(objectTransform);
            }
            hierarchySize = fullHierarchy.Count;
            InitialTRS = Matrix4x4.TRS(oTransform.localPosition, oTransform.localRotation, oTransform.localScale);

            initialMouthMatrix = mouthpiece.worldToLocalMatrix;
            InitialParentMatrix = oTransform.parent.localToWorldMatrix;
            InitialParentMatrixWorldToLocal = oTransform.parent.worldToLocalMatrix;
            InitialTRS = Matrix4x4.TRS(oTransform.localPosition, oTransform.localRotation, oTransform.localScale);
            initialTransformMatrix = oTransform.localToWorldMatrix;

            rootScale = skinController.transform.localScale.x;

            controllers = new List<RigGoalController>();
            for (int i = 0; i < hierarchySize; i++)
            {
                controllers.Add(fullHierarchy[i].GetComponent<RigGoalController>());
            }

            if (mode == AnimationTool.PoseEditMode.FK || fullHierarchy.Count == 1)
            {

                movedObjects = new List<GameObject>() { oTransform.gameObject };
                startPositions = new List<Vector3>() { oTransform.localPosition };
                endPositions = new List<Vector3>() { oTransform.localPosition };

                startRotations = new List<Quaternion>() { oTransform.localRotation };
                endRotations = new List<Quaternion>() { oTransform.localRotation };

                startScales = new List<Vector3> { oTransform.localScale };
                endScales = new List<Vector3> { oTransform.localScale };

                fromRotation = Quaternion.FromToRotation(Vector3.forward, oTransform.localPosition) * Vector3.forward;
                if (hierarchySize > 2)
                {
                    initialRotation = fullHierarchy[hierarchySize - 2].localRotation;

                    movedObjects.Add(fullHierarchy[hierarchySize - 2].gameObject);
                    startPositions.Add(fullHierarchy[hierarchySize - 2].localPosition);
                    endPositions.Add(fullHierarchy[hierarchySize - 2].localPosition);

                    startRotations.Add(fullHierarchy[hierarchySize - 2].localRotation);
                    endRotations.Add(fullHierarchy[hierarchySize - 2].localRotation);

                    startScales.Add(fullHierarchy[hierarchySize - 2].localScale);
                    endScales.Add(fullHierarchy[hierarchySize - 2].localScale);
                }
            }
            else
            {

                movedObjects = new List<GameObject>();
                startPositions = new List<Vector3>();
                endPositions = new List<Vector3>();
                startRotations = new List<Quaternion>();
                endRotations = new List<Quaternion>();
                startScales = new List<Vector3>();
                endScales = new List<Vector3>();

                fullHierarchy.ForEach(x =>
                {
                    movedObjects.Add(x.gameObject);
                    startPositions.Add(x.localPosition);
                    endPositions.Add(x.localPosition);
                    startRotations.Add(x.localRotation);
                    endRotations.Add(x.localRotation);
                    startScales.Add(x.localScale);
                    endScales.Add(x.localScale);
                });
            }
        }



        public PoseManipulation(Transform objectTransform, RigController controller, Transform mouthpiece, RotationAxis axis)
        {
            MeshController = controller;
            oTransform = objectTransform;
            poseMode = AnimationTool.PoseEditMode.AC;
            switch (axis)
            {
                case RotationAxis.X:
                    acAxis = oTransform.right;
                    initForward = oTransform.up;
                    break;
                case RotationAxis.Y:
                    acAxis = oTransform.up;
                    initForward = oTransform.forward;
                    break;
                case RotationAxis.Z:
                    acAxis = oTransform.forward;
                    initForward = oTransform.right;
                    break;
            }
            previousAngle = Vector3.SignedAngle(initForward, mouthpiece.position - objectTransform.position, acAxis);

            movedObjects = new List<GameObject>() { oTransform.gameObject };
            startPositions = new List<Vector3>() { oTransform.localPosition };
            endPositions = new List<Vector3>() { oTransform.localPosition };
            startRotations = new List<Quaternion>() { oTransform.localRotation };
            endRotations = new List<Quaternion>() { oTransform.localRotation };
            startScales = new List<Vector3>() { oTransform.localScale };
            endScales = new List<Vector3>() { oTransform.localScale };
        }

        public void SetDestination(Transform mouthpiece)
        {
            Matrix4x4 transformation = mouthpiece.localToWorldMatrix * initialMouthMatrix;
            if (poseMode == AnimationTool.PoseEditMode.AC)
            {
                float currentAngle = Vector3.SignedAngle(initForward, mouthpiece.position - oTransform.position, acAxis);

                float angleOffset = Mathf.DeltaAngle(previousAngle, currentAngle);
                oTransform.Rotate(acAxis, angleOffset, Space.World);
                endRotations[0] = oTransform.localRotation;
                previousAngle = currentAngle;
                return;
            }
            if (poseMode == AnimationTool.PoseEditMode.FK || fullHierarchy.Count == 1)
            {

                Matrix4x4 transformed = InitialParentMatrixWorldToLocal *
                    transformation * InitialParentMatrix *
                    InitialTRS;
                Maths.DecomposeMatrix(transformed, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                targetPosition = position;
                targetRotation = rotation;
            }
            else
            {
                Matrix4x4 target = transformation * initialTransformMatrix;
                Maths.DecomposeMatrix(target, out Vector3 position, out Quaternion rotation, out Vector3 scale);
                targetPosition = position;
                targetRotation = rotation * Quaternion.Euler(-180, 0, 0);
            }
        }

        public bool TrySolver()
        {
            if (poseMode == AnimationTool.PoseEditMode.AC)
            {
                return true;
            }
            if (poseMode == AnimationTool.PoseEditMode.FK || fullHierarchy.Count == 1)
            {
                if (hierarchySize > 2)
                {
                    Vector3 to = Quaternion.FromToRotation(Vector3.forward, targetPosition) * Vector3.forward;
                    fullHierarchy[hierarchySize - 2].localRotation = initialRotation * Quaternion.FromToRotation(fromRotation, to);
                    endRotations[1] = fullHierarchy[hierarchySize - 2].localRotation;
                }
                else if (hierarchySize <= 2)
                {
                    oTransform.localPosition = targetPosition;
                    endPositions[0] = targetPosition;
                }
                oTransform.localRotation = targetRotation;
                endRotations[0] = targetRotation;
                return true;
            }
            else
            {
                Setup();
                if (Compute())
                    Apply();

                return true;
            }
        }

        public CommandMoveObjects GetCommand()
        {
            return new CommandMoveObjects(movedObjects, startPositions, startRotations, startScales, endPositions, endRotations, endScales);
        }

        public bool Setup()
        {
            // rotation curves * hierarchy size + root position curves
            p = 3 * hierarchySize + 3;
            theta = GetAllValues(p);

            currentState = new State()
            {
                position = MeshController.transform.InverseTransformPoint(oTransform.position),
                rotation = oTransform.rotation
            };
            desiredState = new State()
            {
                position = MeshController.transform.InverseTransformPoint(targetPosition),
                rotation = targetRotation
            };

            double[,] Js = ds_dtheta(p);

            double[,] DT_D = new double[p, p];
            //Root rotation
            for (int i = 0; i < 3; i++)
            {
                DT_D[i, i] = 1d;
            }
            //nonRoot rotation
            for (int i = 3; i < (hierarchySize - 1) * 3; i++)
            {
                if (fullHierarchy[i / 3].TryGetComponent<RigGoalController>(out RigGoalController controller))
                {
                    DT_D[i, i] = controller.stiffness;
                }
                else
                {
                    DT_D[i, i] = 1d;
                }
            }
            //root position
            for (int i = 0; i < 3; i++)
            {
                int j = 3 * hierarchySize + i;
                if (fullHierarchy[0].TryGetComponent<RigGoalController>(out RigGoalController controller))
                {
                    DT_D[i, i] = controller.stiffness;
                }
                else
                {
                    DT_D[j, j] = 1d;
                }
            }

            double[,] Delta_s_prime = new double[7, 1];
            for (int i = 0; i < 3; i++)
            {
                Delta_s_prime[i, 0] = desiredState.position[i] - currentState.position[i];
            }
            if ((currentState.rotation * Quaternion.Inverse(desiredState.rotation)).w < 0)
                desiredState.rotation = new Quaternion(-desiredState.rotation.x, -desiredState.rotation.y, -desiredState.rotation.z, -desiredState.rotation.w);
            for (int i = 0; i < 4; i++)
            {
                Delta_s_prime[i + 3, 0] = desiredState.rotation[i] - currentState.rotation[i];
            }

            double wm = 20;
            double wd = 50;

            Q_opt = Maths.Add(Maths.Add(Maths.Multiply(2d * wm, Maths.Multiply(Maths.Transpose(Js), Js)), Maths.Multiply(2d * wd, DT_D)), Maths.Multiply((double)Mathf.Pow(10, -6), Maths.Identity(p)));

            double[,] B_opt = Maths.Multiply(-2d * wm, Maths.Multiply(Maths.Transpose(Js), Delta_s_prime));
            b_opt = Maths.ArrayToColumnArray(B_opt);

            lowerBound = InitializeUBound(p);
            upperBound = InitializeVBound(p);

            delta_theta_0 = new double[p];

            s = new double[p];
            for (int i = 0; i < p; i++)
            {
                s[i] = 1d;
                delta_theta_0[i] = 0d;
            }

            return true;
        }

        public bool Compute()
        {
            alglib.minqpstate state_opt;
            alglib.minqpreport rep;

            alglib.minqpcreate(p, out state_opt);
            alglib.minqpsetquadraticterm(state_opt, Q_opt);
            alglib.minqpsetlinearterm(state_opt, b_opt);
            alglib.minqpsetstartingpoint(state_opt, delta_theta_0);
            alglib.minqpsetbc(state_opt, lowerBound, upperBound);
            alglib.minqpsetscale(state_opt, s);

            alglib.minqpsetalgobleic(state_opt, 0.0, 0.0, 0.0, 0);
            alglib.minqpoptimize(state_opt);
            alglib.minqpresults(state_opt, out delta_theta, out rep);

            return true;
        }

        private bool Apply()
        {
            double[] new_theta = new double[p];
            for (int i = 0; i < 3; i++)
            {
                int j = 3 * hierarchySize + i;
                new_theta[j] = delta_theta[j] + theta[j];
            }
            for (int l = 0; l < hierarchySize; l++)
            {
                Transform currentTransform = fullHierarchy[l];
                int i = l * 3;
                currentTransform.localRotation *= Quaternion.Euler((float)delta_theta[i], (float)delta_theta[i + 1], (float)delta_theta[i + 2]);
            }
            Transform rootTransform = fullHierarchy[0];
            int k = hierarchySize * 3;
            rootTransform.localPosition = new Vector3((float)new_theta[k], (float)new_theta[k + 1], (float)new_theta[k + 2]);

            for (int c = 0; c < fullHierarchy.Count; c++)
            {
                endPositions[c] = fullHierarchy[c].localPosition;
                endRotations[c] = fullHierarchy[c].localRotation;
                endScales[c] = fullHierarchy[c].localScale;
            }

            return true;
        }

        private double[] GetAllValues(int p)
        {
            double[] theta = new double[p];
            for (int l = 0; l < hierarchySize; l++)
            {
                Transform currentTransform = fullHierarchy[l];
                theta[3 * l + 0] = Mathf.DeltaAngle(0, currentTransform.localEulerAngles.x);
                theta[3 * l + 1] = Mathf.DeltaAngle(0, currentTransform.localEulerAngles.y);
                theta[3 * l + 2] = Mathf.DeltaAngle(0, currentTransform.localEulerAngles.z);
            }
            Transform root = fullHierarchy[0];
            theta[3 * hierarchySize + 0] = root.localPosition.x;
            theta[3 * hierarchySize + 1] = root.localPosition.y;
            theta[3 * hierarchySize + 2] = root.localPosition.z;
            return theta;
        }

        double[,] ds_dtheta(int p)
        {
            double[,] Js = new double[7, p];
            float dtheta = 1;

            for (int l = 0; l < hierarchySize; l++)
            {
                Transform currentTransform = fullHierarchy[l];
                Quaternion currentRotation = currentTransform.localRotation;
                for (int i = 0; i < 3; i++)
                {
                    Vector3 rotation = Vector3.zero;
                    rotation[i] = 1;
                    currentTransform.localRotation *= Quaternion.Euler(rotation);

                    Vector3 plusPosition = MeshController.transform.InverseTransformPoint(oTransform.position);
                    Quaternion plusRotation = oTransform.rotation;

                    currentTransform.localRotation = currentRotation;

                    Vector3 minusPosition = MeshController.transform.InverseTransformPoint(oTransform.position);
                    Quaternion minusRotation = oTransform.rotation;

                    int col = 3 * l + i;

                    Js[0, col] = (plusPosition.x - minusPosition.x) * dtheta;
                    Js[1, col] = (plusPosition.y - minusPosition.y) * dtheta;
                    Js[2, col] = (plusPosition.z - minusPosition.z) * dtheta;
                    Js[3, col] = (plusRotation.x - minusRotation.x) * dtheta;
                    Js[4, col] = (plusRotation.y - minusRotation.y) * dtheta;
                    Js[5, col] = (plusRotation.z - minusRotation.z) * dtheta;
                    Js[6, col] = (plusRotation.w - minusRotation.w) * dtheta;
                }
            }
            dtheta = GlobalState.Instance.dthetaRoot;
            Transform rootTransform = fullHierarchy[0];
            Vector3 rootPosition = rootTransform.localPosition;
            for (int i = 0; i < 3; i++)
            {
                rootPosition[i] += 0.1f;
                rootTransform.localPosition = rootPosition;

                Vector3 plusPosition = MeshController.transform.InverseTransformPoint(oTransform.position);
                Quaternion plusRotation = oTransform.rotation;

                rootPosition[i] -= 0.1f;
                rootTransform.localPosition = rootPosition;

                Vector3 minusPosition = MeshController.transform.InverseTransformPoint(oTransform.position);
                Quaternion minusRotation = oTransform.rotation;

                int col = 3 * hierarchySize + i;

                Js[0, col] = (plusPosition.x - minusPosition.x) * dtheta;
                Js[1, col] = (plusPosition.y - minusPosition.y) * dtheta;
                Js[2, col] = (plusPosition.z - minusPosition.z) * dtheta;
                Js[3, col] = (plusRotation.x - minusRotation.x) * dtheta;
                Js[4, col] = (plusRotation.y - minusRotation.y) * dtheta;
                Js[5, col] = (plusRotation.z - minusRotation.z) * dtheta;
                Js[6, col] = (plusRotation.w - minusRotation.w) * dtheta;
            }
            return Js;
        }



        double[] InitializeUBound(int n)
        {
            double[] u = new double[n];
            for (int i = 0; i < hierarchySize; i++)
            {
                Quaternion currentRotation = fullHierarchy[i].localRotation;
                int index = i * 3;
                Vector3 rotBounds = rotationBounds(currentRotation, controllers[i].LowerAngleBound);
                u[index] = Mathf.Min(0, rotBounds[0]);
                u[index + 1] = Mathf.Min(0, rotBounds[1]);
                u[index + 2] = Mathf.Min(0, rotBounds[2]);
            }
            for (int j = 0; j < 3; j++)
            {
                int index = hierarchySize * 3 + j;
                u[index] = -10;
            }
            return u;
        }

        double[] InitializeVBound(int n)
        {
            double[] v = new double[n];
            for (int i = 0; i < hierarchySize; i++)
            {
                Quaternion currentRotation = fullHierarchy[i].localRotation;
                int index = i * 3;
                Vector3 rotBounds = rotationBounds(currentRotation, controllers[i].UpperAngleBound);
                v[index] = Mathf.Max(0, rotBounds[0]);
                v[index + 1] = Mathf.Max(0, rotBounds[1]);
                v[index + 2] = Mathf.Max(0, rotBounds[2]);
            }
            for (int j = 0; j < 3; j++)
            {
                int index = hierarchySize * 3 + j;
                v[index] = 10;
            }
            return v;
        }


        public Vector3 rotationBounds(Quaternion rotation, Vector3 bound)
        {
            Vector3 result = new Vector3();
            rotation.x /= rotation.w;
            rotation.y /= rotation.w;
            rotation.z /= rotation.w;
            rotation.w = 1;

            float angleX = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);
            result.x = -(angleX) + bound.x;

            float angleY = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.y);
            result.y = -(angleY) + bound.y;

            float angleZ = 2f * Mathf.Rad2Deg * Mathf.Atan(rotation.z);
            result.z = -(angleZ) + bound.z;

            return result;
        }

    }
}
