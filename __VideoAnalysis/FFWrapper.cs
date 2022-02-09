using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace __VideoAnalysis
{
	public unsafe partial class FFWrapper
	{


		public class Decoder : IDisposable
		{
			public Decoder()
			{
				IsDone = false;
				FFWrapper.debugMode = false;
				logger = new Logger();
				logger.loglevs = MediaLogMessageType.Debug;
				ffmpeg_init();
				frameidx = 0;

				ffmpeg.av_log_set_flags(ffmpeg.AV_LOG_SKIP_REPEATED);





				need_stop = false;

			}
			public static bool IsDone = false;
			private av_log_set_callback_callback FFmpegLogCallback;
			private AVIOInterruptCB_callback StreamReadInterruptCallback;
			private AVCodecContext_get_format Format_D3D11;
			public AVCodecContext* Codecctx;
			public AVFormatContext* Fmtctx;
			public AVCodec* Codec;
			public AVFrame* cpuframe;
			public AVPacket* packet;
			public int _streamindex;
			private long frameidx = 0;
			public static int alignment = 0;
			public MediaLogMessageType log_level
			{
				get
				{
					return (MediaLogMessageType)ffmpeg.av_log_get_level();
				}
				set
				{
					ffmpeg.av_log_set_level(_FFmpegLogLevels[value]);
				}
			}


			public static bool need_stop;
			public int decoder_reorder_pts = -1;
			public bool Open(string filename)
			{
				decoder_reorder_pts = -1;
				log_level = logger.loglevs;
				FFmpegLogCallback = new av_log_set_callback_callback(OnFFmpegMessageLogged);
				ffmpeg.av_log_set_callback(FFmpegLogCallback);


				Fmtctx = ffmpeg.avformat_alloc_context();
				StreamReadInterruptCallback = new AVIOInterruptCB_callback(OnStreamReadInterrupt);
				Fmtctx->interrupt_callback.callback = StreamReadInterruptCallback;
				Fmtctx->interrupt_callback.opaque = Fmtctx;
				int err_code;
				fixed (AVFormatContext** Fmtctx1 = &(Fmtctx))
					err_code = ffmpeg.avformat_open_input(Fmtctx1, filename, null, null);
				if (err_code != 0)
				{
					Dispose();
					return false;
				}

				err_code = ffmpeg.avformat_find_stream_info(Fmtctx, null);
				if (err_code != 0)
				{
					Dispose();
					return false;
				}

				Codec = null;
				fixed (AVCodec** t = &Codec)
					_streamindex = ffmpeg.av_find_best_stream(this.Fmtctx, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, t, 0);
				if (_streamindex < 0)
				{
					for (int i01 = 0; i01 < Fmtctx->nb_streams; i01++)
					{
						//Mark only video stream
						//if (i01==_streamindex)
						if (Fmtctx->streams[i01]->codecpar->codec_type != AVMediaType.AVMEDIA_TYPE_VIDEO)
						{
							if (_streamindex < 0) _streamindex = i01;
							Fmtctx->streams[i01]->discard = AVDiscard.AVDISCARD_ALL;
						}
					}
				}
				if (_streamindex < 0)
				{
					Dispose();
					return false;
				}
				alignment = (int)ffmpeg.av_cpu_max_align();
				Codecctx = ffmpeg.avcodec_alloc_context3(Codec);
				ffmpeg.avcodec_parameters_to_context(Codecctx, Fmtctx->streams[_streamindex]->codecpar);
				if (Codec->id == AVCodecID.AV_CODEC_ID_H264 &&/*Stream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_H264 &&*/ Fmtctx->streams[_streamindex]->codecpar->extradata_size > 0 && Codecctx->extradata == null)
				//if (/*Stream->codecpar->codec_id == AVCodecID.AV_CODEC_ID_H264 &&*/ (Stream->codecpar->extradata != null && Codecctx->extradata == null /*|| Stream->codecpar->extradata_size > 0*/))
				{
					//shouldn't happen at all, keeping it as to do
					if (Codecctx->extradata != null)
					{
						ffmpeg.av_free(Codecctx->extradata);
						Codecctx->extradata = null;
						Codecctx->extradata_size = 0;
					}

					//Codecctx->extradata = (byte*)Marshal.AllocHGlobal(Stream->codec->extradata_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE);
					Codecctx->extradata = (byte*)ffmpeg.av_mallocz((ulong)(Fmtctx->streams[_streamindex]->codecpar->extradata_size + ffmpeg.AV_INPUT_BUFFER_PADDING_SIZE));
					if (Codecctx->extradata == null)
					{
						Dispose();
						return false;

					}

					System.Buffer.MemoryCopy((void*)Fmtctx->streams[_streamindex]->codecpar->extradata, (void*)Codecctx->extradata, (long)(Fmtctx->streams[_streamindex]->codecpar->extradata_size), (long)(Fmtctx->streams[_streamindex]->codecpar->extradata_size));
					//_ = Tool.Memcpy((IntPtr)Codecctx->extradata, (long)(Stream->codecpar->extradata_size), (IntPtr)Stream->codecpar->extradata,	(long)(Stream->codecpar->extradata_size));

					Codecctx->extradata_size = Fmtctx->streams[_streamindex]->codecpar->extradata_size;
				}
				Codecctx->pkt_timebase = Fmtctx->streams[_streamindex]->time_base;
				Codecctx->time_base = new AVRational() { den = ffmpeg.AV_TIME_BASE, num = 1001 };
				err_code = ffmpeg.avcodec_open2(Codecctx, Codec, null);
				if (err_code != 0)
				{
					Dispose();
					return false;
				}


				return true;
			}
			private long prev_pts;

			public void FactoryLoop(Pool pool)
			{

				int ferr = 0;

				var start_time = ffmpeg.av_gettime();
				prev_pts = start_time;
				while (ferr >= 0 && need_stop == false)
				{
					packet = ffmpeg.av_packet_alloc();
					ferr = ffmpeg.av_read_frame(Fmtctx, packet);
					if (ferr < 0)
					{
						break;
					}
					else if (packet->stream_index == _streamindex)
					{
						var a010 = ffmpeg.AV_TIME_BASE;
						var b010 = (int)((1) * 1000);
						//if (packet0->pts == ffmpeg.AV_NOPTS_VALUE)
						{
							packet->pts = packet->dts;
							packet->pts = ffmpeg.av_rescale_q_rnd(packet->pts, Fmtctx->streams[_streamindex]->time_base, new AVRational() { den = a010, num = b010 }, (AVRounding)(AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX));
							packet->dts = ffmpeg.av_rescale_q_rnd(packet->dts, Fmtctx->streams[_streamindex]->time_base, new AVRational() { den = a010, num = b010 }, (AVRounding)(AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX));
							packet->duration = ffmpeg.av_rescale_q(packet->duration, Fmtctx->streams[_streamindex]->time_base, new AVRational() { den = a010, num = b010 });
							packet->pos = -1;
						}

						////perhaps not
						//if (packet->pts == ffmpeg.AV_NOPTS_VALUE)
						//{
						//	//Write PTS
						//	AVRational time_base1 = Fmtctx->streams[_streamindex]->time_base;
						//	//Duration between 2 frames (us)
						//	var calc_duration = (double)ffmpeg.AV_TIME_BASE / ffmpeg.av_q2d(Fmtctx->streams[_streamindex]->r_frame_rate);
						//	//Parameters
						//	packet->pts = (long)((double)(frameidx * calc_duration) / (double)(ffmpeg.av_q2d(time_base1) * ffmpeg.AV_TIME_BASE)) - Fmtctx->streams[_streamindex]->start_time;
						//	packet->dts = packet->pts;
						//	packet->duration = (long)((double)calc_duration / (double)(ffmpeg.av_q2d(time_base1) * ffmpeg.AV_TIME_BASE));
						//}
						ferr = 0;
						ferr = ffmpeg.avcodec_send_packet(Codecctx, packet);
						if (ferr < 0)
						{
							ferr = 1;
							//some files seem to decode with errors missing picture in access unit with size 1
							//but they play, ignoring this and not breaking on this error 
							//break; 
						}
						if (ferr >= 0)
						{
							while (true)
							{
								cpuframe = ffmpeg.av_frame_alloc();
								ferr = ffmpeg.avcodec_receive_frame(Codecctx, cpuframe);
								if (ferr == ffmpeg.AVERROR(ffmpeg.EAGAIN))
								{

									ferr = 0;
									break;
								}
								else if (ferr == ffmpeg.AVERROR(ffmpeg.AVERROR_EOF))
								{
									break;
								}
								else if (ferr < 0)
								{
									break;
								}
								if (decoder_reorder_pts == -1)
								{
									cpuframe->pts = cpuframe->best_effort_timestamp == ffmpeg.AV_NOPTS_VALUE ? cpuframe->pts : cpuframe->best_effort_timestamp;
								}
								else if (decoder_reorder_pts == 0)
								{
									cpuframe->pts = cpuframe->pkt_dts == ffmpeg.AV_NOPTS_VALUE ? cpuframe->pts : cpuframe->pkt_dts;
								}
								var pta = cpuframe->best_effort_timestamp;
								frameidx++;
								//pool.Add(cpuframe);
								pool.Add(new MediaFrame(cpuframe,frameidx));

								ffmpeg.av_frame_unref(cpuframe);
								if (pta != ffmpeg.AV_NOPTS_VALUE)
								{
									var pts_time = ffmpeg.av_rescale_q_rnd(pta, new AVRational() { den = ffmpeg.AV_TIME_BASE, num = 1001 }, new AVRational() { den = ffmpeg.AV_TIME_BASE, num = (int)(1 * 1000) + 1 }, (AVRounding)(AVRounding.AV_ROUND_NEAR_INF | AVRounding.AV_ROUND_PASS_MINMAX)); 
									if (pts_time > prev_pts)
									{
										ffmpeg.av_usleep((uint)(pts_time - prev_pts) * 1000);
									}
									prev_pts = pts_time;
									////https://www.fatalerrors.org/a/ffmpeg-time-base-time-stamp-pts-dts-delay-control-delay.html
									//AVRational time_base = Fmtctx->streams[_streamindex]->time_base;
									//AVRational time_base_q = new AVRational() { den = ffmpeg.AV_TIME_BASE, num = 1001 };
									//var pts_time = ffmpeg.av_rescale_q(pta, time_base, time_base_q) * 1000;
									//if (pts_time > prev_pts)
									//{
									//	ffmpeg.av_usleep((uint)(pts_time - prev_pts));
									//}
									//prev_pts = pts_time;
									////var now_time = ffmpeg.av_gettime() - (start_time);
									////if (pts_time > now_time)
									////    ffmpeg.av_usleep((uint)(pts_time - now_time));

								}
								//ffmpeg.av_usleep(30000);
							}
						}
						else
							ferr = 0;
						ffmpeg.av_packet_unref(packet);
						ffmpeg.av_frame_unref(cpuframe);

					}
					ffmpeg.av_packet_unref(packet);
					ffmpeg.av_frame_unref(cpuframe);
				}
				ffmpeg.av_packet_unref(packet);
				if (cpuframe != null)
					ffmpeg.av_frame_unref(cpuframe);
			}
			public void Dispose()
			{
				fixed (AVFrame** f = &cpuframe)
					ffmpeg.av_frame_free(f);
				fixed (AVPacket** p = &packet)
					ffmpeg.av_packet_free(p);

				if (!(Codecctx == null))
				{
					fixed (AVCodecContext** Cmtctx_0 = &Codecctx)
						ffmpeg.avcodec_free_context(Cmtctx_0);

					//if (Hwdectx != null)
					//{
					//	fixed (AVBufferRef** av1 = &(Hwdectx))
					//	{
					//		ffmpeg.av_buffer_unref(av1);

					//	}
					//}

				}
				if (Codec != null)
				{
					Codec = null;
				}
				if ((Fmtctx != null))
				{
					FFmpegLogCallback = null;
					ffmpeg.av_log_set_callback(null);
					Fmtctx->interrupt_callback.callback = null;
					Fmtctx->interrupt_callback.opaque = null;

					if (Fmtctx->pb != null)
						ffmpeg.avio_flush(Fmtctx->pb);

					fixed (AVFormatContext** fmmp = &(Fmtctx))
						ffmpeg.avformat_close_input(fmmp);






					Format_D3D11 = null;
					logger?.log_m(1, 1, "Input close success!" + ".\n");

				}
				else
				{
					Format_D3D11 = null;

					logger?.log_m(1, 1, "Input close: AVFormatContext is null. Success!" + ".\n");

				}
				ffmpeg_deinit();
				logger.Dispose();
				FFmpegLogBuffer.Clear();
				_ign_mess.Clear();
				logger?.log_m(1, 1, $"Frames procesed: {frameidx}.\n");
				IsDone = true;
				//throw new NotImplementedException();
			}
			private static unsafe void OnFFmpegMessageLogged(void* p0, int level, string format, byte* vl)
			{
				const int lineSize = 1024;
				string line = "";
				if (level > ffmpeg.av_log_get_level()) return;
				lock (FFmpegLogBufferSyncLock)
				{

					if (FFmpegLogBuffer == null)
					{
						FFmpegLogBuffer = new List<string>(1024) { };
					}
					var lineBuffer = stackalloc byte[lineSize];
					Tool.Zeromem((IntPtr)lineBuffer, lineSize);
					var printPrefix = 1;
					ffmpeg.av_log_format_line(p0, level, format, vl, (byte*)lineBuffer, lineSize, &printPrefix);

					if (lineBuffer != null && (*lineBuffer != 0))
					{
						line = Tool.PtrToStringUTF32((byte*)lineBuffer);
						FFmpegLogBuffer.Add(line);

						var messageType = MediaLogMessageType.Debug;
						if (FFmpegLogLevels.ContainsKey(level))
							messageType = FFmpegLogLevels[level];


						if (!line.EndsWith("\n", StringComparison.Ordinal))
						{
							return;
						}
						line = string.Join(string.Empty, FFmpegLogBuffer);
						line = line.TrimEnd();
						FFmpegLogBuffer.Clear();
						logger?.log_m(-1, 0, line);
					}

					line = string.Empty;
				}


			}
			const int ErrorResult = 1;
			const int OkResult = 0;
			private unsafe int OnStreamReadInterrupt(void* opaque)
			{



				if (need_stop == true)
				{
					//passed_input = true;

					logger?.log_m(1, 1, $"{nameof(OnStreamReadInterrupt)} was requested an immediate read exit." + ".\n");
					//closerequest1 = 1;
					return ErrorResult;
				}
				return OkResult;

			}

		}
		public static void ffmpeg_deinit()
		{

			_ = ffmpeg.avformat_network_deinit();
		}
		public static Logger logger;
		public static void ffmpeg_init()
		{
			var ffmpegPath = Path.Combine(Environment.CurrentDirectory, Environment.Is64BitProcess ? "ffmpeg_x64" : "ffmpeg_x86");
			//ffmpeg.RootPath = ffmpegPath;

			var ffr = ffmpeg.LibraryDependenciesMap.Keys.Reverse().ToArray();
			foreach (string lib in ffr)
			{
				var dependencies = ffmpeg.LibraryDependenciesMap[lib];
				dependencies.Where(n => !ffmpeg.LoadedLibraries.ContainsKey(n) && !n.Equals(lib))
						.ToList()
						.ForEach(n => FFmpeg.AutoGen.Native.LibraryLoader.LoadNativeLibrary(ffmpegPath, n, ffmpeg.LibraryVersionMap[n]));
				var ptr = FFmpeg.AutoGen.Native.LibraryLoader.LoadNativeLibrary(ffmpegPath, lib, ffmpeg.LibraryVersionMap[lib]);
				logger?.log_m(0, 1, $"Loading library {lib} .. { (ptr == IntPtr.Zero ? "failed." : "success.")}\n");
				if (ptr != IntPtr.Zero) ffmpeg.LoadedLibraries.Add(lib, ptr);
			}
			//#pragma warning disable CS0618 // Type or member is obsolete
			ffmpeg.avdevice_register_all();
			ffmpeg.avformat_network_init();
			//#pragma warning restore CS0618 // Type or member is obsolete
		}


		private static bool debugMode = false;
		private static readonly object mlock = new Object();
		public static readonly List<string> _ign_mess = new List<string>() { "Decoding SEI", "tcp_read_packet", "FU type ", "missing picture in access unit with size ", "Decoding SPS", "Decoding PPS", "Decoding VPS", "Decoding VUI", "thread_release_buffer called on pic ", " id=0 len=", "no frame!", "tcp_read_packet:", "rfps", "frame with POC", "Loaded sym: ", "ret=1 c=", ", nuh_layer_id: ", "profile bitstream", "(Coded slice of a non-IDR picture), nal_ref_idc:", "stts: ", "nal_unit_type:", "Skipped PREFIX SEI", "unknown SEI type " };
		public static unsafe int CC(string ss, string sf)
		{
			var cf = sf.Length;
			var cs = ss.Length;

			if (cf > cs)
				return 0;
			for (int vi = 0; vi <= (cs - cf); vi++)
			{

				for (int vi2 = 0; vi2 < cf; vi2++)
				{

					if (ss[vi + vi2] != sf[vi2])
					{
						goto anul;
					}

				}
				return 1;
			anul:;
			}
			return 0;

		}
		public static bool parse_mes(string sr)
		{

			for (int xs = 0; xs < _ign_mess.Count; xs++)
			{
				if (CC(sr, _ign_mess[xs]) > 0)
				{
					goto check;

				}
			}

			if (CC(sr, "ct_type:") > 0 && CC(sr, "pic_struct:") > 0)
				return false;
			if (CC(sr, "id=") > 0 && CC(sr, "len=") > 0)
				return false;
			if (CC(sr, "count=") > 0 && CC(sr, "duration=") > 0)
				return false;
			return true;
		check:
			return false;

		}


		public enum MediaLogMessageType
		{
			Supress = 0,
			Quiet = 1,
			Fatal = 2,
			Error = 3,
			Warning = 4,
			Info = 5,
			Verbose = 6,
			Debug = 7,
			Trace = 8,
			Max = 9
		}
		public MediaLogMessageType log_level
		{
			get
			{
				return (MediaLogMessageType)ffmpeg.av_log_get_level();
			}
			set
			{
				ffmpeg.av_log_set_level(_FFmpegLogLevels[value]);
			}
		}

		private static readonly IReadOnlyDictionary<int, MediaLogMessageType> FFmpegLogLevels =
			 new Dictionary<int, MediaLogMessageType>
			 {
					 { ffmpeg.AV_LOG_QUIET, MediaLogMessageType.Supress },
					 { ffmpeg.AV_LOG_PANIC, MediaLogMessageType.Quiet },
					 { ffmpeg.AV_LOG_FATAL, MediaLogMessageType.Fatal },
					 { ffmpeg.AV_LOG_ERROR, MediaLogMessageType.Error },
					 { ffmpeg.AV_LOG_WARNING, MediaLogMessageType.Warning },
					 { ffmpeg.AV_LOG_INFO, MediaLogMessageType.Info },
					 { ffmpeg.AV_LOG_VERBOSE, MediaLogMessageType.Verbose },
					 { ffmpeg.AV_LOG_DEBUG, MediaLogMessageType.Debug },
					 { ffmpeg.AV_LOG_TRACE, MediaLogMessageType.Trace },
					 { ffmpeg.AV_LOG_MAX_OFFSET, MediaLogMessageType.Max }
			 };
		private static readonly IReadOnlyDictionary<MediaLogMessageType, int> _FFmpegLogLevels =
			 new Dictionary<MediaLogMessageType, int>
			 {
					 { MediaLogMessageType.Supress, ffmpeg.AV_LOG_QUIET  },
					 {MediaLogMessageType.Quiet, ffmpeg.AV_LOG_PANIC  },
					 { MediaLogMessageType.Fatal, ffmpeg.AV_LOG_FATAL  },
					 { MediaLogMessageType.Error, ffmpeg.AV_LOG_ERROR  },
					 { MediaLogMessageType.Warning, ffmpeg.AV_LOG_WARNING  },
					 { MediaLogMessageType.Info, ffmpeg.AV_LOG_INFO  },
					 { MediaLogMessageType.Verbose, ffmpeg.AV_LOG_VERBOSE  },
					 { MediaLogMessageType.Debug, ffmpeg.AV_LOG_DEBUG  },
					 { MediaLogMessageType.Trace, ffmpeg.AV_LOG_TRACE  },
					 { MediaLogMessageType.Max, ffmpeg.AV_LOG_MAX_OFFSET  }
			 };
		private static readonly object FFmpegLogBufferSyncLock = new object();
		private static List<string> FFmpegLogBuffer = new List<string>(1024);
	}


}
