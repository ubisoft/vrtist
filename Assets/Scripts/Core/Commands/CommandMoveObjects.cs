using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandMoveObjects : ICommand
    {
        List<string> objectNames;

        List<Vector3> beginPositions;
        List<Quaternion> beginRotations;
        List<Vector3> beginScales;

        List<Vector3> endPositions;
        List<Quaternion> endRotations;
        List<Vector3> endScales;

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

        public override void Undo()
        {
            int count = objectNames.Count;
            for (int i = 0; i < count; i++)
            {
                string objectName = objectNames[i];

                SyncData.SetTransform(objectName, beginPositions[i], beginRotations[i], beginScales[i]);
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
                CommandManager.SendEvent(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
            }
        }
        public override void Submit()
        {
            if (objectNames.Count > 0)
            {
                CommandManager.AddCommand(this);
                int count = objectNames.Count;
                for (int i = 0; i < count; i++)
                {
                    string objectName = objectNames[i];
                    CommandManager.SendEvent(MessageType.Transform, SyncData.nodes[objectName].prefab.transform);
                }
            }
        }
    }
}
