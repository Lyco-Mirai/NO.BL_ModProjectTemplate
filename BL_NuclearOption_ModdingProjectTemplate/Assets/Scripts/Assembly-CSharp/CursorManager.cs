using UnityEngine;

public static class CursorManager
{
	private static bool visible;

	private static CursorFlags flags;

	private static bool forceHidden;

	private static bool setup;

	public static bool Visible
	{
		get
		{
			return visible;
		}
		private set
		{
			if (visible != value)
			{
				visible = value;
				Cursor.visible = value;
				SetLockState();
			}
		}
	}

	private static void Setup()
	{
		setup = true;
	}

	private static void Application_focusChanged(bool focus)
	{
		if (Application.isPlaying && focus && !visible)
		{
			Visible = true;
			Refresh();
		}
	}

	public static CursorFlags GetFlags()
	{
		return flags;
	}

	public static bool GetFlag(CursorFlags flag)
	{
		return (flags & flag) != 0;
	}

	public static void SetFlag(CursorFlags flag, bool value)
	{
		_ = flags;
		if (value)
		{
			flags |= flag;
		}
		else
		{
			flags &= ~flag;
		}
		Refresh();
	}

	public static void ClearGameplayFlags()
	{
		CursorFlags cursorFlags = CursorFlags.GameMenu | CursorFlags.Map | CursorFlags.SelectionMenu | CursorFlags.Dialogue | CursorFlags.Chat | CursorFlags.CameraControlUI;
		flags &= ~cursorFlags;
		Refresh();
	}

	public static void ForceHidden(bool hidden)
	{
		forceHidden = hidden;
		Refresh();
	}

	public static void Refresh()
	{
		if (!setup)
		{
			Setup();
		}
		if (forceHidden)
		{
			Visible = false;
		}
		else
		{
			Visible = flags != CursorFlags.None;
		}
	}

	private static void SetLockState()
	{
		Cursor.lockState = ((!visible) ? CursorLockMode.Locked : CursorLockMode.None);
	}
}
