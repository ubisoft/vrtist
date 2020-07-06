using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(ColorReference))]
    public class ColorReferenceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                bool useConstant = property.FindPropertyRelative("useConstant").boolValue;

                // Draw Label (advances "position")
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                var rect = new Rect(position.position, Vector2.one * 20); // small square

                if (EditorGUI.DropdownButton(rect, new GUIContent(GetDropdownTexture()), FocusType.Keyboard,
                    new GUIStyle() { fixedWidth = 50.0f, border = new RectOffset(1, 1, 1, 1) }))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Constant"), useConstant, () => SetProperty(property, true));
                    menu.AddItem(new GUIContent("Reference"), !useConstant, () => SetProperty(property, false));
                    menu.ShowAsContext();
                }

                position.position += Vector2.right * 20;

                Color constantColorValue = property.FindPropertyRelative("constant").colorValue;

                if (useConstant)
                {
                    Rect colorPickerRect = position;
                    colorPickerRect.width -= 20;
                    Color newConstantColorValue = EditorGUI.ColorField(colorPickerRect, GUIContent.none, constantColorValue, true, true, false);
                    if (GUI.changed)
                    {
                        property.FindPropertyRelative("constant").colorValue = newConstantColorValue;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                }
                else
                {
                    SerializedProperty referenceProperty = property.FindPropertyRelative("reference");

                    ColorVariableDrawer.DrawColorVariable(position, referenceProperty, GUIContent.none, true);


                    ////ScriptableObject referenceSO = referenceProperty.objectReferenceValue as ScriptableObject;
                    //EditorGUI.PropertyField(position, referenceProperty);
                    //if (GUI.changed)
                    //{
                    //    //referenceSO.ApplyModifiedProperties();
                    //}
                }
            }
            EditorGUI.EndProperty();
        }

        private void SetProperty(SerializedProperty property, bool value)
        {
            property.FindPropertyRelative("useConstant").boolValue = value;
            property.serializedObject.ApplyModifiedProperties();
        }

        private Texture GetDropdownTexture()
        {
            return UIUtils.LoadIcon("editor-dropdown").texture;
        }
    }
}
