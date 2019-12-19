using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandRemoveFromSelection : ICommand
    {
        List<GameObject> objects = new List<GameObject>();

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
            if(objects.Count > 0)
                CommandManager.AddCommand(this);
        }

        public override void Serialize(SceneSerializer serializer)
        {

        }

    }
}