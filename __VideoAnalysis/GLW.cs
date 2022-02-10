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
		public partial class GLW : System.Windows.Forms.Control, IDisposable
		{
			public GLW() : base()
			{
				IsCreated = false;
				this.DoubleBuffered = true;
				this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
				this.UpdateStyles();
			}
			public new void Dispose()
			{
				texture?.Dispose();
				Ctx?.Dispose();
			}
			protected override void OnPaint(PaintEventArgs e)
			{
				//ignore
				return;
			}



			private unsafe void UpdateBack(FFWrapper.MediaFrame _frame)
			{
				if (texture == null)
				{
					texture = new RGBA_texture(PixelInternalFormat.Rgba8, PixelFormat.Bgra, PixelFormat.Bgra, new System.Drawing.Size(_frame.pFrame->width, _frame.pFrame->height));
				}
				texture.Upload(_frame.pFrame->data[0]);
				if (glerr.glGetError() != wwgl.ErrorCode.NoError)
				{
					_ = MessageBox.Show(glerr.err.ToString());
				}
			}
			private void DrawMeLikeYouMeanIt(FFWrapper.MediaFrame _frame)
			{
				if (!(Ctx.IsCurrent))
					Ctx?.MakeCurrent(_window);
				UpdateBack(_frame);
				gl.ClearColor(System.Drawing.Color.Black);
				gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
				if (glerr.glGetError() != wwgl.ErrorCode.NoError)
				{
					_ = MessageBox.Show(glerr.err.ToString());
				}

				System.Drawing.Size v = FFWrapper.Tool.FitInsideKeepAspectRatio(new System.Drawing.Size(texture.plane0.size.Width, texture.plane0.size.Height), this.ClientRectangle.Size);

				gl.Viewport((this.ClientRectangle.Width - v.Width) / 2, (this.ClientRectangle.Height - v.Height) / 2, v.Width, v.Height);

				gl.ActiveTexture(TextureUnit.Texture0);
				gl.BindTexture(TextureTarget.Texture2D, texture.plane0.Id[0]);
				_Shader.Use();
				gl.Begin(PrimitiveType.Quads);
				//texture flip fix
				gl.TexCoord2(0.0f, 1.0f); gl.Vertex3(-1.0f, -1.0f, 1.0f);  // Bottom Left Of The Texture and Quad
				gl.TexCoord2(1.0f, 1.0f); gl.Vertex3(1.0f, -1.0f, 1.0f);  // Bottom Right Of The Texture and Quad
				gl.TexCoord2(1.0f, 0.0f); gl.Vertex3(1.0f, 1.0f, 1.0f);  // Top Right Of The Texture and Quad
				gl.TexCoord2(0.0f, 0.0f); gl.Vertex3(-1.0f, 1.0f, 1.0f);  // Top Left Of The Texture and Quad
				gl.End();
				_Shader.Release();
				Ctx?.SwapBuffers();
				gl.Flush();
				if (glerr.glGetError() != wwgl.ErrorCode.NoError)
				{
					_ = MessageBox.Show(glerr.err.ToString());
				}
			}
			public void Drawz(FFWrapper.MediaFrame _frame)
			{
				if (!IsCreated)
					return;
				if (this.InvokeRequired)
				{
					this?.Invoke((MethodInvoker)(() =>
					{
						DrawMeLikeYouMeanIt(_frame);
					}));
				}
				else
				{
					DrawMeLikeYouMeanIt(_frame);
				}
			}

			protected override void OnParentChanged(EventArgs e)
			{
				base.OnParentChanged(e);
				this.HandleCreated += _HandleCreated;
			}

			private void _HandleCreated(object sender, EventArgs e)
			{
				_window = OpenTK.Platform.Utilities.CreateWindowsWindowInfo(this.Handle);
				Ctx = new GraphicsContext(new GraphicsMode(32), _window, 2, 1, GraphicsContextFlags.Default);
				Ctx.MakeCurrent(_window);
				Ctx.LoadAll();
				FFWrapper.logger?.log_m(1, 1, $"Somehow it worked.\n");
				glerr.debugMode = true;
				_Shader = new Shader("Vertex.vert", "Fragment.frag");
				ConfigureShader();
				IsCreated = true;
			}
			private void ConfigureShader()
			{
				_Shader.Use();
				gl.BindAttribLocation(_Shader.Prog_handle, 0, "Position");
				gl.BindAttribLocation(_Shader.Prog_handle, 1, "TexCoord");
				_Shader.SetInt("tex_y", 0);
				_Shader.Use();
				_Shader.Release();
			}
			private bool IsCreated = false;
			private OpenTK.Graphics.GraphicsContext Ctx;
			private OpenTK.Platform.IWindowInfo _window;
			public static int Texture_slots = 1;
			public static int packalignment = 1;
			public static int unpackalignment = 1;
			public RGBA_texture texture;
			private Shader _Shader;
		}
		public static class glerr
		{
			public static bool debugMode = false;
			public static wwgl.ErrorCode err;
			public static wwgl.ErrorCode glGetError()
			{

				if (debugMode)
				{
					err = wwgl.ErrorCode.NoError;
					err = wwgl.GL.GetError();
					return err;
				}

				err = wwgl.ErrorCode.NoError;
				return err;
			}
		}
	}

}
