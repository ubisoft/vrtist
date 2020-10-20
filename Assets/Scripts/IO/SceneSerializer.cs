using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRtist
{
    public class TransformSerializer
    {
        public TransformSerializer()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;
        }
        [JsonProperty("path")]
        public string path;
        [JsonProperty("position")]
        public Vector3 position;
        [JsonProperty("rotation")]
        public Quaternion rotation;
        [JsonProperty("scale")]
        public Vector3 scale;

        public void InitNonSerializedData()
        {
        }

        public void Deserialize(GameObject root)
        {
            Transform t = root.transform.Find(path);
            if(t == null)
            {
                Debug.LogWarning("Can't find " + path);
                return;
            }
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
        }
    }

    public class AssetSerializer
    {
        [JsonProperty("id")]
        public int id;
        [JsonProperty("geometrySerializer", NullValueHandling = NullValueHandling.Ignore)]
        public GeometryParameters geometrySerializer = null;
        [JsonProperty("paintSerializer", NullValueHandling = NullValueHandling.Ignore)]
        public PaintParameters paintSerializer = null;

        [JsonProperty("transforms")]
        List<TransformSerializer> transforms = new List<TransformSerializer>();

        Dictionary<string, TransformSerializer> transformsByPath = new Dictionary<string, TransformSerializer>();

        public void InitNonSerializedData()
        {
            foreach(TransformSerializer transform in transforms)
            {
                transformsByPath[transform.path] = transform;
                transform.InitNonSerializedData();
            }
        }

        public TransformSerializer GetOrCreateTransformSerializer(string path)
        {
            if (transformsByPath.ContainsKey(path))
                return transformsByPath[path];
            TransformSerializer transformSerializer = new TransformSerializer();
            transformSerializer.path = path;
            transformsByPath[path] = transformSerializer;
            transforms.Add(transformSerializer);

            return transformSerializer;
        }

        public void CreateDeletedSerializer(string path)
        {
            if(geometrySerializer != null)
                geometrySerializer.CreateDeletedSerializer(path);
        }

        public void CreateDuplicateSerializer(string path, string name)
        {
            if (geometrySerializer != null)
                geometrySerializer.CreateDuplicateSerializer(path, name);
        }

        public void Deserialize()
        {
            Transform rootTransform = null;

            if (paintSerializer != null)
            {
                GameObject root = Utils.FindGameObject("Paintings");
                rootTransform = paintSerializer.Deserialize(root.transform);
            }
            if (geometrySerializer != null)
            {
                GameObject root = Utils.FindGameObject("Imported Geometries");
                rootTransform = geometrySerializer.Deserialize(root.transform);
            }

            Parameters parameters = rootTransform.GetComponent<ParametersController>().GetParameters();
            parameters.id = id;
            Parameters.idGen = Math.Max(id + 1, Parameters.idGen);

            for (int i = 0; i < transforms.Count; i++)
            {
                transforms[i].Deserialize(rootTransform.gameObject);
            }
        }
    }

    [DataContract]
    public class SceneSerializer
    {
        [JsonProperty("assets")]
        public List<AssetSerializer> assets = new List<AssetSerializer>();

        Dictionary<int, AssetSerializer> assetById = new Dictionary<int, AssetSerializer>();

        private static SceneSerializer currentSerializer = new SceneSerializer();
        public static SceneSerializer CurrentSerializer
        {
            get { return currentSerializer; }
        }

        public void AddAsset(ParametersController parametersController)
        {
            Parameters parameters = parametersController.GetParameters();
            AssetSerializer assetSerializer = new AssetSerializer();
            assetSerializer.id = parameters.id;
            assets.Add(assetSerializer);
            assetById[parameters.id] = assetSerializer;
            
            switch(parameters)
            {
                case PaintParameters paintParameters:
                    assetSerializer.paintSerializer = paintParameters;
                    break;
                case GeometryParameters geometryParameters:
                    assetSerializer.geometrySerializer = geometryParameters;
                    break;
            }
        }

        public void RemoveAsset(Parameters parameters)
        {
            assets.Remove(assetById[parameters.id]);
            assetById.Remove(parameters.id);
        }

        public AssetSerializer GetAssetSerializer(int id)
        {
            return assetById[id];
        }

        public void InitNonSerializedData()
        {
            foreach(AssetSerializer asset in assets)
            {
                assetById[asset.id] = asset;
                asset.InitNonSerializedData();
            }
        }

        public void Deserialize()
        {
            for(int i = 0; i < assets.Count; i++)
            {
                assets[i].Deserialize();                
            }
            InitNonSerializedData();
        }

        public static void ClearGroup(string groupName)
        {
            var group = Utils.FindGameObject(groupName);
            for (int i = group.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(group.transform.GetChild(i).gameObject);
            }

        }

        public static void Clear()
        {
            ClearGroup("Lights");
            ClearGroup("Paintings");
            ClearGroup("Cameras");
            ClearGroup("Imported Geometries");
            GameObject.Destroy(SyncData.GetTrash());

            CommandManager.Clear();
        }

        public static void Save(string filename)
        {
            // serialize / deserialize to copy currentSerializer into a new data structure (instead of referncing it)
            string currentJson = JsonConvert.SerializeObject(currentSerializer, Newtonsoft.Json.Formatting.None);
            SceneSerializer newSerializer = JsonConvert.DeserializeObject<SceneSerializer>(currentJson);

            // init acceleration data
            newSerializer.InitNonSerializedData();

            // gather remaining undo commands
            CommandManager.Serialize(newSerializer);

            // convert to Json
            string json = JsonConvert.SerializeObject(newSerializer, Newtonsoft.Json.Formatting.Indented);
            // write to file
            System.IO.File.WriteAllText(filename, json);
        }

        public static SceneSerializer Load(string filename)
        {
            // read json file
            string json = System.IO.File.ReadAllText(filename);
            // create data structure from json
            SceneSerializer deserialized = JsonConvert.DeserializeObject<SceneSerializer>(json);

            // clear scene and command manager
            SceneSerializer.Clear();

            // Apply data structure to actually create and manage game objects
            deserialized.Deserialize();            

            // keep current serialization state
            currentSerializer = deserialized;

            return deserialized;
        }
    }
}