using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace VRtist
{
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class CentimeterFloatAttribute : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(CentimeterFloatAttribute))]
    internal sealed class CentimeterFloatDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.floatValue = 0.01f * EditorGUI.FloatField(position, property.displayName + " (cm)", property.floatValue * 100.0f);
        }
    }
}
