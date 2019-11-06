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

        public IOMetaData()
        {
            id = idGen++;
        }

        public void InitId()
        {
            id = idGen++;
        }

        public Type type;
        public int id;
        static public int idGen = 0;
    }
}