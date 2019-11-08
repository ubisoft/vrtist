using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GeometryController : ParametersController
    {
        public GeometryParameters parameters = new GeometryParameters();
        public override Parameters GetParameters() { return parameters; }
    }
}
