using UnityEngine;

/// <summary>
/// Continuously rotates this transform so that child "BirdSlot" objects parented to it
/// orbit around its origin together. Bird visuals sit at the slots and inherit motion.
/// One shared clock = no timing/sync problems between multiple birds.
/// </summary>
public class BirdOrbitRig : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 30f;
    [SerializeField] private Vector3 axis = Vector3.up;

    void Update()
    {
        transform.Rotate(axis.normalized, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
