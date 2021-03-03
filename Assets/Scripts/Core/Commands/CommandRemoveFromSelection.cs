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
    /// Command to remove a list of objects from the current selection.
    /// </summary>
    public class CommandRemoveFromSelection : ICommand
    {
        readonly List<GameObject> objects = new List<GameObject>();

        public CommandRemoveFromSelection(GameObject selectedObject)
        {
            objects = new List<GameObject>
            {
                selectedObject
            };
        }

        public CommandRemoveFromSelection(List<GameObject> selectedObjects)
        {
            objects = selectedObjects;
        }

        public override void Undo()
        {
            foreach (GameObject o in objects)
            {
                if (null == o) { continue; }
                Selection.AddToSelection(o);
            }
        }

        public override void Redo()
        {
            foreach (GameObject o in objects)
            {
                if (null == o) { continue; }
                Selection.RemoveFromSelection(o);
            }
        }

        public override void Submit()
        {
            if (objects.Count > 0)
                CommandManager.AddCommand(this);
        }
    }
}
