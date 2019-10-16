using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LoadFBX : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            GameObject o = new GameObject();
            o.name = "ROOT";

            //AssimpIO.Import(@"D:\unity\VRSamples\Assets\Models\Cabane\cabane.fbx", o.transform);
            AssimpIO.Import(@"D:\FBX\bidule\faces.fbx", o.transform);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}