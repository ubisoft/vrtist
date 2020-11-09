using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace VRtist
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
        public bool tempVisible = true;
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
            if (collections.Count == 0)
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

        public void AddInstance(GameObject obj, string collectionInstanceName = "/")
        {
            instances.Add(new Tuple<GameObject, string>(obj, collectionInstanceName));
            SyncData.mixer.OnInstanceAdded(obj);
        }
        public void RemoveInstance(GameObject obj)
        {
            foreach (Tuple<GameObject, string> item in instances)
            {
                if (item.Item1 == obj)
                {
                    instances.Remove(item);
                    SyncData.mixer.OnInstanceRemoved(obj);
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
        public bool tempVisible;
        public Vector3 offset;

        public bool IsVisible()
        {
            if (!visible || !tempVisible)
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

    public static class SyncData
    {
        static int gameObjectNameId = 0;
        static readonly long timestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        static GameObject trash = null;

        public static Dictionary<GameObject, Node> instancesToNodes = new Dictionary<GameObject, Node>();
        public static Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        public static Dictionary<string, CollectionNode> collectionNodes = new Dictionary<string, CollectionNode>();
        public static Dictionary<string, Transform> instanceRoot = new Dictionary<string, Transform>();
        public static Node prefabNode = new Node();
        public static Node rootNode = new Node();
        public static string OffsetTransformName = "__Offset";

        public static string currentSceneName = "";
        public static HashSet<string> sceneCollections = new HashSet<string>();

        public static Dictionary<string, string> greasePencilsNameToPrefab = new Dictionary<string, string>();

        public static Transform root = null;
        public static Transform prefab = null;

        public static MixerInterface mixer;

        public static void Init(Transform p, Transform r)
        {
            prefab = p;
            root = r;
            prefabNode.prefab = prefab.gameObject;
            nodes.Add(prefab.name, prefabNode);

            rootNode.prefab = root.gameObject;
            nodes.Add(root.name, rootNode);

            instanceRoot["/"] = root;
        }

        public static GameObject GetTrash()
        {
            if (trash == null)
            {
                trash = new GameObject("__Trash__");
                trash.SetActive(false);
            }
            return trash;
        }

        public static bool IsInTrash(GameObject obj)
        {
            GameObject trash = GetTrash();
            if (obj.transform.parent.parent.gameObject == trash)
                return true;
            return false;
        }

        public static CollectionNode CreateCollectionNode(CollectionNode parent, string name)
        {
            if (name == "__Trash__")
                return null;

            CollectionNode newNode = new CollectionNode(name);
            collectionNodes.Add(name, newNode);
            if (parent != null)
                parent.AddChild(newNode);
            return newNode;
        }

        public static Node CreateNode(string name, Node parentNode = null)
        {
            Node newNode = new Node();
            if (nodes.ContainsKey(name)) // secu
                nodes.Remove(name);
            nodes.Add(name, newNode);
            if (null != parentNode)
                parentNode.AddChild(newNode);
            return newNode;
        }

        public static void AddInstanceToNode(GameObject obj, Node node, string collectionInstanceName)
        {
            node.AddInstance(obj, collectionInstanceName);
            instancesToNodes[obj] = node;
        }

        public static void RemoveInstanceFromNode(GameObject obj)
        {
            if (instancesToNodes.ContainsKey(obj))
            {
                Node node = instancesToNodes[obj];
                node.RemoveInstance(obj);
                instancesToNodes.Remove(obj);
            }
        }

        public static void FindObjects(ref List<Transform> objects, Transform t, string name)
        {
            if (t.name == name)
                objects.Add(t);

            for (int i = 0; i < t.childCount; i++)
                FindObjects(ref objects, t.GetChild(i), name);
        }

        public static Transform FindRecursive(Transform t, string objectName)
        {
            if (t.name == objectName)
                return t;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform res = FindRecursive(t.GetChild(i), objectName);
                if (null != res)
                    return res;
            }
            return null;
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

        public static void AddCollectionInstance(Transform transform, string collectionName)
        {
            CollectionNode collectionNode = SyncData.collectionNodes.ContainsKey(collectionName) ? SyncData.collectionNodes[collectionName] : SyncData.CreateCollectionNode(null, collectionName);
            Node instanceNode = SyncData.nodes.ContainsKey(transform.name) ? SyncData.nodes[transform.name] : SyncData.CreateNode(transform.name);

            instanceNode.prefab = transform.gameObject;
            instanceNode.collectionInstance = collectionNode;
            collectionNode.AddPrefabInstanceNode(instanceNode);
        }

        public static void RemoveObjectFromCollection(string collectionName, string objectName)
        {
            if (collectionName == "__Trash__")
            {
                return;
            }

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
                    RemoveObjectFromScene(offsetObject, objectName, obj.name);
                }
            }

            collectionNode.RemoveObject(nodes[objectName]);
        }

        public static void RemoveObjectFromScene(Transform transform, string objectName, string collectionInstanceName)
        {
            Transform obj = FindRecursive(transform, objectName);
            if (null == obj)
                return;

            if (nodes.ContainsKey(objectName))
            {
                Node objectNode = nodes[objectName];
                objectNode.instances.Remove(new Tuple<GameObject, string>(obj.gameObject, collectionInstanceName));
            }

            for (int i = 0; i < obj.childCount; i++)
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
                RemoveObjectFromScene(obj, child.name, childCollectionInstanceName);
            }

            GameObject.Destroy(obj.gameObject);
        }

        public static void AddObjectToCollection(string collectionName, string objectName)
        {
            if (collectionName == "__Trash__")
            {
                RemovePrefab(objectName);
                return;
            }

            if (!collectionNodes.ContainsKey(collectionName))
                return;
            CollectionNode collectionNode = collectionNodes[collectionName];

            if (!nodes.ContainsKey(objectName))
                GetOrCreatePrefabPath(objectName);

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

                    AddObjectToDocument(offsetObject, objectNode.prefab.name, subCollectionInstanceName);
                }
            }

            foreach (Tuple<GameObject, string> item in objectNode.instances)
            {
                ApplyVisibility(item.Item1);
            }
        }

        public static void RemoveCollectionFromCollection(string parentCollectionName, string collectionName)
        {
            if (collectionNodes.ContainsKey(parentCollectionName))
            {
                CollectionNode parentCollectionNode = collectionNodes[parentCollectionName];
                parentCollectionNode.RemoveChild(collectionNodes[collectionName]);
            }
        }

        public static void AddCollectionToCollection(string parentCollectionName, string collectionName)
        {
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

        public static void AddCollection(string collectionName, Vector3 offset, bool visible, bool tempVisible)
        {
            if (collectionName == "__Trash__")
                return;

            CollectionNode collectionNode = collectionNodes.ContainsKey(collectionName) ? collectionNodes[collectionName] : CreateCollectionNode(null, collectionName);

            collectionNode.visible = visible;
            collectionNode.tempVisible = tempVisible;
            collectionNode.offset = offset;

            List<Node> nodes = new List<Node>();
            GetRecursiveObjectsOfCollection(collectionNode, ref nodes);
            foreach (Node node in nodes)
            {
                node.ComputeContainerVisibility();

                foreach (Tuple<GameObject, string> item in node.instances)
                {
                    ApplyVisibility(item.Item1, node.containerVisible, "");
                }

            }

            // collection instances management
            foreach (Node prefabInstanceNode in collectionNode.prefabInstanceNodes)
            {
                foreach (Tuple<GameObject, string> item in prefabInstanceNode.instances)
                {
                    ApplyVisibility(item.Item1);
                    GameObject offsetObject = item.Item1.transform.Find(OffsetTransformName).gameObject;
                    offsetObject.transform.localPosition = offset;
                }
            }
        }

        public static void RemoveCollection(string collectionName)
        {
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

        public static void Delete(string objectName)
        {
            Node node = nodes[objectName];
            for (int n = node.instances.Count - 1; n >= 0; n--)
            {
                GameObject gobj = node.instances[n].Item1;

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
                    RemoveInstanceFromNode(gobj);
                    GameObject.Destroy(gobj);
                }
                else
                {
                    RemoveInstanceFromNode(gobj);

                    for (int j = 0; j < gobj.transform.childCount; j++)
                        Reparent(gobj.transform.GetChild(j), gobj.transform.parent);

                    GameObject.Destroy(gobj);
                }
            }
        }

        public static void Rename(string srcName, string dstName)
        {
            if (nodes.ContainsKey(srcName))
            {
                Node node = nodes[srcName];
                node.prefab.name = dstName;
                foreach (Tuple<GameObject, string> obj in node.instances)
                {
                    obj.Item1.name = dstName;
                    mixer.OnObjectRenamed(obj.Item1);
                }
                nodes[dstName] = node;
                nodes.Remove(srcName);
            }
        }

        public static void DeleteCollectionInstance(GameObject obj)
        {
            SyncData.RemoveInstanceFromNode(obj);
            for (int i = obj.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = obj.transform.GetChild(i);
                DeleteCollectionInstance(child.gameObject);
            }
        }

        public static void AddCollectionToScene(CollectionNode collectionNode, Transform transform, string collectionInstanceName)
        {
            foreach (Node collectionObject in collectionNode.objects)
            {
                AddObjectToDocument(transform, collectionObject.prefab.name, collectionInstanceName);
            }
            foreach (CollectionNode collectionChild in collectionNode.children)
            {
                AddCollectionToScene(collectionChild, transform, collectionInstanceName);
            }
        }

        public static string CreateUniqueName(string baseName)
        {
            if (baseName.Length > 48)
                baseName = baseName.Substring(0, 48);

            string name = baseName + "." + String.Format("{0:X}", (Dns.GetHostName() + timestamp.ToString()).GetHashCode()) + "." + gameObjectNameId.ToString();
            gameObjectNameId++;
            return name;
        }

        public static string GetMaterialName(GameObject gobject)
        {
            return "Mat_" + gobject.name;
        }

        public static GameObject CreateFullHierarchyPrefab(GameObject original)
        {
            string rootPath = "__VRtist_Asset_Bank__";
            GameObject root = null;
            foreach (var trans in original.GetComponentsInChildren<Transform>())
            {
                string path = trans.parent != null ? trans.parent.name + "/" + trans.name : trans.name;
                if (path.StartsWith(rootPath))
                {
                    path = path.Substring(rootPath.Length + 1);
                }
                Transform res = GetOrCreatePrefabPath(path);
                res.localPosition = trans.localPosition;
                res.localRotation = trans.localRotation;
                res.localScale = trans.localScale;

                if (trans.gameObject == original) { root = res.gameObject; }
                MeshFilter meshFilter = trans.GetComponent<MeshFilter>();
                if (null != meshFilter && null != meshFilter.sharedMesh)
                {
                    MixerUtils.ConnectMesh(res, meshFilter.sharedMesh);
                }

                MeshRenderer meshRenderer = trans.GetComponent<MeshRenderer>();
                if (null != meshRenderer && null != meshRenderer.sharedMaterials)
                {
                    MeshRenderer dstMeshRenderer = res.GetComponent<MeshRenderer>();
                    if(null != dstMeshRenderer)
                        dstMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;
                }
            }
            return root;
        }

        public static GameObject CreateInstance(GameObject gObject, Transform parent, string name = null, bool isPrefab = false)
        {
            GameObject intermediateParent = new GameObject();
            intermediateParent.transform.parent = parent;
            Transform srcParent = gObject.transform.parent;
            if (null != srcParent)
            {
                intermediateParent.transform.localPosition = srcParent.localPosition;
                intermediateParent.transform.localRotation = srcParent.localRotation;
                intermediateParent.transform.localScale = srcParent.localScale;
            }

            GameObject res;
            GameObjectBuilder builder = gObject.GetComponent<GameObjectBuilder>();
            if (builder)
            {
                res = builder.CreateInstance(gObject, intermediateParent.transform, isPrefab);
            }
            else
            {
                // duplicate object or subobject
                res = GameObject.Instantiate(gObject, intermediateParent.transform);
            }

            string appliedName;
            if (null == name)
            {
                string baseName = gObject.name.Split('.')[0];
                appliedName = CreateUniqueName(baseName);
            }
            else
            {
                appliedName = name;
            }
            res.name = appliedName;
            intermediateParent.name = appliedName + "_parent";

            // Name material too
            MeshRenderer meshRenderer = res.GetComponentInChildren<MeshRenderer>(true);
            if (null != meshRenderer)
            {
                meshRenderer.material.name = GetMaterialName(res);
            }

            return res;
        }

        public static GameObject AddObjectToDocument(Transform transform, string objectName, string collectionInstanceName = "/")
        {
            if (!nodes.ContainsKey(objectName))
                return null;
            Node objectNode = nodes[objectName];

            ////////////////////////////////////////////////////////////////
            // WARNING : this should not be tolerated !!!!
            // Check if parent of this Object has been instantiated
            // If not, add parent to document (instantiate)
            ////////////////////////////////////////////////////////////////
            Node parentNode = objectNode.parent;
            if (null != parentNode)
            {
                bool found = false;
                foreach (Tuple<GameObject, string> item in parentNode.instances)
                {
                    if (item.Item2 == collectionInstanceName)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    transform = AddObjectToDocument(transform, parentNode.prefab.name, collectionInstanceName).transform;
                    Debug.LogWarning("Adding object to Document but parent object has not been instantiated : " + objectName);
                }
            }
            ////////////////////////////////////////////////////////////////

            foreach (Tuple<GameObject, string> item in objectNode.instances)
            {
                if (item.Item2 == collectionInstanceName)
                    return null; // already instantiated
            }


            GameObject instance = SyncData.CreateInstance(objectNode.prefab, transform, objectName);
            AddInstanceToNode(instance, objectNode, collectionInstanceName);

            // Reparent to parent
            Transform parent = transform;
            if (null != objectNode.parent)
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
            foreach (Node childNode in childrenNodes)
            {
                foreach (Tuple<GameObject, string> t in childNode.instances)
                    if (t.Item2 == collectionInstanceName)
                        children.Add(t.Item1);
            }
            foreach (GameObject childObject in children)
            {
                Reparent(childObject.transform, instance.transform);
            }

            if (null != objectNode.collectionInstance)
            {
                CollectionNode collectionNode = objectNode.collectionInstance;

                GameObject offsetObject = new GameObject(SyncData.OffsetTransformName);
                offsetObject.transform.parent = instance.transform;
                offsetObject.transform.localPosition = -collectionNode.offset;
                offsetObject.transform.localRotation = Quaternion.identity;
                offsetObject.transform.localScale = Vector3.one;
                offsetObject.SetActive(collectionNode.visible & collectionNode.tempVisible & objectNode.visible & objectNode.tempVisible);

                string subCollectionInstanceName = "/" + instance.name;
                if (collectionInstanceName.Length > 1)
                    subCollectionInstanceName = collectionInstanceName + subCollectionInstanceName;

                instanceRoot[subCollectionInstanceName] = offsetObject.transform;
                AddCollectionToScene(collectionNode, offsetObject.transform, subCollectionInstanceName);
            }

            ApplyVisibility(instance);

            return instance;
        }

        public static void SetScene(string sceneName)
        {
            if (sceneName == currentSceneName)
                return;

            currentSceneName = sceneName;

            sceneCollections.Clear();
            instancesToNodes.Clear();

            List<GameObject> objectToRemove = new List<GameObject>();

            foreach (KeyValuePair<string, Node> nodePair in nodes)
            {
                Node node = nodePair.Value;
                List<Tuple<GameObject, string>> remainingObjects = new List<Tuple<GameObject, string>>();
                foreach (Tuple<GameObject, string> t in node.instances)
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

            foreach (GameObject obj in objectToRemove)
            {
                GameObject.Destroy(obj);
            }
        }

        public static void Reparent(Transform t, Transform parent)
        {
            Transform parentTransform = t.parent;
            Vector3 position = parentTransform.localPosition;
            Quaternion rotation = parentTransform.localRotation;
            Vector3 scale = parentTransform.localScale;

            parentTransform.parent = parent;
            parentTransform.localPosition = position;
            parentTransform.localRotation = rotation;
            parentTransform.localScale = scale;
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

        public static GameObject CreateGameObject(string name)
        {
            GameObject gObjectParent = new GameObject(name + "_parent");
            GameObject gObject = new GameObject(name);
            gObject.transform.parent = gObjectParent.transform;
            return gObject;
        }

        public static Transform FindChild(Transform transform, string childName)
        {
            int count = transform.childCount;
            for (int i = 0; i < count; i++)
            {
                Transform child = transform.GetChild(i);
                Transform found = child.Find(childName);
                if (null != found)
                    return found;
            }
            return null;
        }

        public static Transform GetOrCreatePrefabPath(string path)
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
                    GameObject parentObject = CreateGameObject(parentName);
                    Reparent(parentObject.transform, prefab);
                    parentNode.prefab = parentObject;
                }
            }

            string objectName = splitted[splitted.Length - 1];

            Transform transform = null;
            if (nodes.TryGetValue(objectName, out Node child))
            {
                transform = child.prefab.transform;
            }

            Node node = null;
            if (null == transform)
            {
                transform = CreateGameObject(objectName).transform;
                Reparent(transform, prefab);
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

        public static void ApplyCollectionVisibility(CollectionNode collectionNode, string instanceName = "", bool inheritVisible = true)
        {
            foreach (Node n in collectionNode.objects)
            {
                foreach (Tuple<GameObject, string> item in n.instances)
                {
                    if (item.Item2 == instanceName)
                        ApplyVisibility(item.Item1, collectionNode.visible && collectionNode.tempVisible && inheritVisible, instanceName);
                }
            }

            foreach (CollectionNode c in collectionNode.children)
            {
                ApplyCollectionVisibility(c, instanceName, collectionNode.visible && collectionNode.tempVisible && c.visible && inheritVisible);
            }
        }

        public static void EnableComponents(GameObject obj, bool enable)
        {
            Component[] components = obj.GetComponents<Component>();
            foreach (Component component in components)
            {
                Type componentType = component.GetType();
                var prop = componentType.GetProperty("enabled");
                if (null != prop)
                {
                    prop.SetValue(component, enable);
                }
            }
        }

        public static void ApplyVisibility(GameObject obj, bool inheritVisible = true, string instanceName = "")
        {
            Node node = nodes[obj.name];
            CollectionNode collectionNode = node.collectionInstance;
            if (null != collectionNode)
            {
                instanceName = instanceName + "/" + obj.name;
                foreach (Node n in collectionNode.objects)
                {
                    foreach (Tuple<GameObject, string> item in n.instances)
                    {
                        if (item.Item2 == instanceName)
                            ApplyVisibility(item.Item1, collectionNode.visible && collectionNode.tempVisible && node.visible && node.tempVisible && inheritVisible, item.Item2);
                    }
                }

                ApplyCollectionVisibility(collectionNode, instanceName, collectionNode.visible && collectionNode.tempVisible && node.visible && node.tempVisible && inheritVisible);
                obj = obj.transform.Find(OffsetTransformName).gameObject;
            }

            EnableComponents(obj, node.containerVisible & node.visible & node.tempVisible & inheritVisible);

            // Enable/Disable light
            mixer.SetLightEnabled(obj, node.containerVisible & node.visible & node.tempVisible & inheritVisible);
        }

        public static bool IsInstanceParentVisible(Transform root, GameObject instance)
        {
            bool parentIsVisible = true;
            Transform parentObject = instance.transform.parent.parent;
            while (parentObject && parentIsVisible && parentObject != root)
            {
                if (parentObject.name == "__Offset")
                    parentObject = parentObject.parent;
                nodes.TryGetValue(parentObject.name, out Node parentNode);
                if (null == parentNode)
                    break;
                if (!parentNode.visible || !parentNode.tempVisible)
                {
                    parentIsVisible = false;
                    break;
                }
                parentObject = parentObject.parent.parent;
            }
            return parentIsVisible;
        }

        public static void ApplyVisibilityToInstances(Transform root, Transform transform)
        {
            if (!nodes.ContainsKey(transform.name))
                return;

            Node node = nodes[transform.name];
            foreach (Tuple<GameObject, string> t in node.instances)
            {
                GameObject obj = t.Item1;
                ApplyVisibility(obj, IsInstanceParentVisible(root, obj));
            }
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
            }
        }

        public static GameObject Duplicate(GameObject srcInstance, string name = null)
        {
            string srcname = srcInstance.name;
            if (!nodes.ContainsKey(srcname))
            {
                Debug.LogError("Duplicate Error : nodes does not contain " + srcname);
                return null;
            }
            Node srcNode = nodes[srcname];
            GameObject srcPrefab = srcNode.prefab;

            GameObject prefabClone = SyncData.CreateInstance(srcInstance, srcPrefab.transform.parent.parent, name);
            Node prefabCloneNode = CreateNode(prefabClone.name, srcNode.parent);
            prefabCloneNode.prefab = prefabClone;
            GameObject clone = AddObjectToDocument(root, prefabClone.name, "/");

            return clone;
        }

        public static void RemovePrefab(string objectName)
        {
            Node node = nodes[objectName];
            GameObject.Destroy(node.prefab);
            if (null != node.parent)
                node.parent.RemoveChild(node);
            nodes.Remove(objectName);
        }

        public static Node GetOrCreateNode(GameObject newPrefab)
        {
            if (!nodes.TryGetValue(newPrefab.name, out Node node))
            {
                node = CreateNode(newPrefab.name);
                node.prefab = newPrefab;
            }
            return node;
        }

        /// <summary>
        /// Create a new prefab node using the given GameObject as the prefab then instantiate it into the scene.
        /// Note that this function does not put the given prefab game object into the prefabs.
        /// </summary>
        /// <param name="newPrefab">Prefab</param>
        /// <returns>Instantiated prefab</returns>
        public static GameObject InstantiatePrefab(GameObject newPrefab)
        {
            GetOrCreateNode(newPrefab);
            GameObject instance = AddObjectToDocument(root, newPrefab.name);
            return instance;
        }

        public static GameObject InstantiateFullHierarchyPrefab(GameObject prefab)
        {
            GameObject res = InstantiatePrefab(prefab);
            Node node = nodes[prefab.name];
            foreach (var child in node.children)
            {
                InstantiateFullHierarchyPrefab(child.prefab);
            }
            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unityPrefab"></param>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static GameObject InstantiateUnityPrefab(GameObject unityPrefab, Matrix4x4 matrix)
        {
            GameObject newPrefab = SyncData.CreateInstance(unityPrefab, prefab, isPrefab: true);

            newPrefab.transform.localPosition = matrix.GetColumn(3);
            newPrefab.transform.localRotation = Quaternion.AngleAxis(180, Vector3.forward) * Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
            newPrefab.transform.localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);

            return InstantiatePrefab(newPrefab);
        }

        /// <summary>
        /// Set the transform of the given object (its prefab) and to all of its instances.
        /// </summary>
        /// <param name="objectName">object name</param>
        /// <param name="matrix">local transform</param>
        public static void SetTransform(string objectName, Matrix4x4 matrix)
        {
            Node node = nodes[objectName];
            node.prefab.transform.localPosition = new Vector3(matrix.GetColumn(3).x, matrix.GetColumn(3).y, matrix.GetColumn(3).z);
            node.prefab.transform.localRotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
            node.prefab.transform.localScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
            ApplyTransformToInstances(node.prefab.transform);
        }
        public static void SetTransform(string objectName, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Node node = nodes[objectName];
            node.prefab.transform.localPosition = position;
            node.prefab.transform.localRotation = rotation;
            node.prefab.transform.localScale = scale;
            ApplyTransformToInstances(node.prefab.transform);
        }
    }
}

