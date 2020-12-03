using UnityEngine;
using UnityEngine.Animations;

namespace VRtist
{
    public enum ConstraintType
    {
        Parent,
        LookAt
    }

    public static class ConstraintUtility
    {
        public static IConstraint GetConstraint(ConstraintType constraintType, GameObject gobject)
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
            ParametersController parametersController = gobject.GetComponent<ParametersController>();
            if(null != parametersController)
                parametersController.DisconnectWorldScale();

            T constraint = gobject.GetComponent<T>();
            GameObject.Destroy(constraint);
            GlobalState.FireObjectConstraint(gobject);
        }

        public static void UpdateParentConstraintTranslationOffset(ParentConstraint constraint, Vector3 initOffset, float initScale)
        {
            Vector3 offset = initOffset * GlobalState.WorldScale / initScale;
            constraint.SetTranslationOffset(0, offset);
        }

        public static void AddParentConstraint(GameObject gobject, GameObject target)
        {
            ParentConstraint constraint = gobject.GetComponent<ParentConstraint>();
            if (null == constraint)
            {
                constraint = gobject.AddComponent<ParentConstraint>();
                ParametersController parametersController = gobject.GetComponent<ParametersController>();
                if(null == parametersController)
                {
                    parametersController = gobject.AddComponent<ParametersController>();
                }
                parametersController.initParentConstraintScale = GlobalState.WorldScale;
                parametersController.initParentConstraintOffset = Vector3.Scale(target.transform.InverseTransformPoint(gobject.transform.position), target.transform.lossyScale);

                parametersController.ConnectWorldScale();
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
            source.sourceTransform = target.transform;
            source.weight = 1f;
            constraint.SetSource(0, source);

            constraint.constraintActive = true;

            GlobalState.FireObjectConstraint(gobject);
        }
    }

    public class CommandAddConstraint : ICommand
    {
        ConstraintType constraintType;
        GameObject gobject;
        GameObject target;

        public CommandAddConstraint(ConstraintType constraintType, GameObject gobject, GameObject target)
        {
            this.constraintType = constraintType;
            this.gobject = gobject;
            this.target = target;
        }

        public override void Undo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintUtility.RemoveConstraint<ParentConstraint>(gobject);
                    MixerClient.GetInstance().SendRemoveParentConstraint(gobject);
                    break;
                case ConstraintType.LookAt:
                    ConstraintUtility.RemoveConstraint<LookAtConstraint>(gobject);
                    MixerClient.GetInstance().SendRemoveLookAtConstraint(gobject);
                    break;
            }
        }

        public override void Redo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintUtility.AddParentConstraint(gobject, target);
                    MixerClient.GetInstance().SendAddParentConstraint(gobject, target);
                    break;
                case ConstraintType.LookAt:
                    ConstraintUtility.AddLookAtConstraint(gobject, target);
                    MixerClient.GetInstance().SendAddLookAtConstraint(gobject, target);
                    break;
            }
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }

    public class CommandRemoveConstraint : ICommand
    {
        ConstraintType constraintType;
        GameObject gobject;
        GameObject target;

        public CommandRemoveConstraint(ConstraintType constraintType, GameObject gobject)
        {
            this.constraintType = constraintType;
            this.gobject = gobject;
            IConstraint constraint = ConstraintUtility.GetConstraint(constraintType, gobject);
            if (null != constraint && constraint.sourceCount > 0)
            {
                target = constraint.GetSource(0).sourceTransform.gameObject;
            }
        }

        public override void Redo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintUtility.RemoveConstraint<ParentConstraint>(gobject);
                    MixerClient.GetInstance().SendRemoveParentConstraint(gobject);
                    break;
                case ConstraintType.LookAt:
                    ConstraintUtility.RemoveConstraint<LookAtConstraint>(gobject);
                    MixerClient.GetInstance().SendRemoveLookAtConstraint(gobject);
                    break;
            }
        }

        public override void Undo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintUtility.AddParentConstraint(gobject, target);
                    MixerClient.GetInstance().SendAddParentConstraint(gobject, target);
                    break;
                case ConstraintType.LookAt:
                    ConstraintUtility.AddLookAtConstraint(gobject, target);
                    MixerClient.GetInstance().SendAddLookAtConstraint(gobject, target);
                    break;
            }
        }

        public override void Submit()
        {
            if (null == target) { return; }
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
