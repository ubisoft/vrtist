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

            //AssimpIO.Import(@"D:\unity\VRSamples\Assets\Models\Cabane\cabane.fbx", o.transform);
            //AssimpIO.Import(@"D:\FBX\bidule\faces.fbx", o.transform);
            //AssimpIO.Import(@"D:\FBX\fireTruck.fbx", o.transform);

            //task = Task.Run(async () => await AssimpIO.ImportAssimpFile(@"D:\FBX\fireTruck.fbx"));
            importer.Import(@"D:\FBX\fireTruck.fbx", gameObject);
            importer.Import(@"D:\unity\VRSamples\Assets\Models\Cabane\cabane.fbx", gameObject);
            importer.Import(@"D:\FBX\bidule\faces.fbx", gameObject);
            importer.Import(@"D:\FBX\Batman\batman.obj", gameObject);
        }

        // Update is called once per frame
        void Update()
        {
        }

        
    }

}