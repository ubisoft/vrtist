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

        public void Apply(GameObject root)
        {
            Transform t = root.transform.Find(path);
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
        }
    }

    public class AssetSerializer
    {
        [JsonProperty("filename")]
        public string filename;
        [JsonProperty("id")]
        public int id;
        [JsonProperty("type")]
        public IOMetaData.Type type;

        [JsonProperty("transforms")]
        List<TransformSerializer> transforms = new List<TransformSerializer>();

        [JsonProperty("deleted")]
        List<string> deleted = new List<string>();

        [JsonProperty("clones")]
        List<Tuple<string, string>> clones = new List<Tuple<string, string>>();

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
            deleted.Add(path);
        }

        public void CreateDuplicateSerializer(string path, string name)
        {
            clones.Add(new Tuple<string,string>(path, name));
        }

        public void Apply()
        {
            GameObject root = SceneSerializer.FindGameObject(SceneSerializer.rootsByTypes[type]);

            AssimpIO geometryImporter = new AssimpIO();
            geometryImporter.Import(IOUtilities.GetAbsoluteFilename(filename), root.transform, type, true);
            Transform rootTransform = root.transform.GetChild(root.transform.childCount - 1);

            for (int i = 0; i < clones.Count; i++)
            {
                Tuple<string, string> clone = clones[i];
                Transform child = rootTransform.Find(clone.Item1);
                var newInstance = Utils.CreateInstance(child.gameObject, child.parent);
                newInstance.name = clone.Item2;
            }

            for (int i = 0; i < deleted.Count; i++)
            {
                string deletedPath = deleted[i];
                Transform child = rootTransform.Find(deletedPath);
                if (child)
                    GameObject.Destroy(child.gameObject);
            }

            for (int i = 0; i < transforms.Count; i++)
            {
                transforms[i].Apply(rootTransform.gameObject);
            }
        }
    }

    [DataContract]
    public class SceneSerializer
    {
        [JsonProperty("assets")]
        public List<AssetSerializer> assets = new List<AssetSerializer>();

        Dictionary<int, AssetSerializer> assetById = new Dictionary<int, AssetSerializer>();

        private static string currentJson = null;

        public static Dictionary<IOMetaData.Type, string> rootsByTypes = new Dictionary<IOMetaData.Type, string>() {
            { IOMetaData.Type.Geometry ,  "Imported Geometries" },
            { IOMetaData.Type.Paint ,  "Paintings" },
            { IOMetaData.Type.Light ,  "Lights" },
            { IOMetaData.Type.Camera ,  "Cameras" },
        };

        public static GameObject FindWorld()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "World")
                {
                    return roots[i];
                }
            }
            return null;
        }

        public static GameObject FindGameObject(string name)
        {
            GameObject world = FindWorld();
            if (!world)
                return null;

            int childrenCount = world.transform.childCount;
            for (int i = 0; i < childrenCount; i++)
            {
                GameObject child = world.transform.GetChild(i).gameObject;
                if (child.name == name)
                    return child;
            }

            return null;
        }

        public void AddAsset(IOMetaData metaData)
        {
            AssetSerializer assetSerializer = new AssetSerializer();
            assetSerializer.filename = metaData.filename;
            assetSerializer.id = metaData.id;
            assetSerializer.type = metaData.type;
            assets.Add(assetSerializer);
            assetById[metaData.id] = assetSerializer;

            if (assetSerializer.type == IOMetaData.Type.Paint)
            {                
                OBJExporter.Export(IOUtilities.GetAbsoluteFilename(assetSerializer.filename), metaData.gameObject);
            }
        }

        public void RemoveAsset(IOMetaData metaData)
        {
            assets.Remove(assetById[metaData.id]);
            assetById.Remove(metaData.id);
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

        public static void Save(string filename)
        {
            SceneSerializer serializer = null;
            if (currentJson == null)
            {
                serializer = new SceneSerializer();
            }
            else
            {
                serializer = JsonConvert.DeserializeObject<SceneSerializer>(currentJson);
                serializer.InitNonSerializedData();
            }

            CommandManager.Serialize(serializer);

            string json = JsonConvert.SerializeObject(serializer, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filename, json);
        }        

        public void Apply()
        {
            for(int i = 0; i < assets.Count; i++)
            {
                assets[i].Apply();                
            }
        }

        public static void ClearGroup(string groupName)
        {
            var group = SceneSerializer.FindGameObject(groupName);
            for (int i = group.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(group.transform.GetChild(i).gameObject);
            }

        }

        public static void Clear()
        {
            foreach(var elem in rootsByTypes)
            {
                ClearGroup(elem.Value);
            }
            GameObject.Destroy(Utils.GetOrCreateTrash());

            CommandManager.Clear();
        }

        public static SceneSerializer Load(string filename)
        {
            string json = System.IO.File.ReadAllText(filename);
            SceneSerializer deserialized = JsonConvert.DeserializeObject<SceneSerializer>(json);

            SceneSerializer.Clear();

            deserialized.Apply();

            if(currentJson == null)
                currentJson = json;

            return deserialized;
        }
    }
}