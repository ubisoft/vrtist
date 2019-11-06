using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class IOGeometryMetaData : IOMetaData
    {
        public string filename;
        public List<string> deleted = new List<string>();
        public List<Tuple<string, string>> clones = new List<Tuple<string, string>>();
    }
}
