using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    void Awake()
    {
        if (TransitionPanelManager.Instance != null)
        {
            TransitionPanelManager.Instance.ChangeScene("Historia");
            return;
        }

        SceneManager.LoadScene("Historia");
    }

  
}
