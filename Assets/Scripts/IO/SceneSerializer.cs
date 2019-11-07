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

    public class LightSerializer
    {
        [JsonProperty("type")]
        IOLightMetaData.LightType type;
        public LightSerializer()
        {
        }
        public LightSerializer(IOMetaData metaData)
        {
            IOLightMetaData lightMetaData = metaData as IOLightMetaData;
            type = lightMetaData.lightType;

        }

        public Transform Apply(Transform parent)
        {
            GameObject light;
            GameObject lightPrefab = null;
            switch (type)
            {
                case IOLightMetaData.LightType.Point:
                    lightPrefab = Resources.Load("Prefabs/Point") as GameObject;
                    break;
                case IOLightMetaData.LightType.Spot :
                    lightPrefab = Resources.Load("Prefabs/Spot") as GameObject;
                    break;
                case IOLightMetaData.LightType.Sun:
                    lightPrefab = Resources.Load("Prefabs/Sun") as GameObject;
                    break;
            }

            light = Utils.CreateInstance(lightPrefab, parent);
            IOLightMetaData metaData = light.GetComponentInChildren<IOLightMetaData>();            
            metaData.lightType = type;
            return light.transform;
        }
    }
    public class CameraSerializer
    {
        public CameraSerializer()
        { }
        public CameraSerializer(IOMetaData metaData)
        {
            IOCameraMetaData cameraMetaData = metaData as IOCameraMetaData;
        }

        public Transform Apply(Transform parent)
        {
            GameObject cam = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, parent);
            return cam.transform;
        }
    }

    public class PaintSerializer
    {        
        [JsonProperty("color")]
        Color color;
        [JsonProperty("controlPoints")]
        Vector3[] controlPoints;
        [JsonProperty("controlPointsRadius")]
        float[] controlPointsRadius;

        public PaintSerializer()
        { }
        public PaintSerializer(IOMetaData metaData)
        {
            IOPaintMetaData paintMetaData = metaData as IOPaintMetaData;
            color = paintMetaData.color;
            //filename = paintMetaData.filename;
            controlPoints = paintMetaData.controlPoints;
            controlPointsRadius = paintMetaData.controlPointsRadius;
            //OBJExporter.Export(IOUtilities.GetAbsoluteFilename(filename), metaData.gameObject);
        }
        public Transform Apply(Transform parent)
        {
            //AssimpIO geometryImporter = new AssimpIO();
            //geometryImporter.Import(IOUtilities.GetAbsoluteFilename(filename), parent, IOMetaData.Type.Paint, true);
            GameObject paint = Utils.CreatePaint(parent, color);
            IOPaintMetaData metaData = paint.AddComponent<IOPaintMetaData>();
            metaData.type = IOMetaData.Type.Paint;
            //metaData.filename = filename;
            metaData.color = color;
            metaData.controlPoints = controlPoints;
            metaData.controlPointsRadius  = controlPointsRadius;

            // set mesh components
            var freeDraw = new FreeDraw(controlPoints, controlPointsRadius);
            MeshFilter meshFilter = paint.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            mesh.Clear();
            mesh.vertices = freeDraw.vertices;
            mesh.normals = freeDraw.normals;
            mesh.triangles = freeDraw.triangles;
            
            MeshCollider collider = paint.AddComponent<MeshCollider>();

            return paint.transform;
        }
    }
    public class GeometrySerializer
    {
        [JsonProperty("filename")]
        public string filename;
        [JsonProperty("deleted")]
        List<string> deleted = new List<string>();
        [JsonProperty("clones")]
        List<Tuple<string, string>> clones = new List<Tuple<string, string>>();

        public GeometrySerializer()
        { }
        public GeometrySerializer(IOMetaData metaData)
        {
            IOGeometryMetaData geometryMetaData = metaData as IOGeometryMetaData;
            filename = geometryMetaData.filename;
            deleted = geometryMetaData.deleted;
            clones = geometryMetaData.clones;
        }

        public void CreateDeletedSerializer(string path)
        {
            deleted.Add(path);
        }

        public void CreateDuplicateSerializer(string path, string name)
        {
            clones.Add(new Tuple<string, string>(path, name));
        }


        public Transform Apply(Transform parent)
        {
            AssimpIO geometryImporter = new AssimpIO();
            geometryImporter.Import(IOUtilities.GetAbsoluteFilename(filename), parent, true);

            Transform transform = parent.GetChild(parent.childCount - 1);
            IOGeometryMetaData metaData = transform.GetComponentInChildren<IOGeometryMetaData>();            
            metaData.filename = filename;            

            for (int i = 0; i < clones.Count; i++)
            {
                Tuple<string, string> clone = clones[i];
                Transform child = transform.Find(clone.Item1);
                var newInstance = Utils.CreateInstance(child.gameObject, child.parent);
                newInstance.name = clone.Item2;
                metaData.clones.Add(new Tuple<string,string>(clone.Item1, clone.Item2));
            }

            for (int i = 0; i < deleted.Count; i++)
            {
                string deletedPath = deleted[i];
                Transform child = transform.Find(deletedPath);
                if (child)
                {
                    GameObject.Destroy(child.gameObject);
                    metaData.deleted.Add(deletedPath);
                }
            }


            return transform;
        }

    }

    public class AssetSerializer
    {
        [JsonProperty("id")]
        public int id;
        [JsonProperty("type")]
        public IOMetaData.Type type;
        [JsonProperty("lightSerializer", NullValueHandling = NullValueHandling.Ignore)]
        public LightSerializer lightSerializer = null;
        [JsonProperty("cameraSerializer", NullValueHandling = NullValueHandling.Ignore)]
        public CameraSerializer cameraSerializer = null;
        [JsonProperty("geometrySerializer", NullValueHandling = NullValueHandling.Ignore)]
        public GeometrySerializer geometrySerializer = null;
        [JsonProperty("paintSerializer", NullValueHandling = NullValueHandling.Ignore)]
        public PaintSerializer paintSerializer = null;

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

        public void Apply()
        {
            GameObject root = Utils.FindGameObject(SceneSerializer.rootsByTypes[type]);

            Transform rootTransform = null;

            if (lightSerializer != null)
            {
                rootTransform = lightSerializer.Apply(root.transform);                
            }
            if (cameraSerializer != null)
            { 
                rootTransform = cameraSerializer.Apply(root.transform);                
            }
            if (paintSerializer != null)
            { 
                rootTransform = paintSerializer.Apply(root.transform);
            }
            if (geometrySerializer != null)
            { 
                rootTransform = geometrySerializer.Apply(root.transform);
            }

            IOMetaData metaData = rootTransform.GetComponent<IOMetaData>();
            metaData.id = id;
            IOMetaData.idGen = Math.Max(id + 1, IOMetaData.idGen);
            metaData.type = type;

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

        public void AddAsset(IOMetaData metaData)
        {
            AssetSerializer assetSerializer = new AssetSerializer();
            assetSerializer.id = metaData.id;
            assetSerializer.type = metaData.type;
            assets.Add(assetSerializer);
            assetById[metaData.id] = assetSerializer;

            switch(metaData.type)
            {
                case IOMetaData.Type.Paint:
                    assetSerializer.paintSerializer = new PaintSerializer(metaData);
                    break;
                case IOMetaData.Type.Geometry:
                    assetSerializer.geometrySerializer = new GeometrySerializer(metaData);
                    break;
                case IOMetaData.Type.Light:
                    assetSerializer.lightSerializer = new LightSerializer(metaData);
                    break;
                case IOMetaData.Type.Camera:
                    assetSerializer.cameraSerializer = new CameraSerializer(metaData);
                    break;
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
            var group = Utils.FindGameObject(groupName);
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

            currentJson = json;

            return deserialized;
        }
    }
}