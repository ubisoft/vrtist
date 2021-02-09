namespace VRtist
{
    public class LobbyTool : ToolBase
    {
        protected override void Init()
        {
            enableToggleTool = false;
            SetTooltips();
        }

        public override void SetTooltips()
        {
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Primary, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Secondary, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Trigger, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Grip, false);
            Tooltips.SetVisible(VRDevice.PrimaryController, Tooltips.Location.Joystick, false);
        }

        protected override void DoUpdate()
        {
            // Nothing to do
        }
    }
}
