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
