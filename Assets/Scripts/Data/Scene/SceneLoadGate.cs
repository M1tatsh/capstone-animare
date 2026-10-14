using System.Collections.Generic;
using Data.Scene;
using UnityEngine;

public class SceneLoadGate : MonoBehaviour
{
    [SerializeField] Behaviour[] disabledUntilStart; // store objects here

    //objects added while the game is running (for example the spawned player), see DisableUntilStart.
    private readonly List<Behaviour> runtimeDisabledUntilStart = new List<Behaviour>();
    private bool isWaitingForStart;

    void Awake()
    {
        if (GameFlow.HasStarted) return;
        foreach (var obj in disabledUntilStart) obj.enabled = false;
        WaitForStart();
    }

    void OnDestroy() => GameFlow.Started -= Enable;

    /// <summary>
    /// For objects that don't exist before the game runs, like the spawned player.
    /// Switches the behaviour off until the title screen's Start button is pressed.
    /// Does nothing if the game has already started.
    /// </summary>
    public void DisableUntilStart(Behaviour behaviour)
    {
        if (GameFlow.HasStarted || behaviour == null) return;

        behaviour.enabled = false;
        runtimeDisabledUntilStart.Add(behaviour);
        WaitForStart();
    }

    //subscribes only once, however many times it is called.
    void WaitForStart()
    {
        if (isWaitingForStart) return;
        isWaitingForStart = true;
        GameFlow.Started += Enable;
    }

    void Enable()
    {
        foreach (var obj in disabledUntilStart) obj.enabled = true;

        //in the order they were added, so the player's input is switched on before the scripts that use it.
        foreach (var obj in runtimeDisabledUntilStart)
        {
            if (obj != null) obj.enabled = true;
        }

        runtimeDisabledUntilStart.Clear();
        GameFlow.Started -= Enable;
        isWaitingForStart = false;
    }
}
