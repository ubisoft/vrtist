using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandImportGeometry : ICommand
    {
        Transform parent = null;
        GameObject importedObject = null;
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;

        public CommandImportGeometry(string commandName, GameObject root)
        {
            name = commandName;
            importedObject = root;
            parent = root.transform.parent;
        }

        private GameObject GetOrCreateTrash()
        {
            string trashName = "__Trash__";
            GameObject trash = GameObject.Find(trashName);
            if (!trash)
            {
                trash = new GameObject();
                trash.name = trashName;
            }
            return trash;
        }

        public override void Undo()
        {
            importedObject.SetActive(false);
            importedObject.transform.parent = GetOrCreateTrash().transform;
        }

        public override void Redo()
        {
            importedObject.transform.parent = parent;
            importedObject.transform.localPosition = position;
            importedObject.transform.localRotation = rotation;
            importedObject.transform.localScale = scale;
            importedObject.SetActive(true);
        }

        public override void Submit()
        {
            importedObject.SetActive(true);
            position = importedObject.transform.localPosition;
            rotation = importedObject.transform.localRotation;
            scale = importedObject.transform.localScale;
            CommandManager.AddCommand(this);
        }
    }

}