using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class OIIOAPI
{
    public enum BASETYPE
    {
        UNKNOWN, NONE,
        UCHAR, UINT8 = UCHAR, CHAR, INT8 = CHAR,
        USHORT, UINT16 = USHORT, SHORT, INT16 = SHORT,
        UINT, UINT32 = UINT, INT, INT32 = INT,
        ULONGLONG, UINT64 = ULONGLONG, LONGLONG, INT64 = LONGLONG,
        HALF, FLOAT, DOUBLE, STRING, PTR, LASTBASE
    };
    
    [DllImport("oiio_wrapper")]
    public static extern int oiio_open_image(string path);

    [DllImport("oiio_wrapper")]
    public static extern int oiio_close_image();

    [DllImport("oiio_wrapper")]
    public static extern int oiio_get_image_info(ref int width, ref int height, ref int nchannels, ref BASETYPE format);

    [DllImport("oiio_wrapper")]
    public static extern int oiio_fill_image_data(IntPtr data, int rgb_to_rgba);
}
