using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class ColorReference
    {
        public bool useConstant = true;
        public Color constant = Color.grey;
        public ColorVariable reference;

        public Color Value
        {
            get 
            {
                return (useConstant || reference == null ) ? constant : reference.value;
            }

            set
            {
                if (useConstant)
                {
                    constant = value;
                }
            }
        }
    }
}
