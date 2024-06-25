using System.Text.RegularExpressions;

namespace Celeste64;

class Program
{
	// Moved to Celeste64.Launcher project
}

public class CommandParser
{
	/*
		Simple (perhaps to a fault) command arg matcher
		Each match represents an arg
		Group #2 is the arg name
		Group #4 is the arg value (if it exists)
	*/
	public readonly string Pattern = """-{1,2}([a-z0-9-]+)(="?([^"-]+)"?)?\b""";
	public readonly Regex Exp;
	public readonly List<string> Flags = [];
	public readonly Dictionary<string, string> Args = new();


	public CommandParser(string[] args)
	{
		Exp = new(Pattern);

		string argsStr = String.Join(" ", args);

		foreach (Match match in Exp.Matches(argsStr))
		{
			if (match.Groups[2].Value == string.Empty)
			{
				Flags.Add(match.Groups[1].Value);
			}
			else
			{
				Args.Add(match.Groups[1].Value, match.Groups[3].Value);
			}
		}
	}

	/// <summary>
	/// Check if the command line args contain the specified arg name
	/// </summary>
	/// <param name="name">The name to search for</param>
	/// <returns>Whether the element exists</returns>
	public bool Has(string name)
	{
		if (Flags.Contains(name)) return true;
		if (Args.ContainsKey(name)) return true;

		return false;
	}

	/// <summary>
	/// Get the value of a command line arg
	/// Args with values take priority over flags without values (flags).
	/// </summary>
	/// <param name="name">The arg name to search for</param>
	/// <returns>
	/// An empty string if a flag was found with the name
	/// The value of the arg if one was found
	/// Null if neither a flag nor a value could be found
	/// </returns>
	public string? Get(string name)
	{
		if (Args.ContainsKey(name)) return Args[name];
		if (Flags.Contains(name)) return String.Empty;

		return null;
	}
}
