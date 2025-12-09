using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager INSTANCE;

    [Header("Camera")]
    [SerializeField] private GameObject camera;
    [SerializeField] private Vector3 cameraOffset;

    [Header("Swap Settings")]
    [SerializeField] private float timeToSwap = 60f;
    private Coroutine swapCoroutine;

    private void Awake()
    {
        if (INSTANCE != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            INSTANCE = this;
            DontDestroyOnLoad(this);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            swapCoroutine = StartCoroutine(SwapCoroutine());
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoin;
        }
    }

    private void OnClientJoin(ulong clientId)
    {
        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        PlayerRole role = playerObj.GetComponent<PlayerRole>();

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCount % 2 == 1)
            role.currentRole.Value = PlayerRole.Role.Hider;
        else
            role.currentRole.Value = PlayerRole.Role.Seeker;

        Debug.Log($"Player {clientId} rejoint : role = {role.currentRole.Value}");
    }

    public CameraScript CameraLookMe(Transform playerTransform)
    {
        GameObject camObject = Instantiate(camera, playerTransform);
        CameraScript newCamera = camObject.GetComponent<CameraScript>();
        newCamera.cible = playerTransform;
        return newCamera;
    }
    public void CursorLock()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator SwapCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(timeToSwap);
            SwapRolesServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    public void SwapRolesServerRpc()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerRole role = client.PlayerObject.GetComponent<PlayerRole>();

            if (role.currentRole.Value == PlayerRole.Role.Hider)
                role.currentRole.Value = PlayerRole.Role.Seeker;
            else
                role.currentRole.Value = PlayerRole.Role.Hider;
        }

        Debug.Log("SWAP");
    }
}
