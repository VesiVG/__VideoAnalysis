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
            public GLW(bool vsync = false) : base()
            {
                IsCreated = false;
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
                this.UpdateStyles();
                glmode = GlMode.Advanced_Programmable;
                Vsync = vsync;
                glerr.debugMode = true;
            }
            public new void Dispose()
            {
                texture?.Dispose();
                Ctx?.Dispose();
            }
            //protected override void OnPaint(PaintEventArgs e)
            //{
            //	//ignore, it's not drawable by winforms
            //	return;
            //}

            private unsafe void UpdateBack(FFWrapper.MediaFrame _frame)
            {
                if (texture == null)
                {
                    if (FFWrapper.Tool.MatchPer(_frame.pixelFormat) == FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGRA)
                    {
                        //rgba
                        texture = new RGBA_texture(PixelInternalFormat.Rgba, PixelFormat.Bgra, PixelFormat.Bgra, new System.Drawing.Size(_frame.pFrame->width, _frame.pFrame->height));
                    }
                    else if (FFWrapper.Tool.MatchPer(_frame.pixelFormat) == FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_BGR24)
                    {
                        //rgb24
                        texture = new RGB_texture(PixelInternalFormat.Rgba, PixelFormat.Bgra, PixelFormat.Bgr, new System.Drawing.Size(_frame.pFrame->width, _frame.pFrame->height));
                    }
                    else if (FFWrapper.Tool.MatchPer(_frame.pixelFormat) == FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_YUV420P)
                    {
                        //yuv420p
                        texture = new YUV420P_texture(PixelInternalFormat.Rgba, PixelFormat.Bgra, PixelFormat.Red, new System.Drawing.Size(_frame.pFrame->width, _frame.pFrame->height));
                    }
                }
                texture?.Load(_frame);


                if (glerr.glGetError() != wwgl.ErrorCode.NoError)
                {
                    _ = MessageBox.Show(glerr.err.ToString());
                }
            }
            private void DrawMeLikeYouMeanIt(FFWrapper.MediaFrame _frame)
            {
                if (!(Ctx.IsCurrent))
                    Ctx?.MakeCurrent(_window);
                if (_frame != null)
                {
                    UpdateBack(_frame);
                    _frame.Dispose();
                    if (glmode != GlMode.Advanced_Programmable) gl.Flush();
                }
                gl.ClearColor(System.Drawing.Color.DarkGray);
                gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                if (glerr.glGetError() != wwgl.ErrorCode.NoError)
                {
                    _ = MessageBox.Show(glerr.err.ToString());
                }

                System.Drawing.Size v = FFWrapper.Tool.FitInsideKeepAspectRatio(new System.Drawing.Size(texture.Size.Width, texture.Size.Height), this.ClientRectangle.Size);

                gl.Viewport((this.ClientRectangle.Width - v.Width) / 2, (this.ClientRectangle.Height - v.Height) / 2, v.Width, v.Height);
                if (texture is YUV420P_texture)
                {
                    gl.ActiveTexture(TextureUnit.Texture2);
                    gl.BindTexture(TextureTarget.Texture2D, texture.plane[2].Id);
                    gl.ActiveTexture(TextureUnit.Texture1);
                    gl.BindTexture(TextureTarget.Texture2D, texture.plane[1].Id);

                }
                if (glmode == GlMode.Simple_FixedFunctionImmediate)
                {
                    gl.Enable(EnableCap.Texture2D);
                }
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, texture.plane[0].Id);

                if (glmode == GlMode.Simple_FixedFunctionImmediate)
                {
                    gl.MatrixMode(MatrixMode.Projection);
                    gl.LoadIdentity();
                    gl.Ortho(-1, 1, -1, 1, -1, 1);
                    gl.MatrixMode(MatrixMode.Modelview);
                    gl.LoadIdentity();
                    gl.Begin(PrimitiveType.Quads);
                    gl.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                    //texture flip fix
                    gl.TexCoord2(0.0f, 1.0f); gl.Vertex3(-1.0f, -1.0f, 1.0f);  // Bottom Left Of The Texture and Quad
                    gl.TexCoord2(1.0f, 1.0f); gl.Vertex3(1.0f, -1.0f, 1.0f);  // Bottom Right Of The Texture and Quad
                    gl.TexCoord2(1.0f, 0.0f); gl.Vertex3(1.0f, 1.0f, 1.0f);  // Top Right Of The Texture and Quad
                    gl.TexCoord2(0.0f, 0.0f); gl.Vertex3(-1.0f, 1.0f, 1.0f);  // Top Left Of The Texture and Quad
                    gl.End();
                }
                else
                {
                    _Shader?.Use();
                    _Shader?.SetInt("pixelformat", (texture is YUV420P_texture) ? 3 : 0);
                    if (glmode == GlMode.Simple_VertexArray)
                    {
                        gl.EnableClientState(ArrayCap.VertexArray);
                        gl.VertexPointer(3, VertexPointerType.Float, 0, quadVertices);
                        gl.EnableClientState(ArrayCap.TextureCoordArray);
                        gl.TexCoordPointer(2, TexCoordPointerType.Float, 0, texv);
                        gl.DrawArrays(PrimitiveType.Quads, 0, 4);
                        gl.DisableClientState(ArrayCap.VertexArray);
                        gl.DisableClientState(ArrayCap.TextureCoordArray);
                    }
                    else if (glmode == GlMode.Advanced_Programmable)
                    {
                        //gl.EnableClientState(ArrayCap.VertexArray);
                        gl.BindVertexArray(_vertexArrayObject);
                        gl.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedShort, 0);
                    }
                    _Shader?.Release();
                }

                Ctx?.SwapBuffers();
                if (glmode == GlMode.Simple_FixedFunctionImmediate)
                {
                    gl.Disable(EnableCap.Texture2D);
                }
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
                    this?.EndInvoke(this?.BeginInvoke((MethodInvoker)(() =>
                    {
                        DrawMeLikeYouMeanIt(_frame);
                    })));
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
                if (glmode == GlMode.Simple_FixedFunctionImmediate || glmode == GlMode.Simple_VertexArray)
                {
                    Ctx = new GraphicsContext(new GraphicsMode(32), _window, 2, 1, GraphicsContextFlags.Default);
                }
                else if (glmode == GlMode.Advanced_Programmable)
                {
                    Ctx = new GraphicsContext(new GraphicsMode(32), _window, 4, 4, GraphicsContextFlags.Default);
                }
                Ctx.MakeCurrent(_window);
                Ctx.LoadAll();
                FFWrapper.logger?.log_m(1, 1, $"Loading mode {mode} .\n");
                if (glmode == GlMode.Advanced_Programmable)
                {
                    _Shader = new Shader("Vertex.vert", "Fragment.frag");
                }
                else if (glmode == GlMode.Simple_VertexArray)
                {
                    _Shader = new Shader("Vertex_s.vert", "Fragment_s.frag");
                }
                if (_Shader != null)
                {
                    ConfigureShader();
                }
                var vsync_supported = OpenTK.Platform.Windows.Wgl.SupportsExtension("WGL_EXT_swap_control") && OpenTK.Platform.Windows.Wgl.SupportsFunction("wglGetSwapIntervalEXT") && OpenTK.Platform.Windows.Wgl.SupportsFunction("wglSwapIntervalEXT");
                var vsync_tear_supported = OpenTK.Platform.Windows.Wgl.SupportsExtension("WGL_EXT_swap_control_tear");
                FFWrapper.logger?.log_m(0, 1, $"VSync {(vsync_supported ? "is" : "is not")} supported. Vsync adaptive (tear control) {(vsync_tear_supported ? "is" : "is not")} supported.\n");
                //var p1 = OpenTK.Platform.Windows.Wgl.Ext.GetSwapInterval();
                //var p2 = Ctx.SwapInterval;
                //_ = OpenTK.Platform.Windows.Wgl.Ext.SwapInterval(Vsync ? 1 : 0);
                Ctx.SwapInterval = Vsync ? 1 : 0;
                //var p3 = OpenTK.Platform.Windows.Wgl.Ext.GetSwapInterval();
                if (Ctx.SwapInterval >= 0)
                    FFWrapper.logger?.log_m(0, 1, $"Requested VSync: {Vsync}. VSync {(Ctx.SwapInterval > 0 ? "is" : "is not")} enabled with SwapInterval = [{Ctx.SwapInterval}]. \n");
                else
                    FFWrapper.logger?.log_m(0, 1, $"Requested VSync: {Vsync}. VSync is driver enabled/disabled, for this to work it needs to be set to application-controlled. \n");
                IsCreated = true;
            }
            private void ConfigureShader()
            {
                _Shader.Use();
                if (glmode == GlMode.Simple_FixedFunctionImmediate || glmode == GlMode.Simple_VertexArray)
                {
                    gl.BindAttribLocation(_Shader.Prog_handle, 0, "Position");
                    gl.BindAttribLocation(_Shader.Prog_handle, 1, "TexCoord");
                }
                else if (glmode == GlMode.Advanced_Programmable)
                {
                    _vertexArrayObject = gl.GenVertexArray();
                    gl.BindVertexArray(_vertexArrayObject);

                    _vertexBufferObject = gl.GenBuffer();
                    gl.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
                    gl.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

                    _elementBufferObject = gl.GenBuffer();
                    gl.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
                    gl.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);
                    var vertexLocation = gl.GetAttribLocation(_Shader.Prog_handle, "Position");
                    gl.EnableVertexAttribArray(vertexLocation);
                    gl.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
                    var texCoordLocation = gl.GetAttribLocation(_Shader.Prog_handle, "TexCoord");
                    gl.EnableVertexAttribArray(texCoordLocation);
                    gl.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));


                }
                _Shader?.SetInt("tex_y", 0);
                _Shader?.SetInt("tex_u", 1);
                _Shader?.SetInt("tex_v", 2);
                //_Shader?.SetInt("pixelformat", 0);
                _Shader?.Use();
                _Shader?.Release();
            }
            //			private readonly float[] _vertices =
            //{
            //            // Position         Texture coordinates
            //             0.5f,  0.5f, 0.0f, 1.0f, 1.0f, // top right
            //             0.5f, -0.5f, 0.0f, 1.0f, 0.0f, // bottom right
            //            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f, // bottom left
            //            -0.5f,  0.5f, 0.0f, 0.0f, 1.0f  // top left
            //        };
            float[] quadVertices = {   -1.0f,-1.0f, 0.0f,
                                                1.0f,-1.0f, 0.0f,
                                                1.0f, 1.0f, 0.0f,
                                               -1.0f, 1.0f, 0.0f};
            float[] texv =  {           0.0f, 1.0f,
                                                1.0f, 1.0f,
                                                1.0f, 0.0f,
                                                0.0f, 0.0f};
            private readonly float[] _vertices =
            {
            // Position         Texture coordinates
            -1.0f, -1.0f, 0.0f, 0.0f, 1.0f, // top right
             1.0f, -1.0f, 0.0f, 1.0f, 1.0f, // bottom right
             1.0f,  1.0f, 0.0f, 1.0f, 0.0f, // bottom left
            -1.0f,  1.0f, 0.0f, 0.0f, 0.0f  // top left
        };
            private readonly ushort[] _indices =
            {
            0, 1, 3,
            1, 2, 3
            };

            private int _elementBufferObject;

            private int _vertexBufferObject;

            private int _vertexArrayObject;
            private bool IsCreated = false;
            private bool Vsync = false;
            private OpenTK.Graphics.GraphicsContext Ctx;
            private OpenTK.Platform.IWindowInfo _window;
            public static int Texture_slots = 1;
            public static int packalignment = 1;
            public static int unpackalignment = 1;
            public GlMode glmode;
            public Frame texture;
            private Shader _Shader;
            public enum GlMode
            {
                Simple_FixedFunctionImmediate,
                Simple_VertexArray,
                Advanced_Programmable
            }
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
