using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// Plays the skippable Intro flyover (Cutscene 1, ~8s) and Outro portal sequence (Cutscene 2, ~6s)
/// using Cinemachine (already installed in Packages/manifest.json).
/// Reads the skip prompt key directly from the player's Jump binding and safely restores
/// player control, cursor state, HUD visibility, and Time.timeScale = 1 even when skipped.
/// </summary>
public sealed class TutorialCutsceneController : MonoBehaviour
{
    [Header("Input & Player Control")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private RigidbodyFpsController playerMotor;
    [SerializeField] private FpsCameraRig playerCameraRig;
    [SerializeField] private WeaponController playerWeapons;
    [SerializeField] private Transform playerSpawnPoint;

    [Header("UI & Camera")]
    [SerializeField] private UIDocument tutorialUiDoc;
    [SerializeField] private CinemachineCamera cutsceneVCam;

    [Header("Cutscene 1: Intro Flyover (~9.5s, Slow Glide)")]
    [SerializeField] private Transform[] introWaypoints = System.Array.Empty<Transform>();
    [SerializeField] private Transform[] introLookTargets = System.Array.Empty<Transform>();
    [SerializeField, Min(1f)] private float introDuration = 9.5f;

    [Header("Cutscene 2: Outro (~6s, Slow Drift)")]
    [SerializeField] private Transform[] outroWaypoints = System.Array.Empty<Transform>();
    [SerializeField] private Transform exitPortalVisual;
    [SerializeField, Min(1f)] private float outroDuration = 6.0f;

    [Header("Camera Smoothing")]
    [SerializeField, Min(0.2f)] private float rotationDamping = 2.2f;

    [Header("Next Scene Transition")]
    [SerializeField] private string targetScene = "Level1";
    [SerializeField] private string loadingSceneName = "Loading";

    private VisualElement hudContainer;
    private VisualElement cutsceneOverlay;
    private VisualElement cutsceneBanner;
    private Label cutsceneSkipLabel;
    private CinemachineBrain brain;
    private InputAction skipAction;
    private Rigidbody playerBody;
    private bool inCutscene;

    private void Awake()
    {
        if (playerMotor != null)
            playerBody = playerMotor.GetComponent<Rigidbody>();

        if (Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        if (inputActions != null)
        {
            InputActionAsset clone = Object.Instantiate(inputActions);
            InputActionMap gameplay = clone.FindActionMap("Gameplay", throwIfNotFound: false);
            skipAction = gameplay?.FindAction("Jump", throwIfNotFound: false);
        }

        if (cutsceneVCam != null)
        {
            cutsceneVCam.Priority.Value = -10;
            cutsceneVCam.enabled = false;
        }

        if (exitPortalVisual != null)
            exitPortalVisual.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    private void OnDestroy()
    {
        if (skipAction != null)
            skipAction.Disable();
    }

    private void Start()
    {
        CacheUI();
        Time.timeScale = 1f;
        StartCoroutine(PlayIntroRoutine());
    }

    private void CacheUI()
    {
        if (tutorialUiDoc == null || tutorialUiDoc.rootVisualElement == null) return;
        VisualElement root = tutorialUiDoc.rootVisualElement;
        hudContainer = root.Q<VisualElement>("tutorial-hud-container");
        cutsceneOverlay = root.Q<VisualElement>("cutscene-overlay");
        cutsceneBanner = root.Q<VisualElement>("cutscene-banner");
        cutsceneSkipLabel = root.Q<Label>("cutscene-skip-label");

        if (cutsceneSkipLabel != null)
            cutsceneSkipLabel.text = $"PRESS [{GetSkipKeyDisplay()}] TO SKIP";
    }

    private IEnumerator PlayIntroRoutine()
    {
        UpdateCameraAlongPath(introWaypoints, introLookTargets, 0f, 0f, snapRotation: true);
        SetCutsceneState(active: true, showCompleteBanner: false);
        if (skipAction != null) skipAction.Enable();

        // Wait 1 frame so stale Jump presses from loading aren't treated as skip
        yield return null;

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            if (skipAction != null && skipAction.WasPressedThisFrame())
                break;

            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / introDuration);
            UpdateCameraAlongPath(introWaypoints, introLookTargets, Mathf.SmoothStep(0f, 1f, t), dt, snapRotation: false);
            yield return null;
        }

        if (skipAction != null) skipAction.Disable();
        SnapPlayerToStart();
        SetCutsceneState(active: false, showCompleteBanner: false);
    }

    public void PlayOutroCutscene()
    {
        if (inCutscene) return;
        StartCoroutine(PlayOutroRoutine());
    }

    private IEnumerator PlayOutroRoutine()
    {
        if (outroWaypoints.Length >= 1 && cutsceneVCam != null)
        {
            cutsceneVCam.transform.position = outroWaypoints[0].position;
            if (exitPortalVisual != null)
            {
                Vector3 dir0 = exitPortalVisual.position + Vector3.up * 0.2f - outroWaypoints[0].position;
                if (dir0.sqrMagnitude > 0.001f)
                    cutsceneVCam.transform.rotation = Quaternion.LookRotation(dir0, Vector3.up);
            }
        }

        SetCutsceneState(active: true, showCompleteBanner: true);
        if (skipAction != null) skipAction.Enable();

        yield return null;

        float elapsed = 0f;
        while (elapsed < outroDuration)
        {
            if (skipAction != null && skipAction.WasPressedThisFrame())
                break;

            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / outroDuration);

            // Animate the exit portal opening cleanly over the first 1.2 seconds
            if (exitPortalVisual != null)
            {
                float openT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 1.2f));
                exitPortalVisual.localScale = Vector3.Lerp(new Vector3(0.01f, 0.01f, 0.01f), Vector3.one, openT);
            }

            if (outroWaypoints.Length >= 2 && cutsceneVCam != null)
            {
                Vector3 pos = Vector3.Lerp(outroWaypoints[0].position, outroWaypoints[1].position, Mathf.SmoothStep(0f, 1f, t));
                cutsceneVCam.transform.position = pos;
                if (exitPortalVisual != null)
                {
                    Vector3 lookDir = exitPortalVisual.position + Vector3.up * 0.2f - pos;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
                        float rotBlend = 1f - Mathf.Exp(-rotationDamping * dt);
                        cutsceneVCam.transform.rotation = Quaternion.Slerp(cutsceneVCam.transform.rotation, targetRot, rotBlend);
                    }
                }
            }

            yield return null;
        }

        if (skipAction != null) skipAction.Disable();
        if (exitPortalVisual != null) exitPortalVisual.localScale = Vector3.one;

        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (!string.IsNullOrEmpty(loadingSceneName) && Application.CanStreamedLevelBeLoaded(loadingSceneName))
        {
            LoadingData.nextScene = targetScene;
            SceneManager.LoadScene(loadingSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    private void SetCutsceneState(bool active, bool showCompleteBanner)
    {
        inCutscene = active;
        CacheUI();

        if (playerMotor != null)
        {
            playerMotor.enabled = !active;
            if (active && playerBody != null)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
                playerBody.isKinematic = true;
            }
            else if (!active && playerBody != null)
            {
                playerBody.isKinematic = false;
            }
        }

        if (playerCameraRig != null) playerCameraRig.enabled = !active;
        if (playerWeapons != null) playerWeapons.enabled = !active;

        if (hudContainer != null)
            hudContainer.style.display = active ? DisplayStyle.None : DisplayStyle.Flex;

        if (cutsceneOverlay != null)
            cutsceneOverlay.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;

        if (cutsceneBanner != null)
            cutsceneBanner.style.display = showCompleteBanner ? DisplayStyle.Flex : DisplayStyle.None;

        if (cutsceneVCam != null)
        {
            if (!active && brain != null)
                StartCoroutine(RestoreImmediateBlendRoutine());

            cutsceneVCam.Priority.Value = active ? 100 : -10;
            cutsceneVCam.enabled = active;
        }

        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    private IEnumerator RestoreImmediateBlendRoutine()
    {
        var previousBlend = brain.DefaultBlend;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        yield return null;
        brain.DefaultBlend = previousBlend;
    }

    private void SnapPlayerToStart()
    {
        if (playerMotor == null || playerSpawnPoint == null) return;
        if (playerBody != null)
        {
            playerBody.position = playerSpawnPoint.position;
            playerBody.rotation = playerSpawnPoint.rotation;
            playerBody.linearVelocity = Vector3.zero;
        }
        else
        {
            playerMotor.transform.SetPositionAndRotation(playerSpawnPoint.position, playerSpawnPoint.rotation);
        }
    }

    private void UpdateCameraAlongPath(Transform[] waypoints, Transform[] lookTargets, float normalizedT, float dt, bool snapRotation)
    {
        if (cutsceneVCam == null || waypoints == null || waypoints.Length < 2) return;

        float scaled = normalizedT * (waypoints.Length - 1);
        int idx = Mathf.Min(waypoints.Length - 2, Mathf.FloorToInt(scaled));
        float localT = scaled - idx;

        Vector3 pos = EvaluateCatmullRom(waypoints, idx, localT);
        cutsceneVCam.transform.position = pos;

        Quaternion targetRot;
        if (lookTargets != null && lookTargets.Length == waypoints.Length)
        {
            Vector3 lookPos = EvaluateCatmullRom(lookTargets, idx, localT);
            Vector3 dir = lookPos - pos;
            targetRot = dir.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(dir, Vector3.up)
                : cutsceneVCam.transform.rotation;
        }
        else
        {
            targetRot = Quaternion.Slerp(waypoints[idx].rotation, waypoints[idx + 1].rotation, localT);
        }

        if (snapRotation)
        {
            cutsceneVCam.transform.rotation = targetRot;
        }
        else
        {
            float rotBlend = 1f - Mathf.Exp(-rotationDamping * dt);
            cutsceneVCam.transform.rotation = Quaternion.Slerp(cutsceneVCam.transform.rotation, targetRot, rotBlend);
        }
    }

    private static Vector3 EvaluateCatmullRom(Transform[] points, int segmentIdx, float t)
    {
        int last = points.Length - 1;
        Vector3 p0 = points[Mathf.Max(0, segmentIdx - 1)].position;
        Vector3 p1 = points[segmentIdx].position;
        Vector3 p2 = points[Mathf.Min(last, segmentIdx + 1)].position;
        Vector3 p3 = points[Mathf.Min(last, segmentIdx + 2)].position;

        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private string GetSkipKeyDisplay()
    {
        if (skipAction == null || skipAction.bindings.Count == 0)
            return "JUMP";

        for (int i = 0; i < skipAction.bindings.Count; i++)
        {
            InputBinding b = skipAction.bindings[i];
            if (b.isComposite || b.isPartOfComposite) continue;
            if (!string.IsNullOrEmpty(b.groups) && b.groups.Contains("Keyboard"))
                return skipAction.GetBindingDisplayString(i, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpperInvariant();
        }
        return skipAction.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions).ToUpperInvariant();
    }
}
