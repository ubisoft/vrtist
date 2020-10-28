using UnityEngine;

namespace VRtist
{
    public class CommandRecordAnimations : ICommand
    {
        GameObject gObject;
        AnimationSet oldAnimationSet;
        AnimationSet newAnimationSet;
        public CommandRecordAnimations(GameObject obj, AnimationSet oldAnimationSet, AnimationSet newAnimationSet)
        {
            gObject = obj;
            this.oldAnimationSet = oldAnimationSet;
            this.newAnimationSet = newAnimationSet;
        }
        public override void Undo()
        {
            if(null == oldAnimationSet)
            {
                GlobalState.Animation.ClearAnimations(gObject);
                return;
            }
            GlobalState.Animation.SetObjectAnimation(gObject, oldAnimationSet);
        }

        public override void Redo()
        {
            GlobalState.Animation.SetObjectAnimation(gObject, newAnimationSet);
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