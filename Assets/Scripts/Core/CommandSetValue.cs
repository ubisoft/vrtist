using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VRtist
{
    public class CommandSetValue<T> : ICommand
    {
        Dictionary<int, T> oldValues = new Dictionary<int, T>();
        T newValue;
        string objectPath;
        string componentName;
        string fieldName;

        public CommandSetValue(string commandName, string propertyPath)
        {
            name = commandName;            
            ICommand.SplitPropertyPath(propertyPath, out objectPath, out componentName, out fieldName);

            foreach (var selectedItem in Selection.selection)
            {
                GameObject gObject = objectPath.Length > 0 ? selectedItem.Value.transform.Find(objectPath).gameObject : selectedItem.Value;
                Component component = gObject.GetComponent(componentName);

                FieldInfo fieldInfo = component.GetType().GetField(fieldName);
                oldValues[selectedItem.Key] = (T)fieldInfo.GetValue(component);
            }
        }

        public override void Submit()
        {
            foreach (var selectedItem in Selection.selection)
            {
                GameObject gObject = objectPath.Length > 0 ? selectedItem.Value.transform.Find(objectPath).gameObject : selectedItem.Value;
                Component component = gObject.GetComponent(componentName);
                FieldInfo fieldInfo = component.GetType().GetField(fieldName);

                newValue = (T)fieldInfo.GetValue(component);
                break;
            }

            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            foreach (var keyValuePair in oldValues)
            {
                GameObject selectedGameObject = Selection.selection[keyValuePair.Key];
                GameObject gObject = objectPath.Length > 0 ? selectedGameObject.transform.Find(objectPath).gameObject : selectedGameObject;
                Component component = gObject.GetComponent(componentName);
                FieldInfo fieldInfo = component.GetType().GetField(fieldName);
                fieldInfo.SetValue(component, keyValuePair.Value);
            }
            //OnValueChange();
        }
        public override void Redo()
        {
            foreach (var selectedItem in Selection.selection)
            {
                GameObject selectedGameObject = selectedItem.Value;
                GameObject gObject = objectPath.Length > 0 ? selectedGameObject.transform.Find(objectPath).gameObject : selectedGameObject;
                Component component = gObject.GetComponent(componentName);
                FieldInfo fieldInfo = component.GetType().GetField(fieldName);

                fieldInfo.SetValue(component, newValue);
            }
            //OnValueChange();
        }

        public override void Serialize(SceneSerializer serializer)
        {

        }


    }

}