using System.IO; // Path
using UnityEngine;
using UnityEditor;

using System.Runtime.InteropServices;

[UnityEditor.AssetImporters.ScriptedImporter(5, "tx")]
public class TxImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    [SerializeField] public Vector2Int imageDimensions;

    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        // NOTE: repere bas gauche, Y up.

        int ret = OIIOAPI.oiio_open_image(assetPath);
        if (ret == 0)
        {
            Debug.Log("could not open " + assetPath);
            return;
        }

        int width = -1;
        int height = -1;
        int nchannels = -1;
        OIIOAPI.BASETYPE format = OIIOAPI.BASETYPE.NONE;
        ret = OIIOAPI.oiio_get_image_info(ref width, ref height, ref nchannels, ref format);
        if (ret == 0)
        {
            Debug.Log("Could not get width/height of " + assetPath);
            return;
        }

        imageDimensions.Set(width, height);
        TextureFormat textureFormat = Format2Format(format, nchannels);
        var image = new Texture2D(width, height, textureFormat, false, true); // with mips, linear

        int do_rgb_to_rgba = 0;
        if ((format == OIIOAPI.BASETYPE.FLOAT && nchannels == 3)
            || (format == OIIOAPI.BASETYPE.HALF && nchannels == 3))
        {
            do_rgb_to_rgba = 1;
        }
        //Color[] pixels = image.GetPixels();
        var pixels = image.GetRawTextureData();
        GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        ret = OIIOAPI.oiio_fill_image_data(handle.AddrOfPinnedObject(), do_rgb_to_rgba);
        if (ret == 1)
        {
            image.LoadRawTextureData(pixels);
            //image.SetPixels(pixels);
            image.Apply();
        }
        else
        {
            Debug.Log("Could not fill texture data of " + assetPath);
            return;
        }

        

#if UNITY_2017_3_OR_NEWER
        var filename = Path.GetFileNameWithoutExtension(assetPath);
        ctx.AddObjectToAsset(filename, image);
        ctx.SetMainObject(image);
#else
        ctx.SetMainObject(image);
#endif
    }

    private TextureFormat Format2Format(OIIOAPI.BASETYPE format, int nchannels)
    {
        TextureFormat defaultFormat = TextureFormat.RGBA32;

        switch (format)
        {
            case OIIOAPI.BASETYPE.UCHAR:
            case OIIOAPI.BASETYPE.CHAR:
            {
                switch (nchannels)
                {
                        case 1: return TextureFormat.R8;
                        case 2: return TextureFormat.RG16;
                        case 3: return TextureFormat.RGB24;
                        case 4: return TextureFormat.RGBA32;
                        default: return defaultFormat;
                }
            }
            
            case OIIOAPI.BASETYPE.HALF:
            {
                switch (nchannels)
                {
                    case 1: return TextureFormat.RHalf;
                    case 2: return TextureFormat.RGHalf;
                    case 3: return TextureFormat.RGBAHalf; // RGBHalf is NOT SUPPORTED
                    case 4: return TextureFormat.RGBAHalf;
                    default: return defaultFormat;
                }
            }

            case OIIOAPI.BASETYPE.FLOAT:
            {
                switch (nchannels)
                {
                    case 1: return TextureFormat.RFloat;
                    case 2: return TextureFormat.RGFloat;
                    case 3: return TextureFormat.RGBAFloat; // RGBFloat is NOT SUPPORTED
                    case 4: return TextureFormat.RGBAFloat;
                    default: return defaultFormat;
                }
            }

            default: return defaultFormat;
        }
    }
}
