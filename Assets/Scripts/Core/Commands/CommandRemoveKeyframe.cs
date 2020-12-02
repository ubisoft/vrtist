using UnityEngine;

namespace VRtist
{
    public class CommandRemoveKeyframe : ICommand
    {
        GameObject gObject;
        AnimatableProperty property;
        AnimationKey oldAnimationKey = null;

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
            GlobalState.Animation.AddKeyframe(gObject, property, oldAnimationKey);
            MixerClient.GetInstance().SendAddKeyframe(new SetKeyInfo { objectName = gObject.name, property = property, key = oldAnimationKey });
        }

        public override void Redo()
        {
            GlobalState.Animation.RemoveKeyframe(gObject, property, oldAnimationKey.frame);
            MixerClient.GetInstance().SendRemoveKeyframe(new SetKeyInfo { objectName = gObject.name, property = property, key = oldAnimationKey });
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
