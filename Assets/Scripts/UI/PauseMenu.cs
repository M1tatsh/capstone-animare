using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MainMenu
{

    void Update()
    {
        
    }

    public override void PlayGame()
    {
        GameController.ResetGameSpeed();
        
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PauseGame()
    {

        GameController.SetGameSpeed(0);
    }
}
