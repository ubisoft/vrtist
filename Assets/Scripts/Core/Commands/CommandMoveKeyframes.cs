using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandMoveKeyframes : CommandGroup
    {
        GameObject gObject;
        public CommandMoveKeyframes(GameObject obj, int frame, int newFrame) : base("Move Keyframes")
        {
            gObject = obj;

            new CommandMoveKeyframe(gObject, "location", 0, frame, newFrame).Submit();
            new CommandMoveKeyframe(gObject, "location", 1, frame, newFrame).Submit();
            new CommandMoveKeyframe(gObject, "location", 2, frame, newFrame).Submit();
            Quaternion q = gObject.transform.localRotation;
            // convert to ZYX euler
            Vector3 angles = Maths.ThreeAxisRotation(q);
            new CommandMoveKeyframe(gObject, "rotation_euler", 0, frame, newFrame).Submit();
            new CommandMoveKeyframe(gObject, "rotation_euler", 1, frame, newFrame).Submit();
            new CommandMoveKeyframe(gObject, "rotation_euler", 2, frame, newFrame).Submit();

            CameraController controller = gObject.GetComponent<CameraController>();
            if (null != controller)
            {
                new CommandMoveKeyframe(gObject, "lens", -1, frame, newFrame).Submit();
            }

            LightController lcontroller = gObject.GetComponent<LightController>();
            if (null != lcontroller)
            {
                new CommandMoveKeyframe(gObject, "energy", -1, frame, newFrame).Submit();
                new CommandMoveKeyframe(gObject, "color", 0, frame, newFrame).Submit();
                new CommandMoveKeyframe(gObject, "color", 1, frame, newFrame).Submit();
                new CommandMoveKeyframe(gObject, "color", 2, frame, newFrame).Submit();
            }

            if (null == gObject.GetComponent<ParametersController>())
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandMoveKeyframe(gObject, "scale", 0, frame, newFrame).Submit();
                new CommandMoveKeyframe(gObject, "scale", 1, frame, newFrame).Submit();
                new CommandMoveKeyframe(gObject, "scale", 2, frame, newFrame).Submit();
            }
        }

        public override void Undo()
        {
            base.Undo();
            MixerClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
        }

        public override void Redo()
        {
            base.Redo();
            MixerClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);

        }
        public override void Submit()
        {
            base.Submit();
            MixerClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
        }

        public override void Serialize(SceneSerializer serializer)
        {

        }
    }
}