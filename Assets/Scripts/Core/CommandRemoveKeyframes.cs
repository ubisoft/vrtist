using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandRemoveKeyframes : CommandGroup
    {
        GameObject gObject;
        public CommandRemoveKeyframes(GameObject obj) : base("Remove Keyframes")
        {
            gObject = obj;
            int frame = GlobalState.currentFrame;

            new CommandRemoveKeyframe(gObject, "location", 0, frame).Submit();
            new CommandRemoveKeyframe(gObject, "location", 1, frame).Submit();
            new CommandRemoveKeyframe(gObject, "location", 2, frame).Submit();

            new CommandRemoveKeyframe(gObject, "rotation_euler", 0, frame).Submit();
            new CommandRemoveKeyframe(gObject, "rotation_euler", 1, frame).Submit();
            new CommandRemoveKeyframe(gObject, "rotation_euler", 2, frame).Submit();

            CameraController controller = gObject.GetComponent<CameraController>();
            if (null != controller)
            {
                new CommandRemoveKeyframe(gObject, "lens", -1, frame).Submit();
            }

            LightController lcontroller = gObject.GetComponent<LightController>();
            if (null != lcontroller)
            {
                new CommandRemoveKeyframe(gObject, "energy", -1, frame).Submit();
                new CommandRemoveKeyframe(gObject, "color", 0, frame).Submit();
                new CommandRemoveKeyframe(gObject, "color", 1, frame).Submit();
                new CommandRemoveKeyframe(gObject, "color", 2, frame).Submit();
            }

            if (null == gObject.GetComponent<ParametersController>())
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandRemoveKeyframe(gObject, "scale", 0, frame).Submit();
                new CommandRemoveKeyframe(gObject, "scale", 1, frame).Submit();
                new CommandRemoveKeyframe(gObject, "scale", 2, frame).Submit();
            }
        }

        public override void Undo()
        {
            base.Undo();
            NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
        }

        public override void Redo()
        {
            base.Redo();
            NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);

        }
        public override void Submit()
        {
            base.Submit();
            NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
        }

        public override void Serialize(SceneSerializer serializer)
        {

        }
    }
}