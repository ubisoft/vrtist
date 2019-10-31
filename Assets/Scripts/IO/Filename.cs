using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class Filename : MonoBehaviour
    {
        Filename()
        {
            id = idGen++;
        }

        public string filename;
        public int id;

        static int idGen = 0;
    }
}