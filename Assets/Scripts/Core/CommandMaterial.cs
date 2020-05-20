
using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;

namespace VRtist
{
    public class AssignMaterialInfo
    {
        public string objectName;
        public string materialName;
    }

    public struct MaterialValue
    {
        public Color color;
        public float roughness;
        public float metallic;

        public MaterialValue(Color color, float roughness, float metallic)
        {
            this.color = color;
            this.roughness = roughness;
            this.metallic = metallic;
        }
    }

    public class CommandMaterial : ICommand
    {
        // Store for each gameObject its old value
        private Dictionary<GameObject, MaterialValue> oldValues = new Dictionary<GameObject, MaterialValue>();

        // The new value
        private MaterialValue newValue;

        public CommandMaterial(GameObject gobject, MaterialValue value)
        {
            oldValues[gobject] = GetMaterialValue(gobject);
            newValue = value;
        }

        public CommandMaterial(GameObject gobject, Color color, float roughness, float metallic)
        {
            oldValues[gobject] = GetMaterialValue(gobject);
            UpdateMaterial(color, roughness, metallic);
        }

        public CommandMaterial(List<GameObject> gobjects, MaterialValue value)
        {
            foreach(GameObject gobject in gobjects)
            {
                oldValues[gobject] = GetMaterialValue(gobject);
            }
            newValue = value;
        }

        public CommandMaterial(List<GameObject> gobjects, Color color, float roughness, float metallic)
        {
            foreach(GameObject gobject in gobjects)
            {
                oldValues[gobject] = GetMaterialValue(gobject);
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

        private MaterialValue GetMaterialValue(GameObject gobject)
        {
            MeshRenderer renderer = gobject.GetComponentInChildren<MeshRenderer>();
            MaterialValue value = new MaterialValue();
            if(null != renderer)
            {
                value.color = renderer.material.GetColor("_BaseColor");
                if(renderer.material.HasProperty("_Smoothness")) { value.roughness = 1f - renderer.material.GetFloat("_Smoothness"); }
                else { value.roughness = renderer.material.GetFloat("_Roughness"); }
                value.metallic = renderer.material.GetFloat("_Metallic");
            }
            return value;
        }

        private void SetMaterialValue(GameObject gobject, MaterialValue value)
        {
            MeshRenderer[] renderers = gobject.GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer renderer in renderers)
            {
                foreach(Material material in renderer.materials)
                {
                    material.SetColor("_BaseColor", value.color);
                    if(renderer.material.HasProperty("_Smoothness")) { material.SetFloat("_Smoothness", 1f - value.roughness); }
                    else { material.SetFloat("_Roughness", value.roughness); }
                    material.SetFloat("_Metallic", value.metallic);
                }
            }
        }

        public override void Redo()
        {
            foreach(GameObject gobject in oldValues.Keys)
            {
                // Set the prefab and the object.
                // Then when we duplicate the object, the material is also duplicated
                Node node = SyncData.nodes[gobject.name];
                SetMaterialValue(node.prefab, newValue);
                SetMaterialValue(gobject, newValue);
            }
        }

        public override void Undo()
        {
            foreach(KeyValuePair<GameObject, MaterialValue> item in oldValues)
            {
                Node node = SyncData.nodes[item.Key.name];
                SetMaterialValue(node.prefab, item.Value);
                SetMaterialValue(item.Key, item.Value);
            }
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            foreach(GameObject gobject in oldValues.Keys)
            {
                MeshRenderer renderer = gobject.GetComponentInChildren<MeshRenderer>();
                renderer.material.name = $"Mat_{gobject.name}";
                CommandManager.SendEvent(MessageType.Material, renderer.material);
                CommandManager.SendEvent(MessageType.AssignMaterial, new AssignMaterialInfo { objectName = gobject.name, materialName = renderer.material.name });
            }
        }

        public override void Serialize(SceneSerializer serializer)
        {
            // Empty
        }
    }
}
