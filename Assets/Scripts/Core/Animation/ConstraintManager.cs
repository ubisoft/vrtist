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

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Animations;

namespace VRtist
{
    public enum ConstraintType
    {
        Parent,
        LookAt,
        Unknown,
    }

    /// <summary>
    /// Internal constraint representation.
    /// </summary>
    public class Constraint
    {
        public GameObject gobject;
        public Transform target;
        public ConstraintType constraintType;
        public GameObject lineGameObject = null;
        public LineRenderer lineRenderer = null;
    }

    /// <summary>
    /// Manage all constraints of the scene and their display.
    /// </summary>
    public class ConstraintManager : MonoBehaviour
    {
        static readonly List<Constraint> constraints = new List<Constraint>();

        // Update is called once per frame
        void LateUpdate()
        {
            UpdateConstraintVisualization(gameObject);
        }

        public static List<Constraint> GetObjectConstraints(GameObject gobject)
        {
            List<Constraint> objectConstraints = new List<Constraint>();
            foreach (Constraint constraint in constraints)
            {
                if (constraint.gobject == gobject || constraint.target == gobject.transform)
                {
                    objectConstraints.Add(constraint);
                }
            }
            return objectConstraints;
        }

        public static bool FindConstraint(GameObject gobject, ConstraintType constraintType, out Constraint constraint, out int index)
        {
            index = -1;
            constraint = null;
            foreach (Constraint c in constraints)
            {
                index++;
                if (c.gobject == gobject && c.constraintType == constraintType)
                {
                    constraint = c;
                    return true;
                }
            }
            return false;
        }

        public static int GetConstraintIndex(Constraint constraint)
        {
            return constraints.IndexOf(constraint);
        }

        public static Component GetConstraint(ConstraintType constraintType, GameObject gobject)
        {
            switch (constraintType)
            {
                case ConstraintType.Parent: return gobject.GetComponent<ParentConstraint>();
                case ConstraintType.LookAt: return gobject.GetComponent<LookAtConstraint>();
                case ConstraintType.Unknown: break;
            }
            return null;
        }

        public static bool IsLocked(GameObject gobject)
        {
            ParentConstraint parentConstraint = gobject.GetComponent<ParentConstraint>();
            return null != parentConstraint;
        }

        public static List<Constraint> GetAllConstraints()
        {
            return constraints;
        }

        public static void RemoveConstraint<T>(GameObject gobject) where T : UnityEngine.Component
        {
            T component = gobject.GetComponent<T>();
            IConstraint constraint = component as IConstraint;

            ParametersController parametersController = gobject.GetComponent<ParametersController>();
            if (null != parametersController)
            {
                ConstraintType constraintType = ConstraintType.Unknown;
                switch (component)
                {
                    case ParentConstraint _:
                        constraintType = ConstraintType.Parent;
                        break;
                    case LookAtConstraint _:
                        constraintType = ConstraintType.LookAt;
                        break;
                }

                foreach (Constraint con in constraints)
                {
                    if (con.gobject == gobject && con.constraintType == constraintType)
                    {
                        GameObject.Destroy(con.lineGameObject);
                        constraints.Remove(con);
                        break;
                    }
                }
            }

            GameObject source = constraint.GetSource(0).sourceTransform.gameObject;
            ParametersController sourceParametersController = source.GetComponent<ParametersController>();
            if (null != sourceParametersController)
            {
                sourceParametersController.RemoveConstraintHolder(gobject);
            }

            constraint.RemoveSource(0);
            GameObject.Destroy(component);
            GlobalState.FireObjectConstraint(gobject);
        }

        public static void UpdateConstraintVisualization(GameObject constraintVisualization)
        {
            foreach (Constraint constraint in constraints)
            {
                GameObject lineGameObject = constraint.lineGameObject;
                if (null == lineGameObject)
                {
                    constraint.lineGameObject = new GameObject();
                    lineGameObject = constraint.lineGameObject;
                    lineGameObject.layer = LayerMask.NameToLayer("CameraHidden");
                    lineGameObject.transform.parent = constraintVisualization.transform;
                }
                LineRenderer lineRenderer = constraint.lineRenderer;
                if (null == lineRenderer)
                {
                    constraint.lineRenderer = lineGameObject.AddComponent<LineRenderer>();
                    ConstraintLineController controller = lineGameObject.AddComponent<ConstraintLineController>();
                    lineGameObject.name = "line";
                    lineRenderer = constraint.lineRenderer;
                    lineRenderer.positionCount = 2;
                    lineRenderer.material = Resources.Load<Material>("Materials/Dash");
                    controller.SetGizmoVisible(GlobalState.Instance.settings.DisplayGizmos);
                }
                lineRenderer.SetPosition(0, constraint.gobject.transform.position);
                lineRenderer.SetPosition(1, constraint.target.position);
                lineRenderer.startWidth = 0.001f / GlobalState.WorldScale;
                lineRenderer.endWidth = 0.001f / GlobalState.WorldScale;
            }
        }

        public static void AddConstraint(GameObject source, GameObject target, ConstraintType type, int index = -1)
        {
            switch (type)
            {
                case ConstraintType.Parent: AddParentConstraint(source, target, index); break;
                case ConstraintType.LookAt: AddLookAtConstraint(source, target, index); break;
            }
        }
        public static void InsertConstraint(int index, Constraint constraint)
        {
            AddConstraint(constraint.gobject, constraint.target.gameObject, constraint.constraintType, index);
        }

        public static void AddParentConstraint(GameObject gobject, GameObject target, int index = -1)
        {
            ParentConstraint constraint = gobject.GetComponent<ParentConstraint>();
            if (null == constraint)
            {
                constraint = gobject.AddComponent<ParentConstraint>();
                ParametersController parametersController = gobject.GetComponent<ParametersController>();
                if (null == parametersController)
                {
                    gobject.AddComponent<ParametersController>();
                }
                Constraint newConstraint = new Constraint { gobject = gobject, target = target.transform, constraintType = ConstraintType.Parent };
                if (index == -1)
                    constraints.Add(newConstraint);
                else
                    constraints.Insert(index, newConstraint);
                ParametersController targetParametersController = target.GetComponent<ParametersController>();
                if (null == targetParametersController)
                {
                    targetParametersController = target.AddComponent<ParametersController>();
                }
                targetParametersController.AddConstraintHolder(gobject);
            }
            else
            {
                // update visual target for LineRenderer
                foreach (Constraint c in constraints)
                {
                    if (c.gobject == gobject && c.constraintType == ConstraintType.Parent)
                    {
                        c.target = target.transform;
                        break;
                    }
                }
            }

            ConstraintSource source;
            if (constraint.sourceCount == 0)
            {
                source = new ConstraintSource();
                constraint.AddSource(source);
            }
            else
            {
                source = constraint.GetSource(0);
            }
            source.sourceTransform = target.transform;
            source.weight = 1f;
            constraint.SetSource(0, source);

            constraint.translationAtRest = gobject.transform.localPosition;
            constraint.rotationAtRest = gobject.transform.localRotation.eulerAngles;

            Vector3 offset = Vector3.Scale(target.transform.InverseTransformPoint(gobject.transform.position), target.transform.lossyScale);
            constraint.SetTranslationOffset(0, offset);

            Quaternion quat = Quaternion.Inverse(target.transform.rotation) * gobject.transform.rotation;
            constraint.SetRotationOffset(0, quat.eulerAngles);

            constraint.constraintActive = true;

            GlobalState.FireObjectConstraint(gobject);
        }

        public static void AddLookAtConstraint(GameObject gobject, GameObject target, int index = -1)
        {
            LookAtConstraint constraint = gobject.GetComponent<LookAtConstraint>();
            if (null == constraint)
            {
                constraint = gobject.AddComponent<LookAtConstraint>();
                ParametersController parametersController = gobject.GetComponent<ParametersController>();
                if (null == parametersController)
                {
                    gobject.AddComponent<ParametersController>();
                }
                Constraint newConstraint = new Constraint { gobject = gobject, target = target.transform, constraintType = ConstraintType.LookAt };
                if (index == -1)
                    constraints.Add(newConstraint);
                else
                    constraints.Insert(index, newConstraint);

                ParametersController targetParametersController = target.GetComponent<ParametersController>();
                if (null == targetParametersController)
                {
                    targetParametersController = target.AddComponent<ParametersController>();
                }
                targetParametersController.AddConstraintHolder(gobject);
            }
            else
            {
                // update visual target for LineRenderer
                foreach (Constraint c in constraints)
                {
                    if (c.gobject == gobject && c.constraintType == ConstraintType.LookAt)
                    {
                        c.target = target.transform;
                        break;
                    }
                }
            }
            ConstraintSource source;
            if (constraint.sourceCount == 0)
            {
                source = new ConstraintSource();
                constraint.AddSource(source);
            }
            else
            {
                source = constraint.GetSource(0);
            }

            source.sourceTransform = target.transform;
            source.weight = 1f;
            constraint.SetSource(0, source);
            constraint.rotationOffset = new Vector3(0, 180, 0);

            constraint.constraintActive = true;

            GlobalState.FireObjectConstraint(gobject);
        }

        public static void Clear()
        {
            for (int i = constraints.Count - 1; i >= 0; --i)
            {
                Constraint constraint = constraints[i];
                switch (constraint.constraintType)
                {
                    case ConstraintType.Parent: RemoveConstraint<ParentConstraint>(constraint.gobject); break;
                    case ConstraintType.LookAt: RemoveConstraint<LookAtConstraint>(constraint.gobject); break;
                }
            }

        }
    }
}