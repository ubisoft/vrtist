using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRtist
{
    public class Utils
    {
        static GameObject trash = null;
        static int gameObjectNameId = 0;
        static long timestamp = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        static string hostname = Dns.GetHostName();
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
            if (baseName.Length > 48)
                baseName = baseName.Substring(0, 48);
            string name = baseName + "." + String.Format("{0:X}", (hostname + timestamp.ToString()).GetHashCode()) + "." + gameObjectNameId.ToString();
            gameObjectNameId++;
            return name;
        }

        public static GameObject CreateInstance(GameObject gObject, Transform parent, string name = null)
        {
            GameObject intermediateParent = new GameObject();
            intermediateParent.transform.parent = parent;
            Transform srcParent = gObject.transform.parent;
            if (null != srcParent)
            {
                intermediateParent.transform.localPosition = gObject.transform.parent.localPosition;
                intermediateParent.transform.localRotation = gObject.transform.parent.localRotation;
                intermediateParent.transform.localScale = gObject.transform.parent.localScale;
            }

            GameObject res;
            GameObjectBuilder builder = gObject.GetComponent<GameObjectBuilder>();
            if (builder)
            {
                res = builder.CreateInstance(gObject, intermediateParent.transform);
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
                appliedName = CreateUniqueName(res, baseName);
            }
            else
            {
                appliedName = name;
            }
            res.name = appliedName;
            intermediateParent.name = appliedName + "_parent";

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


        public static RenderTexture CreateRenderTexture(int width, int height, int depth, RenderTextureFormat format, bool randomWrite) {
            RenderTexture renderTexture = new RenderTexture(width, height, depth, format);
            renderTexture.enableRandomWrite = randomWrite;
            renderTexture.Create();
            return renderTexture;
        }
        public static RenderTexture CreateRenderTexture(RenderTexture source) {
            return CreateRenderTexture(source.width, source.height, 0, source.format, true);
        }

        public static void TryDispose(System.IDisposable obj) {
            if(null == obj) { return; }
            obj.Dispose();
        }
        public static void TryDestroy(UnityEngine.Object obj) {
            if(null == obj) { return; }
            UnityEngine.Object.Destroy(obj);
        }

        public static void SwapBuffers(ref ComputeBuffer buf1, ref ComputeBuffer buf2) {
            var temp = buf1;
            buf1 = buf2;
            buf2 = temp;
        }
    }
}
