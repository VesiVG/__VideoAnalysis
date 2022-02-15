using FFmpeg.AutoGen;
using System;
using System.Runtime.InteropServices;

namespace __VideoAnalysis
{
    public unsafe partial class FFWrapper
    {
        public static class Tool
        {
            public static string Err2String(int error)
            {
                var bufferSize = 1024;
                var buffer = stackalloc byte[bufferSize];
                _ = ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
                var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
                return message;
            }
            public static void Zeromem(IntPtr buffer, int buffSize)
            {
                for (int i = 0; i < buffSize / 8; i += 8)
                {
                    Marshal.WriteInt64(buffer, i, 0x00);
                }

                for (int i = buffSize % 8; i < -1; i--)
                {
                    Marshal.WriteByte(buffer, buffSize - i, 0x00);
                }
            }
            public static string PtrToStringUTF32(byte* stringAddress)
            {
                if (stringAddress == null) return null;
                if (*stringAddress == 0) return string.Empty;

                return Marshal.PtrToStringAnsi((IntPtr)stringAddress);
            }
         public static AVPixelFormat MatchPer(AVPixelFormat fmt)
         {
            AVPixelFormat src = fmt;
            switch (fmt)
            {
               case AVPixelFormat.AV_PIX_FMT_NV12:
               case AVPixelFormat.AV_PIX_FMT_DXVA2_VLD:
               case AVPixelFormat.AV_PIX_FMT_D3D11:
                  src = AVPixelFormat.AV_PIX_FMT_NV12;
                  break;
               case AVPixelFormat.AV_PIX_FMT_YUVJ420P:
                  src = AVPixelFormat.AV_PIX_FMT_YUV420P;
                  break;
            }
            return src;
         }
         //https://github.com/emgucv/emgucv/blob/master/Emgu.CV/PInvoke/CvInvokeImgproc.cs
         public static System.Drawing.Size FitInsideKeepAspectRatio(System.Drawing.Size current, System.Drawing.Size max)
            {
                double scale = Math.Min((double)max.Width / current.Width, (double)max.Height / current.Height);
                int a1 = (int)(current.Width * scale);
                a1 = a1 % 2 == 0 ? a1 : a1 - 1;
                a1 = Math.Max(a1, 0);
                int a2 = (int)(current.Height * scale);
                a2 = a2 % 2 == 0 ? a2 : a2 - 1;
                a2 = Math.Max(a2, 0);
                return new System.Drawing.Size(Math.Min(a1, max.Width), Math.Min(a2, max.Height));
            }


        }
    }
}
