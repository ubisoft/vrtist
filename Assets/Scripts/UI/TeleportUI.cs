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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportUI : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color activeColor = new Color(0, 38, 64);
    [ColorUsage(true, true)]
    public Color impossibleColor = new Color(64, 0, 0);

    private LineRenderer line = null;
    private Transform plane = null;

    // Start is called before the first frame update
    void Start()
    {
        line = transform.Find("Ray")?.GetComponent<LineRenderer>();
        plane = transform.Find("Target/Plane"); // Target/Plane
    }

    public void SetActiveColor()
    {
        SetEmissiveColor(activeColor);
    }

    public void SetImpossibleColor()
    {
        SetEmissiveColor(impossibleColor);
    }

    private void SetEmissiveColor(Color color)
    {
        if (line != null)
        {
            line.material.SetColor("_EmissiveColor", color);
        }

        if (plane != null)
        {
            plane.GetComponent<MeshRenderer>()?.material.SetColor("_EmissiveColor", color);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
