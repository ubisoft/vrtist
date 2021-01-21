using UnityEngine;
using UnityEngine.Animations;

namespace VRtist
{
    /// <summary>
    /// Command to add a constraint on an object.
    /// </summary>
    public class CommandAddConstraint : ICommand
    {
        readonly ConstraintType constraintType;
        readonly GameObject gobject;
        readonly GameObject target;

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
                    ConstraintManager.RemoveConstraint<ParentConstraint>(gobject);
                    MixerClient.Instance.SendRemoveParentConstraint(gobject);
                    break;
                case ConstraintType.LookAt:
                    ConstraintManager.RemoveConstraint<LookAtConstraint>(gobject);
                    MixerClient.Instance.SendRemoveLookAtConstraint(gobject);
                    break;
            }
        }

        public override void Redo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintManager.AddParentConstraint(gobject, target);
                    MixerClient.Instance.SendAddParentConstraint(gobject, target);
                    break;
                case ConstraintType.LookAt:
                    ConstraintManager.AddLookAtConstraint(gobject, target);
                    MixerClient.Instance.SendAddLookAtConstraint(gobject, target);
                    break;
            }
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }

    /// <summary>
    /// Command to remove a constraint of an object.
    /// </summary>
    public class CommandRemoveConstraint : ICommand
    {
        readonly ConstraintType constraintType;
        readonly GameObject gobject;
        readonly GameObject target;

        public CommandRemoveConstraint(ConstraintType constraintType, GameObject gobject)
        {
            this.constraintType = constraintType;
            this.gobject = gobject;
            Component component = ConstraintManager.GetConstraint(constraintType, gobject);
            if (null != component)
            {
                IConstraint constraint = component as IConstraint;
                if (constraint.sourceCount > 0)
                    target = constraint.GetSource(0).sourceTransform.gameObject;
            }
        }

        public override void Redo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintManager.RemoveConstraint<ParentConstraint>(gobject);
                    MixerClient.Instance.SendRemoveParentConstraint(gobject);
                    break;
                case ConstraintType.LookAt:
                    ConstraintManager.RemoveConstraint<LookAtConstraint>(gobject);
                    MixerClient.Instance.SendRemoveLookAtConstraint(gobject);
                    break;
            }
        }

        public override void Undo()
        {
            switch (constraintType)
            {
                case ConstraintType.Parent:
                    ConstraintManager.AddParentConstraint(gobject, target);
                    MixerClient.Instance.SendAddParentConstraint(gobject, target);
                    break;
                case ConstraintType.LookAt:
                    ConstraintManager.AddLookAtConstraint(gobject, target);
                    MixerClient.Instance.SendAddLookAtConstraint(gobject, target);
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
