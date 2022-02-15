using System;
using System.Collections.Generic;
using System.Windows.Forms;
using gl = OpenTK.Graphics.OpenGL.GL;
using wwgl = OpenTK.Graphics.OpenGL;
namespace __VideoAnalysis
{
	partial class Program
	{
		public partial class GLW
		{
			public abstract class Frame : IDisposable
			{
				public static int _current_tex = 0;
				public static int slot_multiplier = 0;
				public System.Drawing.Size Size = System.Drawing.Size.Empty;
				public System.Collections.Generic.List<Texture> plane;
				public PboPool pool;
				
				public void Upload(IntPtr bra, int planeslot = 0)
				{
					gl.BindTexture(wwgl.TextureTarget.Texture2D, plane[planeslot].Id);
					gl.TexSubImage2D(wwgl.TextureTarget.Texture2D, 0, 0, 0, plane[planeslot].size.Width, plane[planeslot].size.Height, plane[planeslot].pixel_upload_format, plane[planeslot].pixel_transfer_type, (IntPtr)bra);

				}


					public void Load(FFWrapper.MediaFrame bra)
				{
					pool.Initialize(bra);
					pool.MoveIt(pool.position_bottom);
					bra.Dispose();
					int pos = pool.GetP(pool.position_bottom)*pool.pbuffers;
					Upload(pool.pbs[pos], new System.Drawing.Rectangle(0, 0, plane[0].size.Width, plane[0].size.Height), 0);
					if (this is YUV420P_texture)
					{
						Upload(pool.pbs[pos+1], new System.Drawing.Rectangle(0, 0, plane[1].size.Width, plane[1].size.Height), 1);
						Upload(pool.pbs[pos+2], new System.Drawing.Rectangle(0, 0, plane[2].size.Width, plane[2].size.Height), 2);
					}
					pool.position_bottom = pool.GetN(pool.position_bottom);
				}
				public void Upload(Pbo _pb, System.Drawing.Rectangle roi, int planeslot = 0)
				{
					_pb.Wait_Sync();

					gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, _pb._ID);
					gl.BindTexture(wwgl.TextureTarget.Texture2D, plane[planeslot].Id);
					gl.TexSubImage2D(wwgl.TextureTarget.Texture2D, 0, roi.X, roi.Y, roi.Width, roi.Height, plane[planeslot].pixel_upload_format, plane[planeslot].pixel_transfer_type, IntPtr.Zero);
					//wwgl.GL.BindTexture(wwgl.TextureTarget.Texture2D, 0);
					gl.BindBuffer(wwgl.BufferTarget.PixelUnpackBuffer, 0);
					_pb.Create_Sync();

				}
				public void Dispose()
				{
					if (plane != null && plane.Count > 0)
					{
						for (int cnt = 0; cnt < plane.Count; cnt++) plane[cnt].Dispose();
						plane.Clear();
					}
					pool.Dispose();
				}
			}
			public unsafe class RGBA_texture : Frame, IDisposable
			{
				public RGBA_texture(wwgl.PixelInternalFormat internalformat, wwgl.PixelFormat pixelformat, wwgl.PixelFormat pixeluploadfmt, System.Drawing.Size size)
				{
					plane = new System.Collections.Generic.List<Texture>();
					plane.Add(new Texture(internalformat, pixelformat, pixeluploadfmt, size, 0));
					Size = size;
					pool = new PboPool(new System.Drawing.Size[1] { size }, wwgl.BufferUsageHint.DynamicDraw, 1, 4);
				}
			}
			public unsafe class RGB_texture : Frame, IDisposable
			{
				public RGB_texture(wwgl.PixelInternalFormat internalformat, wwgl.PixelFormat pixelformat, wwgl.PixelFormat pixeluploadfmt, System.Drawing.Size size)
				{
					plane = new System.Collections.Generic.List<Texture>();
					plane.Add(new Texture(internalformat, pixelformat, pixeluploadfmt, size, 0));
					Size = size;
					pool = new PboPool(new System.Drawing.Size[1] { size }, wwgl.BufferUsageHint.DynamicDraw, 1, 3);
				}

			}
			public unsafe class YUV420P_texture : Frame, IDisposable
			{
				public YUV420P_texture(wwgl.PixelInternalFormat internalformat, wwgl.PixelFormat pixelformat, wwgl.PixelFormat pixeluploadfmt, System.Drawing.Size size)
				{
					plane = new System.Collections.Generic.List<Texture>();
					var _size = new System.Drawing.Size[3] { size, new System.Drawing.Size(size.Width / 2, size.Height / 2), new System.Drawing.Size(size.Width / 2, size.Height / 2) };
					plane.Add(new Texture(internalformat, pixelformat, pixeluploadfmt, _size[0], 0));
					plane.Add(new Texture(internalformat, pixelformat, pixeluploadfmt, _size[1], 1));
					plane.Add(new Texture(internalformat, pixelformat, pixeluploadfmt, _size[2], 2));
					Size = size;
					pool = new PboPool(_size, wwgl.BufferUsageHint.DynamicDraw, 3, 1);
				}
			}
			public abstract class iTexture
			{
				public wwgl.PixelInternalFormat internal_format;
				public wwgl.PixelFormat pixel_internalformat;
				public wwgl.PixelFormat pixel_upload_format;
				public wwgl.PixelType pixel_transfer_type;
				public int slot;
				public System.Drawing.Size size;
				public wwgl.PixelType Compute_Type()
				{
					var frb = wwgl.PixelType.UnsignedByte;
					//var frb = force_type == 1 ? (bbit2 > 1 ? wwgl.PixelType.UnsignedShort : wwgl.PixelType.UnsignedByte) : (force_type == 2 ? wwgl.PixelType.UnsignedShort : (force_type == 8 ? wwgl.PixelType.Float : wwgl.PixelType.UnsignedByte));

					return frb;
				}
			}
			public unsafe class Texture : iTexture, IDisposable
			{
				public Texture(wwgl.PixelInternalFormat internalformat, wwgl.PixelFormat pixelformat, wwgl.PixelFormat pixeluploadfmt, System.Drawing.Size size1, int texture_slot)
				{
					slot = texture_slot;
					Id = gl.GenTexture();
					gl.PixelStore(wwgl.PixelStoreParameter.UnpackAlignment, unpackalignment);
					gl.PixelStore(wwgl.PixelStoreParameter.PackAlignment, packalignment);
					internal_format = internalformat;
					pixel_internalformat = pixelformat;
					pixel_upload_format = pixeluploadfmt;
					pixel_transfer_type = Compute_Type();
					size = size1;
					CreateTexture();
					
				}
				public void CreateTexture()
				{
					wwgl.GL.ActiveTexture(wwgl.TextureUnit.Texture0 + slot);
					wwgl.GL.BindTexture(wwgl.TextureTarget.Texture2D, Id);

					wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureBaseLevel, 0);
					wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureMaxLevel, 0);

					wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureMinFilter, (int)(wwgl.TextureMinFilter.Linear));
					wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureMagFilter, (int)(wwgl.TextureMagFilter.Linear));
					wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureWrapS, (int)(wwgl.TextureWrapMode.ClampToEdge));
					wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureWrapT, (int)(wwgl.TextureWrapMode.ClampToEdge));

					wwgl.GL.TexImage2D(wwgl.TextureTarget.Texture2D, 0, internal_format, size.Width, size.Height, 0, pixel_internalformat, pixel_transfer_type, IntPtr.Zero);

					wwgl.GL.BindTexture(wwgl.TextureTarget.Texture2D, Id);

					if (glerr.glGetError() != wwgl.ErrorCode.NoError)
					{
						_ = MessageBox.Show(glerr.err.ToString());
					}
				}
				public int Id;
				public void Dispose()
				{
					gl.DeleteTexture(Id);
				}
			}
		}
	}

}
