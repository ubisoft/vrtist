using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to clear all the animations on an object.
    /// </summary>
    public class CommandClearAnimations : ICommand
    {
        readonly GameObject gObject;
        readonly AnimationSet animationSet;
        public CommandClearAnimations(GameObject obj)
        {
            gObject = obj;
            animationSet = GlobalState.Animation.GetObjectAnimation(obj);
        }

        public override void Undo()
        {
            if (null != animationSet)
            {
                SceneManager.SetObjectAnimation(gObject, animationSet);
            }
        }
        public override void Redo()
        {
            SceneManager.ClearObjectAnimations(gObject);
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}