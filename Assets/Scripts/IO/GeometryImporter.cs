using UnityEngine;

namespace VRtist
{
    public class GeometryImporter : MonoBehaviour
    {
        private AssimpIO importer = null;
        public GameObjectChangedEvent objectLoaded = new GameObjectChangedEvent();

        void Start()
        {
            importer = GetComponent<AssimpIO>();
            importer.importEventTask += OnGeometryLoaded;
        }

        private void OnGeometryLoaded(object sender, AssimpIO.ImportTaskEventArgs e)
        {
            if (e.Error)
                return;
            objectLoaded.Invoke(e.Root.gameObject);
        }

        public void ImportObject(string filename, Transform parent)
        {
            importer.Import(filename, parent);
        }
    }
}
