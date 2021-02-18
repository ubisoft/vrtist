using UnityEngine;

namespace VRtist
{

    public class CurveInfo
    {
        public string objectName;
        public Curve curve;
    }

    /// <summary>
    /// Command to set the whole animation of an object.
    /// </summary>
    public class CommandRecordAnimations : ICommand
    {
        readonly GameObject gObject;
        readonly AnimationSet oldAnimationSet;
        readonly AnimationSet newAnimationSet;

        public CommandRecordAnimations(GameObject obj, AnimationSet oldAnimationSet, AnimationSet newAnimationSet)
        {
            gObject = obj;
            this.oldAnimationSet = oldAnimationSet;
            this.newAnimationSet = newAnimationSet;
        }

        public override void Undo()
        {
            if (null == oldAnimationSet)
            {
                SceneManager.ClearObjectAnimations(gObject);
                return;
            }
            SceneManager.SetObjectAnimation(gObject, oldAnimationSet);
        }

        public override void Redo()
        {
            SceneManager.SetObjectAnimation(gObject, newAnimationSet);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
