using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

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

		}
	}
}
