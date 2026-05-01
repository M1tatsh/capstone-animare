using UnityEngine;

public class EndScript : MonoBehaviour
{
    public GameObject canvas;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            canvas.SetActive(true);
            GameController.SetGameSpeed(0);
        }
    }
}
