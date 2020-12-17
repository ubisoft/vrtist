using System.Collections;
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

    public class Constraint
    {
        public GameObject gobject;
        public Transform target;
        public ConstraintType constraintType;
        public GameObject lineGameObject = null;
        public LineRenderer lineRenderer = null;
    }

    public class ConstraintManager : MonoBehaviour
    {
        public static List<Constraint> constraints = new List<Constraint>();

        // Update is called once per frame
        void Update()
        {
            UpdateConstraintVisualization(gameObject);
        }

        public static Component GetConstraint(ConstraintType constraintType, GameObject gobject)
        {
            switch (constraintType)
            {
                case ConstraintType.Parent: return gobject.GetComponent<ParentConstraint>();
                case ConstraintType.LookAt: return gobject.GetComponent<LookAtConstraint>();
            }
            return null;
        }

        public static bool IsLocked(GameObject gobject)
        {
            ParentConstraint parentConstraint = gobject.GetComponent<ParentConstraint>();
            return null != parentConstraint;
        }

        public static void RemoveConstraint<T>(GameObject gobject) where T : UnityEngine.Object
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

            constraint.RemoveSource(0);
            GameObject.Destroy(component);
            GlobalState.FireObjectConstraint(gobject);
        }

        public static void UpdateConstraintVisualization(GameObject constraintVisualization)
        {
            foreach (Constraint constraint in constraints)
            {
                GameObject lineGameObject = constraint.lineGameObject;
                if(null == lineGameObject)
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
                    lineRenderer = constraint.lineRenderer;
                    lineRenderer.positionCount = 2;
                    lineRenderer.material = Resources.Load<Material>("Materials/Dash");
                }
                lineRenderer.SetPosition(0, constraint.gobject.transform.position);
                lineRenderer.SetPosition(1, constraint.target.position);
                lineRenderer.startWidth = 0.001f / GlobalState.WorldScale;
                lineRenderer.endWidth = 0.001f / GlobalState.WorldScale;
            }
        }

        public static void AddParentConstraint(GameObject gobject, GameObject target)
        {
            ParentConstraint constraint = gobject.GetComponent<ParentConstraint>();
            if (null == constraint)
            {
                constraint = gobject.AddComponent<ParentConstraint>();
                ParametersController parametersController = gobject.GetComponent<ParametersController>();
                if (null == parametersController)
                {
                    parametersController = gobject.AddComponent<ParametersController>();
                }
                constraints.Add(new Constraint { gobject = gobject, target = target.transform, constraintType = ConstraintType.Parent });
                parametersController.initParentConstraintScale = GlobalState.WorldScale;
                parametersController.initParentConstraintOffset = Vector3.Scale(target.transform.InverseTransformPoint(gobject.transform.position), target.transform.lossyScale);
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

        public static void AddLookAtConstraint(GameObject gobject, GameObject target)
        {
            LookAtConstraint constraint = gobject.GetComponent<LookAtConstraint>();
            if (null == constraint)
            {
                constraint = gobject.AddComponent<LookAtConstraint>();
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

            ParametersController parametersController = gobject.GetComponent<ParametersController>();
            if (null == parametersController)
            {
                parametersController = gobject.AddComponent<ParametersController>();
            }
            constraints.Add(new Constraint { gobject = gobject, target = target.transform, constraintType = ConstraintType.LookAt });
            source.sourceTransform = target.transform;
            source.weight = 1f;
            constraint.SetSource(0, source);
            constraint.rotationOffset = new Vector3(0, 180, 0);

            constraint.constraintActive = true;

            GlobalState.FireObjectConstraint(gobject);
        }
    }
}