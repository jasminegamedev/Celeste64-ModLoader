using System.Diagnostics;
namespace Celeste64;

/// <summary>
/// Fuji Custom
/// This class improves logging functionality by better distinguishing between info messages, warnings, and logs
/// It also provides functions for writing logs to the log file, and opening the log file
/// This wraps fosters Log events by subscribing to the OnInfo, OnWarn, and OnError events
/// </summary>
public static class LogHelper
{
	public enum LogLevel
	{
		Info,
		Warn,
		Error,
		Verbose
	}

	private const string LogFileName = "Log.txt";

	private static string? _logPath;
	/// <summary>
	/// Provides the current Log.txt path, or null if none is available yet.
	/// </summary>
	public static string? LogPath
	{
		get
		{
			if (_logPath == null)
			{
				if (App.UserPath == string.Empty)
					return null;
				_logPath = Path.Combine(App.UserPath, LogFileName);
			}

			return _logPath;
		}
	}

	// This should never be accessed directly
	private static readonly LoggerWriter Logs = new(true);

	/*
	 A simple Assembly.GetCallingAssembly() is not good enough for our needs here. 
	 We need to travel up the stack to get the actual assembly name.

	 Even then, this gets the wrong assembly name sometimes :(
	 todo: figure out a 100% reliable way to get this
	*/
	/* Decommissioned due to instability */
	/* public static string? AsmName => new StackTrace().GetFrame(5)?.GetMethod()?.DeclaringType?.Assembly.GetName().Name; */

	private static ReadOnlySpan<char> GetLogLine(LogLevel lev, ReadOnlySpan<char> text)
	{
		return $"[{lev}] {text}";
	}

	public static void PushLogLine(LogLevel lev, ReadOnlySpan<char> text, ConsoleColor color = ConsoleColor.White)
	{
		ReadOnlySpan<char> outtext = GetLogLine(lev, text);
		Logs.Append(outtext);
		Console.ForegroundColor = color;
		Console.Out.WriteLine(outtext);
		Console.ResetColor();
	}

	public static void Initialize()
	{
		Log.OnInfo += Info;
		Log.OnWarn += Warn;
		Log.OnError += Error;
	}

	public static void Info(ReadOnlySpan<char> text)
	{
		PushLogLine(LogLevel.Info, text);
	}

	public static void Warn(ReadOnlySpan<char> text)
	{
		PushLogLine(LogLevel.Warn, text, ConsoleColor.Yellow);
	}

	public static void Error(ReadOnlySpan<char> text)
	{
		PushLogLine(LogLevel.Error, text, ConsoleColor.Red);
	}

	public static void Verbose(ReadOnlySpan<char> text)
	{
		if (!Settings.EnableAdditionalLogging) return;

		PushLogLine(LogLevel.Verbose, text, ConsoleColor.Cyan);
	}

	public static void Error(ReadOnlySpan<char> text, Exception ex)
	{
		Error($"{text}\n {ex}");
	}

	public static void OpenLog()
	{
		string? path = LogPath;
		if (File.Exists(path))
		{
			new Process { StartInfo = new ProcessStartInfo(path) { UseShellExecute = true } }.Start();
		}
	}

	/// <summary>
	/// TextWriter wrapper to facilitate and guarantee thread safety.
	/// </summary>
	private class LoggerWriter
	{
		private TextWriter? textWriter;

		// Why is this so complicated? Turns out we want to be able to log before we know where to do it
		// as such we initially buffer to a memory stream, and once the path is available we have to switch
		// to the file stream and flush all data.
		// This is also not asynchronous in any way since it will occur once ever, as such the cost is amortized
		private TextWriter? TextWriter
		{
			get
			{
				if (LogPath != null && textWriter == null)
				{
					StreamWriter streamWriter = new StreamWriter(LogPath, false);
					bufferStreamWriter.BaseStream.Position = 0; // Make sure the `CopyTo` reads from the beginning
					bufferStreamWriter.BaseStream.CopyTo(streamWriter.BaseStream);
					textWriter = TextWriter.Synchronized(streamWriter);
				}

				return textWriter;
			}
		}
		private readonly bool autoFlush;
		private readonly StreamWriter bufferStreamWriter;

		private TextWriter CurrentBuffer => TextWriter ?? bufferStreamWriter;

		public LoggerWriter(bool flushAutomatically)
		{
			bufferStreamWriter = new StreamWriter(new MemoryStream());
			autoFlush = flushAutomatically;
		}

		public void Append(ReadOnlySpan<char> message)
		{
			CurrentBuffer.WriteLine(message);
			if (autoFlush)
				Flush();
		}

		public async void Flush()
		{
			await Task.Run(CurrentBuffer.Flush);
		}
	}
}
