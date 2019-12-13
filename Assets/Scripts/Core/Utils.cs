using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRtist
{
    public class Utils
    {
        static GameObject trash = null;
        static int gameObjectNameId = -1;
        public static GameObject GetTrash()
        {
            if (trash == null)
            {                
                trash = new GameObject("__Trash__");
                trash.SetActive(false);
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
            if(gameObjectNameId == -1)
            {
                Regex rx = new Regex(@".*?\.(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                HashSet<string> childrenName = new HashSet<string>();

                GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (GameObject go in allObjects)
                {
                    MatchCollection matches = rx.Matches(go.name);
                    foreach (Match match in matches)
                    {
                        GroupCollection groups = match.Groups;
                        if (groups.Count == 2)
                        {
                            string strValue = groups[1].Value;
                            int value;
                            Int32.TryParse(strValue, out value);
                            if (value > gameObjectNameId)
                                gameObjectNameId = value;
                        }
                    }
                }
                gameObjectNameId++;
            }
                        
            string name = baseName + "." + gameObjectNameId.ToString();
            gameObjectNameId++;
            return name;
        }

        public static GameObject CreateInstance(GameObject gObject, Transform parent)
        {            
            GameObject res;
            GameObjectBuilder builder = gObject.GetComponent<GameObjectBuilder>();
            if (builder)
            {
                res = builder.CreateInstance(gObject, parent);
                ParametersController parametersController = res.GetComponentInParent<ParametersController>();
                if (parametersController)
                {
                    Parameters parameters = parametersController.GetParameters();
                    if (parameters != null)
                        parameters.InitId();
                }
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
            renderer.material.name = "Paint_" + color.ToString();

            PaintController paintController = lineObject.AddComponent<PaintController>();

            return lineObject;
        }


    }
}
