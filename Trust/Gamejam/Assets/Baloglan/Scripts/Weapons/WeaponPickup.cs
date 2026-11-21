using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(SphereCollider))]
public sealed class WeaponPickup : MonoBehaviour
{
    [SerializeField] WeaponDefinition definition;

    Rigidbody body;
    BoxCollider bodyCollider;
    SphereCollider pickupTrigger;
    WeaponItem item;
    WeaponController dropper;
    Collider dropperCollider;
    double dropperLockUntil;

    public WeaponDefinition Definition => definition;
    internal float HalfLength => GetComponent<BoxCollider>().size.z * 0.5f;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<BoxCollider>();
        pickupTrigger = GetComponent<SphereCollider>();
        if (definition != null)
            item = new WeaponItem(definition);
    }

    internal void InitializeDrop(WeaponItem droppedItem, WeaponController owner, Vector3 velocity)
    {
        item = droppedItem;
        definition = droppedItem.Definition;
        dropper = owner;
        dropperCollider = owner.GetComponent<Collider>();
        dropperLockUntil = Time.timeAsDouble + 0.35;
        if (dropperCollider != null && dropperCollider.enabled)
            Physics.IgnoreCollision(bodyCollider, dropperCollider);

        body.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        if (dropper == null || Time.timeAsDouble < dropperLockUntil)
            return;

        if (dropperCollider == null || !pickupTrigger.bounds.Intersects(dropperCollider.bounds))
        {
            if (dropperCollider != null && dropperCollider.enabled && dropperCollider.gameObject.activeInHierarchy)
                Physics.IgnoreCollision(bodyCollider, dropperCollider, false);

            dropper = null;
            dropperCollider = null;
        }
    }

    void OnTriggerEnter(Collider other) => TryCollect(other);
    void OnTriggerStay(Collider other) => TryCollect(other);

    void TryCollect(Collider other)
    {
        if (item == null)
            return;

        WeaponController player = other.GetComponentInParent<WeaponController>();
        if (player != null && player != dropper)
            player.TryPickUp(this);
    }

    internal WeaponItem Take()
    {
        if (item == null)
            return null;

        WeaponItem result = item;
        item = null;
        gameObject.SetActive(false);
        Destroy(gameObject);
        return result;
    }
}
