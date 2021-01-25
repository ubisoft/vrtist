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
