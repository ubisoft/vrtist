using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace VRtist
{
    public enum MessageType
    {
        JoinRoom = 1,
        CreateRoom,
        LeaveRoom,

        Command = 100,
        Delete,
        Camera,
        Light,
        MeshConnection,
        Rename,
        Duplicate,
        SendToTrash,
        RestoreFromTrash,
        Texture,
        AddCollectionToCollection,
        RemoveCollectionFromCollection,
        AddObjectToCollection,
        RemoveObjectFromCollection,
        AddObjectToScene,
        AddCollectionToScene,
        CollectionInstance,
        Collection,
        CollectionRemoved,
        SetScene,
        Optimized_Commands = 200,
        Transform,
        Mesh,
        Material,
    }

    public class NetCommand
    {
        public byte[] data;
        public MessageType messageType;
        public int id;

        public NetCommand()
        {
        }
        public NetCommand(byte[] d, MessageType mtype, int mid = 0 )
        {
            data = d;
            messageType = mtype;
            id = mid;
        }
    }
    
    public class NetGeometry
    {        
        public class Node
        {
            public Node parent = null; // parent (hierarchy) of this node
            public List<Node> children = new List<Node>(); // children (hierarchy) of this node
            public GameObject prefab;
            public List<Tuple<GameObject, string>> instances = new List<Tuple<GameObject, string>>(); // instances of this node in scene
            public CollectionNode collectionInstance = null;    // this node is an instance of a collection
            public List<CollectionNode> collections = new List<CollectionNode>(); // Collections containing this node
            public bool visible = true;
            public bool containerVisible = true; // combination of Collections containing this node is visible

            public Node()
            {
                prefab = null;
            }
            public Node(GameObject gObject)
            {
                prefab = gObject;
            }

            public void AddChild(Node node)
            {
                node.parent = this;
                children.Add(node);
            }

            public void RemoveChild(Node node)
            {
                children.Remove(node);
                node.parent = null;
            }

            public void ComputeContainerVisibility()
            {
                if(collections.Count == 0)
                {
                    containerVisible = true;
                    return;
                }

                containerVisible = false;
                foreach (CollectionNode collection in collections)
                {
                    if (collection.IsVisible())
                    {
                        containerVisible = true;
                        break;
                    }
                }
            }

            public void AddCollection(CollectionNode collectionNode)
            {
                collections.Add(collectionNode);
                ComputeContainerVisibility();
            }
            public void RemoveCollection(CollectionNode collectionNode)
            {
                collections.Remove(collectionNode);
                ComputeContainerVisibility();
            }

            public void AddInstance(GameObject obj, string collectionInstanceName)
            {
                instances.Add(new Tuple<GameObject, string>(obj, collectionInstanceName));
            }
            public void RemoveInstance(GameObject obj)
            {
                foreach(Tuple<GameObject, string> item in instances)
                {
                    if(item.Item1 == obj)
                    {
                        instances.Remove(item);
                        break;
                    }
                }
            }
        }
        public class CollectionNode
        {
            public CollectionNode parent = null;                                // Parent collection
            public List<CollectionNode> children = new List<CollectionNode>();  // Children of collection
            public List<Node> objects = new List<Node>();                       // Objects in collection
            public List<Node> prefabInstanceNodes = new List<Node>();           // Instances of collection
            public string name;
            public bool visible;
            public Vector3 offset;

            public bool IsVisible()
            {
                if (!visible)
                    return false;
                if (null == parent)
                    return visible;
                return parent.IsVisible();
            }

            public CollectionNode(string collectionName)
            {
                name = collectionName;
            }
            public void AddChild(CollectionNode node)
            {
                node.parent = this;
                children.Add(node);
            }
            public void RemoveChild(CollectionNode node)
            {
                node.parent = null;
                children.Remove(node);
            }
            public void AddObject(Node node)
            {
                objects.Add(node);
                node.AddCollection(this);
            }
            public void RemoveObject(Node node)
            {
                node.RemoveCollection(this);
                objects.Remove(node);
            }
            public void AddPrefabInstanceNode(Node obj)
            {
                prefabInstanceNodes.Add(obj);
            }
            public void RemovePrefabInstanceNode(Node obj)
            {
                prefabInstanceNodes.Remove(obj);
            }
        }

        public static CollectionNode CreateCollectionNode(CollectionNode parent, string name)
        {
            CollectionNode newNode = new CollectionNode(name);
            collectionNodes.Add(name, newNode);
            if(parent != null)
                parent.AddChild(newNode);
            return newNode;
        }

        public static Node CreateNode(string name, Node parentNode = null)
        {
            Node newNode = new Node();
            nodes.Add(name, newNode);
            if (null != parentNode)
                parentNode.AddChild(newNode);
            return newNode;
        }

        public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
        public static Material currentMaterial = null;

        public static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public static Dictionary<string, Material[]> meshesMaterials = new Dictionary<string, Material[]>();
        public static Dictionary<string, HashSet<MeshFilter>> meshInstances = new Dictionary<string, HashSet<MeshFilter>>();

        public static Dictionary<string, byte[]> textureData = new Dictionary<string, byte[]>();
        public static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public static HashSet<string> texturesFlipY = new HashSet<string>();

        public static Dictionary<GameObject, Node> instancesToNodes = new Dictionary<GameObject, Node>();
        public static Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        public static Dictionary<string, CollectionNode> collectionNodes = new Dictionary<string, CollectionNode>();
        public static Dictionary<string, Transform> instanceRoot = new Dictionary<string, Transform>();
        public static Node prefabNode = new Node();
        public static Node rootNode = new Node();
        public static string OffsetTransformName = "__Offset";

        public static string currentSceneName = "";
        public static HashSet<string> sceneCollections = new HashSet<string>();

        public static byte[] StringsToBytes(string[] values)
        {
            int size = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(values[i]);
                size += sizeof(int) + utf8.Length;
            }
                

            byte[] bytes = new byte[size];
            Buffer.BlockCopy(BitConverter.GetBytes(values.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
                Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, index, sizeof(int));
                Buffer.BlockCopy(utf8, 0, bytes, index + sizeof(int), value.Length);
                index += sizeof(int) + value.Length;
            }
            return bytes;
        }

        public static byte[] StringToBytes(string value)
        {
            byte[] bytes = new byte[sizeof(int) + value.Length];
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(value);
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, bytes, 0, sizeof(int));
            Buffer.BlockCopy(utf8, 0, bytes, sizeof(int), value.Length);
            return bytes;
        }

        public static byte[] TriangleIndicesToBytes(int[] vectors)
        {
            byte[] bytes = new byte[sizeof(int) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length / 3), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(vectors[i]), 0, bytes, index, sizeof(int));
                index += sizeof(int);
            }
            return bytes;
        }

        public static byte[] Vector3ToBytes(Vector3[] vectors)
        {
            byte[] bytes = new byte[3 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector3 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, index + 2 * sizeof(float), sizeof(float));
                index += 3 * sizeof(float);
            }
            return bytes;
        }

        public static Color GetColor(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Color(buffer[0], buffer[1], buffer[2], buffer[3]);
        }

        public static byte[] ColorToBytes(Color color)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(color.r), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.g), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.b), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(color.a), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static Vector3 GetVector3(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[3];
            int size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector3(buffer[0], buffer[1], buffer[2]);
        }

        public static byte[] Vector3ToBytes(Vector3 vector)
        {
            byte[] bytes = new byte[3 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(vector.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static Vector2 GetVector2(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[2];
            int size = 2 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Vector2(buffer[0], buffer[1]);
        }

        public static byte[] Vector2ToBytes(Vector2[] vectors)
        {
            byte[] bytes = new byte[2 * sizeof(float) * vectors.Length + sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(vectors.Length), 0, bytes, 0, sizeof(int));
            int index = sizeof(int);
            for (int i = 0; i < vectors.Length; i++)
            {
                Vector2 vector = vectors[i];
                Buffer.BlockCopy(BitConverter.GetBytes(vector.x), 0, bytes, index + 0, sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(vector.y), 0, bytes, index + sizeof(float), sizeof(float));
                index += 2 * sizeof(float);
            }
            return bytes;
        }

        public static Quaternion GetQuaternion(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[4];
            int size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            return new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);
        }
        public static byte[] QuaternionToBytes(Quaternion quaternion)
        {
            byte[] bytes = new byte[4 * sizeof(float)];

            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.x), 0, bytes, 0, sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.y), 0, bytes, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.z), 0, bytes, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(quaternion.w), 0, bytes, 3 * sizeof(float), sizeof(float));
            return bytes;
        }

        public static bool GetBool(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0] == 1 ? true : false;
        }

        public static byte[] boolToBytes(bool value)
        {
            byte[] bytes = new byte[sizeof(int)];
            int v = value ? 1 : 0;
            Buffer.BlockCopy(BitConverter.GetBytes(v), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static int GetInt(byte[] data, ref int currentIndex)
        {
            int[] buffer = new int[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(int));
            currentIndex += sizeof(int);
            return buffer[0];
        }

        public static byte[] intToBytes(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(int));
            return bytes;
        }

        public static float GetFloat(byte[] data, ref int currentIndex)
        {
            float[] buffer = new float[1];
            Buffer.BlockCopy(data, currentIndex, buffer, 0, sizeof(float));
            currentIndex += sizeof(float);
            return buffer[0];
        }

        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, bytes, 0, sizeof(float));
            return bytes;
        }

        public static byte[] ConcatenateBuffers(List<byte[]> buffers)
        {
            int totalLength = 0;
            foreach (byte[] buffer in buffers)
            {
                totalLength += buffer.Length;
            }
            byte[] resultBuffer = new byte[totalLength];
            int index = 0;
            foreach (byte[] buffer in buffers)
            {
                int size = buffer.Length;
                Buffer.BlockCopy(buffer, 0, resultBuffer, index, size);
                index += size;
            }
            return resultBuffer;
        }

        public static void Rename(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string[] srcPath = GetString(data, bufferIndex, out bufferIndex).Split('/');
            string[] dstPath = GetString(data, bufferIndex, out bufferIndex).Split('/');

            string srcName = srcPath[srcPath.Length - 1];
            string dstName = dstPath[dstPath.Length - 1];

            if(nodes.ContainsKey(srcName))
            {
                Node node = nodes[srcName];
                node.prefab.name = dstName;
                foreach (Tuple<GameObject, string> obj in node.instances)
                {
                    obj.Item1.name = dstName;
                }
                nodes[dstName] = node;
                nodes.Remove(srcName);
            }
        }

        public static void Delete(Transform prefab, byte[] data)
        {
            int bufferIndex = 0;
            string[] ObjectPath = GetString(data, bufferIndex, out bufferIndex).Split('/');
            string objectName = ObjectPath[ObjectPath.Length - 1];

            Node node = nodes[objectName];
            for(int i = node.instances.Count - 1 ; i>=0; i--)
                Delete(node.instances[i].Item1);
        }

        public static void DeleteCollectionInstance(GameObject obj)
        {
            RemoveInstanceToNode(obj);
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = obj.transform.GetChild(i);
                DeleteCollectionInstance(child.gameObject);
            }
        }

        public static void Delete(GameObject gobj)
        {            
            Node node = nodes[gobj.name];
            if (null != node && null != node.collectionInstance)
            {
                GameObject offset = gobj.transform.Find(OffsetTransformName).gameObject;
                for (int i = offset.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = offset.transform.GetChild(i);
                    DeleteCollectionInstance(child.gameObject);
                }

                offset.transform.parent = null;
                GameObject.Destroy(offset);

                for (int j = 0; j < gobj.transform.childCount; j++)
                {
                    Reparent(gobj.transform.GetChild(j), gobj.transform.parent);
                }

                GameObject.Destroy(node.prefab);
                RemoveInstanceToNode(gobj);
                GameObject.Destroy(gobj);                
            }
            else
            {
                RemoveInstanceToNode(gobj);

                for (int j = 0; j < gobj.transform.childCount; j++)
                    Reparent(gobj.transform.GetChild(j), gobj.transform.parent);

                GameObject.Destroy(gobj);
            }        
        }

        public static void Duplicate(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform srcPath = FindPath(root, data, 0, out bufferIndex);
            if (srcPath == null)
                return;

            string dstName = GetString(data, bufferIndex, out bufferIndex);
            Vector3 position = GetVector3(data, ref bufferIndex);
            Quaternion rotation = GetQuaternion(data, ref bufferIndex);
            Vector3 scale = GetVector3(data, ref bufferIndex);

            GameObject newGameObject = GameObject.Instantiate(srcPath.gameObject, srcPath.parent);
            newGameObject.name = dstName;
            newGameObject.transform.localPosition = position;
            newGameObject.transform.localRotation = rotation;
            newGameObject.transform.localScale = scale;
        }

        public static void BuildSendToTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform objectPath = FindPath(root, data, 0, out bufferIndex);
            if (null == objectPath)
                return;
            objectPath.parent = Utils.GetTrash().transform;
        }
        public static void BuildRestoreFromTrash(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string objectName = GetString(data, 0, out bufferIndex);
            string dstPath = GetString(data, bufferIndex, out bufferIndex);
            Transform trf = Utils.GetTrash().transform.Find(objectName);
            if (null != trf)
            {
                Transform parent = CreateObjectPrefab(root, dstPath);
                trf.parent = parent;
            }
        }

        public static void BuildTexture(byte[] data)
        {
            int bufferIndex = 0;
            string path = GetString(data, 0, out bufferIndex);

            int size = GetInt(data, ref bufferIndex);

            byte[] buffer = new byte[size];
            Buffer.BlockCopy(data, bufferIndex, buffer, 0, size);

            textureData[path] = buffer;
        }

        public static void GetRecursiveObjectsOfCollection(CollectionNode collectionNode, ref List<Node> nodes)
        {
            foreach (Node node in collectionNode.objects)
            {
                nodes.Add(node);
            }

            foreach (CollectionNode childCollection in collectionNode.children)
            {
                GetRecursiveObjectsOfCollection(childCollection, ref nodes);
            }
        }

        public static void BuildCollection(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, bufferIndex, out bufferIndex);
            bool visible = GetBool(data, ref bufferIndex);
            Vector3 offset = GetVector3(data, ref bufferIndex);

            CollectionNode collectionNode = collectionNodes.ContainsKey(collectionName) ? collectionNodes[collectionName] : CreateCollectionNode(null, collectionName);

            collectionNode.visible = visible;
            collectionNode.offset = offset;

            List<Node> nodes = new List<Node>();
            GetRecursiveObjectsOfCollection(collectionNode, ref nodes);
            foreach (Node node in nodes)
            {
                node.ComputeContainerVisibility();

                foreach(Tuple<GameObject,string> item in node.instances)
                {
                    ApplyVisibility(item.Item1);
                }

            }

            // collection instances management
            foreach (Node prefabInstanceNode in collectionNode.prefabInstanceNodes)
            {
                foreach(Tuple<GameObject, string> item in prefabInstanceNode.instances)
                {
                    ApplyVisibility(item.Item1);
                    GameObject offsetObject = item.Item1.transform.Find(OffsetTransformName).gameObject;
                    offsetObject.transform.localPosition = offset;
                }
            }
        }

        public static void BuildCollectionRemoved(byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, bufferIndex, out bufferIndex);

            if (!collectionNodes.ContainsKey(collectionName))
                return;

            CollectionNode collectionNode = collectionNodes[collectionName];
            foreach (Node node in collectionNode.objects)
            {
                node.RemoveCollection(collectionNode);
                foreach (Tuple<GameObject, string> item in node.instances)
                {
                    ApplyVisibility(item.Item1);
                }
            }

            foreach (CollectionNode child in collectionNode.children)
            {
                child.parent = collectionNode.parent;
            }

            collectionNodes.Remove(collectionName);
        }

        public static void BuildAddCollectionToCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = GetString(data, bufferIndex, out bufferIndex);
            string collectionName = GetString(data, bufferIndex, out bufferIndex);

            CollectionNode parentNode = null;
            if (collectionNodes.ContainsKey(parentCollectionName))
            {
                parentNode = collectionNodes[parentCollectionName];
            }

            if (!collectionNodes.ContainsKey(collectionName))
            {
                CreateCollectionNode(parentNode, collectionName);
            }
            else
            {
                CollectionNode collectionNode = collectionNodes[collectionName];
                if (null != collectionNode.parent)
                    collectionNode.parent.RemoveChild(collectionNode);

                if (null != parentNode)
                    parentNode.AddChild(collectionNode);
            }

        }

        public static void BuildRemoveCollectionFromCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string parentCollectionName = GetString(data, bufferIndex, out bufferIndex);
            string collectionName = GetString(data, bufferIndex, out bufferIndex);

            if (collectionNodes.ContainsKey(parentCollectionName))
            {
                CollectionNode parentCollectionNode = collectionNodes[parentCollectionName];
                parentCollectionNode.RemoveChild(collectionNodes[collectionName]);
            }
        }

        public static void BuildAddObjectToCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, bufferIndex, out bufferIndex);
            string objectName = GetString(data, bufferIndex, out bufferIndex);

            if (!collectionNodes.ContainsKey(collectionName))
                return;
            CollectionNode collectionNode = collectionNodes[collectionName];

            Node objectNode = nodes[objectName];
            collectionNode.AddObject(objectNode);

            foreach (Node prefabInstanceNode in collectionNode.prefabInstanceNodes)
            {
                foreach (Tuple<GameObject, string> t in prefabInstanceNode.instances)
                {
                    GameObject obj = t.Item1;
                    Transform offsetObject = obj.transform.Find(OffsetTransformName);
                    if (null == offsetObject)
                        continue;

                    string subCollectionInstanceName = "/" + obj.name;
                    if (t.Item2.Length > 1)
                        subCollectionInstanceName = t.Item2 + subCollectionInstanceName;

                    BuildAddObjectToScene(offsetObject, objectNode.prefab.name, subCollectionInstanceName);
                }
            }

            foreach(Tuple<GameObject, string> item in objectNode.instances)
            {
                ApplyVisibility(item.Item1);
            }
            
        }

        public static void BuildRemoveObjectFromCollection(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, bufferIndex, out bufferIndex);
            string objectName = GetString(data, bufferIndex, out bufferIndex);

            if (!collectionNodes.ContainsKey(collectionName))
                return;

            CollectionNode collectionNode = collectionNodes[collectionName];

            foreach (Node prefabInstanceNode in collectionNode.prefabInstanceNodes)
            {
                foreach (Tuple<GameObject, string> t in prefabInstanceNode.instances)
                {
                    GameObject obj = t.Item1;
                    Transform offsetObject = obj.transform.Find(OffsetTransformName);
                    if (null == offsetObject)
                        continue;
                    BuildRemoveObjectFromScene(offsetObject, objectName, obj.name);
                }
            }

            collectionNode.RemoveObject(nodes[objectName]);
        }

        public static void BuildCollectionInstance(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out bufferIndex);
            string collectionName = GetString(data, bufferIndex, out bufferIndex);

            CollectionNode collectionNode = collectionNodes.ContainsKey(collectionName) ? collectionNodes[collectionName] : CreateCollectionNode(null, collectionName);
            Node instanceNode = nodes.ContainsKey(transform.name) ? nodes[transform.name] : CreateNode(transform.name);

            instanceNode.prefab = transform.gameObject;
            instanceNode.collectionInstance = collectionNode;
            collectionNode.AddPrefabInstanceNode(instanceNode);
        }

        public static void FindObjects(ref List<Transform> objects, Transform t, string name)
        {
            if (t.name == name)
                objects.Add(t);

            for (int i = 0; i < t.childCount; i++)
                FindObjects(ref objects, t.GetChild(i), name);
        }
        public static void BuildAddObjectToScene(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string objectName = GetString(data, bufferIndex, out bufferIndex);
            BuildAddObjectToScene(root, objectName, "/");
        }

        public static Transform FindRecursive(Transform t, string objectName)
        {
            if (t.name == objectName)
                return t;
            for(int i = 0 ; i < t.childCount ; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), objectName);
                if (null != res)
                    return res;
            }
            return null;
        }

        public static void BuildRemoveObjectFromScene(Transform root, string objectName, string collectionInstanceName)
        {
            Transform obj = FindRecursive(root,objectName);
            if (null == obj)
                return;

            if (nodes.ContainsKey(objectName))
            {
                Node objectNode = nodes[objectName];
                objectNode.instances.Remove(new Tuple<GameObject, string>(obj.gameObject, collectionInstanceName));
            }

            Transform parent = obj.parent;
            for(int i = 0 ; i < obj.childCount; i++)
            {
                GameObject child = obj.GetChild(i).gameObject;
                string childCollectionInstanceName = objectName;
                if (nodes.ContainsKey(child.name))
                {
                    childCollectionInstanceName = collectionInstanceName;
                    Node childNode = nodes[child.name];                    
                    if (childNode.collectionInstance != null)
                    {
                        childCollectionInstanceName = child.name;
                    }
                }
                BuildRemoveObjectFromScene(obj, child.name, childCollectionInstanceName);
            }

            GameObject.Destroy(obj.gameObject);
        }

        public static void AddInstanceToNode(GameObject obj, Node node, string collectionInstanceName)
        {
            node.AddInstance(obj, collectionInstanceName);
            instancesToNodes[obj] = node;
        }

        public static void RemoveInstanceToNode(GameObject obj)
        {
            if(instancesToNodes.ContainsKey(obj))
            {
                Node node = instancesToNodes[obj];
                node.RemoveInstance(obj);
                instancesToNodes.Remove(obj);
            }
        }
        public static void BuildAddObjectToScene(Transform root, string objectName, string collectionInstanceName)
        {
            if (!nodes.ContainsKey(objectName))
                return;
            Node objectNode = nodes[objectName];

            foreach(Tuple<GameObject, string> item in objectNode.instances)
            {
                if (item.Item2 == collectionInstanceName)
                    return; // already instantiated
            }

            GameObject instance = GameObject.Instantiate(objectNode.prefab);
            instance.name = objectName;
            AddInstanceToNode(instance, objectNode, collectionInstanceName);

            // Reparent to parent
            Transform parent = root;
            if(null != objectNode.parent)
            { 
                foreach (Tuple<GameObject, string> t in objectNode.parent.instances)
                {
                    if (t.Item2 == collectionInstanceName)
                    {
                        parent = t.Item1.transform;
                        break;
                    }
                }
            }
            Reparent(instance.transform, parent);

            // Reparent children
            List<Node> childrenNodes = objectNode.children;
            List<GameObject> children = new List<GameObject>();
            foreach(Node childNode in childrenNodes)
            {
                foreach (Tuple<GameObject, string> t in childNode.instances)
                    if (t.Item2 == collectionInstanceName)
                        children.Add(t.Item1);
            }
            foreach(GameObject childObject in children)
            {
                Reparent(childObject.transform, instance.transform);
            }

            if (null != objectNode.collectionInstance)
            {
                CollectionNode collectionNode = objectNode.collectionInstance;

                GameObject offsetObject = new GameObject(OffsetTransformName);
                offsetObject.transform.parent = instance.transform;
                offsetObject.transform.localPosition = -collectionNode.offset;
                offsetObject.transform.localRotation = Quaternion.identity;
                offsetObject.transform.localScale = Vector3.one;
                offsetObject.SetActive(collectionNode.visible & objectNode.visible);

                string subCollectionInstanceName = "/" + instance.name;
                if (collectionInstanceName.Length > 1)
                    subCollectionInstanceName = collectionInstanceName + subCollectionInstanceName;

                instanceRoot[subCollectionInstanceName] = offsetObject.transform;

                foreach (Node collectionObject in collectionNode.objects)
                {
                    BuildAddObjectToScene(offsetObject.transform, collectionObject.prefab.name, subCollectionInstanceName);
                }
                foreach(CollectionNode collectionChild in collectionNode.children)
                {
                    foreach (Node collectionObject in collectionChild.objects)
                    {
                        BuildAddObjectToScene(offsetObject.transform, collectionObject.prefab.name, subCollectionInstanceName);
                    }
                }
            }

            ApplyVisibility(instance);
        }

        public static void BuilAddCollectionToScene(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            string collectionName = GetString(data, bufferIndex, out bufferIndex);
            sceneCollections.Add(collectionName);
        }

        public static void BuilSetScene(Transform root, byte[] data)
        {
            int bufferIndex = 0;
            currentSceneName = GetString(data, bufferIndex, out bufferIndex);

            sceneCollections.Clear();
            instancesToNodes.Clear();

            List<GameObject> objectToRemove = new List<GameObject>();

            foreach (KeyValuePair<string, Node> nodePair in nodes)
            {
                Node node = nodePair.Value;
                List<Tuple<GameObject, string>> remainingObjects = new List<Tuple<GameObject, string>>();
                foreach(Tuple<GameObject, string> t in node.instances)
                {
                    GameObject obj = t.Item1;
                    Transform parent = obj.transform;
                    while (parent && parent != root)
                        parent = parent.parent;
                    if (parent != root)
                        remainingObjects.Add(new Tuple<GameObject, string>(obj, t.Item2));
                    else
                        objectToRemove.Add(obj);
                }
                node.instances = remainingObjects;
            }

            foreach(GameObject obj in objectToRemove)
            {
                GameObject.Destroy(obj);
            }
        }

        public static void Reparent(Transform t, Transform parent)
        {
            Vector3 position = t.localPosition;
            Quaternion rotation = t.localRotation;
            Vector3 scale = t.localScale;
            
            t.parent = parent;
            t.localPosition = position;
            t.localRotation = rotation;
            t.localScale = scale;
        }

        public static Material DefaultMaterial()
        {
            string name = "defaultMaterial";
            if (materials.ContainsKey(name))
                return materials[name];

            Shader hdrplit = Shader.Find("VRtist/BlenderImport");
            Material material = new Material(hdrplit);
            material.name = name;
            material.SetColor("_BaseColor", new Color(0.8f, 0.8f, 0.8f));
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Roughness", 0.5f);
            materials[name] = material;

            return material;
        }

        public static Texture2D LoadTextureOIIO(string filePath, bool isLinear)
        {
            // TODO: need to flip? Repere bottom left, Y-up
            int ret = OIIOAPI.oiio_open_image(filePath);
            if (ret == 0)
            {
                Debug.Log("Could not open image " + filePath + " with OIIO.");
                return null;
            }

            int width = -1;
            int height = -1;
            int nchannels = -1;
            OIIOAPI.BASETYPE format = OIIOAPI.BASETYPE.NONE;
            ret = OIIOAPI.oiio_get_image_info(ref width, ref height, ref nchannels, ref format);
            if (ret == 0)
            {
                Debug.Log("Could not get info about image " + filePath + " with OIIO");
                return null;
            }

            TextureFormat textureFormat = Format2Format(format, nchannels);
            Texture2D image = new Texture2D(width, height, textureFormat, true, isLinear); // with mips

            // NOTE: Unity does not have RGBFloat/Half formats. So if a texture has these formats
            // we convert it to a 4 channels RGBAFloat/Half texture.
            int do_rgb_to_rgba = 0;
            if ((format == OIIOAPI.BASETYPE.FLOAT && nchannels == 3)
                || (format == OIIOAPI.BASETYPE.HALF && nchannels == 3))
            {
                do_rgb_to_rgba = 1;
            }

            var pixels = image.GetRawTextureData();
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            ret = OIIOAPI.oiio_fill_image_data(handle.AddrOfPinnedObject(), do_rgb_to_rgba);
            if (ret == 1)
            {
                image.LoadRawTextureData(pixels);
                image.Apply();
            }
            else
            {
                Debug.Log("Could not fill texture data of " + filePath + " with OIIO.");
                return null;
            }

            return image;
        }

        private static TextureFormat Format2Format(OIIOAPI.BASETYPE format, int nchannels)
        {
            // TODO: handle compressed formats.

            TextureFormat defaultFormat = TextureFormat.RGBA32;

            switch (format)
            {
                case OIIOAPI.BASETYPE.UCHAR:
                case OIIOAPI.BASETYPE.CHAR:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.R8;
                            case 2: return TextureFormat.RG16;
                            case 3: return TextureFormat.RGB24;
                            case 4: return TextureFormat.RGBA32;
                            default: return defaultFormat;
                        }
                    }

                case OIIOAPI.BASETYPE.USHORT:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.R16;
                            // R16_G16, R16_G16_B16 and R16_G16_B16_A16 do not exist
                            default: return defaultFormat;
                        }
                    }

                case OIIOAPI.BASETYPE.HALF:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.RHalf;
                            case 2: return TextureFormat.RGHalf;
                            case 3: return TextureFormat.RGBAHalf; // RGBHalf is NOT SUPPORTED
                            case 4: return TextureFormat.RGBAHalf;
                            default: return defaultFormat;
                        }
                    }

                case OIIOAPI.BASETYPE.FLOAT:
                    {
                        switch (nchannels)
                        {
                            case 1: return TextureFormat.RFloat;
                            case 2: return TextureFormat.RGFloat;
                            case 3: return TextureFormat.RGBAFloat; // RGBFloat is NOT SUPPORTED
                            case 4: return TextureFormat.RGBAFloat;
                            default: return defaultFormat;
                        }
                    }

                default: return defaultFormat;
            }
        }

        public static Texture2D LoadTextureDXT(string filePath, bool isLinear)
        {
            byte[] ddsBytes = System.IO.File.ReadAllBytes(filePath);

            byte[] format = { ddsBytes[84],ddsBytes[85],ddsBytes[86],ddsBytes[87], 0 };
            string sFormat = System.Text.Encoding.UTF8.GetString(format);
            TextureFormat textureFormat;

            if (sFormat != "DXT1")
                textureFormat = TextureFormat.DXT1;
            else if (sFormat != "DXT5")
                textureFormat = TextureFormat.DXT5;
            else return null;

            byte ddsSizeCheck = ddsBytes[4];
            if (ddsSizeCheck != 124)
                throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

            int height = ddsBytes[13] * 256 + ddsBytes[12];
            int width = ddsBytes[17] * 256 + ddsBytes[16];

            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

            Texture2D texture = new Texture2D(width, height, textureFormat, true, isLinear);
            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return texture;
        }

        public static Texture2D LoadTextureFromBuffer(byte[] data, bool isLinear)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, true, isLinear);
            bool res = tex.LoadImage(data);
            if (!res)
                return null;

            return tex;
        }

        public static Texture2D GetTexture(string filePath, bool isLinear)
        {
            if(textureData.ContainsKey(filePath))
            {
                byte[] data = textureData[filePath];
                textureData.Remove(filePath);
                return LoadTexture(filePath, data, isLinear);
            }
            if(textures.ContainsKey(filePath))
            {
                return textures[filePath];
            }
            return null;
        }

        public static Texture2D LoadTexture(string filePath, byte[] data, bool isLinear)
        {
            string directory = Path.GetDirectoryName(filePath);
            string withoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string ddsFile = directory + "/" + withoutExtension + ".dds";

            if (File.Exists(ddsFile))
            {
                Texture2D t = LoadTextureDXT(ddsFile, isLinear);
                if (null != t)
                {
                    textures[filePath] = t;
                    texturesFlipY.Add(filePath);
                    return t;
                }
            }

            if (File.Exists(filePath))
            {
                //byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                //Texture2D t = LoadTextureFromBuffer(bytes, isLinear);
                Texture2D t = LoadTextureOIIO(filePath, isLinear);
                if(null != t)
                {
                    textures[filePath] = t;
                    texturesFlipY.Add(filePath);
                    return t;
                }
            }

            Texture2D texture = LoadTextureFromBuffer(data, isLinear);
            if(null != texture)
                textures[filePath] = texture;
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(data, 0, data.Length);
                }
            }
            catch
            {
                Debug.LogWarning("Could not write : " + filePath);
            }

            return texture;
        }


        public static void BuildMaterial(byte[] data)
        {
            int currentIndex = 0;

            string name = GetString(data, currentIndex, out currentIndex);
            float opacity = GetFloat(data, ref currentIndex);
            string opacityTexturePath = GetString(data, currentIndex, out currentIndex);

            Material material;
            if (materials.ContainsKey(name))
                material = materials[name];
            else
            {
                Shader importShader = (opacityTexturePath.Length > 0 || opacity < 1.0f)
                    ? Shader.Find("VRtist/BlenderImportTransparent")
                    : Shader.Find("VRtist/BlenderImport");
                material = new Material(importShader);
                material.name = name;
                material.enableInstancing = true;
                materials[name] = material;
            }

            //
            // OPACITY
            //
            material.SetFloat("_Opacity", opacity);
            if (opacityTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(opacityTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseOpacityMap", 1f);
                    material.SetTexture("_OpacityMap", tex);
                    if (texturesFlipY.Contains(opacityTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // BASE COLOR
            //
            Color baseColor = GetColor(data, ref currentIndex);
            material.SetColor("_BaseColor", baseColor);
            string baseColorTexturePath = GetString(data, currentIndex, out currentIndex);
            if (baseColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(baseColorTexturePath, false);
                if(tex != null)
                {
                    material.SetFloat("_UseColorMap", 1f);
                    material.SetTexture("_ColorMap", tex);
                    if(texturesFlipY.Contains(baseColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }

            }

            //
            // METALLIC
            //
            float metallic = GetFloat(data, ref currentIndex);
            material.SetFloat("_Metallic", metallic);
            string metallicTexturePath = GetString(data, currentIndex, out currentIndex);
            if (metallicTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(metallicTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseMetallicMap", 1f);
                    material.SetTexture("_MetallicMap", tex);
                    if (texturesFlipY.Contains(metallicTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // ROUGHNESS
            //
            float roughness = GetFloat(data, ref currentIndex);
            material.SetFloat("_Roughness", roughness);
            string roughnessTexturePath = GetString(data, currentIndex, out currentIndex);
            if (roughnessTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(roughnessTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseRoughnessMap", 1f);
                    material.SetTexture("_RoughnessMap", tex);
                    if (texturesFlipY.Contains(roughnessTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // NORMAL
            //
            string normalTexturePath = GetString(data, currentIndex, out currentIndex);
            if (normalTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(normalTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseNormalMap", 1f);                    
                    material.SetTexture("_NormalMap", tex);
                    if (texturesFlipY.Contains(normalTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }

            //
            // EMISSION
            //
            Color emissionColor = GetColor(data, ref currentIndex);
            material.SetColor("_EmissiveColor", baseColor);
            string emissionColorTexturePath = GetString(data, currentIndex, out currentIndex);
            if (emissionColorTexturePath.Length > 0)
            {
                Texture2D tex = GetTexture(emissionColorTexturePath, true);
                if (tex != null)
                {
                    material.SetFloat("_UseEmissiveMap", 1f);
                    material.SetTexture("_EmissiveMap", tex);
                    if (texturesFlipY.Contains(emissionColorTexturePath))
                        material.SetVector("_UvScale", new Vector4(1, -1, 0, 0));
                }
            }


            currentMaterial = material;
        }

        public static Transform FindPath(Transform root, byte[] data, int startIndex, out int bufferIndex)
        {
            int pathLength = (int)BitConverter.ToUInt32(data, startIndex);
            string path = System.Text.Encoding.UTF8.GetString(data, 4, pathLength);
            bufferIndex = startIndex + pathLength + 4;

            char[] separator = { '/' };
            string[] splitted = path.Split(separator);
            Transform parent = root;
            foreach (string subPath in splitted)
            {
                Transform transform = parent.Find(subPath);
                if (transform == null)
                {
                    return null;
                }
                parent = transform;
            }
            return parent;
        }

        public static string GetString(byte[] data, int startIndex, out int bufferIndex)
        {
            int strLength = (int)BitConverter.ToUInt32(data, startIndex);
            string str = System.Text.Encoding.UTF8.GetString(data, startIndex + 4, strLength);
            bufferIndex = startIndex + strLength + 4;
            return str;
        }

        public static string GetPathName(Transform root, Transform transform)
        {
            string result = transform.name;
            while(transform.parent && transform.parent != root)
            {
                transform = transform.parent;
                result = transform.name + "/" + result;
            }
            return result;
        }

        public static void ApplyReparent(Node parent, Node node)
        {
            if (null != node.parent)
                node.parent.RemoveChild(node);

            // reparent parents
            if (null != parent)
            {
                // get instance names of children
                Dictionary<string, Transform> children = new Dictionary<string, Transform>();
                foreach (Tuple<GameObject, string> instanceElem in node.instances)
                {
                    children[instanceElem.Item2] = instanceElem.Item1.transform;
                }

                parent.AddChild(node);

                // find parents by instance name
                foreach (Tuple<GameObject, string> instanceElem in parent.instances)
                {
                    if (!children.ContainsKey(instanceElem.Item2))
                        continue;
                    Reparent(children[instanceElem.Item2], instanceElem.Item1.transform);
                }
            }
            else // reparent to null (root)
            {
                foreach (Tuple<GameObject, string> instanceElem in node.instances)
                {
                    Transform t = instanceElem.Item1.transform;
                    string instanceName = instanceElem.Item2;
                    Reparent(t, instanceRoot[instanceName]);
                }
            }
        }

        public static Transform CreateObjectPrefab(Transform root, string path)
        {
            string[] splitted = path.Split('/');
            string parentName = splitted.Length >= 2 ? splitted[splitted.Length - 2] : "";

            Node parentNode = null;
            if (parentName.Length > 0)
            {
                if (nodes.ContainsKey(parentName))
                {
                    parentNode = nodes[parentName];
                }
                else
                {
                    parentNode = CreateNode(parentName);
                    GameObject parentObject = new GameObject(parentName);
                    Reparent(parentObject.transform, root);
                    parentNode.prefab = parentObject;
                }
            }
          
            string objectName = splitted[splitted.Length - 1];
            Transform transform = root.Find(objectName);
            Node node = null;
            if (null == transform)
            {
                transform = new GameObject(objectName).transform;
                Reparent(transform, root);
                node = CreateNode(objectName, parentNode);
                node.prefab = transform.gameObject;
            }

            if (null == node)
                node = nodes[objectName];

            if (node.parent != parentNode)
            {
                ApplyReparent(parentNode, node);
            }

            return transform;
        }

        public static Transform BuildPath(Transform root, byte[] data, int startIndex, bool includeLeaf, out int bufferIndex)
        {
            string path = GetString(data, startIndex, out bufferIndex);
            return CreateObjectPrefab(root, path);
        }

        public static void ApplyVisibility(GameObject obj)
        {
            Node node = nodes[obj.name];
            if (null != node.collectionInstance)
                obj = obj.transform.Find(OffsetTransformName).gameObject;

            Component[] components = obj.GetComponents<Component>();
            foreach(Component component in components)
            {
                Type componentType = component.GetType();
                var prop = componentType.GetProperty("enabled");
                if(null != prop)
                {
                    prop.SetValue(component, node.containerVisible & node.visible);
                }
            }
      
            //obj.SetActive(node.containerVisible & node.visible);
        }

        public static void ApplyTransformToInstances(Transform transform)
        {
            if (!nodes.ContainsKey(transform.name))
                return;

            Node node = nodes[transform.name];
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject obj = t.Item1;
                obj.transform.localPosition = transform.localPosition;
                obj.transform.localRotation = transform.localRotation;
                obj.transform.localScale = transform.localScale;
                ApplyVisibility(obj);
            }
        }

        public static Transform BuildTransform(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out currentIndex);

            float[] buffer = new float[4];
            bool[] boolBuffer = new bool[1];
            int size = 3 * sizeof(float);

            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localPosition = new Vector3(buffer[0], buffer[1], buffer[2]);

            size = 4 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localRotation = new Quaternion(buffer[0], buffer[1], buffer[2], buffer[3]);

            size = 3 * sizeof(float);
            Buffer.BlockCopy(data, currentIndex, buffer, 0, size);
            currentIndex += size;
            transform.localScale = new Vector3(buffer[0], buffer[1], buffer[2]);

            size = sizeof(bool);
            Buffer.BlockCopy(data, currentIndex, boolBuffer, 0, size);
            currentIndex += size;

            nodes[transform.name].visible = (bool)boolBuffer[0];

            ApplyTransformToInstances(transform);
            return transform;
        }

        public static NetCommand BuildTransformCommand(Transform root,Transform transform)
        {
            Transform current = transform;
            string path = current.name;
            while(current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }

            byte[] name = StringToBytes(path);
            byte[] positionBuffer = Vector3ToBytes(transform.localPosition);
            byte[] rotationBuffer = QuaternionToBytes(transform.localRotation);
            byte[] scaleBuffer = Vector3ToBytes(transform.localScale);
            byte[] visibilityBuffer = boolToBytes(transform.gameObject.activeSelf);

            List<byte[]> buffers = new List<byte[]>{ name, positionBuffer, rotationBuffer, scaleBuffer, visibilityBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Transform);
            return command;
        }

        public static NetCommand BuildMaterialCommand(Material material)
        {            
            byte[] name = StringToBytes(material.name);
            float op = 1f;
            if (material.HasProperty("_Opacity"))
                op = material.GetFloat("_Opacity");
            byte[] opacity = FloatToBytes(op);
            byte[] opacityMapTexture = StringToBytes("");
            byte[] baseColor = ColorToBytes(material.GetColor("_BaseColor"));
            byte[] baseColorTexture = StringToBytes("");
            byte[] metallic = FloatToBytes(material.GetFloat("_Metallic"));
            byte[] metallicTexture = StringToBytes("");
            byte[] roughness = FloatToBytes(1f - material.GetFloat("_Smoothness"));
            byte[] roughnessTexture = StringToBytes("");
            byte[] normalMapTexture = StringToBytes("");
            byte[] emissionColor = ColorToBytes(material.GetColor("_EmissionColor"));
            byte[] emissionColorTexture = StringToBytes("");

            List<byte[]> buffers = new List<byte[]> { name, opacity, opacityMapTexture, baseColor, baseColorTexture, metallic, metallicTexture, roughness, roughnessTexture, normalMapTexture, emissionColor, emissionColorTexture };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Material);
            return command;
        }

        public static NetCommand BuildCameraCommand(Transform root, CameraInfo cameraInfo)
        {
            Transform current = cameraInfo.transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] name = StringToBytes(path);

            Camera cam = cameraInfo.transform.GetComponentInChildren<Camera>(true);
            int sensorFit = (int)cam.gateFit;

            byte[] paramsBuffer = new byte[6 * sizeof(float) + 1 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(cam.focalLength), 0, paramsBuffer, 0 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.nearClipPlane), 0, paramsBuffer, 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.farClipPlane), 0, paramsBuffer, 2 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(1.8f), 0, paramsBuffer, 3 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(sensorFit), 0, paramsBuffer, 4 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.x), 0, paramsBuffer, 4 * sizeof(float) + sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(cam.sensorSize.y), 0, paramsBuffer, 5 * sizeof(float) + sizeof(int), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { name, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Camera);
            return command;
        }

        public static NetCommand BuildLightCommand(Transform root, LightInfo lightInfo)
        {
            Transform current = lightInfo.transform;
            string path = current.name;
            while (current.parent && current.parent != root)
            {
                current = current.parent;
                path = current.name + "/" + path;
            }
            byte[] name = StringToBytes(path);

            Light light = lightInfo.transform.GetComponentInChildren<Light>();
            int shadow = light.shadows != LightShadows.None ? 1 : 0;
            Color color = light.color;

            float power = 0f;
            float spotSize = 0;
            float spotBlend = 0;

            float worldScale = root.parent.localScale.x;
            float intensity = light.intensity / (worldScale * worldScale);

            switch (light.type)
            {
                case LightType.Point:
                    power = intensity * 10f;
                    break;
                case LightType.Directional:
                    power = intensity / 1.5f;
                    break;
                case LightType.Spot:
                    power = intensity / (0.4f / 3f);
                    spotSize = light.spotAngle / 180f * 3.14f;
                    spotBlend = 1f - (light.innerSpotAngle / 100f);
                    break;
            }

            byte[] paramsBuffer = new byte[2 * sizeof(int) + 7 * sizeof(float)];
            Buffer.BlockCopy(BitConverter.GetBytes((int)light.type), 0, paramsBuffer, 0 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(shadow), 0, paramsBuffer, 1 * sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.r), 0, paramsBuffer, 2 * sizeof(int), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.g), 0, paramsBuffer, 2 * sizeof(int) + 1 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.b), 0, paramsBuffer, 2 * sizeof(int) + 2 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(light.color.a), 0, paramsBuffer, 2 * sizeof(int) + 3 * sizeof(float), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(power), 0, paramsBuffer, 2 * sizeof(int) + 4 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(spotSize), 0, paramsBuffer, 2 * sizeof(int) + 5 * sizeof(float), sizeof(float));
            Buffer.BlockCopy(BitConverter.GetBytes(spotBlend), 0, paramsBuffer, 2 * sizeof(int) + 6 * sizeof(float), sizeof(float));

            List<byte[]> buffers = new List<byte[]> { name, paramsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Light);
            return command;

        }

        public static NetCommand BuildRenameCommand(Transform root, RenameInfo rename)
        {
            byte[] srcPath = StringToBytes(GetPathName(root, rename.srcTransform));
            byte[] dstName = StringToBytes(rename.newName);

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Rename);
            return command;
        }

        public static NetCommand BuildDuplicateCommand(Transform root, DuplicateInfos duplicate)
        {
            byte[] srcPath = StringToBytes(GetPathName(root, duplicate.srcObject.transform));
            byte[] dstName = StringToBytes(duplicate.dstObject.name);

            Transform transform = duplicate.dstObject.transform;
            byte[] positionBuffer = Vector3ToBytes(transform.localPosition);
            byte[] rotationBuffer = QuaternionToBytes(transform.localRotation);
            byte[] scaleBuffer = Vector3ToBytes(transform.localScale);

            List<byte[]> buffers = new List<byte[]> { srcPath, dstName, positionBuffer, rotationBuffer, scaleBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Duplicate);
            return command;
        }

        public static NetCommand BuildSendToTrashCommand(Transform root, SendToTrashInfo sendToTrash)
        {
            byte[] path = StringToBytes(GetPathName(root, sendToTrash.transform));
            List<byte[]> buffers = new List<byte[]> { path };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.SendToTrash);
            return command;
        }

        public static NetCommand BuildRestoreFromTrashCommand(Transform root, RestoreFromTrashInfo sendToTrash)
        {
            string path = "";
            if (sendToTrash.transform.parent != root) 
                path = GetPathName(root, sendToTrash.transform.parent);

            byte[] nameBuffer = StringToBytes(sendToTrash.transform.name);
            byte[] pathBuffer = StringToBytes(path);

            List<byte[]> buffers = new List<byte[]> { nameBuffer, pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.RestoreFromTrash);
            return command;
        }

        public static NetCommand BuildMeshCommand(MeshInfos meshInfos)
        {
            Mesh mesh = meshInfos.meshFilter.mesh;
            byte[] name = StringToBytes(mesh.name);

            byte[] positions = Vector3ToBytes(mesh.vertices);
            byte[] normals = Vector3ToBytes(mesh.normals);
            byte[] uvs = Vector2ToBytes(mesh.uv);

            byte[] materialIndices = new byte[sizeof(int) + mesh.subMeshCount * 2 * sizeof(int)];
            Buffer.BlockCopy(BitConverter.GetBytes(mesh.subMeshCount), 0, materialIndices, 0, sizeof(int));
            int offset = sizeof(int);

            for (int i = 0; i <  mesh.subMeshCount; i++)
            {
                SubMeshDescriptor subMesh = mesh.GetSubMesh(i);
                int start = subMesh.indexStart / 3;
                Buffer.BlockCopy(BitConverter.GetBytes(start), 0, materialIndices, offset, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(i), 0, materialIndices, offset + sizeof(int), sizeof(int));
                offset += 2 * sizeof(int);
            }

            byte[] triangles = TriangleIndicesToBytes(mesh.triangles);

            Material[] materials = meshInfos.meshRenderer.sharedMaterials;
            string[] materialNames = new string[materials.Length];
            int index = 0;
            foreach (Material material in materials)
            {
                materialNames[index++] = material.name;

            }
            byte[] materialsBuffer = StringsToBytes(materialNames);

            List<byte[]> buffers = new List<byte[]> { name, positions, normals, uvs, materialIndices, triangles, materialsBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Mesh);
            return command;
        }

        public static NetCommand BuildMeshConnectionCommand(Transform root, MeshConnectionInfos meshConnectionInfos)
        {
            Transform transform = meshConnectionInfos.meshTransform;
            Mesh mesh = transform.GetComponent<MeshFilter>().mesh;
            string path = GetPathName(root, transform);

            byte[] pathBuffer = StringToBytes(path);
            byte[] nameBuffer = StringToBytes(mesh.name);

            List<byte[]> buffers = new List<byte[]> { pathBuffer, nameBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.MeshConnection);
            return command;
        }

        public static NetCommand BuildDeleteCommand(Transform root, DeleteInfo deleteInfo)
        {
            Transform transform = deleteInfo.meshTransform;
            string path = GetPathName(root, transform);
            byte[] pathBuffer = StringToBytes(path);

            List<byte[]> buffers = new List<byte[]> { pathBuffer };
            NetCommand command = new NetCommand(ConcatenateBuffers(buffers), MessageType.Delete);
            return command;
        }

        public static void BuildCamera(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string path = GetString(data, 0, out currentIndex);


            Transform transform = root;
            if (transform == null)
                return;
            
            GameObject camGameObject = null;
            string[] splittedPath = path.Split('/');
            string name = splittedPath[splittedPath.Length - 1];
            Transform camTransform = transform.Find(name);
            if (camTransform == null)
            {
                camGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, transform);
                camGameObject.name = name;
                Node node = CreateNode(name);
                node.prefab = camGameObject;

                //camGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                //camGameObject.transform.GetChild(0).localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                camGameObject = camTransform.gameObject;
            }

            float focal = BitConverter.ToSingle(data, currentIndex);
            float near = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float far = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float aperture = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            currentIndex += 4 * sizeof(float);

            Camera.GateFitMode gateFit = (Camera.GateFitMode)BitConverter.ToInt32(data, currentIndex);
            if (gateFit == Camera.GateFitMode.None)
                gateFit = Camera.GateFitMode.Horizontal;
            currentIndex += sizeof(Int32);

            float sensorWidth = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float sensorHeight = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            Camera cam = camGameObject.GetComponentInChildren<Camera>(true);

            // Is it necessary ?
            /////////////////
            CameraController cameraController = camGameObject.GetComponent<CameraController>();
            CameraParameters cameraParameters = (CameraParameters)cameraController.GetParameters();
            cameraParameters.focal = focal;
            //cameraParameters.gateFit = gateFit;
            /////////////////

            cam.focalLength = focal;
            cam.gateFit = gateFit;

            cameraParameters.focal = focal;
            cam.focalLength = focal;
            cam.sensorSize = new Vector2(sensorWidth, sensorHeight);

            cameraController.FireValueChanged();
        }

        public static void BuildLight(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string path = GetString(data, 0, out currentIndex);
            Transform transform = root;/*BuildPath(root, data, 0, false, out currentIndex);*/
            if (transform == null)
                return;

            LightType lightType = (LightType)BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            GameObject lightGameObject = null;
            string[] splittedPath = path.Split('/');
            string name = splittedPath[splittedPath.Length - 1];
            Transform lightTransform = transform.Find(name);
            if (lightTransform == null)
            {
                switch (lightType)
                {
                    case LightType.Directional:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Sun") as GameObject, transform);
                        break;
                    case LightType.Point:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Point") as GameObject, transform);
                        break;
                    case LightType.Spot:
                        lightGameObject = Utils.CreateInstance(Resources.Load("Prefabs/Spot") as GameObject, transform);
                        break;
                }
                //lightGameObject.transform.GetChild(0).Rotate(0f, 180f, 0f);
                lightGameObject.name = name;
                Node node = CreateNode(name);
                node.prefab = lightGameObject;
            }
            else
            {
                lightGameObject = lightTransform.gameObject;
            }

            // Read data
            int shadow = BitConverter.ToInt32(data, currentIndex);
            currentIndex += sizeof(Int32);

            float ColorR = BitConverter.ToSingle(data, currentIndex);
            float ColorG = BitConverter.ToSingle(data, currentIndex + sizeof(float));
            float ColorB = BitConverter.ToSingle(data, currentIndex + 2 * sizeof(float));
            float ColorA = BitConverter.ToSingle(data, currentIndex + 3 * sizeof(float));
            Color lightColor = new Color(ColorR, ColorG, ColorB, ColorA);
            currentIndex += 4 * sizeof(float);

            float power = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotSize = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);
            float spotBlend = BitConverter.ToSingle(data, currentIndex);
            currentIndex += sizeof(float);

            // Set data to all instances
            LightController lightController = lightGameObject.GetComponent<LightController>();
            LightParameters lightParameters = (LightParameters)lightController.GetParameters();

            foreach (Tuple<GameObject, string> t in nodes[lightGameObject.name].instances)
            {
                GameObject gobj = t.Item1;
                LightController lightContr = gobj.GetComponent<LightController>();
                lightContr.parameters = lightParameters;
            }
            lightParameters.color = lightColor;
            switch(lightType)
            {
                case LightType.Point:
                    lightParameters.intensity = power / 10f;
                    break;
                case LightType.Directional:
                    lightParameters.intensity = power * 1.5f;
                    break;
                case LightType.Spot:
                    lightParameters.intensity = power * 0.4f / 3f;
                    break;
            }

            if (lightType == LightType.Spot)
            {
                lightParameters.SetRange(1000f);
                lightParameters.SetOuterAngle(spotSize * 180f / 3.14f);
                lightParameters.SetInnerAngle((1f - spotBlend) * 100f);
            }
            lightParameters.castShadows = shadow != 0 ? true : false;
            lightController.FireValueChanged();
        }

        public static MeshFilter GetOrCreateMeshFilter(GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = obj.AddComponent<MeshFilter>();
            return meshFilter;
        }

        public static MeshRenderer GetOrCreateMeshRenderer(GameObject obj)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = obj.AddComponent<MeshRenderer>();
            return meshRenderer;
        }
        public static MeshCollider GetOrCreateMeshCollider(GameObject obj)
        {
            MeshCollider meshCollider = obj.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = obj.AddComponent<MeshCollider>();
            return meshCollider;
        }

        public static Transform ConnectMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            Transform transform = BuildPath(root, data, 0, true, out currentIndex);
            string meshName = GetString(data, currentIndex, out currentIndex);

            GameObject gobject = transform.gameObject;

            Mesh mesh = meshes[meshName];


            gobject.tag = "PhysicObject";

            if (!meshInstances.ContainsKey(meshName))
                meshInstances[meshName] = new HashSet<MeshFilter>();

            MeshFilter meshFilter = GetOrCreateMeshFilter(gobject);
            meshInstances[meshName].Add(meshFilter);

            foreach(MeshFilter filter in meshInstances[meshName])
            {
                filter.mesh = mesh;
                GameObject obj = filter.gameObject;
                MeshRenderer meshRenderer = GetOrCreateMeshRenderer(obj);
                meshRenderer.sharedMaterials = meshesMaterials[meshName];
                GetOrCreateMeshCollider(obj);

                if (nodes.ContainsKey(obj.name))
                {
                    foreach(Tuple<GameObject, string> t in nodes[obj.name].instances)
                    {
                        GameObject instance = t.Item1;
                        MeshFilter instanceMeshFilter = GetOrCreateMeshFilter(instance);
                        instanceMeshFilter.mesh = mesh;

                        MeshRenderer instanceMeshRenderer = GetOrCreateMeshRenderer(instance);
                        instanceMeshRenderer.sharedMaterials = meshesMaterials[meshName];

                        GetOrCreateMeshCollider(instance);
                    }
                }
            }

            return transform;
        }

        public static Mesh BuildMesh(Transform root, byte[] data)
        {
            int currentIndex = 0;
            string meshName = GetString(data, currentIndex, out currentIndex);

            int verticesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int size = verticesCount * sizeof(float) * 3;
            Vector3[] vertices = new Vector3[verticesCount];
            float[] float3Values = new float[verticesCount * 3];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            int idx = 0;
            for(int i = 0; i < verticesCount; i++)
            {
                vertices[i].x = float3Values[idx++];
                vertices[i].y = float3Values[idx++];
                vertices[i].z = float3Values[idx++];
            }
            currentIndex += size;

            int normalsCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            size = normalsCount * sizeof(float) * 3;
            Vector3[] normals = new Vector3[normalsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < verticesCount; i++)
            {
                normals[i].x = float3Values[idx++];
                normals[i].y = float3Values[idx++];
                normals[i].z = float3Values[idx++];
            }
            currentIndex += size;

            UInt32 UVsCount = BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;

            size = (int)UVsCount * sizeof(float) * 2;
            Vector2[] uvs = new Vector2[UVsCount];
            Buffer.BlockCopy(data, currentIndex, float3Values, 0, size);
            idx = 0;
            for (int i = 0; i < UVsCount; i++)
            {
                uvs[i].x = float3Values[idx++];
                uvs[i].y = float3Values[idx++];
            }
            currentIndex += size;

            int materialIndicesCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            int[] materialIndices = new int[materialIndicesCount * 2];
            size = materialIndicesCount * 2 * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, materialIndices, 0, size);
            currentIndex += size;

            int indicesCount = (int)BitConverter.ToUInt32(data, currentIndex) * 3;
            currentIndex += 4;
            int[] indices = new int[indicesCount];
            size = indicesCount * sizeof(int);
            Buffer.BlockCopy(data, currentIndex, indices, 0, size);
            currentIndex += size;


            int materialCount = (int)BitConverter.ToUInt32(data, currentIndex);
            currentIndex += 4;
            Material[] meshMaterials;
            if (materialCount == 0)
            {
                meshMaterials = new Material[1];
                meshMaterials[0] = DefaultMaterial();
                materialCount = 1;
            }
            else
            {
                meshMaterials = new Material[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    int materialNameSize = (int)BitConverter.ToUInt32(data, currentIndex);
                    string materialName = System.Text.Encoding.UTF8.GetString(data, currentIndex + 4, materialNameSize);
                    currentIndex += materialNameSize + 4;

                    meshMaterials[i] = null;
                    if (materials.ContainsKey(materialName))
                    {
                        meshMaterials[i] = materials[materialName];
                    }
                    else
                    {
                        meshMaterials[i] = DefaultMaterial();
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = meshName;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            if (materialCount == 1) // only one submesh
                mesh.triangles = indices;
            else
            {
                int remainingTringles = indicesCount / 3;
                int currentTriangleIndex = 0;
                mesh.subMeshCount = materialCount;

                int[][] subIndices = new int[materialCount][];
                int[] trianglesPerMaterialCount = new int[materialCount];
                int[] subIndicesIndices = new int[materialCount];
                for (int i = 0; i < materialCount; i++)
                {
                    trianglesPerMaterialCount[i] = 0;
                    subIndicesIndices[i] = 0;
                }

                // count
                for (int i = 0; i < materialIndicesCount; i++)
                {
                    int triangleCount = remainingTringles;
                    if (i < (materialIndicesCount - 1))
                    {
                        triangleCount = materialIndices[(i + 1) * 2] - materialIndices[i * 2];
                        remainingTringles -= triangleCount;
                    }
                    int materialIndex = materialIndices[i * 2 + 1];
                    trianglesPerMaterialCount[materialIndex] += triangleCount;
                }

                //allocate
                for(int i = 0; i < materialCount; i++)
                {
                    subIndices[i] = new int[trianglesPerMaterialCount[i] * 3];
                }

                // fill
                remainingTringles = indicesCount / 3;
                for (int i = 0; i < materialIndicesCount; i++)
                {
                    // allocate triangles
                    int triangleCount = remainingTringles;
                    if (i < (materialIndicesCount - 1))
                    {
                        triangleCount = materialIndices[(i + 1) * 2] - materialIndices[i * 2];
                        remainingTringles -= triangleCount;
                    }
                    int materialIndex = materialIndices[i * 2 + 1];
                    int dataSize = triangleCount * 3 * sizeof(int);
                    Buffer.BlockCopy(indices, currentTriangleIndex, subIndices[materialIndex], subIndicesIndices[materialIndex], dataSize);
                    subIndicesIndices[materialIndex] += dataSize;
                    currentTriangleIndex += dataSize;
                }

                // set
                for(int i = 0; i < materialCount; i++)
                {
                    mesh.SetTriangles(subIndices[i], i);
                }
            }

            mesh.RecalculateBounds();
            meshes[meshName] = mesh;
            meshesMaterials[meshName] = meshMaterials;
           
            return mesh;
        }
    }

    public class NetworkClient : MonoBehaviour
    {
        private static NetworkClient _instance;
        public Transform root;
        public Transform prefab;
        public int port = 12800;

        Thread thread = null;
        bool alive = true;
        bool connected = false;

        Socket socket = null;
        List<NetCommand> receivedCommands = new List<NetCommand>();
        List<NetCommand> pendingCommands = new List<NetCommand>();

        public void Awake()
        {
            _instance = this;
        }

        public static NetworkClient GetInstance()
        {
            return _instance;
        }

        void OnDestroy()
        {
            Join();
        }

        void Start()
        {
            Connect();
            NetGeometry.prefabNode.prefab = prefab.gameObject;
            NetGeometry.nodes.Add(prefab.name, NetGeometry.prefabNode);

            NetGeometry.rootNode.prefab = root.gameObject;
            NetGeometry.nodes.Add(root.name, NetGeometry.rootNode);

            NetGeometry.instanceRoot["/"] = root;
        }

        public void Connect()
        {
            connected = false;
            string[] args = System.Environment.GetCommandLineArgs();
            string room = "Local";
            string hostname = "localhost";
            int port = 12800;

            //hostname = "lgy-wks-054880";
            //room = "thomas.capelle";

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--room")
                {
                    room = args[i + 1];
                }

                if (args[i] == "--hostname")
                {
                    hostname = args[i + 1];
                }

                if (args[i] == "--port")
                {
                    Int32.TryParse(args[i + 1], out port);
                }

            }
            
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
            if (ipHostInfo.AddressList.Length == 0)
                return;

            IPAddress ipAddress = null;
            for (int i = ipHostInfo.AddressList.Length - 1; i >= 0; i --)
            {
                IPAddress addr = ipHostInfo.AddressList[i];
                if (addr.ToString().Contains(":"))
                    continue;
                ipAddress = addr;
                break;
            }

            if (null == ipAddress)
                return;
                
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP  socket.  
            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.  
            try
            {
                socket.Connect(remoteEP);
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                return;
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
                return;
            }

            JoinRoom(room);
            connected = true;

            thread = new Thread(new ThreadStart(Run));
            thread.Start();
        }

        public void Join()
        {
            if (thread == null)
                return;
            alive = false;
            thread.Join();
            socket.Disconnect(false);
        }

        NetCommand ReadMessage()
        {
            int count = socket.Available;
            if (count < 14)
                return null;

            byte[] header = new byte[14];
            socket.Receive(header, 0, 14, SocketFlags.None);

            var size = BitConverter.ToInt64(header, 0);
            var commandId = BitConverter.ToInt32(header, 8);
            Debug.Log("Received Command Id " + commandId);
            var mtype = BitConverter.ToUInt16(header, 8 + 4);

            byte[] data = new byte[size];
            long remaining = size;
            long current = 0;
            while (remaining > 0)
            {
                int sizeRead = socket.Receive(data, (int)current, (int)remaining, SocketFlags.None);
                current += sizeRead;
                remaining -= sizeRead;
            }


            NetCommand command = new NetCommand(data, (MessageType)mtype);
            return command;
        }

        void WriteMessage(NetCommand command)
        {
            byte[] sizeBuffer = BitConverter.GetBytes((Int64)command.data.Length);
            byte[] commandId = BitConverter.GetBytes((Int32)command.id);
            byte[] typeBuffer = BitConverter.GetBytes((Int16)command.messageType);
            List<byte[]> buffers = new List<byte[]> { sizeBuffer, commandId, typeBuffer, command.data };

            socket.Send(NetGeometry.ConcatenateBuffers(buffers));
        }

        void AddCommand(NetCommand command)
        {
            lock (this)
            {
                pendingCommands.Add(command);
            }
        }

        public void SendTransform(Transform transform)
        {
            NetCommand command = NetGeometry.BuildTransformCommand(root, transform);
            AddCommand(command);
        }

        public void SendMesh(MeshInfos meshInfos)
        {
            NetCommand command = NetGeometry.BuildMeshCommand(meshInfos);
            AddCommand(command);
        }

        public void SendMeshConnection(MeshConnectionInfos meshConnectionInfos)
        {
            NetCommand command = NetGeometry.BuildMeshConnectionCommand(root, meshConnectionInfos);
            AddCommand(command);
        }

        public void SendDelete(DeleteInfo deleteInfo)
        {
            NetCommand command = NetGeometry.BuildDeleteCommand(root, deleteInfo);
            AddCommand(command);
        }

        public void SendMaterial(Material material)
        {
            NetCommand command = NetGeometry.BuildMaterialCommand(material);
            AddCommand(command);
        }
        public void SendCamera(CameraInfo cameraInfo)
        {
            NetCommand command = NetGeometry.BuildCameraCommand(root, cameraInfo);
            AddCommand(command);
        }
        public void SendLight(LightInfo lightInfo)
        {
            NetCommand command = NetGeometry.BuildLightCommand(root, lightInfo);
            AddCommand(command);
        }
        public void SendRename(RenameInfo rename)
        {
            NetCommand command = NetGeometry.BuildRenameCommand(root, rename);
            AddCommand(command);
        }

        public void SendDuplicate(DuplicateInfos duplicate)
        {
            NetCommand command = NetGeometry.BuildDuplicateCommand(root, duplicate);
            AddCommand(command);
        }

        public void SendToTrash(SendToTrashInfo sendToTrash)
        {
            NetCommand command = NetGeometry.BuildSendToTrashCommand(root, sendToTrash);
            AddCommand(command);
        }

        public void RestoreFromTrash(RestoreFromTrashInfo restoreFromTrash)
        {
            NetCommand command = NetGeometry.BuildRestoreFromTrashCommand(root, restoreFromTrash);
            AddCommand(command);
        }

        public void JoinRoom(string roomName)
        {
            NetCommand command = new NetCommand(System.Text.Encoding.UTF8.GetBytes(roomName), MessageType.JoinRoom);
            AddCommand(command);
        }

        void Send(byte[] data)
        {
            lock (this)
            {
                socket.Send(data);
            }
        }

        void Run()
        {
            while(alive)
            {
                NetCommand command = ReadMessage();
                if(command != null)
                {
                    if(command.messageType > MessageType.Command)
                    {
                        lock (this)
                        {
                            receivedCommands.Add(command);
                        }
                    }
                }

                lock (this)
                {
                    if (pendingCommands.Count > 0)
                    {
                        foreach (NetCommand pendingCommand in pendingCommands)
                        {
                            WriteMessage(pendingCommand);
                        }
                        pendingCommands.Clear();
                    }
                }
            }
        }

        void Update()
        {
            lock (this)
            {
                if (receivedCommands.Count == 0)
                    return;

                foreach (NetCommand command in receivedCommands)
                {
                    Debug.Log("Command Id " + command.id.ToString());
                    switch (command.messageType)
                    {
                        case MessageType.Mesh:
                            NetGeometry.BuildMesh(prefab, command.data);
                            break;
                        case MessageType.MeshConnection:
                            NetGeometry.ConnectMesh(prefab, command.data);
                            break;
                        case MessageType.Transform:
                            NetGeometry.BuildTransform(prefab, command.data);
                            break;
                        case MessageType.Material:
                            NetGeometry.BuildMaterial(command.data);
                            break;
                        case MessageType.Camera:
                            NetGeometry.BuildCamera(prefab, command.data);
                            break;
                        case MessageType.Light:
                            NetGeometry.BuildLight(prefab, command.data);
                            break;
                        case MessageType.Delete:
                            NetGeometry.Delete(prefab, command.data);
                            break;
                        case MessageType.Rename:
                            NetGeometry.Rename(prefab, command.data);
                            break;
                        case MessageType.Duplicate:
                            NetGeometry.Duplicate(prefab, command.data);
                            break;
                        case MessageType.SendToTrash:
                            NetGeometry.BuildSendToTrash(prefab, command.data);
                            break;
                        case MessageType.RestoreFromTrash:
                            NetGeometry.BuildRestoreFromTrash(prefab, command.data);
                            break;
                        case MessageType.Texture:
                            NetGeometry.BuildTexture(command.data);
                            break;
                        case MessageType.Collection:
                            NetGeometry.BuildCollection(command.data);
                            break;
                        case MessageType.CollectionRemoved:
                            NetGeometry.BuildCollectionRemoved(command.data);
                            break;
                        case MessageType.AddCollectionToCollection:
                            NetGeometry.BuildAddCollectionToCollection(prefab, command.data);
                            break;
                        case MessageType.RemoveCollectionFromCollection:
                            NetGeometry.BuildRemoveCollectionFromCollection(prefab, command.data);
                            break;
                        case MessageType.AddObjectToCollection:
                            NetGeometry.BuildAddObjectToCollection(prefab, command.data);
                            break;
                        case MessageType.RemoveObjectFromCollection:
                            NetGeometry.BuildRemoveObjectFromCollection(prefab, command.data);
                            break;
                        case MessageType.CollectionInstance:
                            NetGeometry.BuildCollectionInstance(prefab, command.data);
                            break;
                        case MessageType.AddObjectToScene:
                            NetGeometry.BuildAddObjectToScene(root, command.data);
                            break;
                        case MessageType.AddCollectionToScene:
                            NetGeometry.BuilAddCollectionToScene(root, command.data);
                            break;
                        case MessageType.SetScene:
                            NetGeometry.BuilSetScene(root, command.data);
                            break;
                    }
                }
                receivedCommands.Clear();
            }
        }

        public void SendEvent<T>(MessageType messageType, T data)
        {
            if(!connected) { return; }
            switch(messageType)
            {
                case MessageType.Transform:
                    SendTransform(data as Transform); break;
                case MessageType.Mesh:
                    SendMesh(data as MeshInfos); break;
                case MessageType.MeshConnection:
                    SendMeshConnection(data as MeshConnectionInfos); break;
                case MessageType.Delete:
                    SendDelete(data as DeleteInfo); break;
                case MessageType.Material:
                    SendMaterial(data as Material); break;
                case MessageType.Camera:
                    SendCamera(data as CameraInfo);
                    SendTransform((data as CameraInfo).transform);
                    break;
                case MessageType.Light:
                    SendLight(data as LightInfo); 
                    SendTransform((data as LightInfo).transform);
                    break;
                case MessageType.Rename:
                    SendRename(data as RenameInfo); break;
                case MessageType.Duplicate:
                    SendDuplicate(data as DuplicateInfos); break;
                case MessageType.SendToTrash:
                    SendToTrash(data as SendToTrashInfo); break;
                case MessageType.RestoreFromTrash:
                    RestoreFromTrash(data as RestoreFromTrashInfo); break;
            }
        }
    }
}