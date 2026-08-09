using JamesFrowen.ScriptableVariables.UI;
using NuclearOption.SceneLoading;
using UnityEngine.SceneManagement;

public class ReturnToMenuSceneButton : ButtonController
{
	protected override void onClick()
	{
		SceneManager.LoadScene(MapLoader.MainMenu);
	}
}
