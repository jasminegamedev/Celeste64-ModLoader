namespace Celeste64.Mod.Helpers;

public static class InputHelper
{
	public static Vec2 MouseDelta => ImGuiManager.WantCaptureMouse 
		? Vec2.Zero 
		: Input.State.Mouse.Position - Input.LastState.Mouse.Position; 
}