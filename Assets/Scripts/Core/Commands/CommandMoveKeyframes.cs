using UnityEngine;

namespace VRtist
{
    public class CommandMoveKeyframes : CommandGroup
    {
        GameObject gObject;
        public CommandMoveKeyframes(GameObject obj, int frame, int newFrame) : base("Move Keyframes")
        {
            gObject = obj;
            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(obj);
            if (null == animationSet)
                return;
            foreach (Curve curve in animationSet.curves.Values)
            {
                new CommandMoveKeyframe(gObject, curve.property, frame, newFrame).Submit();
            }
        }

        public override void Undo()
        {
            base.Undo();
            MixerClient.Instance.SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
        }

        public override void Redo()
        {
            base.Redo();
            MixerClient.Instance.SendEvent<string>(MessageType.QueryAnimationData, gObject.name);

        }
        public override void Submit()
        {
            base.Submit();
            MixerClient.Instance.SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
        }
    }
}
