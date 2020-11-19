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

        public void ImportObject(string filename, Transform parent)
        {
            importer.Import(filename, parent);
        }

        public Task<GameObject> ImportObjectAsync(string filename, Transform parent)
        {
            task = new TaskCompletionSource<GameObject>();
            importer.Import(filename, parent);
            return task.Task;
        }
    }
}
