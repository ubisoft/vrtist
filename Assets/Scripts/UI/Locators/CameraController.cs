using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CameraController : ParametersController
    {
        public CameraParameters parameters = new CameraParameters();
        public override Parameters GetParameters() { return parameters; }
    }
}