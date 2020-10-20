using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class GeometryParameters : Parameters
    {
        [JsonProperty("filename")]
        public string filename;
        [JsonProperty("deleted")]
        public List<string> deleted = new List<string>();
        [JsonProperty("clones")]
        public List<Tuple<string, string>> clones = new List<Tuple<string, string>>();

        public void CreateDeletedSerializer(string path)
        {
            deleted.Add(path);
        }

        public void CreateDuplicateSerializer(string path, string name)
        {
            clones.Add(new Tuple<string, string>(path, name));
        }

        public Transform Deserialize(Transform parent)
        {
            AssimpIO geometryImporter = new AssimpIO();
            geometryImporter.Import(IOUtilities.GetAbsoluteFilename(filename), parent, true);

            Transform transform = parent.GetChild(parent.childCount - 1);
            transform.GetComponent<GeometryController>().parameters = this;

            for (int i = 0; i < clones.Count; i++)
            {
                Tuple<string, string> clone = clones[i];
                Transform child = transform.Find(clone.Item1);
                if (child == null)
                {
                    Debug.LogWarning("Can't find " + transform.name + "/" + clone.Item1);
                    continue;
                }
                var newInstance = SyncData.CreateInstance(child.gameObject, child.parent, clone.Item2);
            }

            for (int i = 0; i < deleted.Count; i++)
            {
                string deletedPath = deleted[i];
                Transform child = transform.Find(deletedPath);
                if (child)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }

            return transform;
        }
    }
}