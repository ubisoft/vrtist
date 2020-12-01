namespace VRtist
{
    public class VolumeController : ParametersController
    {
        public VolumeParameters parameters = new VolumeParameters();
        public override Parameters GetParameters() { return parameters; }
    }
}
