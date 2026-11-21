using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class WeaponViewAnimator : MonoBehaviour
{
    [SerializeField] AnimationClip reloadClip;

    static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    static readonly int Fire = Animator.StringToHash("Base Layer.Fire");
    static readonly int Reload = Animator.StringToHash("Base Layer.Reload");
    static readonly int ReloadSpeed = Animator.StringToHash("ReloadSpeed");

    Animator animator;

    void Awake() => animator = GetComponent<Animator>();

    public void PlayIdle() => Play(Idle);
    public void PlayFire() => Play(Fire);

    public void PlayReload(float duration)
    {
        if (animator.isActiveAndEnabled && reloadClip != null)
            animator.SetFloat(ReloadSpeed, reloadClip.length / duration);

        Play(Reload);
    }

    void Play(int state)
    {
        if (animator.isActiveAndEnabled)
            animator.Play(state, 0, 0f);
    }
}
