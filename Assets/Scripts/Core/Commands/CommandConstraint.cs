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
            SceneManager.RemoveObjectConstraint(gobject, constraintType);
        }

        public override void Redo()
        {
            SceneManager.AddObjectConstraint(gobject, constraintType, target);
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
            SceneManager.RemoveObjectConstraint(gobject, constraintType);
        }

        public override void Undo()
        {
            SceneManager.AddObjectConstraint(gobject, constraintType, target);
        }

        public override void Submit()
        {
            if (null == target) { return; }
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
