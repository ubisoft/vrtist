using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class LightBuilder : GameObjectBuilder
    {
        public override GameObject CreateInstance(GameObject source, Transform parent = null)
        {
            GameObject newLight = GameObject.Instantiate(source, parent);
            LightController lightController = source.GetComponentInChildren<LightController>();
            VRInput.DeepSetLayer(newLight, 5);

            newLight.GetComponentInChildren<LightController>().CopyParameters(lightController);

            return newLight;
        }
    }
}
