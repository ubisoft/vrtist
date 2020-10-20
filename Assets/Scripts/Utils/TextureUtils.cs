using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VRtist
{
    public class TextureUtils
    {
        public static Texture2D CreateSmallImage()
        {
            Texture2D smallImage = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            smallImage.LoadRawTextureData(new byte[] { 0, 0, 0, 255 });
            return smallImage;
        }

        public static Texture2D LoadTextureOIIO(string filePath, bool isLinear)
        {
            // TODO: need to flip? Repere bottom left, Y-up
            int ret = OIIOAPI.oiio_open_image(filePath);
            if (ret == 0)
            {
                Debug.LogWarning("Could not open image " + filePath + " with OIIO.");
                return null;
            }

            int width = -1;
            int height = -1;
            int nchannels = -1;
            OIIOAPI.BASETYPE format = OIIOAPI.BASETYPE.NONE;
            ret = OIIOAPI.oiio_get_image_info(ref width, ref height, ref nchannels, ref format);
            if (ret == 0)
            {
                Debug.LogWarning("Could not get info about image " + filePath + " with OIIO");
                return null;
            }

            TexConv conv = new TexConv();
            bool canConvert = Format2Format(format, nchannels, ref conv);
            if (!canConvert)
            {
                Debug.LogWarning("Could not create image from format: " + conv.format + " with option: " + conv.options);
                return CreateSmallImage();
            }
            // TMP
            else if (conv.options.HasFlag(TextureConversionOptions.SHORT_TO_FLOAT) || conv.options.HasFlag(TextureConversionOptions.SHORT_TO_INT))
            {
                Debug.LogWarning("Could not create image from format: " + conv.format + " with option: " + conv.options);
                return CreateSmallImage();
            }

            Texture2D image = new Texture2D(width, height, conv.format, true, isLinear); // with mips

            var pixels = image.GetRawTextureData();
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            ret = OIIOAPI.oiio_fill_image_data(handle.AddrOfPinnedObject(), conv.options.HasFlag(TextureConversionOptions.RGB_TO_RGBA) ? 1 : 0);
            if (ret == 1)
            {
                image.LoadRawTextureData(pixels);
                image.Apply();
            }
            else
            {
                Debug.LogWarning("Could not fill texture data of " + filePath + " with OIIO.");
                return null;
            }

            return image;
        }

        enum TextureConversionOptions
        {
            NO_CONV = 0,
            RGB_TO_RGBA = 1,
            SHORT_TO_INT = 2,
            SHORT_TO_FLOAT = 4,
        }; // TODO: fill, enhance

        class TexConv
        {
            public TextureFormat format;
            public TextureConversionOptions options;
        };

        private static bool Format2Format(OIIOAPI.BASETYPE format, int nchannels, ref TexConv result)
        {
            // TODO: handle compressed formats.

            result.format = TextureFormat.RGBA32;
            result.options = TextureConversionOptions.NO_CONV;

            switch (format)
            {
                case OIIOAPI.BASETYPE.UCHAR:
                case OIIOAPI.BASETYPE.CHAR:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.R8; break;
                        case 2: result.format = TextureFormat.RG16; break;
                        case 3: result.format = TextureFormat.RGB24; break;
                        case 4: result.format = TextureFormat.RGBA32; break;
                        default: return false;
                    }
                    break;

                case OIIOAPI.BASETYPE.USHORT:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.R16; break;
                        case 2: result.format = TextureFormat.RGFloat; result.options = TextureConversionOptions.SHORT_TO_FLOAT; break;
                        case 3: result.format = TextureFormat.RGBAFloat; result.options = TextureConversionOptions.SHORT_TO_FLOAT | TextureConversionOptions.RGB_TO_RGBA; break;
                        case 4: result.format = TextureFormat.RGBAFloat; result.options = TextureConversionOptions.SHORT_TO_FLOAT; break;
                        // R16_G16, R16_G16_B16 and R16_G16_B16_A16 do not exist
                        default: return false;
                    }
                    break;

                case OIIOAPI.BASETYPE.HALF:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.RHalf; break;
                        case 2: result.format = TextureFormat.RGHalf; break;
                        case 3: result.format = TextureFormat.RGBAHalf; result.options = TextureConversionOptions.NO_CONV | TextureConversionOptions.RGB_TO_RGBA; break; // RGBHalf is NOT SUPPORTED
                        case 4: result.format = TextureFormat.RGBAHalf; break;
                        default: return false;
                    }
                    break;

                case OIIOAPI.BASETYPE.FLOAT:
                    switch (nchannels)
                    {
                        case 1: result.format = TextureFormat.RFloat; break;
                        case 2: result.format = TextureFormat.RGFloat; break;
                        case 3: result.format = TextureFormat.RGBAFloat; result.options = TextureConversionOptions.NO_CONV | TextureConversionOptions.RGB_TO_RGBA; break;// RGBFloat is NOT SUPPORTED
                        case 4: result.format = TextureFormat.RGBAFloat; break;
                        default: return false;
                    }
                    break;

                default: return false;
            }

            return true;
        }

        public static Texture2D LoadTextureDXT(string filePath, bool isLinear)
        {
            byte[] ddsBytes = System.IO.File.ReadAllBytes(filePath);

            byte[] format = { ddsBytes[84], ddsBytes[85], ddsBytes[86], ddsBytes[87], 0 };
            string sFormat = System.Text.Encoding.UTF8.GetString(format);
            TextureFormat textureFormat;

            if (sFormat != "DXT1")
                textureFormat = TextureFormat.DXT1;
            else if (sFormat != "DXT5")
                textureFormat = TextureFormat.DXT5;
            else return null;

            byte ddsSizeCheck = ddsBytes[4];
            if (ddsSizeCheck != 124)
                throw new Exception("Invalid DDS DXTn texture. Unable to read");  //this header byte should be 124 for DDS image files

            int height = ddsBytes[13] * 256 + ddsBytes[12];
            int width = ddsBytes[17] * 256 + ddsBytes[16];

            int DDS_HEADER_SIZE = 128;
            byte[] dxtBytes = new byte[ddsBytes.Length - DDS_HEADER_SIZE];
            Buffer.BlockCopy(ddsBytes, DDS_HEADER_SIZE, dxtBytes, 0, ddsBytes.Length - DDS_HEADER_SIZE);

            Texture2D texture = new Texture2D(width, height, textureFormat, true, isLinear);
            texture.LoadRawTextureData(dxtBytes);
            texture.Apply();

            return texture;
        }

        public static Texture2D LoadTextureFromBuffer(byte[] data, bool isLinear)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.ARGB32, true, isLinear);
            bool res = tex.LoadImage(data);
            if (!res)
                return null;

            return tex;
        }
    }
}