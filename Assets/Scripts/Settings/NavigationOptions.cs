/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/NavigationOptions")]
    public class NavigationOptions : ScriptableObject
    {
        [Header("Drone Navigation")]
        public float flightSpeed = 5f;
        public float flightRotationSpeed = 5f;
        public float flightDamping = 5f;

        [Header("Fps Navigation")]
        [Range(0.01f, 10.0f)] public float fpsSpeed = 5f; // TODO: sliders should be in %, but the Options value should be the real one. No added factor (0.03 -> 0.15) in NavigationMode_FPS.
        public float fpsRotationSpeed = 5f; // TODO: sliders should be in %, but the Options value should be the real one. No added factor (0.3 -> 1.5) in NavigationMode_FPS.
        public float fpsDamping = 0f;
        public float fpsGravity = 9.8f;

        [Header("Orbit Navigation")]
        public float orbitScaleSpeed = 0.02f; // 0-1 slider en pct
        public float orbitMoveSpeed = 0.05f; // 0-1 slider *100
        [Tooltip("Speed in degrees/s")] public float orbitRotationalSpeed = 3.0f; // 0-10

        [Header("Teleport Navigation")]
        public bool lockHeight = false;

        [Header("Fly Navigation")]
        [Tooltip("Speed in m/s")] public float flySpeed = 0.2f;

        [NonSerialized]
        public NavigationMode currentNavigationMode = null;

        public bool CanUseControls(NavigationMode.UsedControls controls)
        {
            return (currentNavigationMode == null) ? true : !currentNavigationMode.usedControls.HasFlag(controls);
        }
    }
}
