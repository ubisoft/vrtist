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
        int frame;

        public CommandAddKeyframe(GameObject obj, string channelName, int channelIndex, int frame, float value)
        {
            gObject = obj;
            this.channelName = channelName;
            this.channelIndex = channelIndex;
            this.newValue = value;
            this.frame = frame;

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

            GlobalState.Instance.AddKeyframe(gObject, channelName, channelIndex, frame, oldValue);
        }

        public override void Redo()
        {
            GlobalState.Instance.AddKeyframe(gObject, channelName, channelIndex, frame, newValue);

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