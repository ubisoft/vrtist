using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [CustomPropertyDrawer(typeof(SpaceHeaderAttribute))]
    internal sealed class SpaceHeaderDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            SpaceHeaderAttribute att = attribute as SpaceHeaderAttribute;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = att.lineColor;
            style.richText = true;
            int textSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight * 0.8f);
            string richText =
                  "<size=" + textSize + ">"
                + "<color=#" + ColorUtility.ToHtmlStringRGB(att.lineColor) + ">"
                + "<b>"
                + att.caption
                + "</b></color></size>";
            position.yMin += att.spaceHeight;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, richText, style);
        }

        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + 3.0f * (attribute as SpaceHeaderAttribute).spaceHeight;
        }
    }
}
