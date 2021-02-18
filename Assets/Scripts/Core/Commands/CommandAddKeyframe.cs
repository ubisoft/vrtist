using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to ad a keyframe to a property of an object.
    /// </summary>
    public class CommandAddKeyframe : ICommand
    {
        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly AnimationKey oldAnimationKey = null;
        readonly AnimationKey newAnimationKey = null;

        public CommandAddKeyframe(GameObject obj, AnimatableProperty property, int frame, float value, Interpolation interpolation)
        {
            gObject = obj;
            this.property = property;
            newAnimationKey = new AnimationKey(frame, value, interpolation);

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
            SceneManager.RemoveKeyframe(gObject, property, newAnimationKey);

            if (null != oldAnimationKey)
            {
                SceneManager.AddKeyframe(gObject, property, oldAnimationKey);
            }
        }

        public override void Redo()
        {
            SceneManager.AddKeyframe(gObject, property, newAnimationKey);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}