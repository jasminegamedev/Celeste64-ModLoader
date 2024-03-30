using System.Text.RegularExpressions;

namespace Celeste64.Mod.Data;

internal sealed class SaveManager
{
	internal static SaveManager Instance = new();

	[DisallowHooks]
	internal string GetLastLoadedSave()
	{
		if (File.Exists(Path.Join(App.UserPath, "Saves", "save.metadata")))
			return File.ReadAllText(Path.Join(App.UserPath, "Saves", "save.metadata"));
		else
		{
			File.WriteAllText(Path.Join(App.UserPath, "Saves", "save.metadata"), Save.DefaultFileName);
			return Save.DefaultFileName;
		}
	}

	[DisallowHooks]
	internal void SetLastLoadedSave(string save_name)
	{
		if (File.Exists(Path.Join(App.UserPath, "Saves", "save.metadata")))
			File.WriteAllText(Path.Join(App.UserPath, "Saves", "save.metadata"), save_name);
	}

	[DisallowHooks]
	internal List<string> GetSaves()
	{
		List<string> saves = new List<string>();

		foreach (string file in Directory.GetFiles(Path.Join(App.UserPath)))
		{
			string saveFileName = Path.GetFileName(file);
			if (saveFileName.StartsWith("save") && saveFileName.EndsWith(".json"))
			{
				if (saveFileName == "save.json" && !Path.Exists(Path.Join(App.UserPath, "Saves", saveFileName)))
					File.Copy(Path.Join(App.UserPath, saveFileName), Path.Join(App.UserPath, "Saves", saveFileName));
				else if (!Path.Exists(Path.Join(App.UserPath, "Saves", saveFileName)))
					File.Move(Path.Join(App.UserPath, saveFileName), Path.Join(App.UserPath, "Saves", saveFileName));
			}
		}

		foreach (string savefile in Directory.GetFiles(Path.Join(App.UserPath, "Saves")))
		{
			var saveFileName = Path.GetFileName(savefile);
			if (saveFileName.EndsWith(".json"))
				saves.Add(saveFileName);
		}

		return saves;
	}

	[DisallowHooks]
	internal void CopySave(string filename)
	{
		if (File.Exists(Path.Join(App.UserPath, "Saves", filename)))
		{
			string new_file_name = $"{filename.Split(".json")[0]}(copy).json";
			File.Copy(Path.Join(App.UserPath, "Saves", filename), Path.Join(App.UserPath, "Saves", new_file_name));
		}
	}

	[DisallowHooks]
	internal void NewSave(string? name = null)
	{
		if (string.IsNullOrEmpty(name)) name = $"save_{GetSaveCount()}.json";
		var savePath = Path.Join(App.UserPath, "Saves", name);
		var tempPath = Path.Join(App.UserPath, "Saves", name + ".backup");

		// first save to a temporary file
		{
			using var stream = File.Create(tempPath);
			Save.Instance.Serialize(stream, new Save_V02());
			stream.Flush();
		}

		// validate that the temp path worked, and overwrite existing if it did.
		if (File.Exists(tempPath) && Save.Instance.Deserialize<Save_V02>(File.ReadAllText(tempPath)) != null)
		{
			File.Copy(tempPath, savePath, true);
		}
	}

	[DisallowHooks]
	internal void ChangeFileName(string orignalFileName, string newFileName)
	{
		string invalidCharsPattern = "[\\/:*?\"<>|{}]";

		foreach (string file in GetSaves())
		{
			if (file == orignalFileName)
			{
				if (!newFileName.EndsWith(".json"))
					newFileName += ".json";
				newFileName = Regex.Replace(newFileName, invalidCharsPattern, "_");
				if (File.Exists(Path.Join(App.UserPath, "Saves", newFileName)))
					return;
				File.Move(Path.Join(App.UserPath, "Saves", file), Path.Join(App.UserPath, "Saves", newFileName));
				if (file == Save.Instance.FileName)
					LoadSaveByFileName(newFileName);
			}
		}
	}

	[DisallowHooks]
	internal int GetSaveCount()
	{
		return GetSaves().Count;
	}

	[DisallowHooks]
	internal void DeleteSave(string save)
	{
		if (File.Exists(Path.Join(App.UserPath, "Saves", save)))
		{
			File.Delete(Path.Join(App.UserPath, "Saves", save));
		}

		if (save == Save.DefaultFileName)
		{
			NewSave(Save.DefaultFileName);
		}
	}

	[DisallowHooks]
	internal void LoadSaveByFileName(string fileName)
	{
		Save.LoadSaveByFileName(fileName);
		Instance.SetLastLoadedSave(fileName);
	}
}
