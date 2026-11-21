using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class WeaponViewAnimator : MonoBehaviour
{
    [SerializeField] AnimationClip reloadClip;
    [SerializeField] string idleState = "Base Layer.Idle";
    [SerializeField] string fireState = "Base Layer.Fire";
    [SerializeField] string reloadState = "Base Layer.Reload";

    static readonly int ReloadSpeed = Animator.StringToHash("ReloadSpeed");

    Animator animator;
    int idleStateHash;
    int fireStateHash;
    int reloadStateHash;

    void Awake()
    {
        animator = GetComponent<Animator>();
        idleStateHash = Animator.StringToHash(idleState);
        fireStateHash = Animator.StringToHash(fireState);
        reloadStateHash = Animator.StringToHash(reloadState);
    }

    public void PlayIdle() => Play(idleStateHash);
    public void PlayFire() => Play(fireStateHash);

    public void PlayReload(float duration)
    {
        if (animator.isActiveAndEnabled && reloadClip != null)
            animator.SetFloat(ReloadSpeed, reloadClip.length / duration);

        Play(reloadStateHash);
    }

    void Play(int state)
    {
        if (animator.isActiveAndEnabled)
            animator.Play(state, 0, 0f);
    }
}
