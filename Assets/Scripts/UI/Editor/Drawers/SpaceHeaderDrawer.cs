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
