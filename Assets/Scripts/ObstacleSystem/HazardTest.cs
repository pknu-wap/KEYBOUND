using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HazardTest : MonoBehaviour
{
    [SerializeField] private ObstacleManager obstacleManager;
    [SerializeField] private WallManager wallManager;
    [SerializeField] private Key obstacleKey = Key.D;
    [SerializeField] private Key wallKey = Key.F;
    [ContextMenu("Spawn Test Hazards")]
    public void SpawnTest()
    {
        if (!Application.isPlaying) return;
        obstacleManager?.Spawn(obstacleKey, 1f, 10f, 3f);
        wallManager?.Spawn(wallKey, 1f, 10f);
    }
    [ContextMenu("Clear Test Hazards")]
    public void ClearTest()
    {
        if (!Application.isPlaying) return;
        obstacleManager?.Remove(obstacleKey);
        wallManager?.Remove(wallKey);
    }
}
