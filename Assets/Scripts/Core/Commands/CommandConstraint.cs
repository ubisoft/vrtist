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
