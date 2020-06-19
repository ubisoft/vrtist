using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(PercentageAttribute))]
    internal sealed class PercentageDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.floatValue = 0.01f * EditorGUI.FloatField(position, property.displayName + " (%)", property.floatValue * 100.0f);
        }
    }
}
