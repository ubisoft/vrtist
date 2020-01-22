using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VRtist
{
    public class CommandSetValue<T> : ICommand
    {
        Dictionary<GameObject, T> oldValues = new Dictionary<GameObject, T>();

        T newValue;
        string objectPath;
        string componentName;
        string fieldName;

        public T GetValue(Component component, string fieldName)
        {
            object inst = component;
            string[] fields = fieldName.Split('.');

            FieldInfo fieldInfo = component.GetType().GetField(fields[0]);

            for (int i = 1; i < fields.Length; i++)
            {
                inst = fieldInfo.GetValue(inst);
                fieldInfo = inst.GetType().GetField(fields[i]);
            }
            return (T)fieldInfo.GetValue(inst);
        }

        public void SetValue(Component component, string fieldName, T value)
        {
            object inst = component;
            string[] fields = fieldName.Split('.');

            FieldInfo fieldInfo = component.GetType().GetField(fields[0]);

            for (int i = 1; i < fields.Length; i++)
            {
                inst = fieldInfo.GetValue(inst);
                fieldInfo = inst.GetType().GetField(fields[i]);                
            }
            fieldInfo.SetValue(inst, value);
        }

        public CommandSetValue(string commandName, string propertyPath)
        {
            name = commandName;            
            ICommand.SplitPropertyPath(propertyPath, out objectPath, out componentName, out fieldName);

            foreach (var selectedItem in Selection.selection)
            {
                GameObject gObject = objectPath.Length > 0 ? selectedItem.Value.transform.Find(objectPath).gameObject : selectedItem.Value;
                Component component = gObject.GetComponent(componentName);

                if (null == component)
                    continue;

                oldValues[gObject] = GetValue(component, fieldName);
            }
        }

        public override void Submit()
        {
            foreach (var selectedItem in Selection.selection)
            {
                GameObject gObject = objectPath.Length > 0 ? selectedItem.Value.transform.Find(objectPath).gameObject : selectedItem.Value;
                Component component = gObject.GetComponent(componentName);
                if (null == component)
                    continue;

                newValue = GetValue(component, fieldName);
                break;
            }

            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            foreach (var keyValuePair in oldValues)
            {
                GameObject gObject = objectPath.Length > 0 ? keyValuePair.Key.transform.Find(objectPath).gameObject : keyValuePair.Key;
                Component component = gObject.GetComponent(componentName);
                SetValue(component, fieldName, keyValuePair.Value);

                ParametersController controller = gObject.GetComponent<ParametersController>();
                if(controller)
                    controller.FireValueChanged();
            }
        }
        public override void Redo()
        {
            foreach (var keyValuePair in oldValues)
            {
                GameObject gObject = objectPath.Length > 0 ? keyValuePair.Key.transform.Find(objectPath).gameObject : keyValuePair.Key;
                Component component = gObject.GetComponent(componentName);
                SetValue(component, fieldName, newValue);

                ParametersController controller = gObject.GetComponent<ParametersController>();
                if (controller)
                    controller.FireValueChanged();
            }
        }

        public override void Serialize(SceneSerializer serializer)
        {

        }


    }

}