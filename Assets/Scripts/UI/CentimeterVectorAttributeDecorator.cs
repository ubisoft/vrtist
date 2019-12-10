using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace VRtist
{
    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class CentimeterVector3Attribute : PropertyAttribute
    {
    }

    [CustomPropertyDrawer(typeof(CentimeterVector3Attribute))]
    internal sealed class CentimeterVector3Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.vector3Value = 0.01f * EditorGUI.Vector3Field(position, property.displayName + " (cm)", property.vector3Value * 100.0f);
        }
    }
}
