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
	public static readonly StringBuilder Logs = new StringBuilder();
	/*
	 A simple Assembly.GetCallingAssembly() is not good enough for our needs here. 
	 We need to travel up the stack to get the actual assembly name.

	 Even then, this gets the wrong assembly name sometimes :(
	 todo: figure out a 100% reliable way to get this
	*/
	public static string? AsmName => new StackTrace().GetFrame(4)?.GetMethod()?.DeclaringType?.Assembly.GetName().Name;

	public static void Initialize()
	{
		Log.OnInfo += Info;
		Log.OnWarn += Warn;
		Log.OnError += Error;
	}

	public static void Info(ReadOnlySpan<char> text)
	{
		string outtext = $"[Info] [{AsmName}] {text}";
		Append(outtext);
		Console.Out.WriteLine(outtext);
		WriteToLog();
	}

	public static void Warn(ReadOnlySpan<char> text)
	{
		string outtext = $"[Warning] [{AsmName}] {text}";
		Append(outtext);
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.Out.WriteLine(outtext);
		Console.ResetColor();
		WriteToLog();
	}

	public static void Error(ReadOnlySpan<char> text)
	{
		string outtext = $"[Error] [{AsmName}] {text}";
		Append(outtext);
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Out.WriteLine(outtext);
		Console.ResetColor();
		WriteToLog();
	}

	public static void Verbose(ReadOnlySpan<char> text)
	{
		if (!Settings.EnableAdditionalLogging) return;

		string outtext = $"[Info] [{AsmName}] {text}";
		Append(outtext);
		Console.Out.WriteLine(outtext);
		WriteToLog();
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
