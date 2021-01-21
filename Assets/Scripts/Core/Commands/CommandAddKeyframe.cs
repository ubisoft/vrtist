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
            GlobalState.Animation.RemoveKeyframe(gObject, property, newAnimationKey.frame);
            MixerClient.Instance.SendRemoveKeyframe(new SetKeyInfo { objectName = gObject.name, property = property, key = newAnimationKey });

            if (null != oldAnimationKey)
            {
                GlobalState.Animation.AddFilteredKeyframe(gObject, property, oldAnimationKey);
                MixerClient.Instance.SendAddKeyframe(new SetKeyInfo { objectName = gObject.name, property = property, key = oldAnimationKey });
                return;
            }
        }

        public override void Redo()
        {
            GlobalState.Animation.AddFilteredKeyframe(gObject, property, newAnimationKey);
            MixerClient.Instance.SendAddKeyframe(new SetKeyInfo { objectName = gObject.name, property = property, key = newAnimationKey });
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}