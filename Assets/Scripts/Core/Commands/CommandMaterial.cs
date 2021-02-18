
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
