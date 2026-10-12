using UnityEngine;

namespace Gameplay.Rotation
{ 
    /// <summary>
    /// Static reference class for determining logic at certain view angles.
    /// </summary>
    public static class ViewAxes
    {
        public static Vector3Int Right(int view) => 
            Round(Quaternion.Euler(0, 90 * view, 0) * Vector3.right);
        public static Vector3Int Forward(int view) => 
            Round(Quaternion.Euler(0, 90 * view, 0) * Vector3.forward);

        static Vector3Int Round(Vector3 vector) =>
            new Vector3Int(
                Mathf.RoundToInt(vector.x), 
                Mathf.RoundToInt(vector.y), 
                Mathf.RoundToInt(vector.z)
                );
    }
}

