using UnityEngine;

namespace Data.IO
{
    public class QuitHandler : MonoBehaviour
    {
        public void QuitGame()
        {
            Debug.Log("QuitHandler.QuitGame: Game is exiting...");

            Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
        }
    }   
}