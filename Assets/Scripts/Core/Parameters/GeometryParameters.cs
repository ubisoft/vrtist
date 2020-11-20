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
            return null;
        }
    }
}