using Unity.Netcode;
using UnityEngine;

public class PlayerRole : NetworkBehaviour
{
    public enum Role
    {
        Hider,
        Seeker
    }

    public NetworkVariable<Role> currentRole = new(
        Role.Hider,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    
    private Renderer rend;

    private void Start()
    {
        rend = GetComponentInChildren<Renderer>();

        currentRole.OnValueChanged += (_, newValue) =>
        {
            UpdateColor(newValue);
        };
    }

    void UpdateColor(Role r)
    {
        if (r == Role.Seeker) rend.material.color = Color.red;
        else rend.material.color = Color.blue;
    }
}