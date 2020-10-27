using UnityEngine;

namespace VRtist
{
    public class CommandRecordAnimations : ICommand
    {
        GameObject gObject;
        //AnimationSet animationSets = new AnimationSet();

        private void CopyAnimationChannel(Curve src, Curve dst)
        {
            dst.property = src.property;
            //foreach (AnimationKey srcKey in src.keys)
            //{
            //    dst.keys.Add(new AnimationKey(srcKey.time, srcKey.value));
            //}
        }
        public CommandRecordAnimations(GameObject obj, AnimationSet animSet)
        {
            //gObject = obj;
            //animationSets.xPosition = new AnimationChannel("location", 0);
            //CopyAnimationChannel(animSet.xPosition, animationSets.xPosition);
            //animationSets.yPosition = new AnimationChannel("location", 1);
            //CopyAnimationChannel(animSet.yPosition, animationSets.yPosition);
            //animationSets.zPosition = new AnimationChannel("location", 2);
            //CopyAnimationChannel(animSet.zPosition, animationSets.zPosition);
            //animationSets.xRotation = new AnimationChannel("rotation_euler", 0);
            //CopyAnimationChannel(animSet.xRotation, animationSets.xRotation);
            //animationSets.yRotation = new AnimationChannel("rotation_euler", 1);
            //CopyAnimationChannel(animSet.yRotation, animationSets.yRotation);
            //animationSets.zRotation = new AnimationChannel("rotation_euler", 2);
            //CopyAnimationChannel(animSet.zRotation, animationSets.zRotation);
            //if (null != animSet.lens)
            //{
            //    animationSets.lens = new AnimationChannel("lens", -1);
            //    CopyAnimationChannel(animSet.lens, animationSets.lens);
            //}
            //if (null != animSet.energy)
            //{
            //    animationSets.energy = new AnimationChannel("energy", -1);
            //    CopyAnimationChannel(animSet.energy, animationSets.energy);
            //    animationSets.RColor = new AnimationChannel("color", 0);
            //    CopyAnimationChannel(animSet.RColor, animationSets.RColor);
            //    animationSets.GColor = new AnimationChannel("color", 1);
            //    CopyAnimationChannel(animSet.GColor, animationSets.GColor);
            //    animationSets.RColor = new AnimationChannel("color", 2);
            //    CopyAnimationChannel(animSet.RColor, animationSets.RColor);
            //}
            //if (null != animSet.xScale)
            //{
            //    animationSets.xScale = new AnimationChannel("scale", 0);
            //    CopyAnimationChannel(animSet.xScale, animationSets.xScale);
            //    animationSets.yScale = new AnimationChannel("scale", 1);
            //    CopyAnimationChannel(animSet.yScale, animationSets.yScale);
            //    animationSets.zScale = new AnimationChannel("scale", 2);
            //    CopyAnimationChannel(animSet.zScale, animationSets.zScale);
            //}
        }
        public override void Undo()
        {
            //GlobalState.Instance.ClearAnimations(gObject);
            //ClearAnimationInfo info = new ClearAnimationInfo { gObject = gObject };
            //MixerClient.GetInstance().SendEvent<ClearAnimationInfo>(MessageType.ClearAnimations, info);
        }

        public override void Redo()
        {
            //GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.xPosition);
            //GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.yPosition);
            //GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.zPosition);
            //GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.xRotation);
            //GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.yRotation);
            //GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.zRotation);
            //if (null != animationSets.lens)
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.lens);
            //if (null != animationSets.energy)
            //{
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.energy);
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.RColor);
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.GColor);
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.BColor);
            //}
            //if (null != animationSets.xScale)
            //{
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.xScale);
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.yScale);
            //    GlobalState.Instance.SendAnimationChannel(gObject.name, animationSets.zScale);
            //}
            //MixerClient.GetInstance().SendQueryObjectData(gObject.name);
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