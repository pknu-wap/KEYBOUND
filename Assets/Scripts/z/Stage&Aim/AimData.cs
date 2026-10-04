using Keybound.Keyboard;
using UnityEngine;

[System.Serializable]
public class AimData
{
    // 목표 이름
    public string aimName;

    // 목표로 눌러야 하는 키
    public KeyboardMapKey targetKey;

    // 목표 달성에 필요한 입력 횟수
    [Min(1)]
    public int requiredCount = 1;
}