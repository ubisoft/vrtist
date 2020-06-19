using System;
using UnityEngine;

namespace VRtist
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class PercentageAttribute : PropertyAttribute
    {
    }
}
