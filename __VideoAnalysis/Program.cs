using FFmpeg.AutoGen;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace __VideoAnalysis
{
	partial class Program
	{

		[STAThread]
		static void Main(string[] args)
		{

			bgr_work_done = false;
			bgr_work_need_stop = false;
			mode = RunMode.Opengl;
			opn = new OpenFileDialog();

			opn.Multiselect = false;
			if (mode == RunMode.Opengl)
			{
				OpenTK.ToolkitOptions tko = new OpenTK.ToolkitOptions() { Backend = OpenTK.PlatformBackend.PreferNative, EnableHighResolution = true };
				using (OpenTK.Toolkit.Init(tko))
				{
					RunMe();
				}
			}
			else if (mode == RunMode.Winforms)
			{
				RunMe();
			}
			picture?.Dispose();
			pic?.Dispose();
			frm?.Dispose();
			FFWrapper.Decoder.need_stop = true;
			while (FFWrapper.Decoder.IsDone == false)
			{
				Application.DoEvents();
			}
			pool?.Dispose();
		}
		public static void RunMe()
		{
			var c = opn.ShowDialog();
			if (c == DialogResult.OK)
			{
				pool = new FFWrapper.Pool(5);
				System.Threading.Tasks.Task.Factory.StartNew((Action)(() =>
				{
					var a = new FFWrapper.Decoder();
					if (a.Open(opn.FileName))
					{
						a.FactoryLoop(pool);
					}
					a.Dispose();
				}), System.Threading.Tasks.TaskCreationOptions.LongRunning).ConfigureAwait(false);

			}
			frm = new System.Windows.Forms.Form();
			frm.KeyPreview = true;
			frm.WindowState = FormWindowState.Maximized;
			if (mode == RunMode.Winforms)
			{
				picture = new PictureBox() { Size = new System.Drawing.Size(1280, 720), Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };
			}
			else if (mode == RunMode.Opengl)
			{
				pic = new GLW();
				pic.Size = new System.Drawing.Size(1280, 720);
			}
			opn?.Dispose();
			frm.Shown += Frm_Shown;
			frm.FormClosing += Frm_FormClosing;

			if (mode == RunMode.Winforms)
			{
				frm.Controls.Add(picture);
				picture.Dock = DockStyle.Fill;
			}
			else if (mode == RunMode.Opengl)
			{
				frm.Controls.Add(pic);
				pic.Dock = DockStyle.Fill;
			}
			Application.Run(new ApplicationContext(frm));

			bgr_work_need_stop = true;
		}
		public static PictureBox picture;
		public static GLW pic;
		public static Form frm;
		public static bool bgr_work_done;
		public static bool bgr_work_need_stop;
		public static FFWrapper.Pool pool;
		public static OpenFileDialog opn;
		public static System.Drawing.Size sourceSize;
		public static AVPixelFormat sourcePixelFormat;

		public static int align;
		public static FFWrapper.Converter_video conv;
		public static RunMode mode = RunMode.Winforms;
		public enum RunMode
		{
			Winforms,
			Opengl
		}
		//public static System.Drawing.Graphics gr; //unused
		private static void Frm_FormClosing(object sender, FormClosingEventArgs e)
		{
			e.Cancel = true;
			FFWrapper.Decoder.need_stop = true;
			bgr_work_need_stop = true;
			frm.KeyDown -= Frm_KeyDown;
			frm.FormClosing -= Frm_FormClosing;
			while (bgr_work_done == false)
			{
				Application.DoEvents();
			}
			e.Cancel = false;

		}
		//https://stackoverflow.com/questions/7535812/c-sharp-lockbits-perfomance-int-to-byte
		public unsafe static void UpdateImg(FFWrapper.MediaFrame ff, System.Drawing.Image img)
		{


			int width = ((System.Drawing.Bitmap)img).Width;
			int height = ((System.Drawing.Bitmap)img).Height;

			System.Drawing.Imaging.BitmapData bd = ((System.Drawing.Bitmap)img).LockBits(new System.Drawing.Rectangle(0, 0, width, height),
			  System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

			byte* s0 = (byte*)bd.Scan0.ToPointer();
			int stride = bd.Stride;
			/* Format24bppRgb = 3 bytes
			 * if using RGBA/pArgb, replace with 4 bytes below
			 */
			ffmpeg.av_image_copy_plane(s0, stride, ff.pFrame->data[0], ff.pFrame->linesize[0], width * 4, height);

			// byte* s1 = ff.pFrame->data[0];
			//System.Threading.Tasks.Parallel.For(0, height, (y1) =>
			//	{
			//		int posY = y1 * stride;
			//		byte* cpp = s0 + posY;
			//		byte* cpp2 = s1 + posY;
			//		for (int x = 0; x < width; x++)
			//		{
			//			// Set your pixel values here.
			//			cpp[0] = cpp2[0];
			//			cpp[1] = cpp2[1];
			//			cpp[2] = cpp2[2];
			//			cpp += 3;
			//			cpp2 += 3;
			//		}
			//	});
			((System.Drawing.Bitmap)img).UnlockBits(bd);


		}
		private static void Frm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Q)
			{
				FFWrapper.Decoder.need_stop = true;
				bgr_work_need_stop = true;
				frm.Close();
			}
		}

		private static unsafe void Frm_Shown(object sender, EventArgs e)
		{

			frm.KeyDown += Frm_KeyDown;
			if (mode == RunMode.Opengl)
				Opengl_task();
			else if (mode == RunMode.Winforms)
				WinForms_task();
		}
		private static unsafe void Process_GL()
		{
			int ca = pool.GetNext(out FFWrapper.MediaFrame cfa);
			if (ca == 0)
			{
				sourceSize = new System.Drawing.Size(cfa.pFrame->width, cfa.pFrame->height);
				sourcePixelFormat = (AVPixelFormat)cfa.pFrame->format;
				switch ((AVPixelFormat)cfa.pFrame->format)
				{
					case AVPixelFormat.AV_PIX_FMT_NV12:
					case AVPixelFormat.AV_PIX_FMT_DXVA2_VLD:
					case AVPixelFormat.AV_PIX_FMT_D3D11:
						sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_NV12;
						break;
					case AVPixelFormat.AV_PIX_FMT_YUVJ420P:
						sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
						break;
				}

				align = FFWrapper.Decoder.alignment;
				if (conv == null)
				{
					var _szz = sourceSize;
					conv = new FFWrapper.Converter_video(sourceSize, sourcePixelFormat, _szz, AVPixelFormat.AV_PIX_FMT_BGRA, cfa.pFrame->color_range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0, align);
				}
				if (conv != null)
				{
					var cpp2 = conv.Convert(cfa.pFrame);

					var cpp3 = new FFWrapper.MediaFrame(cpp2);

					ffmpeg.av_frame_unref((cpp2));
					if (cpp3 != null && cpp3.pFrame != null)
					{
						pic?.Drawz(cpp3);
					}
					cfa?.Dispose();
				}
			}
		}
		private static unsafe void Opengl_task()
		{
			System.Threading.Tasks.Task.Factory.StartNew((Action)(() =>
			{
				while (bgr_work_need_stop == false)
				{

					if (bgr_work_need_stop == false)
					{

						if (pool != null)
						{
							var tmr = Stopwatch.StartNew();
							Process_GL();
							var cva2 = tmr.ElapsedMilliseconds;
							var cva = 30;// OpenTK.DisplayDevice.Default.RefreshRate;
							var slp = (1000.0f / Math.Max(cva, 1)) - cva2;
							slp = Math.Max(1.0f, slp);
							if (slp > 0) Thread.Sleep((int)(slp));
							else Thread.Sleep(1);
						}
						else
							Thread.Sleep(1);
					}

				}
				conv?.Dispose();
				bgr_work_done = true;
			}), System.Threading.Tasks.TaskCreationOptions.LongRunning).ConfigureAwait(false);
		}
		public static unsafe void Process_Winforms()
		{
			int ca = pool.GetNext(out FFWrapper.MediaFrame cfa);
			if (ca == 0)
			{
				sourceSize = new System.Drawing.Size(cfa.pFrame->width, cfa.pFrame->height);
				sourcePixelFormat = (AVPixelFormat)cfa.pFrame->format;
				switch ((AVPixelFormat)cfa.pFrame->format)
				{
					case AVPixelFormat.AV_PIX_FMT_NV12:
					case AVPixelFormat.AV_PIX_FMT_DXVA2_VLD:
					case AVPixelFormat.AV_PIX_FMT_D3D11:
						sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_NV12;
						break;
					case AVPixelFormat.AV_PIX_FMT_YUVJ420P:
						sourcePixelFormat = AVPixelFormat.AV_PIX_FMT_YUV420P;
						break;
				}

				align = FFWrapper.Decoder.alignment;
				if (conv == null)
				{
					//gr.ScaleTransform(picture.ClientRectangle.Width / (float)sourceSize.Width, picture.ClientRectangle.Height / (float)sourceSize.Height);
					//var _szz = FFWrapper.Tool.FitInsideKeepAspectRatio(sourceSize, new System.Drawing.Size(1280, 720));
					var _szz = sourceSize;
					//if (sourceSize.Width <= _szz.Width && sourceSize.Height <= _szz.Height)
					//	_szz = sourceSize;
					conv = new FFWrapper.Converter_video(sourceSize, sourcePixelFormat, _szz, AVPixelFormat.AV_PIX_FMT_BGRA, cfa.pFrame->color_range == AVColorRange.AVCOL_RANGE_JPEG ? 1 : 0, align);
				}
				if (conv != null)
				{
					var cpp2 = conv.Convert(cfa.pFrame);

					var cpp3 = new FFWrapper.MediaFrame(cpp2);

					ffmpeg.av_frame_unref((cpp2));
					if (cpp3 != null && cpp3.pFrame != null)
					{
						picture?.Invoke(new MethodInvoker(() =>
						{
							if (picture.Image == null || (picture.Image.Width != cpp3.pFrame->width || picture.Image.Height != cpp3.pFrame->height))
							{
								if (picture.Image != null) picture.Image.Dispose();
								picture.Image = new System.Drawing.Bitmap(cpp3.pFrame->width, cpp3.pFrame->height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
							}
							UpdateImg(cpp3, picture.Image);
									/*  educational purposes:
*  drawimage/drawimageunscaled is faster than using texturebrush 
*  without optimizations, picture?.Refresh() beats them both buuuuut....
*  Invalidate(picture.ClientRectangle) is faster
*  optimizations possible: reuse texturebrush, reuse scaletransform, copyplane from AVframe to texturebrush.image, then draw it. 
*  however, directx interop is fastest still - if only SharpDX were still maintained, because one can use zero-copy on d3d11va
*  getting picture directly to texture through D3D9 shared texture / ffmpeg interop, even faster than using OpenGL. If only...
*  but I'm going to use OpenGL because of zoom and visual filters (gamma correction, edge reconstruction, Retinex SSR, and so on)
*  leaving this for reference

//gr.DrawImage(picture.Image, new System.Drawing.Point(0, 0));
//gr.DrawImageUnscaled(picture.Image, new System.Drawing.Point(0, 0));
//using (var x = new System.Drawing.TextureBrush(picture.Image))
//{
//    //x.WrapMode = System.Drawing.Drawing2D.WrapMode.Clamp;
//    x.ScaleTransform(picture.ClientRectangle.Width / (float)width, picture.ClientRectangle.Height / (float)height);
//    gr.FillRectangle(x, new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), picture.ClientRectangle.Size));
//}

*/
									//picture?.Refresh();

									//picture?.Invalidate();
									picture?.Invalidate(picture.ClientRectangle);
									//picture?.Invalidate(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), picture.ClientRectangle.Size));

								}));
					}
					cfa?.Dispose();
				}
			}
		}
		private static unsafe void WinForms_task()
		{
			System.Threading.Tasks.Task.Factory.StartNew((Action)(() =>
			{
					/*
					 * use this only when using brushes or graphics.drawimage
					 *

					if (gr == null)
					{
						 gr = picture.CreateGraphics();
					}
					*/
				while (bgr_work_need_stop == false)
				{

					if (bgr_work_need_stop == false)
					{
						
						if (pool != null)
						{
							Process_Winforms();
							var tmr = Stopwatch.StartNew();
							var cva = 30;
							var slp = (1000.0f / Math.Max(cva, 1)) - tmr.ElapsedMilliseconds;
							slp = Math.Max(1.0f, slp);
							if (slp > 0) Thread.Sleep((int)slp);
							else Thread.Sleep(1);
						}
						else
							Thread.Sleep(1);
							//ffmpeg.av_usleep((uint)(slp * 1000));
						}

				}
				conv?.Dispose();
				bgr_work_done = true;
			}), System.Threading.Tasks.TaskCreationOptions.LongRunning).ConfigureAwait(false);
		}
	}
}
