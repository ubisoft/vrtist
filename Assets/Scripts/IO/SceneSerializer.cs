using Newtonsoft.Json;
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

        public void Apply(GameObject root)
        {
        }
    }

    public class AssetSerializer
    {
        [JsonProperty("filename")]
        public string filename;
        [JsonProperty("id")]
        public int id;

        [JsonProperty("transforms")]
        List<TransformSerializer> transforms = new List<TransformSerializer>();
        Dictionary<string, TransformSerializer> transformsByPath = new Dictionary<string, TransformSerializer>();

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

        public void Apply(GameObject root)
        {
            AssimpIO geometryImporter = new AssimpIO();

            //geometryImporter.BlockingImport(filename, root.transform);
            for(int i = 0; i < transforms.Count; i++)
            {
                transforms[i].Apply(root);
            }
        }
    }

    [DataContract]
    public class SceneSerializer
    {
        [JsonProperty("assets")]
        public List<AssetSerializer> assets = new List<AssetSerializer>();

        Dictionary<int, AssetSerializer> assetById = new Dictionary<int, AssetSerializer>();

        public void AddAsset(string filename, int id)
        {
            AssetSerializer assetSerializer = new AssetSerializer();
            assetSerializer.filename = filename;
            assetSerializer.id = id;
            assets.Add(assetSerializer);
            assetById[id] = assetSerializer;
        }

        public AssetSerializer GetAssetSerializer(int id)
        {
            return assetById[id];
        }

        GameObject FindWorld()
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

        GameObject FindImportedAssets()
        {
            GameObject world = FindWorld();
            if (!world)
                return null;

            int childrenCount = world.transform.childCount;
            for (int i = 0; i < childrenCount; i++)
            {
                GameObject child = world.transform.GetChild(i).gameObject;
                if (child.name == "Imported Geometries")
                    return child;
            }

            return null;
        }

        public void Save(string filename)
        {
            CommandManager.Serialize(this);

            string json = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filename, json);
        }

        public void Apply()
        {
            GameObject root = FindImportedAssets();
            for(int i = 0; i < assets.Count; i++)
            {
                assets[i].Apply(root);
            }
        }

        public static SceneSerializer Load(string filename)
        {
            string json = System.IO.File.ReadAllText(filename);
            SceneSerializer deserialized = JsonConvert.DeserializeObject<SceneSerializer>(json);
            deserialized.Apply();

            return deserialized;
        }
    }
}