using UnityEngine;

namespace VRtist
{
    public class CommandRemoveKeyframe : ICommand
    {
        GameObject gObject;
        Curve curve = null;
        string channelName;
        int channelIndex;
        AnimationKey animationKey = null;
        float value;
        int frame;
        Interpolation interpolation;

        public CommandRemoveKeyframe(GameObject obj, string channelName, int channelIndex, int frame)
        {
            gObject = obj;
            this.channelName = channelName;
            this.channelIndex = channelIndex;
            this.frame = frame;

            //Dictionary<Tuple<string, int>, AnimationChannel> channels = GlobalState.Instance.GetAnimationChannels(obj);
            //if (null == channels)
            //    return;
            //Tuple<string, int> c = new Tuple<string, int>(channelName, channelIndex);
            //if (channels.TryGetValue(c, out animationChannel))
            //{
            //    if (animationChannel.TryGetIndex(frame, out int index))
            //    {
            //        animationKey = animationChannel.GetKey(index);
            //        value = animationKey.value;
            //        interpolation = animationKey.interpolation;
            //    }
            //}
        }

        public override void Undo()
        {
            //if(null != animationKey)
            //    GlobalState.Instance.AddKeyframe(gObject, channelName, channelIndex, frame, value, interpolation);
        }

        public override void Redo()
        {
            //if (null != animationKey)
            //    GlobalState.Instance.RemoveKeyframe(gObject, channelName, channelIndex, frame);

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