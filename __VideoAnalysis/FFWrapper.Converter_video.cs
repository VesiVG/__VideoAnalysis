using System;
using System.Drawing;
using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
namespace __VideoAnalysis
{
    public unsafe partial class FFWrapper
    {
        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public sealed class Converter_video : IDisposable
        {
            public unsafe IntPtr _convertedFrameBufferPtr;
            public readonly Size _destinationSize;
            public byte_ptrArray4 _dstData;
            public int_array4 _dstLinesize;
            public unsafe SwsContext* _pConvertContext;
            public AVPixelFormat fmt;
            public bool from_full_range;
            public int flgs;
            public Size sourceSize1;
            public AVPixelFormat _destinationPixelFormat;
            public AVPixelFormat _sourcePixelFormat;

            public Converter_video(Size sourceSize, AVPixelFormat sourcePixelFormat,
            Size destinationSize, AVPixelFormat destinationPixelFormat, /*AVColorSpace colorspc, int colorspc0, */int alignment)
            {
                _destinationSize = destinationSize;
                int SwsFlags = 0;
                one = false;
                fmt = destinationPixelFormat;
                _destinationPixelFormat = destinationPixelFormat;
                _sourcePixelFormat = sourcePixelFormat;
                sourceSize1 = sourceSize;

                SwsFlags = ffmpeg.SWS_AREA;
                SwsFlags |= ffmpeg.SWS_PRINT_INFO;
                flgs = SwsFlags;

                if (_pConvertContext == null)
                {
                    _pConvertContext = ffmpeg.sws_getContext(sourceSize.Width, sourceSize.Height, sourcePixelFormat,
                  destinationSize.Width,
                  destinationSize.Height, destinationPixelFormat,
                  SwsFlags, null, null, null);
                }
                if (_pConvertContext == null)
                {

                    return;
                }

                var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixelFormat, destinationSize.Width, destinationSize.Height, alignment);
                _convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
                _dstData = new byte_ptrArray4();
                _dstLinesize = new int_array4();

                var rret = ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_convertedFrameBufferPtr, destinationPixelFormat, destinationSize.Width, destinationSize.Height, alignment);
                //var rret = 0;
                if (rret < 0)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }



            public void Dispose()
            {
                if (_convertedFrameBufferPtr != null)
                    Marshal.FreeHGlobal(_convertedFrameBufferPtr);

                ffmpeg.sws_freeContext(_pConvertContext);
            }
            private static bool one = false;

            public unsafe AVFrame Convert(AVFrame* sourceFrame0/*, Converter_video cc*//*, out AVFrame dstframe*/)
            {

                var cc = this;
                var exa = (cc.from_full_range || sourceFrame0->color_range == AVColorRange.AVCOL_RANGE_JPEG) ? 1 : 0;
                if (!one)
                {
                    one = true;
                    logger?.log_m(0, 1, " Decoding: source frame color_range = [" + sourceFrame0->color_range.ToString() + "]\n");
                }

                SwsContext* pcnv = ffmpeg.sws_getCachedContext(cc._pConvertContext, cc.sourceSize1.Width, cc.sourceSize1.Height, cc._sourcePixelFormat, cc._destinationSize.Width, cc._destinationSize.Height, cc._destinationPixelFormat, cc.flgs, null, null, null);
                int aa = 0;
                if (pcnv == null)
                {
                    aa = ffmpeg.sws_scale(cc._pConvertContext, sourceFrame0->data, sourceFrame0->linesize, 0, sourceFrame0->height, (cc._dstData), (cc._dstLinesize));
                }
                else
                {
                    aa = ffmpeg.sws_scale(pcnv, sourceFrame0->data, sourceFrame0->linesize, 0, sourceFrame0->height, (cc._dstData), (cc._dstLinesize));
                }
                if (aa <= 0)
                {
                    System.Diagnostics.Debugger.Break();
                }
                var data = new byte_ptrArray8();
                data.UpdateFrom(cc._dstData);
                var linesize = new int_array8();
                linesize.UpdateFrom(cc._dstLinesize);

                return new AVFrame
                {
                    data = data,
                    linesize = linesize,
                    width = cc._destinationSize.Width,
                    height = cc._destinationSize.Height,
                    format = (int)cc.fmt,
                    pkt_dts = sourceFrame0->pkt_dts,
                    pkt_pos = sourceFrame0->pkt_pos,
                    pts = sourceFrame0->pts,
                    best_effort_timestamp = sourceFrame0->best_effort_timestamp
                };
            }





        }
    }


}
