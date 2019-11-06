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
            while (gobject && gobject.GetComponent<IOMetaData>() == null)
            {
                gobject = gobject.transform.parent.gameObject;
            }
            return gobject;
        }

        public static string BuildTransformPath(GameObject gobject)
        {
            string res = "";
            while (gobject.GetComponent<IOMetaData>() == null)
            {
                res = "/" + gobject.name + res;
                gobject = gobject.transform.parent.gameObject;
            }

            if (res.Length > 0)
                res = res.Substring(1, res.Length - 1);

            return res;
        }

        public static GameObject CreateInstance(GameObject gObject, Transform parent)
        {
            GameObject res;
            GameObjectBuilder builder = gObject.GetComponent<GameObjectBuilder>();
            if (builder)
                res = builder.CreateInstance(gObject, parent);
            else
                res = GameObject.Instantiate(gObject, parent);

            IOMetaData metaData = res.GetComponent<IOMetaData>();
            if (metaData)
                metaData.InitId();
            return res;
        }


    }
}
