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

using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to move an object or a list of objects or the current selection.
    /// </summary>
    public class CommandMoveObjects : ICommand
    {
        List<GameObject> objects;

        List<Vector3> beginPositions;
        List<Quaternion> beginRotations;
        List<Vector3> beginScales;

        List<Vector3> endPositions;
        List<Quaternion> endRotations;
        List<Vector3> endScales;

        public CommandMoveObjects()
        {

        }

        public CommandMoveObjects(List<GameObject> o, List<Vector3> bp, List<Quaternion> br, List<Vector3> bs, List<Vector3> ep, List<Quaternion> er, List<Vector3> es)
        {
            objects = o;
            beginPositions = bp;
            beginRotations = br;
            beginScales = bs;

            endPositions = ep;
            endRotations = er;
            endScales = es;
        }

        public void AddObject(GameObject gobject, Vector3 endPosition, Quaternion endRotation, Vector3 endScale)
        {
            if (null == beginPositions)
            {
                objects = new List<GameObject>();
                beginPositions = new List<Vector3>();
                endPositions = new List<Vector3>();
                beginRotations = new List<Quaternion>();
                endRotations = new List<Quaternion>();
                beginScales = new List<Vector3>();
                endScales = new List<Vector3>();
            }

            objects.Add(gobject);
            beginPositions.Add(gobject.transform.localPosition);
            endPositions.Add(endPosition);
            beginRotations.Add(gobject.transform.localRotation);
            endRotations.Add(endRotation);
            beginScales.Add(gobject.transform.localScale);
            endScales.Add(endScale);
        }

        public override void Undo()
        {
            int count = objects.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject ob = objects[i];
                SceneManager.SetObjectTransform(ob, beginPositions[i], beginRotations[i], beginScales[i]);
            }
        }
        public override void Redo()
        {
            int count = objects.Count;
            for (int i = 0; i < count; i++)
            {
                GameObject ob = objects[i];
                SceneManager.SetObjectTransform(ob, endPositions[i], endRotations[i], endScales[i]);
            }
        }
        public override void Submit()
        {
            if (null != objects && objects.Count > 0)
            {
                Redo();
                CommandManager.AddCommand(this);
            }
        }
    }
}
