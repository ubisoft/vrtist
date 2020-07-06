using System;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(ColorVariable))]
    public class ColorVariableDrawer : PropertyDrawer
    {
        const int colorPickerWidth = 66;
        const int buttonWidth = 66;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawColorVariable(position, property, label, false);
        }

        public static void DrawColorVariable(Rect position, SerializedProperty property, GUIContent label, bool isEmbedded)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                UnityEngine.Object propertySO = null;

                // Is the field empty of filled with a ScriptableObject?
                if (!property.hasMultipleDifferentValues && property.serializedObject.targetObject != null)// && property.serializedObject.targetObject is ScriptableObject)
                {
                    propertySO = property.serializedObject.targetObject;// as ScriptableObject;
                }

                var guiContent = new GUIContent(property.displayName);
                if (!isEmbedded)
                {
                    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), guiContent);
                }

                Rect propertyRect = position;

                // If the field is empty, reserve some space for the "create" button.
                if (propertySO != null || property.objectReferenceValue == null)
                {
                    propertyRect.width -= buttonWidth;
                }
                else
                {
                    // give some room for the colorpicker
                    propertyRect.width -= colorPickerWidth;
                }

                if (isEmbedded)
                {
                    propertyRect.width -= 20;
                }

                // The field representing the SO
                Type type = typeof(ColorVariable);
                property.objectReferenceValue = EditorGUI.ObjectField(propertyRect, GUIContent.none, property.objectReferenceValue, type, false);
                if (GUI.changed)
                {
                    property.serializedObject.ApplyModifiedProperties();
                }

                // The rect for the "create" button
                var buttonRect = new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);
                if (isEmbedded)
                {
                    buttonRect.position = new Vector2(buttonRect.position.x - 20, buttonRect.position.y);
                }

                // The rect for the color picker
                var colorPickerRect = new Rect(position.x + position.width - colorPickerWidth, position.y, colorPickerWidth, EditorGUIUtility.singleLineHeight);
                if (isEmbedded)
                {
                    colorPickerRect.position = new Vector2(colorPickerRect.position.x - 20, colorPickerRect.position.y);
                }

                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null)
                {
                    ColorVariable propertyColorVariable = property.objectReferenceValue as ColorVariable;
                    EditorGUI.BeginChangeCheck();
                    Color newColor = EditorGUI.ColorField(colorPickerRect, GUIContent.none, propertyColorVariable.value, true, true, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        propertyColorVariable.value = newColor;
                    }
                }
                else
                {
                    if (GUI.Button(buttonRect, "New"))
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
