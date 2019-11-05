using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            GameObjectBuilder builder = gObject.GetComponent<GameObjectBuilder>();
            if (builder)
                return builder.CreateInstance(gObject, parent);

            return GameObject.Instantiate(gObject, parent);
        }


    }
}
