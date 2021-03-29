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

namespace VRtist
{
    /// <summary>
    /// Command to remove (delete) an object from the scene.
    /// </summary>
    public class CommandRemoveGameObject : CommandAddRemoveGameObject
    {
        public CommandRemoveGameObject(GameObject o) : base(o) { }

        public override void Undo()
        {
            if (null == gObject) { return; }
            SceneManager.RestoreObject(gObject, parent);
        }

        public override void Redo()
        {
            if (null == gObject) { return; }
            SceneManager.RemoveObject(gObject);
        }

        public override void Submit()
        {
            ParametersController controller = gObject.GetComponent<ParametersController>();
            if (null != controller && !controller.IsDeletable())
                return;

            ToolsUIManager.Instance.SpawnDeleteInstanceVFX(gObject);

            CommandGroup constraintGroup = null;
            List<Constraint> constraints = ConstraintManager.GetObjectConstraints(gObject);
            if (constraints.Count > 0)
            {
                constraintGroup = new CommandGroup("Constraints");
                foreach (Constraint constraint in constraints)
                {
                    new CommandRemoveConstraint(constraint.constraintType, constraint.gobject).Submit();
                }
            }

            Redo();
            CommandManager.AddCommand(this);

            if (null != constraintGroup)
                constraintGroup.Submit();
        }
    }
}
