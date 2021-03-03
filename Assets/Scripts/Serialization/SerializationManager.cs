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

using System.IO;

using UnityEngine;

namespace VRtist.Serialization
{
    public static class SerializationManager
    {
        public static void Save(string path, IBlob data, bool deleteFolder = false)
        {
            DirectoryInfo folder = Directory.GetParent(path);
            if (!folder.Exists)
            {
                folder.Create();
            }
            else if (deleteFolder)
            {
                folder.Delete(true);
                folder.Create();
            }

            File.WriteAllBytes(path, data.ToBytes());
        }

        public static void Load(string path, IBlob data)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"Cannot load {path}: no such file.");
                return;
            }

            int index = 0;
            data.FromBytes(File.ReadAllBytes(path), ref index);
        }
    }
}
