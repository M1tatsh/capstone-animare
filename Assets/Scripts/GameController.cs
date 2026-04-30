using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject pauseMenu;
    public GameObject menuBackground;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !mainMenu.activeInHierarchy)
        {
            PauseGame();
        }
    }

    public static void SetGameSpeed(float speed)
    {
        Time.timeScale = speed;
        //Time.fixedDeltaTime = speed;
    }
    public static void ResetGameSpeed()
    {
        Time.timeScale = 1.0f;
        //Time.fixedDeltaTime = 1.0f;
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        menuBackground.SetActive(true);
        SetGameSpeed(0);
    }
}
