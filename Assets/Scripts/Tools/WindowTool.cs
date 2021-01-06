namespace VRtist
{
    // NOTE: Derives from SelectorBase to inherit only the Grip functionality, to grip windows toolbars.
    //       Disables all Selection with trigger by overriding DoUpdate() to nothing.

    public class WindowTool : SelectorBase
    {
        public void Start()
        {
            Init();

            gripTooltip = Tooltips.CreateTooltip(rightController.gameObject, Tooltips.Anchors.Grip, "Grab");
            Tooltips.SetTooltipVisibility(gripTooltip, true);
        }

        protected override void DoUpdateGui()
        {
            base.DoUpdate();
        }
    }
}
