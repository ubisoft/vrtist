using UnityEngine;

namespace VRtist
{

    public class CurveInfo
    {
        public string objectName;
        public Curve curve;
    }

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
            if (null == oldAnimationSet)
            {
                GlobalState.Animation.ClearAnimations(gObject);
                MixerClient.Instance.SendClearAnimations(new ClearAnimationInfo { gObject = gObject });
                return;
            }
            GlobalState.Animation.SetObjectAnimation(gObject, oldAnimationSet);
            foreach (Curve curve in oldAnimationSet.curves.Values)
            {
                MixerClient.Instance.SendAnimationCurve(new CurveInfo { objectName = gObject.name, curve = curve });
            }
        }

        public override void Redo()
        {
            GlobalState.Animation.SetObjectAnimation(gObject, newAnimationSet);
            foreach (Curve curve in newAnimationSet.curves.Values)
            {
                MixerClient.Instance.SendAnimationCurve(new CurveInfo { objectName = gObject.name, curve = curve });
            }
        }
        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
