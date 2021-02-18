using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to remove all keyframes at a specific time of an object.
    /// </summary>
    public class CommandRemoveKeyframes : CommandGroup
    {
        readonly GameObject gObject;

        public CommandRemoveKeyframes(GameObject obj) : base("Remove Keyframes")
        {
            gObject = obj;
            int frame = GlobalState.Animation.CurrentFrame;

            foreach (Curve curve in GlobalState.Animation.GetObjectAnimation(obj).curves.Values)
            {
                new CommandRemoveKeyframe(gObject, curve.property, frame).Submit();
            }
        }

        public override void Undo()
        {
            base.Undo();
        }

        public override void Redo()
        {
            base.Redo();

        }

        public override void Submit()
        {
            base.Submit();
        }
    }
}
