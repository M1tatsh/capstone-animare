using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class MainMenu : MonoBehaviour
{
    public CinemachineCamera playCam;
    public void PlayGame()
    {
        CameraManager.SwitchCamera(playCam);

        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
