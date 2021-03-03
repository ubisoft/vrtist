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
using System.Reflection;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to set a value to a component property of an object.
    /// The property is defined by a path like "/Transform/localPosition/x", for example.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    public class CommandSetValue<T> : ICommand
    {
        readonly Dictionary<GameObject, T> oldValues = new Dictionary<GameObject, T>();

        T newValue;
        readonly string objectPath;
        readonly string componentName;
        readonly string fieldName;

        private void GetGenericAttribute(Component component, string fieldName, out object inst, out MemberInfo memberInfo)
        {
            inst = component;
            string[] fields = fieldName.Split('.');

            memberInfo = component.GetType().GetField(fields[0]);
            if (null == memberInfo)
                memberInfo = component.GetType().GetProperty(fields[0]);

            for (int i = 1; i < fields.Length; i++)
            {
                if (memberInfo is FieldInfo)
                    inst = (memberInfo as FieldInfo).GetValue(inst);
                else
                    inst = (memberInfo as PropertyInfo).GetValue(inst);

                memberInfo = inst.GetType().GetField(fields[i]);
                if (null == memberInfo)
                    memberInfo = component.GetType().GetProperty(fields[i]);
            }

        }

        public T GetValue(Component component, string fieldName)
        {
            GetGenericAttribute(component, fieldName, out object inst, out MemberInfo memberInfo);
            if (memberInfo is FieldInfo)
                return (T)(memberInfo as FieldInfo).GetValue(inst);
            else
                return (T)(memberInfo as PropertyInfo).GetValue(inst);
        }

        public void SetValue(Component component, string fieldName, T value)
        {
            GetGenericAttribute(component, fieldName, out object inst, out MemberInfo memberInfo);
            if (memberInfo is FieldInfo)
                (memberInfo as FieldInfo).SetValue(inst, value);
            else
                (memberInfo as PropertyInfo).SetValue(inst, value);
        }

        // Create a command for each currently selected game objects
        public CommandSetValue(string commandName, string propertyPath)
        {
            name = commandName;
            ICommand.SplitPropertyPath(propertyPath, out objectPath, out componentName, out fieldName);

            foreach (var selectedItem in Selection.SelectedObjects)
            {
                GameObject gObject = objectPath.Length > 0 ? selectedItem.transform.Find(objectPath).gameObject : selectedItem;
                Component component = gObject.GetComponent(componentName);

                if (null == component)
                    continue;

                oldValues[gObject] = GetValue(component, fieldName);
            }
        }

        // Create a command for a given game objects and its old value
        public CommandSetValue(GameObject obj, string commandName, string propertyPath, T oldValue)
        {
            name = commandName;
            ICommand.SplitPropertyPath(propertyPath, out objectPath, out componentName, out fieldName);
            Component component = obj.GetComponent(componentName);
            if (null == component) return;
            oldValues[obj] = oldValue;
        }

        // Create a command for a given game objects
        public CommandSetValue(GameObject obj, string commandName, string propertyPath)
        {
            name = commandName;
            ICommand.SplitPropertyPath(propertyPath, out objectPath, out componentName, out fieldName);
            Component component = obj.GetComponent(componentName);
            if (null == component) return;
            oldValues[obj] = GetValue(component, fieldName);
        }

        // Create a command for a set of given game objects
        public CommandSetValue(List<GameObject> objects, string commandName, string propertyPath)
        {
            name = commandName;
            ICommand.SplitPropertyPath(propertyPath, out objectPath, out componentName, out fieldName);
            foreach (GameObject obj in objects)
            {
                Component component = obj.GetComponent(componentName);
                if (null == component) { continue; }
                oldValues[obj] = GetValue(component, fieldName);
            }
        }

        public override void Submit()
        {
            foreach (var selectedItem in Selection.SelectedObjects)
            {
                GameObject gObject = objectPath.Length > 0 ? selectedItem.transform.Find(objectPath).gameObject : selectedItem;
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
            }
        }
        public override void Redo()
        {
            foreach (var keyValuePair in oldValues)
            {
                GameObject gObject = objectPath.Length > 0 ? keyValuePair.Key.transform.Find(objectPath).gameObject : keyValuePair.Key;
                Component component = gObject.GetComponent(componentName);
                SetValue(component, fieldName, newValue);
            }
        }
    }
}
