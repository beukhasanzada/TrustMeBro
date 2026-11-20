using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float hp = 100f;
    [SerializeField] private float bonusTime = 2f;   // сколько секунд даёт убийство

    public bool isDead { get; private set; }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        hp -= damage;
        if (hp <= 0f) Die();
    }

    private void Die()
    {
        isDead = true;

        SoundManager.Instance.PlayDeath(transform.position);
        GameTimer.Instance.AddTime(bonusTime);

    }
}