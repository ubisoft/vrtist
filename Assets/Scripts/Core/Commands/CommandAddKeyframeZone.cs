using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace VRtist
{
    public class CommandAddKeyframeZone : ICommand
    {
        readonly GameObject gObject;
        readonly AnimatableProperty property;
        readonly List<AnimationKey> oldKeys;
        readonly List<AnimationKey> newKeys;

        public CommandAddKeyframeZone(GameObject obj, AnimatableProperty property, int frame, float value, int startFrame, int endFrame, Interpolation interpolation)
        {
            gObject = obj;
            this.property = property;
            oldKeys = new List<AnimationKey>();
            newKeys = new List<AnimationKey>();

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet) return;
            Curve curve = animationSet.GetCurve(property);
            if (null == curve) return;

            AnimationKey newKey = new AnimationKey(frame, value, interpolation);
            curve.GetZoneKeyChanges(newKey, startFrame, endFrame, oldKeys, newKeys);
        }

        public override void Redo()
        {
            newKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }

        public override void Undo()
        {
            newKeys.ForEach(x => SceneManager.RemoveKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
            oldKeys.ForEach(x => SceneManager.AddObjectKeyframe(gObject, property, new AnimationKey(x.frame, x.value, x.interpolation, x.inTangent, x.outTangent), false));
        }
    }
}