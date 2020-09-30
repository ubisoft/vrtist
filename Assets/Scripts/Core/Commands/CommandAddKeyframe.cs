using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframe : ICommand
    {
        GameObject gObject;
        AnimationChannel animationChannel = null;
        string channelName;
        int channelIndex;
        AnimationKey animationKey = null;
        float oldValue;
        float newValue;
        Interpolation oldInterpolation;
        Interpolation newInterpolation;
        int frame;

        public CommandAddKeyframe(GameObject obj, string channelName, int channelIndex, int frame, float value, Interpolation interpolation)
        {
            gObject = obj;
            this.channelName = channelName;
            this.channelIndex = channelIndex;
            this.newValue = value;
            this.frame = frame;
            this.newInterpolation = interpolation;

            Dictionary<Tuple<string, int>, AnimationChannel> channels = GlobalState.Instance.GetAnimationChannels(obj);
            if (null == channels)
                return;
            Tuple<string, int> c = new Tuple<string, int>(channelName, channelIndex);
            if (channels.TryGetValue(c, out animationChannel))
            {
                if (animationChannel.TryGetIndex(frame, out int index))
                {
                    animationKey = animationChannel.GetKey(index);
                    oldValue = animationKey.value;
                    oldInterpolation = animationKey.interpolation;
                }
            }
        }

        public override void Undo()
        {
            if(null == animationKey)
            {
                GlobalState.Instance.RemoveKeyframe(gObject, channelName, channelIndex, frame);
                return;
            }

            GlobalState.Instance.AddKeyframe(gObject, channelName, channelIndex, frame, oldValue, oldInterpolation);
        }

        public override void Redo()
        {
            GlobalState.Instance.AddKeyframe(gObject, channelName, channelIndex, frame, newValue, newInterpolation);

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