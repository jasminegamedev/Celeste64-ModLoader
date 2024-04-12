using Celeste64.Mod;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Celeste64;

public static class Assets
{
	public static float FontSize => Game.RelativeScale * 16;
	public const string AssetFolder = "Content";

	public const string MapsFolder = "Maps";
	public const string MapsExtension = "map";

	public const string TexturesFolder = "Textures";
	public const string TexturesExtension = "png";

	public const string FacesFolder = "Faces";
	public const string FacesExtension = "png";

	public const string ModelsFolder = "Models";
	public const string ModelsExtension = "glb";

	public const string TextFolder = "Text";
	public const string TextExtension = "json";

	public const string AudioFolder = "Audio";
	public const string AudioExtension = "bank";

	public const string SoundsFolder = "Sounds";
	public const string SoundsExtension = "wav";

	public const string MusicFolder = "Music";
	public const string MusicExtension = "wav";

	public const string ShadersFolder = "Shaders";
	public const string ShadersExtension = "glsl";

	public const string FontsFolder = "Fonts";
	public const string FontsExtensionTTF = "ttf";
	public const string FontsExtensionOTF = "otf";

	public const string SpritesFolder = "Sprites";
	public const string SpritesExtension = "png";

	public const string SkinsFolder = "Skins";
	public const string SkinsExtension = "json";

	public const string LibrariesFolder = "Libraries";
	public const string LibrariesExtensionAssembly = "dll";
	public const string LibrariesExtensionSymbol = "pdb";

	public const string FujiJSON = "Fuji.json";
	public const string LevelsJSON = "Levels.json";

	private static string? contentPath = null;

	public static string ContentPath
	{
		get
		{
			if (contentPath == null)
			{
				var baseFolder = AppContext.BaseDirectory;
				var searchUpPath = "";
				int up = 0;
				while (!Directory.Exists(Path.Join(baseFolder, searchUpPath, AssetFolder)) && up++ < 6)
					searchUpPath = Path.Join(searchUpPath, "..");
				if (!Directory.Exists(Path.Join(baseFolder, searchUpPath, AssetFolder)))
					throw new Exception($"Unable to find {AssetFolder} Directory from '{baseFolder}'");
				contentPath = Path.Join(baseFolder, searchUpPath, AssetFolder);
			}

			return contentPath;
		}
	}

	public static readonly ModAssetDictionary<Map> Maps = new(gameMod => gameMod.Maps);
	public static readonly ModAssetDictionary<Shader> Shaders = new(gameMod => gameMod.Shaders);
	public static readonly ModAssetDictionary<Texture> Textures = new(gameMod => gameMod.Textures);
	public static readonly ModAssetDictionary<SkinnedTemplate> Models = new(gameMod => gameMod.Models);
	public static readonly ModAssetDictionary<Subtexture> Subtextures = new(gameMod => gameMod.Subtextures);
	public static readonly ModAssetDictionary<Font> Fonts = new(gameMod => gameMod.Fonts);
	public static readonly ModAssetDictionary<FMOD.Sound> Sounds = new(gameMod => gameMod.Sounds);
	public static readonly ModAssetDictionary<FMOD.Sound> Music = new(gameMod => gameMod.Music);
	public static readonly Dictionary<string, Language> Languages = new(StringComparer.OrdinalIgnoreCase);

	public static List<SkinInfo> EnabledSkins =>
		ModManager.Instance.EnabledMods
			.Where(mod => mod.Loaded)
			.SelectMany(mod => mod.Skins)
			.Where(skin => skin.IsUnlocked())
			.ToList();

	public static List<LevelInfo> Levels { get; private set; } = [];

	private static List<Task> tasks = [];

	private static void LoadDirectoryRecursive<T>(GameMod mod, string folder, string extension, ConcurrentBag<T> into, Action<string, GameMod, ConcurrentBag<T>> task)
	{
		foreach (var file in mod.Filesystem.FindFilesInDirectoryRecursive(folder, extension))
		{
			task(file, mod, into);
		}
	}

	public static void LoadMod(GameMod mod)
	{
		Log.Info($"Loading assets for mod {mod.ModInfo.Id}");

		var maps = new ConcurrentBag<(Map, GameMod)>();
		var images = new ConcurrentBag<(string, Image, GameMod)>();
		var models = new ConcurrentBag<(string, SkinnedTemplate, GameMod)>();
		var sounds = new ConcurrentBag<(string, FMOD.Sound, GameMod)>();
		var music = new ConcurrentBag<(string, FMOD.Sound, GameMod)>();
		var langs = new ConcurrentBag<(Language, GameMod)>();
		tasks = [];

		IModFilesystem modFs = mod.Filesystem;

		// Load maps
		LoadDirectoryRecursive(mod, MapsFolder, MapsExtension, maps, (file, mod, maps) =>
		{
			// Skip the "autosave" folder
			if (file.StartsWith($"{MapsFolder}/autosave", StringComparison.OrdinalIgnoreCase)) return;

			if (mod.Filesystem != null && mod.Filesystem.TryOpenFile(file,
					stream => new Map(GetResourceNameFromVirt(file, MapsFolder), file, stream), out var map))
			{
				maps.Add((map, mod));
			}
		});

		// Load textures
		LoadDirectoryRecursive(mod, TexturesFolder, TexturesExtension, images, (file, mod, images) =>
		{
			if (mod.Filesystem != null && mod.Filesystem.TryLoadImage(file, out var image))
			{
				images.Add((GetResourceNameFromVirt(file, TexturesFolder), image, mod));
			}
		});

		// Load character faces
		LoadDirectoryRecursive(mod, FacesFolder, FacesExtension, images, (file, mod, images) =>
		{
			var name = $"faces/{GetResourceNameFromVirt(file, FacesFolder)}";
			if (mod.Filesystem != null && mod.Filesystem.TryLoadImage(file, out var image))
			{
				images.Add((name, image, mod));
			}
		});

		// Load glb models
		LoadDirectoryRecursive(mod, ModelsFolder, ModelsExtension, models, (file, mod, models) =>
		{
			if (mod.Filesystem != null && mod.Filesystem.TryOpenFile(file, stream => SharpGLTF.Schema2.ModelRoot.ReadGLB(stream),
									out var input))
			{
				var model = new SkinnedTemplate(input);
				models.Add((GetResourceNameFromVirt(file, ModelsFolder), model, mod));
			}
		});

		// Load language files
		LoadDirectoryRecursive(mod, TextFolder, TextExtension, langs, (file, mod, langs) =>
		{
			if (mod.Filesystem != null && mod.Filesystem.TryLoadText(file, out var data))
			{
				if (JsonSerializer.Deserialize(data, LanguageContext.Default.Language) is { } lang)
					langs.Add((lang, mod));
			}
		});

		// Load FMOD audio banks
		var allBankFiles = modFs.FindFilesInDirectoryRecursive(AudioFolder, AudioExtension).ToList();
		// load strings first
		foreach (var file in allBankFiles)
		{
			if (mod.Filesystem != null && file.EndsWith($".strings.{AudioExtension}"))
				mod.Filesystem.TryOpenFile(file, Audio.LoadBankFromStream);
		}
		// load banks second
		foreach (var file in allBankFiles)
		{
			if (mod.Filesystem != null && file.EndsWith($".{AudioExtension}") && !file.EndsWith($".strings.{AudioExtension}"))
				mod.Filesystem.TryOpenFile(file, Audio.LoadBankFromStream);
		}

		// Load wav sounds
		LoadDirectoryRecursive(mod, SoundsFolder, SoundsExtension, sounds, (file, mod, sounds) =>
		{
			if (mod.Filesystem != null && mod.Filesystem.TryOpenFile(file, stream => Audio.LoadWavFromStream(stream),
									out var sound))
			{
				if (sound != null)
				{
					sounds.Add((GetResourceNameFromVirt(file, SoundsFolder), sound.Value, mod));
				}
			}
		});

		// Load wav music
		LoadDirectoryRecursive(mod, MusicFolder, MusicExtension, music, (file, mod, music) =>
		{
			if (mod.Filesystem != null && mod.Filesystem.TryOpenFile(file, stream => Audio.LoadWavFromStream(stream),
									out var song))
			{
				if (song != null)
				{
					music.Add((GetResourceNameFromVirt(file, MusicFolder), song.Value, mod));
				}
			}
		});

		// load level, dialog jsons
		mod.Levels.Clear();
		if (modFs != null && modFs.TryOpenFile(LevelsJSON,
				stream => JsonSerializer.Deserialize(stream, LevelInfoListContext.Default.ListLevelInfo) ?? [],
				out var levels))
		{
			mod.Levels.AddRange(levels);
			Levels.AddRange(levels);
		}

		if (modFs != null)
		{
			// load glsl shaders
			foreach (var file in modFs.FindFilesInDirectoryRecursive(ShadersFolder, ShadersExtension))
			{
				if (modFs.TryOpenFile(file, stream => LoadShader(file, stream), out var shader))
				{
					shader.Name = GetResourceNameFromVirt(file, ShadersFolder);
					Shaders.Add(shader.Name, shader, mod);
				}
			}

			// load font files
			foreach (var file in modFs.FindFilesInDirectoryRecursive(FontsFolder, ""))
			{
				if (file.EndsWith($".{FontsExtensionTTF}") || file.EndsWith($".{FontsExtensionOTF}"))
				{
					if (modFs.TryOpenFile(file, stream => new Font(stream), out var font))
						Fonts.Add(GetResourceNameFromVirt(file, FontsFolder), font, mod);
				}
			}
		}

		// pack sprites into single texture
		if (modFs != null)
		{
			var packer = new Packer
			{
				Trim = false,
				CombineDuplicates = false,
				Padding = 1
			};
			foreach (var file in modFs.FindFilesInDirectoryRecursive(SpritesFolder, SpritesExtension))
			{
				if (modFs.TryOpenFile(file, stream => new Image(stream), out var img))
				{
					packer.Add($"{mod.ModInfo.Id}:{GetResourceNameFromVirt(file, SpritesFolder)}", img);
				}
			}

			var result = packer.Pack();
			var pages = new List<Texture>();
			foreach (var it in result.Pages)
			{
				it.Premultiply();
				pages.Add(new Texture(it));
			}

			foreach (var it in result.Entries)
			{
				string[] nameSplit = it.Name.Split(':');
				Subtextures.Add(nameSplit[1], new Subtexture(pages[it.Page], it.Source, it.Frame), mod);
			}
		}


		// wait for tasks to finish
		{
			foreach (var task in tasks)
				task.Wait();

			foreach (var (name, img, _) in images)
				Textures.Add(name, new Texture(img) { Name = name }, mod);
			foreach (var (map, _) in maps)
				Maps.Add(map.Name, map, mod);
			foreach (var (name, sound, _) in sounds)
				Sounds.Add(name, sound, mod);
			foreach (var (name, song, _) in music)
				Music.Add(name, song, mod);
			foreach (var (name, model, _) in models)
			{
				model.ConstructResources();
				Models.Add(name, model, mod);
			}
			foreach (var (lang, _) in langs)
			{
				if (Languages.TryGetValue(lang.ID, out var existing))
				{
					existing.Absorb(lang, mod);
				}
				else
				{
					lang.OnCreate(mod);
					Languages.Add(lang.ID, lang);
				}
			}
		}

		// Load Skins
		if (ModManager.Instance.VanillaGameMod != null)
		{
			ModManager.Instance.VanillaGameMod.Skins.Add(
				new SkinInfo
				{
					Name = "Madeline",
					Model = "player",
					HideHair = false,
					HairNormal = 0xdb2c00,
					HairNoDash = 0x6ec0ff,
					HairTwoDash = 0xfa91ff,
					HairRefillFlash = 0xffffff,
					HairFeather = 0xf2d450
				}
			);
		}

		if (modFs != null)
		{
			foreach (var file in modFs.FindFilesInDirectoryRecursive(SkinsFolder, SkinsExtension))
			{
				if (modFs.TryOpenFile(file,
						stream => JsonSerializer.Deserialize(stream, SkinInfoContext.Default.SkinInfo), out var skin) && skin.IsValid())
				{
					mod.Skins.Add(skin);
				}
				else
				{
					Log.Warning($"Improperly configured skin: {file}");
				}
			}
		}
	}

	private static void StageVanilla()
	{
		GameMod? vanilla = ModManager.Instance.VanillaGameMod;

		if (vanilla is null)
		{
			throw new Exception("Vanilla mod does not exist. This means something went horribly wrong!");
		}

		LoadMod(vanilla);

		// make sure the active language is ready for use
		Language.Current.Use();
	}

	public static void Unload()
	{
		Levels.Clear();
		Maps.Clear();
		Shaders.Clear();
		Textures.Clear();
		Subtextures.Clear();
		Models.Clear();
		Fonts.Clear();
		Languages.Clear();
		Sounds.Clear();
		Music.Clear();
		Audio.Unload();

		Map.ModActorFactories.Clear();
	}

	public static void Load()
	{
		var timer = Stopwatch.StartNew();
		List<GameMod> loadQueue;

		Unload();

		ModLoader.RegisterAllMods();

		StageVanilla();

		loadQueue = ModManager.Instance.EnabledMods.Where(gm => gm is not VanillaGameMod).ToList();

		// NOTE: Make sure to update ModManager.OnModFileChanged() as well, for hot-reloading to work!

		// Go through all of the mods in queue and load them
		while (loadQueue.Count > 0)
		{
			LoadMod(loadQueue.First());

			loadQueue.RemoveAt(0);
		}

		StringBuilder x = new();

		foreach (var y in Textures)
		{
			x.Append("\nsubtext " + y.Value.Name);
		}

		ModManager.Instance.OnAssetsLoaded();

		Log.Info($"Loaded Assets in {timer.ElapsedMilliseconds}ms");
	}

	internal static string GetResourceNameFromVirt(string virtPath, string folder)
	{
		var ext = Path.GetExtension(virtPath);
		// +1 to account for the forward slash
		return virtPath.AsSpan((folder.Length + 1)..^ext.Length).ToString();
	}

	internal static Shader? LoadShader(string virtPath, Stream file)
	{
		using var reader = new StreamReader(file);
		var code = reader.ReadToEnd();

		StringBuilder vertex = new();
		StringBuilder fragment = new();
		StringBuilder? target = null;

		foreach (var l in code.Split('\n'))
		{
			var line = l.Trim('\r');

			if (line.StartsWith("VERTEX:"))
				target = vertex;
			else if (line.StartsWith("FRAGMENT:"))
				target = fragment;
			else if (line.StartsWith("#include"))
			{
				var path = $"{Path.GetDirectoryName(virtPath)}/{line[9..]}";

				if (ModManager.Instance.GlobalFilesystem.TryLoadText(path, out var include))
					target?.Append(include);
				else
					throw new Exception($"Unable to find shader include: '{path}'");
			}
			else
				target?.AppendLine(line);
		}

		return new Shader(new(
			vertexShader: vertex.ToString(),
			fragmentShader: fragment.ToString()
		));
	}
}
