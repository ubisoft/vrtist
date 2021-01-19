
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public struct MaterialValue
    {
        public Color color;
        public float roughness;
        public float metallic;
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
            foreach (GameObject gobject in gobjects)
            {
                oldValues[gobject] = GetMaterialValue(gobject);
            }
            newValue = value;
        }

        public CommandMaterial(List<GameObject> gobjects, Color color, float roughness, float metallic)
        {
            foreach (GameObject gobject in gobjects)
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
            if (null != renderer)
            {
                value.color = renderer.material.GetColor("_BaseColor");
                if (renderer.material.HasProperty("_Smoothness")) { value.roughness = 1f - renderer.material.GetFloat("_Smoothness"); }
                else { value.roughness = renderer.material.GetFloat("_Roughness"); }
                value.metallic = renderer.material.GetFloat("_Metallic");
            }
            return value;
        }

        private void SetMaterialValue(GameObject gobject, MaterialValue value)
        {
            Material opaqueMat = Resources.Load<Material>("Materials/ObjectOpaque");
            Material transpMat = Resources.Load<Material>("Materials/ObjectTransparent");
            MeshRenderer[] renderers = gobject.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                int i = 0;
                Material[] newMaterials = renderer.materials;
                foreach (Material oldMaterial in renderer.materials)
                {
                    bool previousMaterialWasTransparent = oldMaterial.HasProperty("_Opacity") && oldMaterial.GetFloat("_Opacity") < 0.99f;
                    bool newMaterialIsTransparent = value.color.a < 1.0f;
                    if (previousMaterialWasTransparent != newMaterialIsTransparent)
                    {
                        // swap material type
                        if (newMaterialIsTransparent)
                        {
                            newMaterials[i] = new Material(transpMat);
                        }
                        else
                        {
                            newMaterials[i] = new Material(opaqueMat);
                        }
                    }
                    //else
                    //{
                    //    newMaterials[i] = oldMaterial;
                    //}
                    
                    Material newMaterial = newMaterials[i++];

                    newMaterial.SetColor("_BaseColor", value.color);
                    if (newMaterial.HasProperty("_Opacity")) { newMaterial.SetFloat("_Opacity", value.color.a); }
                    if (newMaterial.HasProperty("_Smoothness")) { newMaterial.SetFloat("_Smoothness", 1f - value.roughness); }
                    else { newMaterial.SetFloat("_Roughness", value.roughness); }
                    newMaterial.SetFloat("_Metallic", value.metallic);
                }
                renderer.materials = newMaterials; // set array
            }
        }

        private void InformModification(GameObject gobject)
        {
            MeshRenderer renderer = gobject.GetComponentInChildren<MeshRenderer>();
            renderer.material.name = $"Mat_{gobject.name}";
            CommandManager.SendEvent(MessageType.Material, renderer.material);
            CommandManager.SendEvent(MessageType.AssignMaterial, new AssignMaterialInfo { objectName = gobject.name, materialName = renderer.material.name });
        }

        public override void Redo()
        {
            foreach (GameObject gobject in oldValues.Keys)
            {
                // Set the prefab and the object.
                // Then when we duplicate the object, the material is also duplicated
                Node node = SyncData.nodes[gobject.name];
                SetMaterialValue(node.prefab, newValue);
                SetMaterialValue(gobject, newValue);

                InformModification(gobject);
            }
        }

        public override void Undo()
        {
            foreach (KeyValuePair<GameObject, MaterialValue> item in oldValues)
            {
                GameObject gobject = item.Key;

                Node node = SyncData.nodes[gobject.name];
                SetMaterialValue(node.prefab, item.Value);
                SetMaterialValue(gobject, item.Value);

                InformModification(gobject);
            }
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
