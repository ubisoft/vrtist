using UnityEngine;

namespace VRtist
{
    public class CommandMoveKeyframe : ICommand
    {
        GameObject gObject;
        AnimatableProperty property;
        int oldFrame;
        int newFrame;

        public CommandMoveKeyframe(GameObject obj, AnimatableProperty property, int frame, int newFrame)
        {
            gObject = obj;
            this.property = property;
            this.oldFrame = frame;
            this.newFrame = newFrame;
        }

        public override void Undo()
        {
            GlobalState.Animation.MoveKeyframe(gObject, property, newFrame, oldFrame);
            MixerClient.GetInstance().SendMoveKeyframe(new MoveKeyInfo { objectName = gObject.name, property = property, frame = newFrame, newFrame = oldFrame });
        }

        public override void Redo()
        {
            GlobalState.Animation.MoveKeyframe(gObject, property, oldFrame, newFrame);
            MixerClient.GetInstance().SendMoveKeyframe(new MoveKeyInfo { objectName = gObject.name, property = property, frame = oldFrame, newFrame = newFrame });
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
