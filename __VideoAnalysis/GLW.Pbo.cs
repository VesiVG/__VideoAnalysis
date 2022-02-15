using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Windows.Forms;
using gl = OpenTK.Graphics.OpenGL.GL;
using wwgl = OpenTK.Graphics.OpenGL;

namespace __VideoAnalysis
{
	partial class Program
	{
		public partial class GLW
		{
			public class PboPool : IDisposable
			{
				public static int MaxPboPerFrame = 4;
				public PboPool(System.Drawing.Size[] size, wwgl.BufferUsageHint hint, int buffers, int bytesperpixel)
				{
					pbs = new System.Collections.Generic.List<Pbo>();
					pbuffers = buffers;
					for (int cntp = 0; cntp < MaxPboPerFrame; cntp++)
					{
						for (int cnt = 0; cnt < (buffers); cnt++)
						{
							pbs.Add(new Pbo(size[cnt], hint, bytesperpixel));
						}
					}
				}
				public void Initialize(FFWrapper.MediaFrame frm)
				{
					_buffer = new FFWrapper.MediaFrame();
					_buffer.BaitAndSwitch(frm);
				}
				public int position_bottom = 2;

				public int GetN(int a)
				{
					if (a == MaxPboPerFrame-1)
						return 0;
					else
						return a + 1;
				}
				public int GetP(int a)
				{
					if (a == 0)
						return MaxPboPerFrame-1;
					else
						return a - 1;
				}
				public int GetO(int a)
				{
					return (a + 2) % MaxPboPerFrame;
				}
				public void MoveIt(int pos)
				{
					if (_buffer == null || _buffer.IsDisposed)
						return;
					//int pos = (position_bottom);
					for (int cnt=0; cnt<pbuffers;cnt++)
					{
						pbs[(pos*pbuffers)+cnt].Upload(cnt, _buffer);
					}
					_buffer?.Dispose();
				}
		
				public void Dispose()
				{
					if (pbs!=null )
					{
						if (pbs.Count > 0)
						{
							for (int cnt=0;cnt<pbs.Count;cnt++)
							{
								pbs[cnt].Dispose();
							}
							pbs.Clear();
						}
					}
				}
				private FFWrapper.MediaFrame _buffer;
				public int pbuffers;
				public System.Collections.Generic.List<Pbo> pbs;
			}
			public class Pbo : IDisposable
			{
				public int _ID;
				public System.Drawing.Size _Size;
				public int _BufSize;
				public int BperP;
				public IntPtr Mapped_ptr;
				public IntPtr sync_obj;
				public wwgl.BufferUsageHint _Hint;
				private readonly object lock2;
				private bool _closed;
				public Pbo(System.Drawing.Size size, wwgl.BufferUsageHint hint, int bytesperpixel)
				{
					_ID = gl.GenBuffer();
					sync_obj = IntPtr.Zero;
					lock2 = new object();
					BperP = bytesperpixel;
					_closed = false;
					lock (lock2) Init(size, hint, bytesperpixel);
				}
				public void Init(System.Drawing.Size size, wwgl.BufferUsageHint hint, int bytesperpixel)
				{
					_Size = size;
					_BufSize = size.Width * size.Height* bytesperpixel;
					_Hint = hint;
					gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, _ID);
					gl.BufferData(wwgl.BufferTarget.PixelUnpackBuffer, _BufSize, IntPtr.Zero, hint);
					gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, 0);

				}
				public void Resize(System.Drawing.Size size, wwgl.BufferUsageHint hint, int bytesperpixel)
				{

					this.Wait_Sync();
					lock (lock2) Init(size, hint, bytesperpixel);
					this.Create_Sync();
				}
				public void Dispose()
				{
					lock (lock2)
					{
						gl.DeleteBuffer(_ID);
						_Size = System.Drawing.Size.Empty;
						_closed = true;
					}
				}
				public void Create_Sync()
				{
					if (this.sync_obj != IntPtr.Zero)
					{
						gl.DeleteSync(this.sync_obj);
						this.sync_obj = IntPtr.Zero;
					}

					if (this.sync_obj == IntPtr.Zero)
					{
						this.sync_obj = gl.FenceSync(wwgl.SyncCondition.SyncGpuCommandsComplete, wwgl.WaitSyncFlags.None);
					}
				}

				public void Wait_Sync()
				{
					const ulong kOneSecondInNanoSeconds = 1000000000;
					wwgl.ClientWaitSyncFlags waitFlags = wwgl.ClientWaitSyncFlags.None;
					ulong waitDuration = 0;
					if (this.sync_obj == IntPtr.Zero)
					{
						return;
					}
					while (true)
					{
						wwgl.WaitSyncStatus waitRet = gl.ClientWaitSync(this.sync_obj, waitFlags, waitDuration);
						if (waitRet == wwgl.WaitSyncStatus.AlreadySignaled || waitRet == wwgl.WaitSyncStatus.ConditionSatisfied)
						{
							break;
						}
						waitFlags = wwgl.ClientWaitSyncFlags.SyncFlushCommandsBit;
						waitDuration = kOneSecondInNanoSeconds;
					}
					if (this.sync_obj != IntPtr.Zero)
					{
						gl.DeleteSync(this.sync_obj);
						this.sync_obj = IntPtr.Zero;
					}
				}
				public unsafe bool Upload(int plane, FFWrapper.MediaFrame src)
				{
					lock (lock2)
					{
						if (_closed)
						{
							return false;
						}
						gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, _ID);
						//gl.BufferData(wwgl.BufferTarget.PixelUnpackBuffer, _BufSize, IntPtr.Zero, _Hint);
						Mapped_ptr = gl.MapBuffer(wwgl.BufferTarget.PixelUnpackBuffer, wwgl.BufferAccess.WriteOnly);
						if (Mapped_ptr != null && Mapped_ptr != IntPtr.Zero)
						{
							//av_image_copy_plane is basically memcpy with stride
							FFmpeg.AutoGen.ffmpeg.av_image_copy_plane((byte*)Mapped_ptr, _Size.Width * BperP, src.pFrame->data[(uint)plane], src.pFrame->linesize[(uint)plane], _Size.Width * BperP, _Size.Height);
							
							gl.UnmapBuffer(wwgl.BufferTarget.PixelUnpackBuffer);
							gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, 0);
							return true;
						}
						gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, 0);
					}
					return false;
				}
			}
		}
	}

}
