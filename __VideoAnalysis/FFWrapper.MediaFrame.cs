using FFmpeg.AutoGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace __VideoAnalysis
{
	public unsafe partial class FFWrapper
	{
		[StructLayout(LayoutKind.Sequential)]
		public class Pool : IDisposable
		{
			public Pool(int maxcap)
			{
				_pool = new List<Content>();
				_locker = new object();
				MaxCapacity = maxcap;
				FramesPassed = 0;
			}
			public int MaxCapacity;
			public static long FramesPassed;
			public struct Content : IDisposable
			{
				public long i;
				public MediaFrame Frame;
				public bool IsEmpty;
				public Content(MediaFrame nv = null, long ii = -1, bool iisemp = true)
				{
					i = ii < 0 ? (nv != null ? nv.Id : 0) : ii;
					Frame = new MediaFrame();
					if (nv != null)
						Frame.BaitAndSwitch(nv);
					//Frame = (nv==null)? new MediaFrame() : nv;
					IsEmpty = false;
				}
				//public Content(MediaFrame val)
				//{
				//    i = 0;
				//    Frame = new MediaFrame();
				//    Frame.BaitAndSwitch(val);
				//    val.Dispose();
				//    IsEmpty = false;
				//}
				//public void New(AVFrame* val)
				//{
				//    i = 0;
				//    Frame = new MediaFrame(val);
				//    IsEmpty = false;
				//}

				public void Dispose()
				{
					Frame.Dispose();
					IsEmpty = true;
					i = -1;
				}
			}
			public void Add(MediaFrame val)
			{
				if (_pool.Count == MaxCapacity)
				{
					RemoveLast();
				}
				lock (_locker) _pool.Add(new Content(val));
			}
			public void Add(AVFrame* val, long i)
			{
				if (_pool.Count == MaxCapacity)
				{
					RemoveLast();
				}
				lock (_locker) _pool.Add(new Content(new MediaFrame(val,i)));
				
			}
			public int GetNext(out MediaFrame _frame)
			{
				if (_pool == null || _pool.Count <= 0)
				{
					_frame = null;
					return -1;
				}
				_frame = new MediaFrame();
				lock (_locker)
				{
					_frame.BaitAndSwitch(_pool[0].Frame);
					FramesPassed = _frame.Id>FramesPassed? _frame.Id : FramesPassed;
					_pool[0].Dispose();
					_pool.RemoveAt(0);
				}
				return 0;
			}
			private void RemoveLast()
			{
				if (_pool == null || _pool.Count <= 0)
					return;
				lock (_locker)
				{
					_pool[_pool.Count - 1].Dispose();
					_pool.RemoveAt(_pool.Count - 1);
				}
			}
			public int GetLast(out MediaFrame _frame)
			{
				if (_pool == null || _pool.Count <= 0)
				{
					_frame = null;
					return -1;
				}
				_frame = new MediaFrame();
				lock (_locker)
				{
					_frame.BaitAndSwitch(_pool[_pool.Count - 1].Frame);
					_pool[_pool.Count - 1].Dispose();
					_pool.RemoveAt(_pool.Count - 1);
				}
				return 0;
			}
			private List<Content> _pool;
			private readonly object _locker;
			public void Dispose()
			{
				if (_pool == null || _pool.Count == 0)
				{
					_pool.Clear();
					return;
				}
				lock (_locker)
				{
					foreach (var fr in _pool)
					{
						fr.Frame?.Dispose();
					}
					_pool.Clear();
				}
			}
		}

		public class MediaFrame : IDisposable
		{
			public MediaFrame()
			{
				pFrame = ffmpeg.av_frame_alloc();
				IsDisposed = false;
				ShowTime = 0;
				Id = 0;
			}
			public MediaFrame(AVFrame* old)
			{
				pFrame = ffmpeg.av_frame_alloc();
				IsDisposed = false;
				ShowTime = 0;
				BaitAndSwitch(old);
				Id = -1;
			}
			public MediaFrame(AVFrame* old, long id)
			{
				pFrame = ffmpeg.av_frame_alloc();
				IsDisposed = false;
				ShowTime = 0;
				BaitAndSwitch(old);
				Id = id;
			}
			public void BaitAndSwitch(AVFrame* old)
			{
				IsDisposed = false;
				ffmpeg.av_frame_move_ref(pFrame, old);
				ShowTime = pFrame->pts;
			}
			public void BaitAndSwitch(MediaFrame old)
			{
				ffmpeg.av_frame_move_ref(pFrame, old.pFrame);
				ShowTime = pFrame->pts;
				Id = old.Id;
			}
			public void Clear()
			{
				ffmpeg.av_frame_unref(pFrame);
				IsDisposed = true;
				ShowTime = 0;
			}
			public AVFrame* pFrame;
			public long ShowTime;
			public long Id;
			public bool IsDisposed;
			public void Dispose()
			{
				fixed (AVFrame** ffixed = &pFrame)
					ffmpeg.av_frame_free(ffixed);
				ShowTime = 0;
				IsDisposed = true;
			}
		}

	}
}
