using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerHealth))]
public class Player : MonoBehaviour
{
    public Transform hand;
    public GameObject equipedItem;

    public PlayerController Controller { get; private set; }
    public PlayerCombat Combat { get; private set; }
    public PlayerHealth Health { get; private set; }

    void Awake()
    {
        Controller = GetComponent<PlayerController>();
        Combat = GetComponent<PlayerCombat>();
        Health = GetComponent<PlayerHealth>();

        if (Combat != null && hand != null)
        {
            Combat.handTransform = hand;
        }
    }
}
