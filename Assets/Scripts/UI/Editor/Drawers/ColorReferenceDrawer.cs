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
                    ColorVarDrawer.DrawColorVar(position, referenceProperty, GUIContent.none, true);
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
