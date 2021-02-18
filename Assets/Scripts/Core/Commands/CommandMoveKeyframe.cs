using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to move a keyframe of a property of an object.
    /// </summary>
    public class CommandMoveKeyframe : ICommand
    {
        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly int oldFrame;
        readonly int newFrame;

        public CommandMoveKeyframe(GameObject obj, AnimatableProperty property, int frame, int newFrame)
        {
            gObject = obj;
            this.property = property;
            this.oldFrame = frame;
            this.newFrame = newFrame;
        }

        public override void Undo()
        {
            SceneManager.MoveKeyframe(gObject, property, newFrame, oldFrame);
        }

        public override void Redo()
        {
            SceneManager.MoveKeyframe(gObject, property, oldFrame, newFrame);
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
