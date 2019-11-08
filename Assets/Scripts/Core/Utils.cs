using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRtist
{
    public class Utils
    {
        public static GameObject GetOrCreateTrash()
        {
            string trashName = "__Trash__";
            GameObject trash = GameObject.Find(trashName);
            if (!trash)
            {
                trash = new GameObject();
                trash.name = trashName;
            }
            return trash;
        }

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
            GameObject world = Utils.FindWorld();
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


        public static GameObject GetRoot(GameObject gobject)
        {
            ParametersController parametersController = gobject.GetComponentInParent<ParametersController>();
            if (!parametersController)
                return null;
            return parametersController.gameObject;
        }

        public static string BuildTransformPath(GameObject gobject)
        {
            string res = "";
            while (gobject.GetComponent<ParametersController>() == null)
            {
                res = "/" + gobject.name + res;
                gobject = gobject.transform.parent.gameObject;
            }

            if (res.Length > 0)
                res = res.Substring(1, res.Length - 1);

            return res;
        }

        public static string CreateUniqueName(GameObject gObject, string baseName)
        {
            Transform parent = gObject.transform.parent;
            HashSet<string> childrenName = new HashSet<string>();
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if(child != gObject)
                    childrenName.Add(child.name);
            }

            int id = 0;
            string name = baseName + "." + id.ToString();
            while (childrenName.Contains(name))
            {
                id++;
                name = baseName + "." + id.ToString();
            }
            return name;
        }

        public static GameObject CreateInstance(GameObject gObject, Transform parent)
        {            
            GameObject res;
            GameObjectBuilder builder = gObject.GetComponent<GameObjectBuilder>();
            if (builder)
            {
                res = builder.CreateInstance(gObject, parent);
                Parameters parameters = res.GetComponentInParent<ParametersController>().GetParameters();
                if (parameters != null)
                    parameters.InitId();
            }
            else
            {
                // duplicate object or subobject
                res = GameObject.Instantiate(gObject, parent);
                ParametersController parametersController = res.GetComponent<ParametersController>();
                if (parametersController)
                {
                    Parameters parameters = parametersController.GetParameters();
                    parameters.InitId(); // reuild id only for objects
                }
            }

            string baseName = gObject.name.Split('.')[0];
            res.name = CreateUniqueName(res, baseName);

            return res;
        }

        public static GameObject CreatePaint(Transform parent, Color color)
        {
            GameObject lineObject = new GameObject();
            lineObject.transform.parent = parent;
            lineObject.name = CreateUniqueName(lineObject, "Paint");

            lineObject.transform.localPosition = Vector3.zero;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;
            lineObject.tag = "PhysicObject";

            Mesh mesh = new Mesh();
            MeshFilter meshFilter = lineObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            MeshRenderer renderer = lineObject.AddComponent<MeshRenderer>();
            Material paintMaterial = Resources.Load("Materials/Paint") as Material;
            renderer.material = GameObject.Instantiate<Material>(paintMaterial);
            renderer.material.SetColor("_BaseColor", color);

            PaintController paintController = lineObject.AddComponent<PaintController>();

            return lineObject;
        }


    }
}
