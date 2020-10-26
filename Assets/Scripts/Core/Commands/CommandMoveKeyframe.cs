using UnityEngine;

namespace VRtist
{
    public class CommandMoveKeyframe : ICommand
    {
        GameObject gObject;
        AnimationChannel animationChannel = null;
        string channelName;
        int channelIndex;
        int oldFrame;
        int newFrame;

        public CommandMoveKeyframe(GameObject obj, string channelName, int channelIndex, int frame, int newFrame)
        {
            gObject = obj;
            this.channelName = channelName;
            this.channelIndex = channelIndex;
            this.oldFrame = frame;
            this.newFrame = newFrame;
        }

        public override void Undo()
        {
            //GlobalState.Instance.MoveKeyframe(gObject, channelName, channelIndex, newFrame, oldFrame);
        }

        public override void Redo()
        {
            //GlobalState.Instance.MoveKeyframe(gObject, channelName, channelIndex, oldFrame, newFrame);

        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Serialize(SceneSerializer serializer)
        {

        }
    }
}