using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
  
  public void StartGame()
    {
        if (TransitionPanelManager.Instance != null)
        {
            TransitionPanelManager.Instance.ChangeScene("Intro");
            return;
        }

        SceneManager.LoadScene("Intro");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
