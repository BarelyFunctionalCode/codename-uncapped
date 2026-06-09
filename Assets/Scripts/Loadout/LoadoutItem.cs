using Unity.Netcode;
using UnityEngine;

public class LoadoutItem : NetworkBehaviour
{
    public Sprite iconSprite;

    [SerializeField] protected float cooldown = 0f;
    public float Cooldown { get => cooldown; protected set => cooldown = value; }
    public NetworkVariable<float> cooldownTimer = new();

    [SerializeField] protected int maxAmmo = 0;
    public int MaxAmmo { get => maxAmmo; protected set => maxAmmo = value; }
    public NetworkVariable<int> ammo = new();

    public NetworkVariable<bool> isEquiped = new();
}
