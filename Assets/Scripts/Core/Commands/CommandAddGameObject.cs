using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to add an object into the scene. 
    /// </summary>
    public class CommandAddGameObject : CommandAddRemoveGameObject
    {
        public CommandAddGameObject(GameObject o) : base(o)
        {
        }

        public override void Undo()
        {
            if (null == gObject) { return; }
            SceneManager.RemoveObject(gObject);
        }
        public override void Redo()
        {
            if (null == gObject) { return; }
            SceneManager.RestoreObject(gObject, parent);
        }
        public override void Submit()
        {
            CommandManager.AddCommand(this);
            if (gObject.GetComponent<LightController>() != null)
            {
                SendLight();
            }
            else if (gObject.GetComponent<CameraController>() != null)
            {
                SendCamera();
            }
            else if (null != gObject.GetComponent<LocatorController>())
            {
                SendEmpty();
            }
            else if (gObject.GetComponent<MeshFilter>() != null)
            {
                SendMesh();
            }
            else
            {
                // For fbx import
                SendEmpty();
            }
        }
    }
}
