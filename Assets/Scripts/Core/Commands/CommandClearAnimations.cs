using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandClearAnimations : ICommand
    {
        GameObject gObject;
        Dictionary<Tuple<string, int>, AnimationChannel> animations = new Dictionary<Tuple<string, int>, AnimationChannel>();
        public CommandClearAnimations(GameObject obj)
        {
            gObject = obj;
            Dictionary<Tuple<string, int>, AnimationChannel> currentAnimations = GlobalState.Instance.GetAnimationChannels(obj);
            if (null == currentAnimations)
                return;
            foreach(var item in currentAnimations)
            {
                AnimationChannel channel = item.Value;
                AnimationChannel channelCopy = new AnimationChannel(channel.name, channel.index);
                foreach (AnimationKey key in channel.keys)
                {
                    AnimationKey newKey = new AnimationKey(key.time, key.value);
                    channelCopy.keys.Add(newKey);
                }
                animations[item.Key] = channelCopy;
            }
        }
        
        public override void Undo()
        {
            foreach (AnimationChannel channel in animations.Values)
            {
                GlobalState.Instance.SendAnimationChannel(gObject.name, channel);
            }
            MixerClient.GetInstance().SendQueryObjectData(gObject.name);
        }
        public override void Redo()
        {
            GlobalState.Instance.ClearAnimations(gObject);

            ClearAnimationInfo info = new ClearAnimationInfo { gObject = gObject };
            MixerClient.GetInstance().SendEvent<ClearAnimationInfo>(MessageType.ClearAnimations, info);
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