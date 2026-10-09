using Data.Scene;
using UnityEngine;

public class SceneLoadGate : MonoBehaviour
{
    [SerializeField] Behaviour[] disabledUntilStart; // store objects here

    void Awake()
    {
        if (GameFlow.HasStarted) return;
        foreach (var obj in disabledUntilStart) obj.enabled = false;
        GameFlow.Started += Enable;
    }

    void OnDestroy() => GameFlow.Started -= Enable;

    void Enable()
    {
        foreach (var obj in disabledUntilStart) obj.enabled = true;
        GameFlow.Started -= Enable;
    }
}
