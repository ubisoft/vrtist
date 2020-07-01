using System;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(HDRColorVariable))]
    public class HDRColorVariableDrawer : PropertyDrawer
    {
        const int colorPickerWidth = 66;
        const int buttonWidth = 66;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                ScriptableObject propertySO = null;
                HDRColorVariable propertyColorVariable = null;

                // Is the field empty of filled with a ScriptableObject?
                if (!property.hasMultipleDifferentValues && property.serializedObject.targetObject != null && property.serializedObject.targetObject is ScriptableObject)
                {
                    propertySO = property.serializedObject.targetObject as ScriptableObject;
                    propertyColorVariable = property.objectReferenceValue as HDRColorVariable;
                }

                var propertyRect = Rect.zero;
                var guiContent = new GUIContent(property.displayName);
                EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), guiContent);

                var indentedPosition = EditorGUI.IndentedRect(position);
                var indentOffset = indentedPosition.x - position.x;
                propertyRect = new Rect(position.x + (EditorGUIUtility.labelWidth - indentOffset), position.y, position.width - (EditorGUIUtility.labelWidth - indentOffset), EditorGUIUtility.singleLineHeight);

                // if the field is empty, reserve some space for the "create" button.
                if (propertySO != null || property.objectReferenceValue == null)
                {
                    propertyRect.width -= buttonWidth;
                }
                else
                {
                    // give some room for the colorpicker
                    propertyRect.width -= colorPickerWidth;
                }

                // The field representing the SO
                Type type = fieldInfo.FieldType;
                property.objectReferenceValue = EditorGUI.ObjectField(propertyRect, GUIContent.none, property.objectReferenceValue, type, false);
                if (GUI.changed)
                {
                    property.serializedObject.ApplyModifiedProperties();
                }

                // The rect for the "create" button
                var buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

                // The rect for the color picker
                var colorPickerRect = new Rect(position.x + position.width - colorPickerWidth, position.y, colorPickerWidth, EditorGUIUtility.singleLineHeight);

                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                {
                    // COLOR PICKER
                    EditorGUI.BeginChangeCheck();
                    Color newColor = EditorGUI.ColorField(colorPickerRect, GUIContent.none, propertyColorVariable.value, true, true, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        propertyColorVariable.value = newColor;
                    }
                }
                else
                {
                    if (GUI.Button(buttonRect, "Create"))
                    {
                        string selectedAssetPath = "Assets/Resources/Data/UI/Colors";
                        if (property.serializedObject.targetObject is MonoBehaviour)
                        {
                            MonoScript ms = MonoScript.FromMonoBehaviour((MonoBehaviour)property.serializedObject.targetObject);
                            selectedAssetPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(ms));
                        }

                        property.objectReferenceValue = CreateAssetWithSavePrompt(type, selectedAssetPath);
                    }
                }
            }
            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }

        // Creates a new ScriptableObject via the default Save File panel
        static ScriptableObject CreateAssetWithSavePrompt(Type type, string path)
        {
            path = EditorUtility.SaveFilePanelInProject("Save ColorVariable", type.Name + ".asset", "asset", "Enter a file name for the ColorVariable.", path);
            if (path == "") return null;
            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            EditorGUIUtility.PingObject(asset);
            return asset;
        }
    }
}
