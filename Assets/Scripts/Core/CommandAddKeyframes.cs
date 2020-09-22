using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframes : CommandGroup
    {
        GameObject gObject;
        public CommandAddKeyframes(GameObject obj) : base("Add Keyframes")
        {
            gObject = obj;
            int frame = GlobalState.currentFrame;

            new CommandAddKeyframe(gObject, "location", 0, frame, gObject.transform.localPosition.x).Submit();
            new CommandAddKeyframe(gObject, "location", 1, frame, gObject.transform.localPosition.y).Submit();
            new CommandAddKeyframe(gObject, "location", 2, frame, gObject.transform.localPosition.z).Submit();
            Quaternion q = gObject.transform.localRotation;
            // convert to ZYX euler
            Vector3 angles = Maths.ThreeAxisRotation(q);
            new CommandAddKeyframe(gObject, "rotation_euler", 0, frame, angles.x).Submit();
            new CommandAddKeyframe(gObject, "rotation_euler", 1, frame, angles.y).Submit();
            new CommandAddKeyframe(gObject, "rotation_euler", 2, frame, angles.z).Submit();

            CameraController controller = gObject.GetComponent<CameraController>();
            if (null != controller)
            {
                new CommandAddKeyframe(gObject, "lens", -1, frame, controller.focal).Submit();
            }

            LightController lcontroller = gObject.GetComponent<LightController>();
            if (null != lcontroller)
            {
                new CommandAddKeyframe(gObject, "energy", -1, frame, lcontroller.GetPower()).Submit();
                new CommandAddKeyframe(gObject, "color", 0, frame, lcontroller.color.r).Submit();
                new CommandAddKeyframe(gObject, "color", 1, frame, lcontroller.color.g).Submit();
                new CommandAddKeyframe(gObject, "color", 2, frame, lcontroller.color.b).Submit();
            }

            if (null == gObject.GetComponent<ParametersController>())
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandAddKeyframe(gObject, "scale", 0, frame, scale.x).Submit();
                new CommandAddKeyframe(gObject, "scale", 1, frame, scale.y).Submit();
                new CommandAddKeyframe(gObject, "scale", 2, frame, scale.z).Submit();
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