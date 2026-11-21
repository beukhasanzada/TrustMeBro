using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public sealed class TutorialCourse : MonoBehaviour
{
    public enum Lesson { Basics, Jump, Slide, Dash, Wallrun, Armory, Range, Blast, Extraction }

    [Serializable]
    public sealed class Station
    {
        public Lesson lesson;
        public BoxCollider entrance;
        public Transform checkpoint;
        public GameObject gate;
        public GameObject equipment;
        public Health target;
        public Health[] crystals;
        public EnemyBase[] support;
        public GameObject timeOrb;
        public BoxCollider finish;
    }

    [SerializeField] private RigidbodyFpsController player;
    [SerializeField] private UIDocument hud;
    [SerializeField] private StyleSheet lessonStyle;
    [SerializeField] private Station[] stations;
    [SerializeField] private float extractionDrainRate = 1f;

    private static readonly (string Title, string Hint, string[] Tasks)[] Instructions =
    {
        ("Orientation", "Look around, then move across the office.", new[] { "Mouse or right stick · Look", "WASD or left stick · Move" }),
        ("Momentum", "On the long floor, hold Jump through a landing to hop again. Release and tap Jump in midair to double-jump.",
            new[] { "Shift / L3 · Sprint", "Space / A · Jump", "Hold Jump · Bunny hop", "Tap Jump again midair · Double jump" }),
        ("Under the shutter", "Build speed before the yellow line and hold Slide until you are through the low shutter.",
            new[] { "Hold Ctrl / B at speed · Slide under the shutter", "Reach the far side" }),
        ("Dash battery", "Dash in your movement direction. You have three charges. Each missing charge takes two seconds to refill.",
            new[] { "Q / RB · Spend a dash charge", "Wait for a charge to refill" }),
        ("Wall crossing", "Sprint along the yellow approach. Jump next to the blue wall, then tap Jump again to push off toward the far deck.",
            new[] { "Run along the wall", "Jump off the wall", "Land on the marked deck" }),
        ("Armory", "Walk over the three guns. Switch through the slots, then drop a gun, step back and walk over it again.",
            new[] { "Pick up all three guns", "Equip slots 1, 2 and 3 / cycle via Tab or D-pad right", "G / D-pad down · Drop a gun", "Pick up the dropped gun" }),
        ("Target practice", "Use the target ahead to feel the recoil. The HUD shows rounds in the magazine and your reserve.",
            new[] { "Right mouse / LT · Aim zoom", "Left mouse / RT · Destroy the target", "R / X · Finish a reload" }),
        ("Shotgun launch", "Select the Shotgun. From the yellow pad, look almost straight down and fire while moving forward onto the raised deck.",
            new[] { "Launch by firing the Shotgun at the floor", "Land on the raised deck" }),
        ("Timed extraction", "Time is your health. Use cover against crystal lasers and keep away from turret knockback and blue slow fields. Kills give time back.",
            new[] { "Collect the gold time orb", "Defeat the ranged crystal and knockback turret", "Defeat the slow drone and pulse turret" })
    };

    private WeaponController weapons;
    private FpsCameraRig cameraRig;
    private TimeManager timer;
    private VisualElement panel;
    private Label heading;
    private Label hint;
    private Label dashStatus;
    private Label[] rows;
    private int stage;
    private int completed;
    private int equippedSlots;
    private int lowestCharges;
    private bool cleared;
    private WeaponAmmoState lastAmmo;
    private Vector3 startPosition;

    private Station Current => stations[stage];

    private void Start()
    {
        weapons = player.GetComponent<WeaponController>();
        cameraRig = player.GetComponentInChildren<FpsCameraRig>();
        timer = TimeManager.Instance;
        lastAmmo = weapons.Ammo;
        player.OnGroundJump += GroundJumped;
        player.OnAirJump += AirJumped;
        player.OnWallJump += WallJumped;
        player.OnBlastJump += BlastJumped;
        weapons.AmmoChanged += AmmoChanged;
        CreatePanel();
        Begin(0);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnGroundJump -= GroundJumped;
            player.OnAirJump -= AirJumped;
            player.OnWallJump -= WallJumped;
            player.OnBlastJump -= BlastJumped;
        }
        if (weapons != null)
            weapons.AmmoChanged -= AmmoChanged;
        panel?.RemoveFromHierarchy();
    }

    private void Update()
    {
        if (!player.isActiveAndEnabled || Time.timeScale == 0f)
            return;

        if (Keyboard.current?.backspaceKey.wasPressedThisFrame == true
            || Gamepad.current?.selectButton.wasPressedThisFrame == true)
            CheckpointManager.Instance.RespawnPlayer(player.transform);

        Vector3 position = player.transform.position;
        if (cleared)
        {
            int next = stage + 1;
            if (next < stations.Length && stations[next].entrance.bounds.Contains(position))
                Begin(next);
            return;
        }

        switch (Current.lesson)
        {
            case Lesson.Basics:
                if (player.LookInput.sqrMagnitude > 0.04f) Complete(0);
                if ((position - startPosition).sqrMagnitude > 5f) Complete(1);
                break;
            case Lesson.Jump:
                if (player.IsSprinting && HorizontalSpeed() > 10f) Complete(0);
                break;
            case Lesson.Slide:
                if (player.IsSliding && Current.finish.bounds.Contains(position)) Complete(0);
                if (Has(0) && position.z > Current.finish.bounds.max.z) Complete(1);
                break;
            case Lesson.Dash:
                string chargeText = $"Charges  {player.DashCharges}/{RigidbodyFpsController.MaxDashCharges}    Recharge  {player.DashRechargeProgress:P0}";
                if (dashStatus.text != chargeText) dashStatus.text = chargeText;
                if (player.DashCharges < lowestCharges)
                {
                    lowestCharges = player.DashCharges;
                    Complete(0);
                }
                if (Has(0) && player.DashCharges > lowestCharges) Complete(1);
                break;
            case Lesson.Wallrun:
                if (player.WallSide != 0) Complete(0);
                if (Has(1) && player.IsGrounded && Current.finish.bounds.Contains(position)) Complete(2);
                break;
            case Lesson.Armory:
                CheckInventory();
                break;
            case Lesson.Range:
                if (cameraRig.IsAiming) Complete(0);
                if (!Current.target.IsAlive && !Has(1))
                {
                    Current.target.gameObject.SetActive(false);
                    Complete(1);
                }
                break;
            case Lesson.Blast:
                if (Has(0) && player.IsGrounded && Current.finish.bounds.Contains(position)) Complete(1);
                break;
            case Lesson.Extraction:
                if (!Current.timeOrb.activeSelf) Complete(0);
                if (Array.TrueForAll(Current.crystals, foe => foe == null || !foe.IsAlive)) Complete(1);
                if (Array.TrueForAll(Current.support, foe => foe == null || foe.health <= 0)) Complete(2);
                break;
        }
    }

    private float HorizontalSpeed() => Vector3.ProjectOnPlane(player.Velocity, Vector3.up).magnitude;

    private void CheckInventory()
    {
        if (weapons.WeaponCount == WeaponController.InventoryCapacity)
        {
            if (!Has(0))
            {
                Complete(0);
                return;
            }

            if (weapons.EquippedIndex >= 0)
                equippedSlots |= 1 << weapons.EquippedIndex;
            if (equippedSlots == (1 << WeaponController.InventoryCapacity) - 1)
                Complete(1);
            if (Has(2)) Complete(3);
        }
        else if (Has(1))
        {
            Complete(2);
        }
    }

    private void GroundJumped(bool automatic)
    {
        if (Current.lesson != Lesson.Jump) return;
        Complete(1);
        if (automatic) Complete(2);
    }

    private void AirJumped()
    {
        if (Current.lesson == Lesson.Jump) Complete(3);
    }

    private void WallJumped()
    {
        if (Current.lesson != Lesson.Wallrun) return;
        Complete(0);
        Complete(1);
    }

    private void BlastJumped()
    {
        if (Current.lesson == Lesson.Blast) Complete(0);
    }

    private void AmmoChanged(WeaponAmmoState ammo)
    {
        if (Current.lesson == Lesson.Range && lastAmmo.IsReloading && !ammo.IsReloading
            && ammo.Weapon == lastAmmo.Weapon && ammo.InMagazine > lastAmmo.InMagazine)
            Complete(2);
        lastAmmo = ammo;
    }

    private void Begin(int index)
    {
        stage = index;
        completed = equippedSlots = 0;
        lowestCharges = player.DashCharges;
        cleared = false;
        startPosition = player.transform.position;
        CheckpointManager.Instance.SetCheckpoint(Current.checkpoint.position, Current.checkpoint.rotation);
        if (Current.equipment != null)
            Current.equipment.SetActive(true);

        if (Current.lesson == Lesson.Extraction)
        {
            timer.drainRate = extractionDrainRate;
            timer.StartTimer();
        }

        var instruction = Instructions[(int)Current.lesson];
        heading.text = $"{index + 1}/{stations.Length}  {instruction.Title}";
        hint.text = instruction.Hint;
        dashStatus.style.display = Current.lesson == Lesson.Dash ? DisplayStyle.Flex : DisplayStyle.None;
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i].style.display = i < instruction.Tasks.Length ? DisplayStyle.Flex : DisplayStyle.None;
            rows[i].RemoveFromClassList("lesson-task--done");
            if (i < instruction.Tasks.Length) rows[i].text = $"○  {instruction.Tasks[i]}";
        }
    }

    private bool Has(int task) => (completed & (1 << task)) != 0;

    private void Complete(int task)
    {
        if (Has(task)) return;
        completed |= 1 << task;
        string[] tasks = Instructions[(int)Current.lesson].Tasks;
        rows[task].text = $"✓  {tasks[task]}";
        rows[task].AddToClassList("lesson-task--done");

        if (completed != (1 << tasks.Length) - 1) return;
        cleared = true;
        if (Current.lesson == Lesson.Extraction)
        {
            timer.StopTimer();
            timer.drainRate = 0f;
            Current.gate.SetActive(true);
            hint.text = "Training complete. Step through the portal to begin Level 1.";
        }
        else
        {
            Current.gate.SetActive(false);
            hint.text = "Gate open. Follow the cyan markers to the next checkpoint.";
        }
    }

    private void CreatePanel()
    {
        panel = new VisualElement { pickingMode = PickingMode.Ignore };
        panel.styleSheets.Add(lessonStyle);
        panel.AddToClassList("retro-frame");
        panel.AddToClassList("lesson-panel");

        var bar = new VisualElement { pickingMode = PickingMode.Ignore };
        bar.AddToClassList("retro-titlebar");
        heading = new Label { pickingMode = PickingMode.Ignore };
        heading.AddToClassList("retro-title");
        bar.Add(heading);
        panel.Add(bar);

        hint = AddLabel("lesson-hint");
        rows = new Label[4];
        for (int i = 0; i < rows.Length; i++)
            rows[i] = AddLabel("lesson-task");
        dashStatus = AddLabel("lesson-task");
        AddLabel("lesson-reset").text = "Backspace / View · Return to checkpoint    Esc · Pause";
        hud.rootVisualElement.Q("hud-root").Add(panel);
    }

    private Label AddLabel(string className)
    {
        var label = new Label { pickingMode = PickingMode.Ignore };
        label.AddToClassList(className);
        panel.Add(label);
        return label;
    }
}
