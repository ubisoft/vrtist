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
