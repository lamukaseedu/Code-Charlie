using UnityEngine;

public class QuitManager : MonoBehaviour
{
    public void QuitGame()
    {
        // Logs a message in the Console so you know it works in the Editor
        Debug.Log("Game is exiting...");

        // Quits the built application
        Application.Quit();

        // Optional: Stops Play Mode if you are testing inside the Unity Editor
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }
}
