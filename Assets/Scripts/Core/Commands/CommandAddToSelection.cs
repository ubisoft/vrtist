using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddToSelection : ICommand
    {
        List<GameObject> objects = new List<GameObject>();

        public CommandAddToSelection(GameObject selectedObject)
        {
            objects = new List<GameObject>();
            objects.Add(selectedObject);
        }

        public CommandAddToSelection(List<GameObject> selectedObjects)
        {
            objects = selectedObjects;
        }
        public override void Undo()
        {
            foreach (GameObject o in objects)
            {
                if (null == o) { continue; }
                Selection.RemoveFromSelection(o);
            }
        }
        public override void Redo()
        {
            foreach (GameObject o in objects)
            {
                if (null == o) { continue; }
                Selection.AddToSelection(o);
            }
        }
        public override void Submit()
        {
            if (objects.Count > 0)
                CommandManager.AddCommand(this);
        }
    }
}
