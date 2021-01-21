using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to move an object or a list of objects or the current selection.
    /// </summary>
    public class CommandMoveObjects : ICommand
    {
        List<string> objectNames;

        List<Vector3> beginPositions;
        List<Quaternion> beginRotations;
        List<Vector3> beginScales;

        List<Vector3> endPositions;
        List<Quaternion> endRotations;
        List<Vector3> endScales;

        public CommandMoveObjects()
        {

        }

        public CommandMoveObjects(List<string> o, List<Vector3> bp, List<Quaternion> br, List<Vector3> bs, List<Vector3> ep, List<Quaternion> er, List<Vector3> es)
        {
            objectNames = o;
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
                objectNames = new List<string>();
                beginPositions = new List<Vector3>();
                endPositions = new List<Vector3>();
                beginRotations = new List<Quaternion>();
                endRotations = new List<Quaternion>();
                beginScales = new List<Vector3>();
                endScales = new List<Vector3>();
            }

            objectNames.Add(gobject.name);
            beginPositions.Add(gobject.transform.localPosition);
            endPositions.Add(endPosition);
            beginRotations.Add(gobject.transform.localRotation);
            endRotations.Add(endRotation);
            beginScales.Add(gobject.transform.localScale);
            endScales.Add(endScale);
        }

        public override void Undo()
        {
            int count = objectNames.Count;
            for (int i = 0; i < count; i++)
            {
                string objectName = objectNames[i];
                SyncData.SetTransform(objectName, beginPositions[i], beginRotations[i], beginScales[i]);
                foreach (var instance in SyncData.nodes[objectName].instances)
                    GlobalState.FireObjectMoving(instance.Item1);
                CommandManager.SendEvent(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
            }
        }
        public override void Redo()
        {
            int count = objectNames.Count;
            for (int i = 0; i < count; i++)
            {
                string objectName = objectNames[i];
                SyncData.SetTransform(objectName, endPositions[i], endRotations[i], endScales[i]);
                foreach (var instance in SyncData.nodes[objectName].instances)
                    GlobalState.FireObjectMoving(instance.Item1);
                CommandManager.SendEvent(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
            }
        }
        public override void Submit()
        {
            if (objectNames.Count > 0)
            {
                Redo();
                CommandManager.AddCommand(this);
            }
        }
    }
}
