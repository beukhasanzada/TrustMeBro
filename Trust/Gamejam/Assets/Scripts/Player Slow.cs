using UnityEngine;

public class PlayerSlow : MonoBehaviour
{
    private float multiplier = 1f;
    private float slowUntil;

    // 1 = обычная скорость, 0.4 = замедлен
    public float SpeedMultiplier => Time.time < slowUntil ? multiplier : 1f;

    public void ApplySlow(float newMultiplier, float duration)
    {
        if (Time.time < slowUntil)
            multiplier = Mathf.Min(multiplier, newMultiplier);
        else
            multiplier = newMultiplier;

        slowUntil = Time.time + duration;
    }
}