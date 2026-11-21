using UnityEngine;

[DisallowMultipleComponent]
public sealed class CrystalAttackAnimationEvents : MonoBehaviour
{
    CrystalRangedEnemy enemy;

    void Awake() => enemy = GetComponentInParent<CrystalRangedEnemy>();

    // The Animator invokes this on frame 45 of RangedAttack.
    public void FireLaserFrame() => enemy?.RequestLaserShot();
}
