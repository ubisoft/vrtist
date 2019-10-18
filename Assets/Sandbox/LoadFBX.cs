using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VRtist
{
    public class LoadFBX : MonoBehaviour
    {
        // Start is called before the first frame update
        public AssimpIO importer;
        void Start()
        {
            importer.Import(@"D:\FBX\Batman\batman.obj", gameObject);
            importer.Import(@"D:\unity\VRSamples\Assets\Models\Cabane\cabane.fbx", gameObject);
            importer.Import(@"D:\FBX\bidule\faces.fbx", gameObject);
            importer.Import(@"D:\FBX\fireTruck.fbx", gameObject);
        }

        // Update is called once per frame
        void Update()
        {
        }

        
    }

}