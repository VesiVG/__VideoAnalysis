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
            public abstract class Frame
            {
                public static int _current_tex = 0;
                public static int slot_multiplier = 0;
            }
            public unsafe class RGBA_texture : Frame, IDisposable
            {
                public Texture plane0;
                public RGBA_texture(wwgl.PixelInternalFormat internalformat, wwgl.PixelFormat pixelformat, wwgl.PixelFormat pixeluploadfmt, System.Drawing.Size size)
                {
                    plane0 = new Texture(internalformat, pixelformat, pixeluploadfmt, size);
                }
                /*
                 * Upload using Texsubimage. Works, but using PBO is better
                 */
                public void Upload(byte* bra)
                {
                    wwgl.GL.BindTexture(wwgl.TextureTarget.Texture2D, plane0.Id[plane0.pos]);
                    wwgl.GL.TexSubImage2D(wwgl.TextureTarget.Texture2D, 0, 0, 0, plane0.size.Width, plane0.size.Height, plane0.pixel_upload_format, plane0.pixel_transfer_type, (IntPtr)bra);

                }
               
                public void Dispose()
                {
                    plane0.Dispose();
                }
            }
            public abstract class iTexture
            {
                public wwgl.PixelInternalFormat internal_format;
                public wwgl.PixelFormat pixel_internalformat;
                public wwgl.PixelFormat pixel_upload_format;
                public wwgl.PixelType pixel_transfer_type;
                public int pos;
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
                public Texture(wwgl.PixelInternalFormat internalformat, wwgl.PixelFormat pixelformat, wwgl.PixelFormat pixeluploadfmt, System.Drawing.Size size1)
                {
                    Slot_Offset = 0;
                    Id = new int[Texture_slots];
                    wwgl.GL.GenTextures(Texture_slots, Id);
                    wwgl.GL.PixelStore(wwgl.PixelStoreParameter.UnpackAlignment, unpackalignment);
                    wwgl.GL.PixelStore(wwgl.PixelStoreParameter.PackAlignment, packalignment);
                    internal_format = internalformat;
                    pixel_internalformat = pixelformat;
                    pixel_upload_format = pixeluploadfmt;
                    pixel_transfer_type = Compute_Type();
                    pos = 0;
                    size = size1;
                    CreateTexture(pos);
                }
                public void CreateTexture(int pos1)
                {
                    wwgl.GL.ActiveTexture(wwgl.TextureUnit.Texture0 + Slot_Offset);
                    wwgl.GL.BindTexture(wwgl.TextureTarget.Texture2D, Id[pos1]);

                    wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureBaseLevel, 0);
                    wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureMaxLevel, 0);


                    wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureMinFilter, (int)(wwgl.TextureMinFilter.Linear));

                    wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureMagFilter, (int)(wwgl.TextureMagFilter.Linear));

                    wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureWrapS, (int)(wwgl.TextureWrapMode.ClampToEdge));
                    wwgl.GL.TexParameter(wwgl.TextureTarget.Texture2D, wwgl.TextureParameterName.TextureWrapT, (int)(wwgl.TextureWrapMode.ClampToEdge));

                    wwgl.GL.TexImage2D(wwgl.TextureTarget.Texture2D, 0, internal_format, size.Width, size.Height, 0, pixel_internalformat, pixel_transfer_type, IntPtr.Zero);

                    wwgl.GL.BindTexture(wwgl.TextureTarget.Texture2D, Id[pos1]);
                    //_ = getmsg();
                    if (glerr.glGetError() != wwgl.ErrorCode.NoError)
                    {
                        _ = MessageBox.Show(glerr.err.ToString());
                    }
                }
                public int[] Id;
                public int Slot_Offset;
                public void Dispose()
                {
                    gl.DeleteTextures(Texture_slots, Id);
                }
            }
        }
    }

}
