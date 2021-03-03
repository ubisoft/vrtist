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
using System.Threading.Tasks;

using UnityEngine;

namespace VRtist
{
    public class GeometryImporter : MonoBehaviour
    {
        public GameObjectChangedEvent objectLoaded = new GameObjectChangedEvent();
        private AssimpIO importer = null;
        private TaskCompletionSource<GameObject> task = null;

        void Start()
        {
            importer = GetComponent<AssimpIO>();
            importer.importEventTask += OnGeometryLoaded;
        }

        private void OnGeometryLoaded(object sender, AssimpIO.ImportTaskEventArgs e)
        {
            // Check for an async Task to complete
            if (null != task && !task.Task.IsCompleted)
            {
                if (e.Error) { task.TrySetException(new Exception($"Failed to import {e.Filename}")); }
                else { task.TrySetResult(e.Root.gameObject); }
            }

            // Always send the event
            if (e.Error) { return; }
            objectLoaded.Invoke(e.Root.gameObject);
        }

        public void ImportObject(string filename, Transform parent, bool synchronous = false)
        {
            importer.Import(filename, parent, synchronous);
        }

        public Task<GameObject> ImportObjectAsync(string filename, Transform parent)
        {
            task = new TaskCompletionSource<GameObject>();
            importer.Import(filename, parent);
            return task.Task;
        }
    }
}
