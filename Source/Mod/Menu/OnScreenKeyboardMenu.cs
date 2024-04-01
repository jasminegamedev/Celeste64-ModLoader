using System;

namespace Celeste64.Mod;

public class OnScreenKeyboardMenu : Menu
{
	public Target Target;
	public Target GameTarget;

	private int currentPage = 0;
	private int currentRow = 0;
	private int currentColumn = 0;

	private const int rows = 7;
	private const int columns = 9;

	private int CurrentPageStart => currentPage * columns * rows;
	private int CurrentIndex => currentRow * columns + currentColumn;
	bool shiftMode;

	string setText = string.Empty;

	InputField Owner;

	internal OnScreenKeyboardMenu(Menu? rootMenu, InputField owner)
	{
		Target = new Target(Overworld.CardWidth, Overworld.CardHeight);
		Game.OnResolutionChanged += () => Target = new Target(Overworld.CardWidth, Overworld.CardHeight);
		GameTarget = new Target(Game.Width, Game.Height);
		RootMenu = rootMenu;

		Owner = owner;
	}

	public override void Initialized()
	{
		base.Initialized();
		currentColumn = 0;
		currentRow = 0;
		currentPage = 0;

		setText = Owner.GetFieldText();
	}

	public override void Closed()
	{
		Owner.SetFieldText(setText);
	}

	private void RenderCharacter(Batcher batch, string character, Vec2 pos, Vec2 size)
	{
		batch.PushMatrix(Matrix3x2.CreateScale(1.1f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), Color.Black);

		batch.PopMatrix();

		batch.PushMatrix(Matrix3x2.CreateScale(0.9f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), Color.White);

		batch.PopMatrix();
	}

	private void RenderCurrentCharacter(Batcher batch, string character, Vec2 pos, Vec2 size)
	{
		batch.PushMatrix(Matrix3x2.CreateScale(1.1f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), Color.Black);

		batch.PopMatrix();

		batch.PushMatrix(Matrix3x2.CreateScale(0.9f) * Matrix3x2.CreateTranslation((pos + new Vec2(size.X * 0.4f - 20, size.Y * 0.4f - 20)) * Game.RelativeScale));
		batch.Text(Language.Current.SpriteFont, character, Vec2.Zero, new Vec2(0.5f, 0), (Time.BetweenInterval(0.1f) ? 0x84FF54 : 0xFCFF59));

		batch.PopMatrix();
	}

	private void RenderCharacters(Batcher batch)
	{
		var bounds = GameTarget.Bounds;
		Vec2 size = new Vec2(bounds.Width, bounds.Height);
		Vec2 offset = new Vec2(20, 20);

		int index = 0;
		for (int i = 0; i < rows && CurrentPageStart + index < (!shiftMode ? KeyboardHandler.TrimKeysList().Count : KeyboardHandler.TrimModifiedKeysList().Count); i++)
		{
			for (int j = 0; j < columns && CurrentPageStart + index < (!shiftMode ? KeyboardHandler.TrimKeysList().Count : KeyboardHandler.TrimModifiedKeysList().Count); j++)
			{
				if (index == currentRow * columns + currentColumn)
				{
					RenderCurrentCharacter(batch, !shiftMode ? KeyboardHandler.TrimKeysList()[CurrentPageStart + index] : KeyboardHandler.TrimModifiedKeysList()[CurrentPageStart + index], new Vec2(j, i) * offset, size);
				}
				else
				{
					RenderCharacter(batch, !shiftMode ? KeyboardHandler.TrimKeysList()[CurrentPageStart + index] : KeyboardHandler.TrimModifiedKeysList()[CurrentPageStart + index], new Vec2(j, i) * offset, size);
				}
				index++;
			}
		}
	}

	protected override void HandleInput()
	{
		if (Controls.Menu.Horizontal.Positive.Pressed)
		{
			if (currentColumn == columns - 1)
			{
				if (((currentPage + 1) * columns * rows) + (currentRow * columns) < (!shiftMode ? KeyboardHandler.TrimKeysList().Count : KeyboardHandler.TrimModifiedKeysList().Count))
				{
					currentPage++;
					currentColumn = 0;
				}
				else if ((currentPage + 1) * columns * rows < (!shiftMode ? KeyboardHandler.TrimKeysList().Count : KeyboardHandler.TrimModifiedKeysList().Count))
				{
					currentPage++;
					currentColumn = 0;
					currentRow = 0;
				}
			}
			else if (CurrentPageStart + CurrentIndex + 1 < (!shiftMode ? KeyboardHandler.TrimKeysList().Count : KeyboardHandler.TrimModifiedKeysList().Count))
			{
				currentColumn += 1;
			}
			Audio.Play(DownSound);
		}
		if (Controls.Menu.Horizontal.Negative.Pressed)
		{
			if (currentColumn == 0)
			{
				if (currentPage > 0)
				{
					currentPage--;
					currentColumn = columns - 1;
				}
			}
			else
			{
				currentColumn -= 1;
			}
			Audio.Play(DownSound);
		}

		if (Controls.Menu.Vertical.Positive.Pressed && (currentRow + 1) < rows && CurrentPageStart + CurrentIndex + columns < (!shiftMode ? KeyboardHandler.TrimKeysList().Count : KeyboardHandler.TrimModifiedKeysList().Count))
		{
			currentRow += 1;
			Audio.Play(DownSound);
		}
		if (Controls.Menu.Vertical.Negative.Pressed && (currentRow - 1) >= 0)
		{
			currentRow -= 1;
			Audio.Play(DownSound);
		}
		/*
		* The next four controls are only relevant to gamepads.
		* We check if a keyboard key is pressed to avoid conflicts.
		*/
		if (Controls.CopyFile.Pressed && KeyboardHandler.Instance.GetPressedKey() == null)
			shiftMode = !shiftMode;

		if (Controls.RenameFile.Pressed && setText.Length > 0 && KeyboardHandler.Instance.GetPressedKey() == null)
		{
			setText = setText.Remove(setText.Length - 1);
		}

		if (Controls.Confirm.Pressed && KeyboardHandler.Instance.GetPressedKey() == null)
		{
			if (shiftMode)
			{
				setText += KeyboardHandler.TrimModifiedKeysList()[CurrentPageStart + CurrentIndex];
			}
			else
			{
				setText += KeyboardHandler.TrimKeysList()[CurrentPageStart + CurrentIndex];
			}
			Audio.Play(UpSound);
		}

		if (Controls.Cancel.ConsumePress() && KeyboardHandler.Instance.GetPressedKey() == null)
		{
			Owner.RootMenu.PopSubMenu();
		}

		if (KeyboardHandler.Instance.GetPressedKey() is Keys.Enter or Keys.Enter2 or Keys.KeypadEnter)
		{
			Owner.RootMenu.PopSubMenu();
		}

		ReadKey();
	}

	protected override void RenderItems(Batcher batch)
	{
		batch.PushMatrix(new Vec2(GameTarget.Bounds.TopLeft.X, GameTarget.Bounds.TopLeft.Y), false);

		if (setText.Length == 0)
			batch.Text(Language.Current.SpriteFont, "Enter text", new Vec2(GameTarget.Bounds.TopCenter.X - Language.Current.SpriteFont.WidthOf("Enter text") / 2, GameTarget.Bounds.TopCenter.Y + 96), Color.CornflowerBlue * 0.6f);

		batch.Text(Language.Current.SpriteFont, setText, new Vec2(GameTarget.Bounds.TopCenter.X - Language.Current.SpriteFont.WidthOf(setText) / 2, GameTarget.Bounds.TopCenter.Y + 96), Color.CornflowerBlue);
		batch.Text(Language.Current.SpriteFont, Loc.Str("OSKUserInstructions"), new Vec2(GameTarget.Bounds.TopCenter.X - Language.Current.SpriteFont.WidthOf(Loc.Str("OSKUserInstructions")) / 2, GameTarget.Bounds.TopCenter.Y + 284), Color.CornflowerBlue);
		RenderCharacters(batch);
	}

	private Keys? key;
	public void ReadKey()
	{
		key = KeyboardHandler.Instance.GetPressedKey();

		if (key != null)
		{
			if (key == Keys.Backspace || key == Keys.KeypadBackspace)
			{
				if (setText.Length > 0)
					setText = setText.Remove(setText.Length - 1);
			}
			else
				setText += KeyboardHandler.GetKeyName(key);
		}
	}
}
