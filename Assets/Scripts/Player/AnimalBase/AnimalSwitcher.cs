using UnityEngine;

public class AnimalSwitcher : MonoBehaviour
{
    [Header("Animal Slots (1~7)")]
    public AnimalBase animal1_Reaper;
    public AnimalBase animal2_Bear;
    public AnimalBase animal3_Cheetah;
    public AnimalBase animal4_Frog;
    public AnimalBase animal5_Monkey;
    public AnimalBase animal6_Ant;
    public AnimalBase animal7_Eagle;

    private AnimalBase currentAnimal;
    private PlayerMovement player;

    void Start()
    {
        player = GetComponent<PlayerMovement>();
        SwitchAnimal(animal1_Reaper);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchAnimal(animal1_Reaper);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchAnimal(animal2_Bear);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchAnimal(animal3_Cheetah);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchAnimal(animal4_Frog);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchAnimal(animal5_Monkey);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchAnimal(animal6_Ant);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchAnimal(animal7_Eagle);
    }

    private void SwitchAnimal(AnimalBase newAnimal)
    {
        if (newAnimal == null || newAnimal == currentAnimal) return;

        Debug.Log(newAnimal.GetType().Name);

        if (currentAnimal != null)
            currentAnimal.OnDeactivate(player);

        currentAnimal = newAnimal;
        currentAnimal.OnActivate(player);
    }
}