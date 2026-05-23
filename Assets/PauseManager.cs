using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{


public void ReturnToMenu()
    {
        if (TransitionPanelManager.Instance != null)
        {
            TransitionPanelManager.Instance.ChangeScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
