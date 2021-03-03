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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOUtilities
{
    public static string projectDirectory = "D:/";
    public static string CreateUniqueName(string name)
    {
        return name + DateTime.Now.ToBinary().ToString();
    }
    public static string GetAbsoluteFilename(string filename)
    {
        if (IsProjectRelative(filename))
            return System.IO.Path.Combine(projectDirectory, GetRelativeFilename(filename));

        return filename;        
    }
    public static string GetRelativeFilename(string filename)
    {
        if(IsProjectRelative(filename))
        {
            if (filename.StartsWith(projectDirectory, StringComparison.InvariantCultureIgnoreCase))
                return filename.Substring(projectDirectory.Length);
            return filename;
        }
        throw new System.Exception("File name is out of project");
    }

    public static void mkdir(string filename)
    {
        string path = System.IO.Path.GetDirectoryName(filename);
        System.IO.Directory.CreateDirectory(path);
    }

    public static bool IsProjectRelative(string filename)
    {
        if (!System.IO.Path.IsPathRooted(filename))
            return true;
        return filename.StartsWith(projectDirectory, StringComparison.InvariantCultureIgnoreCase);            
    }

    public static string CreatePaintFilename(string name)
    {
        return "Paint/" + IOUtilities.CreateUniqueName(name) + ".obj";
    }
}
