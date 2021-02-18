using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to remove a keyframe of a property of an object.
    /// </summary>
    public class CommandRemoveKeyframe : ICommand
    {
        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly AnimationKey oldAnimationKey = null;

        public CommandRemoveKeyframe(GameObject obj, AnimatableProperty property, int frame)
        {
            gObject = obj;
            this.property = property;

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(obj);
            if (null == animationSet)
                return;

            Curve curve = animationSet.GetCurve(property);
            if (null == curve)
                return;

            curve.TryFindKey(frame, out oldAnimationKey);
        }

        public override void Undo()
        {
            SceneManager.AddKeyframe(gObject, property, oldAnimationKey);
        }

        public override void Redo()
        {
            SceneManager.RemoveKeyframe(gObject, property, oldAnimationKey);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
