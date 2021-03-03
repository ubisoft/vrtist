/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace VRtist
{
    /// <summary>
    /// Command manage shot manager.
    /// </summary>
    public class CommandShotManager : ICommand
    {
        private readonly ShotManagerActionInfo oldData;
        private readonly ShotManagerActionInfo newData;
        public CommandShotManager(ShotManagerActionInfo info)
        {
            newData = info;
            oldData = BuildInvertData();
        }

        public CommandShotManager(ShotManagerActionInfo oldInfo, ShotManagerActionInfo newInfo)
        {
            oldData = oldInfo;
            newData = newInfo;
        }

        private ShotManagerActionInfo BuildInvertData()
        {
            ShotManagerActionInfo data = new ShotManagerActionInfo();
            switch (newData.action)
            {
                case ShotManagerAction.AddShot:
                    {
                        data.action = ShotManagerAction.DeleteShot;
                        data.shotIndex = newData.shotIndex + 1;
                    }
                    break;
                case ShotManagerAction.DeleteShot:
                    {
                        data.action = ShotManagerAction.AddShot;
                        data.shotIndex = newData.shotIndex - 1;
                        data.shotName = newData.shotName;
                        data.shotStart = newData.shotStart;
                        data.shotEnd = newData.shotEnd;
                        data.shotColor = newData.shotColor;
                        data.camera = newData.camera;
                        data.shotEnabled = newData.shotEnabled;
                    }
                    break;
                case ShotManagerAction.DuplicateShot:
                    {
                        data.action = ShotManagerAction.DeleteShot;
                        data.shotIndex = newData.shotIndex;
                    }
                    break;
                case ShotManagerAction.MoveShot:
                    {
                        data.action = ShotManagerAction.MoveShot;
                        data.shotIndex = newData.shotIndex + newData.moveOffset;
                        data.moveOffset = -newData.moveOffset;
                    }
                    break;
                case ShotManagerAction.UpdateShot:
                    {
                    }
                    break;
            }
            return data;
        }

        private void Apply(ShotManagerActionInfo info)
        {
            SceneManager.ApplyShotManagegrAction(info);
        }

        public override void Undo()
        {
            Apply(oldData);
        }

        public override void Redo()
        {
            Apply(newData);
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
