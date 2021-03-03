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

using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{

    /// <summary>
    /// Command that assigns a material to an object.
    /// </summary>
    public class CommandMaterial : ICommand
    {
        // Store for each gameObject its old value
        private readonly Dictionary<GameObject, MaterialValue> oldValues = new Dictionary<GameObject, MaterialValue>();

        // The new value
        private MaterialValue newValue;

        public CommandMaterial(GameObject gobject, MaterialValue value)
        {
            oldValues[gobject] = Utils.GetMaterialValue(gobject);
            newValue = value;
        }

        public CommandMaterial(GameObject gobject, Color color, float roughness, float metallic)
        {
            oldValues[gobject] = Utils.GetMaterialValue(gobject);
            UpdateMaterial(color, roughness, metallic);
        }

        public CommandMaterial(List<GameObject> gobjects, MaterialValue value)
        {
            foreach (GameObject gobject in gobjects)
            {
                oldValues[gobject] = Utils.GetMaterialValue(gobject);
            }
            newValue = value;
        }

        public CommandMaterial(List<GameObject> gobjects, Color color, float roughness, float metallic)
        {
            foreach (GameObject gobject in gobjects)
            {
                oldValues[gobject] = Utils.GetMaterialValue(gobject);
            }
            UpdateMaterial(color, roughness, metallic);
        }

        public void UpdateMaterial(MaterialValue value)
        {
            newValue = value;
        }

        public void UpdateMaterial(Color color, float roughness, float metallic)
        {
            newValue.color = color;
            newValue.roughness = roughness;
            newValue.metallic = metallic;
        }

        public override void Redo()
        {
            foreach (GameObject gobject in oldValues.Keys)
            {
                SceneManager.SetObjectMaterialValue(gobject, newValue);
            }
        }

        public override void Undo()
        {
            foreach (KeyValuePair<GameObject, MaterialValue> item in oldValues)
            {
                GameObject gobject = item.Key;
                SceneManager.SetObjectMaterialValue(gobject, item.Value);
            }
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
