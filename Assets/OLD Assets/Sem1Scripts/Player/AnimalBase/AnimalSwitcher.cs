using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AnimalSwitcher : MonoBehaviour
{
    private AnimalBase currentAnimal;
    private PlayerMovement player;

    private AnimalBase[] animals;

    void Start()
    {
        player = GetComponent<PlayerMovement>();

        animals = GetComponents<AnimalBase>();
        SwitchAnimal(animals[0]);
    }

    void Update()
    {
        /*
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchAnimal(animal1_Reaper);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchAnimal(animal2_Bear);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchAnimal(animal3_Cheetah);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchAnimal(animal4_Frog);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchAnimal(animal5_Monkey);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchAnimal(animal6_Ant);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchAnimal(animal7_Eagle);
        */
    }

    private void SwitchAnimal(AnimalBase newAnimal)
    {
        if (newAnimal == null || newAnimal == currentAnimal) return;

        if (currentAnimal != null)
            currentAnimal.OnDeactivate(player);

        currentAnimal = newAnimal;
        currentAnimal.OnActivate(player);
    }

    public void SelectTransformation(int animalIndex)
    {
        SwitchAnimal(animals[animalIndex]);
    }
}