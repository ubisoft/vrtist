using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(ColorVar))]
    public class ColorVarDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawColorVar(position, property, label, false);
        }

        public static void DrawColorVar(Rect position, SerializedProperty property, GUIContent label, bool isEmbedded)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                // Draw Label (advances "position")
                if (!isEmbedded)
                {
                    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                }
                else
                {
                    position.width -= 20;
                }

                bool isHdrValue = property.FindPropertyRelative("isHdr").boolValue;

                Color constantColorValue = property.FindPropertyRelative("value").colorValue;
                Color newConstantColorValue = EditorGUI.ColorField(position, GUIContent.none, constantColorValue, true, true, isHdrValue);
                if (GUI.changed)
                {
                    property.FindPropertyRelative("value").colorValue = newConstantColorValue;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
