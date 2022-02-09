using System;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;

namespace __VideoAnalysis
{
	public unsafe partial class FFWrapper
	{
		public class Logger : IDisposable
		{
			public Logger()
			{

				tracer = new TextWriterTraceListener();
				Trace.Listeners.Add(tracer);
				tracer.Writer = TextWriter.Synchronized(Console.Out);
				//tracer.Writer = TextWriter.Synchronized(Console.Out);
				Clock_Provider.Init();
				mslist = new List<string>();
				IsDisposed = false;
			}
			public static bool IsDisposed;
			public MediaLogMessageType loglevs = MediaLogMessageType.Verbose;
			public List<string> mslist;
			public System.Diagnostics.TextWriterTraceListener tracer;
			public readonly object Listlock = new object();
			public void Dispose()
			{
				tracer.Writer.Flush();
				Trace.Listeners.Remove(tracer);
				tracer.Writer.Close();
				
				tracer.Writer.Dispose();
				lock (Listlock) mslist.Clear();
				Clock_Provider.Clear();
				IsDisposed = true;
				//throw new NotImplementedException();
			}
			public void log_m(int zero_flush, int internal_message, string txt)
			{
				if (Clock_Provider.initialized == false)
				{
					Clock_Provider.Init();
				}

				if (!string.IsNullOrEmpty(txt) && loglevs > MediaLogMessageType.Quiet)
				{

					if (zero_flush < 0)
						Write_messages(-1, internal_message, "[" + Clock_Provider.Now_formatted + "]: " + txt);
					else
						Write_messages(((zero_flush > 0) ? 1 : 0), internal_message, "[" + Clock_Provider.Now_formatted + "]: " + txt);
				}
			}
			public void Write_messages(int flush, int internal_message, string message)
			{

				message = message.TrimEnd();
				lock (Listlock) mslist.Add(message);

				if (internal_message > 0)
				{

					_ = tracer?.Writer.WriteLineAsync(message);

				}
				else if (parse_mes(message))
				{

					_ = tracer?.Writer.WriteLineAsync(message);
					if (flush >= 0)
						_ = tracer?.Writer.FlushAsync();
				}
				if (!debugMode)
				{
					lock (Listlock) mslist.Clear();
					message = "";
					return;
				}

				if ((mslist.Count > 5000) || (flush > 0))
				{
					if (debugMode)
					{
						var fis = Path.Combine(Environment.CurrentDirectory, "log" + Clock_Provider.Now.ToShortDateString().Replace(".","")+ Clock_Provider.Now.ToLongTimeString().Replace(":", "_") + ".log");
						if (File.Exists(fis))
							lock (Listlock) File.AppendAllLines(fis, mslist);
						else
							lock (Listlock) File.WriteAllLines(fis, mslist);

					}
					mslist.Clear();
				}

				message = "";


			}
			private static class Clock_Provider
			{
				public static bool initialized = false;
				private static DateTime _first_inc;
				private static Stopwatch stt;
				private static IFormatProvider provi;
				public static void Init()
				{
					provi = CultureInfo.InvariantCulture;
					stt = Stopwatch.StartNew();
					_first_inc = DateTime.Now;
					stt.Restart();
					initialized = true;

				}
				public static DateTime Now { get => _first_inc.Add(stt.Elapsed); }
				public static string Now_formatted { get => Cnv(_first_inc.Add(stt.Elapsed)); }
				public static void Clear()
				{
					stt.Reset();
					initialized = false;

				}
				private static string Cnv(DateTime ns)
				{
					return String.Concat(ns.Hour.ToString("00", provi), ":", ns.Minute.ToString("00", provi), ":", ns.Second.ToString("00", provi), ".", ns.Millisecond.ToString("000", provi));
				}
			}
		}
	}
}
