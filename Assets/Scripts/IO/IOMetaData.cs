using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class IOMetaData : MonoBehaviour
    {
        public enum Type
        {
            Geometry,
            Paint,
            Light,
            Camera
        }

        IOMetaData()
        {
            id = idGen++;
        }

        public string filename;
        public Type type;

        public int id;
        static int idGen = 0;
    }
}