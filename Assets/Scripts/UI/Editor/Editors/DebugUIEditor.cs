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
    [CustomEditor(typeof(DebugUI))]
    public class DebugUIEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            DebugUI debug = target as DebugUI;

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, Screen.width, Screen.height));
            GUILayout.BeginVertical();
            {
                GUIStyle textStyle = new GUIStyle();
                textStyle.normal.textColor = Color.white;

                //
                // UI OPTIONS
                //

                GUILayout.Space(20);

                GUILayout.Label(new GUIContent("Debug UIOptions"), textStyle, GUILayout.Height(30));

                if (GUILayout.Button("Refresh", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_Refresh();
                }

                if (GUILayout.Button("Relink Widgets <-> Colors", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_ResetAllColors();
                }

                //if (GUILayout.Button("Link Widgets HOVERED Color", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.UIOPTIONS_HoveredColor();
                //}

                //if (GUILayout.Button("Add Collider to UIPanel", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.AddCollidersToUIPanels();
                //}

                //
                // Asset Bank
                //

                //GUILayout.Space(20);

                //GUILayout.Label(new GUIContent("Asset Bank"), textStyle, GUILayout.Height(30));

                //if (GUILayout.Button("Reorder", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.AssetBank_Reorder();
                //}

                //
                // Checkable icons.
                //

                //GUILayout.Space(20);

                //GUILayout.Label(new GUIContent("Checkable Buttons"), textStyle, GUILayout.Height(30));

                //if (GUILayout.Button("SetBaseSprite", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.Checkable_SetBaseSprite();
                //}

                //
                //
                //
                GUILayout.Space(20);

                GUILayout.Label(new GUIContent("Materials"), textStyle, GUILayout.Height(30));

                if (GUILayout.Button("Relink/Fix Widgets Materials", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.MATERIALS_RelinkAndFix();
                }

                //GUILayout.Space(20);

                //GUILayout.Label(new GUIContent("TextMeshPro"), textStyle, GUILayout.Height(30));

                //if (GUILayout.Button("Replace Text <-> TexMeshPro", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.Replace_Text_By_TextMeshPro();
                //}
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}