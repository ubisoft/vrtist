using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{

    public class PaintController : ParametersController
    {
        public PaintParameters parameters = new PaintParameters();
        public override Parameters GetParameters() { return parameters; }
    }
}