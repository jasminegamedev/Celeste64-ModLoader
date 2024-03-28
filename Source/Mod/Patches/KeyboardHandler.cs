using System.Runtime.InteropServices;

namespace Celeste64.Mod;

class KeyboardHandler
{
	public static readonly Dictionary<string, string> KeyValues = new Dictionary<string, string>
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
		{ "Space", "_" },
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
		{ "KeypadA", "A" },
		{ "KeypadB", "B" },
		{ "KeypadC", "C" },
		{ "KeypadD", "D" },
		{ "KeypadE", "E" },
		{ "KeypadF", "F" }
	};
	private Keys? previousKey;
	public static KeyboardHandler Instance = new();

	public static string GetKeyName(Keys? key)
	{
		if (KeyValues.ContainsKey(key.ToString()))
			return KeyValues[key.ToString()];
		else
			return string.Empty;
	}

	public Keys? GetLastKey()
	{
		return previousKey;
	}

	public Keys? GetPressedKey()
	{
		Keys? key = Input.Keyboard.FirstPressed();
		return key;
	}

	public void ReadKeys()
	{
		Keys? key = Input.Keyboard.FirstPressed();
		if (key == null)
			key = previousKey;
		else
			previousKey = key;
	}

}
