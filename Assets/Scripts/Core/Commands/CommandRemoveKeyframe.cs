﻿using UnityEngine;

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
        }

        public override void Redo()
        {
            GlobalState.Animation.RemoveKeyframe(gObject, property, oldAnimationKey.time);
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