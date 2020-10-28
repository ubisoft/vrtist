using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandClearAnimations : ICommand
    {
        GameObject gObject;
        AnimationSet animationSet;
        public CommandClearAnimations(GameObject obj)
        {
            gObject = obj;
            animationSet = GlobalState.Animation.GetObjectAnimation(obj);
        }

        public override void Undo()
        {
            if(null != animationSet)
                GlobalState.Animation.SetObjectAnimation(gObject, animationSet);
            // TODO mixer
        }
        public override void Redo()
        {
            GlobalState.Animation.ClearAnimations(gObject);
            // TODO mixer
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