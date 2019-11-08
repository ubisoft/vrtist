using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Base class for objects parameters
 * 
 * Used by manipulable objects to store values and by serializer as well.
 * the class and all deriving classes must be marked as [Serializable] in order to make it copiable when invoking GameObject.Instantiate 
 * It also makes it accessible in UI
 * 
 * Parameters are instantiated in ParametersController (which is a Component held by GameObjects)
 */

namespace VRtist
{
    [Serializable]
    public class Parameters
    {
        public Parameters()
        {
            id = idGen++;
        }

        public void InitId()
        {
            id = idGen++;
        }

        public int id;
        static public int idGen = 0;
    }
    
}