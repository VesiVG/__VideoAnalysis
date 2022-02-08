using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using FFmpeg.AutoGen;
using System.Windows.Forms;

namespace __VideoAnalysis
{
	class Program
	{
		
		[STAThread]
		static void Main(string[] args)
		{
			bgr_work_done = false;
			bgr_work_need_stop = false;
			opn = new OpenFileDialog();
			//b.InitialDirectory = Environment.CurrentDirectory;
			opn.Multiselect = false;
			
			var c= opn.ShowDialog();
			if (c==DialogResult.OK)
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
			
			frm.WindowState = FormWindowState.Maximized;
			picture = new PictureBox() {Size=new System.Drawing.Size(1280,720), Dock = DockStyle.Fill };
			opn?.Dispose();
            frm.Shown += Frm_Shown;
            frm.FormClosing += Frm_FormClosing;
			frm.Controls.Add(picture);
			picture.Dock = DockStyle.Fill;
			Application.Run(new ApplicationContext(frm));
			bgr_work_need_stop = true;
			//Console.ReadKey();
			
			picture.Dispose();
			
			frm.Dispose();
			FFWrapper.Decoder.need_stop = true;
			while (FFWrapper.Decoder.IsDone == false)
			{
				Application.DoEvents();
			}
			


			pool.Dispose();
			//Console.ReadKey();
			
			//Console.WriteLine("Hello World!");
		}
		public static PictureBox picture;
		public static Form frm;
		public static bool bgr_work_done;
		public static bool bgr_work_need_stop;
		public static FFWrapper.Pool pool;
		public static OpenFileDialog opn;
		public static System.Drawing.Size sourceSize;
		public static AVPixelFormat sourcePixelFormat;
		public static int align;
		public static FFWrapper.Converter_video conv;
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
		public unsafe static void UpdateImg(FFWrapper.MediaFrame ff)
        {
			if (picture.Image == null)
            {
				picture.Image = new System.Drawing.Bitmap(1280, 720, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
			

			//using (System.Drawing.Bitmap bmp = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(@"mybitmap.bmp"))

			{
				int width = ((System.Drawing.Bitmap)picture.Image).Width;
				int height = ((System.Drawing.Bitmap)picture.Image).Height;

				System.Drawing.Imaging.BitmapData bd = ((System.Drawing.Bitmap)picture.Image).LockBits(new System.Drawing.Rectangle(0, 0, width, height),
				  System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

				byte* s0 = (byte*)bd.Scan0.ToPointer();
				byte* s1 = ff.pFrame->data[0];
				int stride = bd.Stride;

			System.Threading.Tasks.Parallel.For(0, height, (y1) =>
				{
					int posY = y1 * stride;
					byte* cpp = s0 + posY;
					byte* cpp2 = s1 + posY;
					for (int x = 0; x < width; x++)
					{
						// Set your pixel values here.
						cpp[0] = cpp2[0];
						cpp[1] = cpp2[1];
						cpp[2] = cpp2[2];
						cpp += 3;
						cpp2 += 3;
					}
				});
				((System.Drawing.Bitmap)picture.Image).UnlockBits(bd);
				picture?.Refresh();
			}
		}
        private static void Frm_KeyDown(object sender, KeyEventArgs e)
        {
           if (e.KeyCode==Keys.Q)
            {
				FFWrapper.Decoder.need_stop = true;
				bgr_work_need_stop = true;
				Application.Exit();
			}
        }

        private static unsafe void Frm_Shown(object sender, EventArgs e)
        {
			
			frm.KeyDown += Frm_KeyDown;
			System.Threading.Tasks.Task.Factory.StartNew((Action)(() =>
			{

				while (bgr_work_need_stop==false)
                {
					
					if (bgr_work_need_stop == false)
                    {
						var ca = pool.GetNext(out FFWrapper.MediaFrame cfa);
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
							if (conv==null)
                            {
								conv = new FFWrapper.Converter_video(sourceSize, sourcePixelFormat, new System.Drawing.Size(1280, 720), AVPixelFormat.AV_PIX_FMT_BGR24,align);
                            }

							var cpp2 =conv.Convert(cfa.pFrame);
							
							var cpp3 = new FFWrapper.MediaFrame(&cpp2);
							
							ffmpeg.av_frame_unref((&cpp2));
							picture?.Invoke(new MethodInvoker(() => UpdateImg(cpp3)));
							cfa.Dispose();
							//Application.DoEvents();
						}
						
						Thread.Sleep(1);
                    }

				}
				conv?.Dispose();
				bgr_work_done = true;
			}), System.Threading.Tasks.TaskCreationOptions.LongRunning).ConfigureAwait(false);
		}
    }
}
