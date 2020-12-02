using UnityEngine;

namespace VRtist
{
    public class CommandAddKeyframes : CommandGroup
    {
        GameObject gObject;
        public CommandAddKeyframes(GameObject obj) : base("Add Keyframes")
        {
            gObject = obj;
            Interpolation interpolation = GlobalState.Settings.interpolation;
            int frame = GlobalState.Animation.CurrentFrame;

            new CommandAddKeyframe(gObject, AnimatableProperty.PositionX, frame, gObject.transform.localPosition.x, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionY, frame, gObject.transform.localPosition.y, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.PositionZ, frame, gObject.transform.localPosition.z, interpolation).Submit();

            // convert to ZYX euler
            Vector3 angles = gObject.transform.localEulerAngles;
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationX, frame, angles.x, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationY, frame, angles.y, interpolation).Submit();
            new CommandAddKeyframe(gObject, AnimatableProperty.RotationZ, frame, angles.z, interpolation).Submit();

            CameraController controller = gObject.GetComponent<CameraController>();
            LightController lcontroller = gObject.GetComponent<LightController>();

            if (null != controller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.CameraFocal, frame, controller.focal, interpolation).Submit();
            }
            else if (null != lcontroller)
            {
                new CommandAddKeyframe(gObject, AnimatableProperty.LightIntensity, frame, lcontroller.GetPower(), interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorR, frame, lcontroller.color.r, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorG, frame, lcontroller.color.g, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ColorB, frame, lcontroller.color.b, interpolation).Submit();
            }
            else
            {
                // Scale
                Vector3 scale = gObject.transform.localScale;
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleX, frame, scale.x, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleY, frame, scale.y, interpolation).Submit();
                new CommandAddKeyframe(gObject, AnimatableProperty.ScaleZ, frame, scale.z, interpolation).Submit();
            }
        }

        public override void Undo()
        {
            base.Undo();
        }

        public override void Redo()
        {
            base.Redo();
        }
        public override void Submit()
        {
            base.Submit();
        }
    }
}
