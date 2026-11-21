using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float hp = 100f;

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

        Destroy(gameObject);
    }
}