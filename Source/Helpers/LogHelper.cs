using System.Diagnostics;
using System.Reflection;
using System.Text;
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

	public static readonly StringBuilder Logs = new StringBuilder();
	/*
	 A simple Assembly.GetCallingAssembly() is not good enough for our needs here. 
	 We need to travel up the stack to get the actual assembly name.

	 Even then, this gets the wrong assembly name sometimes :(
	 todo: figure out a 100% reliable way to get this
	*/
	/* Decommissioned due to instability */
	/* public static string? AsmName => new StackTrace().GetFrame(5)?.GetMethod()?.DeclaringType?.Assembly.GetName().Name; */

	public static string GetLogLine(LogLevel lev, ReadOnlySpan<char> text)
	{
		return $"[{lev}] {text}";
	}

	public static void PushLogLine(LogLevel lev, ReadOnlySpan<char> text, ConsoleColor color = ConsoleColor.White)
	{
		string outtext = GetLogLine(lev, text);
		Append(outtext);
		Console.ForegroundColor = color;
		Console.Out.WriteLine(outtext);
		Console.ResetColor();
		WriteToLog();
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

	public static void WriteToLog()
	{
		if (!Settings.WriteLog)
		{
			return;
		}

		// construct a log message
		const string LogFileName = "Log.txt";
		StringBuilder log = new();
		lock (Logs)
		{
			log.AppendLine(Logs.ToString());

			// write to file
			string path = LogFileName;
			{
				if (App.Running)
				{
					try
					{
						path = Path.Join(App.UserPath, LogFileName);
					}
					catch
					{
						path = LogFileName;
					}
				}

				File.WriteAllText(path, log.ToString());
			}
		}
	}

	public static void OpenLog()
	{
		const string LogFileName = "Log.txt";
		string path = "";
		if (App.Running)
		{
			try
			{
				path = Path.Join(App.UserPath, LogFileName);
			}
			catch
			{
				path = LogFileName;
			}
		}
		if (File.Exists(path))
		{
			new Process { StartInfo = new ProcessStartInfo(path) { UseShellExecute = true } }.Start();
		}
	}

	public static void Append(ReadOnlySpan<char> message)
	{
		lock (Logs)
		{
			Logs.Append(message);
			Logs.Append('\n');
		}
	}
}
