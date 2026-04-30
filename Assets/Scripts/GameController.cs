using UnityEngine;

public class GameController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
}
