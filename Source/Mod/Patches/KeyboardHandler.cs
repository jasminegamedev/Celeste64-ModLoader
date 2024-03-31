using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Celeste64.Mod;

class KeyboardHandler
{
	public static readonly Dictionary<string, string> KeyValues = new Dictionary<string, string>
	{
		{ "A", "a" },
		{ "B", "b" },
		{ "C", "c" },
		{ "D", "d" },
		{ "E", "e" },
		{ "F", "f" },
		{ "G", "g" },
		{ "H", "h" },
		{ "I", "i" },
		{ "J", "j" },
		{ "K", "k" },
		{ "L", "l" },
		{ "M", "m" },
		{ "N", "n" },
		{ "O", "o" },
		{ "P", "p" },
		{ "Q", "q" },
		{ "R", "r" },
		{ "S", "s" },
		{ "T", "t" },
		{ "U", "u" },
		{ "V", "v" },
		{ "W", "w" },
		{ "X", "x" },
		{ "Y", "y" },
		{ "Z", "z" },
		{ "D1", "1" },
		{ "D2", "2" },
		{ "D3", "3" },
		{ "D4", "4" },
		{ "D5", "5" },
		{ "D6", "6" },
		{ "D7", "7" },
		{ "D8", "8" },
		{ "D9", "9" },
		{ "D0", "0" },
		{ "Space", " " },
		{ "Minus", "-" },
		{ "Equals", "=" },
		{ "LeftBracket", "[" },
		{ "RightBracket", "]" },
		{ "Backslash", "\\" },
		{ "Semicolon", ";" },
		{ "Apostrophe", "'" },
		{ "Tilde", "~" },
		{ "Comma", "," },
		{ "Period", "." },
		{ "Slash", "/" },
		{ "Keypad0", "0" },
		{ "Keypad00", "00" },
		{ "Keypad000", "000" },
		{ "Keypad1", "1" },
		{ "Keypad2", "2" },
		{ "Keypad3", "3" },
		{ "Keypad4", "4" },
		{ "Keypad5", "5" },
		{ "Keypad6", "6" },
		{ "Keypad7", "7" },
		{ "Keypad8", "8" },
		{ "Keypad9", "9" },
		{ "KeypadDivide", "/" },
		{ "KeypadMultiply", "*" },
		{ "KeypadMinus", "-" },
		{ "KeypadPlus", "+" },
		{ "KeypadPeroid", "." },
		{ "KeypadEquals", "=" },
		{ "KeypadComma", "," },
		{ "KeypadLeftParen", "(" },
		{ "KeypadRightParen", ")" },
		{ "KeypadLeftBrace", "{" },
		{ "KeypadRightBrace", "}" },
		{ "KeypadTab", "\t" },
		{ "KeypadA", "a" },
		{ "KeypadB", "b" },
		{ "KeypadC", "c" },
		{ "KeypadD", "d" },
		{ "KeypadE", "e" },
		{ "KeypadF", "f" }
	};
	public static readonly Dictionary<string, string> KeyValuesWithModifiers = new Dictionary<string, string>
	{
		{ "A", "A" },
		{ "B", "B" },
		{ "C", "C" },
		{ "D", "D" },
		{ "E", "E" },
		{ "F", "F" },
		{ "G", "G" },
		{ "H", "H" },
		{ "I", "I" },
		{ "J", "J" },
		{ "K", "K" },
		{ "L", "L" },
		{ "M", "M" },
		{ "N", "N" },
		{ "O", "O" },
		{ "P", "P" },
		{ "Q", "Q" },
		{ "R", "R" },
		{ "S", "S" },
		{ "T", "T" },
		{ "U", "U" },
		{ "V", "V" },
		{ "W", "W" },
		{ "X", "X" },
		{ "Y", "Y" },
		{ "Z", "Z" },
		{ "D1", "!" },
		{ "D2", "@" },
		{ "D3", "#" },
		{ "D4", "$" },
		{ "D5", "%" },
		{ "D6", "^" },
		{ "D7", "&" },
		{ "D8", "*" },
		{ "D9", "(" },
		{ "D0", ")" },
		{ "Space", " " },
		{ "Minus", "_" },
		{ "Equals", "+" },
		{ "LeftBracket", "{" },
		{ "RightBracket", "}" },
		{ "Backslash", "|" },
		{ "Semicolon", ":" },
		{ "Apostrophe", "\"" },
		{ "Tilde", "~" },
		{ "Comma", "<" },
		{ "Period", ">" },
		{ "Slash", "?" },
		{ "Keypad0", ")" },
		{ "Keypad00", ")" },
		{ "Keypad000", ")" },
		{ "Keypad1", "!" },
		{ "Keypad2", "@" },
		{ "Keypad3", "#" },
		{ "Keypad4", "$" },
		{ "Keypad5", "%" },
		{ "Keypad6", "^" },
		{ "Keypad7", "&" },
		{ "Keypad8", "*" },
		{ "Keypad9", "(" },
		{ "KeypadDivide", "/" },
		{ "KeypadMultiply", "*" },
		{ "KeypadMinus", "_" },
		{ "KeypadPlus", "+" },
		{ "KeypadPeroid", ">" },
		{ "KeypadEquals", "+" },
		{ "KeypadComma", "<" },
		{ "KeypadLeftParen", "(" },
		{ "KeypadRightParen", ")" },
		{ "KeypadLeftBrace", "{" },
		{ "KeypadRightBrace", "}" },
		{ "KeypadTab", "\t" },
		{ "KeypadA", "A" },
		{ "KeypadB", "B" },
		{ "KeypadC", "C" },
		{ "KeypadD", "D" },
		{ "KeypadE", "E" },
		{ "KeypadF", "F" }
	};

	public static readonly List<string> KeyValuesList = KeyValues.Values.ToList();
	public static readonly List<string> KeyValuesWithModifiersList = KeyValuesWithModifiers.Values.ToList();

	public static KeyboardHandler Instance = new();

	public static List<string> TrimKeysList()
	{
		List<string> newKeysList = new List<string>();
		foreach (string key in KeyValuesList)
		{
			if (key.Length == 1 && !newKeysList.Contains(key) && key != " " && key != "" && key != "\t")
				newKeysList.Add(key);
		}
		return newKeysList;
	}

	public static List<string> TrimModifiedKeysList()
	{
		List<string> newKeysList = new List<string>();
		foreach (string key in KeyValuesWithModifiersList)
		{
			if (key.Length == 1 && !newKeysList.Contains(key) && key != " " && key != "" && key != "\t")
				newKeysList.Add(key);
		}
		return newKeysList;
	}

	public static string GetKeyName(Keys? key)
	{
		string? keyString = key.ToString();

		if (keyString == null) return string.Empty;

		if (Input.Keyboard.Shift)
		{
			if (KeyValuesWithModifiers.ContainsKey(keyString))
				return KeyValuesWithModifiers[keyString];
			else
				return string.Empty;
		}
		else
		{
			if (KeyValues.ContainsKey(keyString))
				return KeyValues[keyString];
			else
				return string.Empty;
		}
	}

	public Keys? GetPressedKey()
	{
		Keys? key = Input.Keyboard.FirstPressed();
		return key;
	}
}
