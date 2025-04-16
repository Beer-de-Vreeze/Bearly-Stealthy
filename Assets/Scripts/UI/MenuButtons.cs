using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    public void StartGame()
    {
        // Load the game scene (assuming it's named "GameScene")
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }

    public void LoadMainMenu()
    {
        // Load the main menu scene (assuming it's named "MainMenu")
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        // Quit the application
        //only if its not webgl build then quit the game
#if !UNITY_WEBGL
        Application.Quit();
#else
        //if webgl build then close the tab
        Application.OpenURL("https://bjeerpeer.itch.io/");
#endif
#if UNITY_EDITOR
        // If running in the editor, stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
