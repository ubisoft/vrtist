using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(CentimeterFloatAttribute))]
    internal sealed class CentimeterFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.floatValue = 0.01f * EditorGUI.FloatField(position, property.displayName + " (cm)", property.floatValue * 100.0f);
        }
    }
}
