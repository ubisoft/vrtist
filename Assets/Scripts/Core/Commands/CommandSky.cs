using UnityEngine;

namespace VRtist
{
    [System.Serializable]
    public class SkySettings
    {
        public Color topColor;
        public Color middleColor;
        public Color bottomColor;
    }

    /// <summary>
    /// Command to change sky properties.
    /// </summary>
    public class CommandSky : ICommand
    {
        readonly SkySettings oldSky;
        readonly SkySettings newSky;

        public CommandSky(SkySettings oldSky, SkySettings newSky)
        {
            this.oldSky = oldSky;
            this.newSky = newSky;
        }

        public override void Undo()
        {
            SceneManager.SetSky(oldSky);
        }

        public override void Redo()
        {
            SceneManager.SetSky(newSky);
        }

        public override void Submit()
        {
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
