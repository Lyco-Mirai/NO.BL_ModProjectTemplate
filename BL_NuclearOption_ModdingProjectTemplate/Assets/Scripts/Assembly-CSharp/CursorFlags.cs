using System;

[Flags]
public enum CursorFlags
{
	None = 0,
	GameMenu = 1,
	Map = 2,
	SelectionMenu = 4,
	Dialogue = 8,
	NotInGame = 0x10,
	Chat = 0x20,
	Loading = 0x40,
	EmptyScene = 0x80,
	CameraControlUI = 0x100
}
