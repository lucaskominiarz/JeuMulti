using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager INSTANCE;

    [Header("Camera")]
    [SerializeField] private GameObject camera;
    [SerializeField] private Vector3 cameraOffset;

    [Header("Swap Settings")]
    [SerializeField] private float timeToSwap = 60f;
    private Coroutine swapCoroutine;
    
    [SerializeField] private TMP_Text endGameText;
    [SerializeField] private Vector3 posHider;
    [SerializeField] private Vector3 posSeeker;

    private void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
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
        if (!IsServer) return;
        Debug.Log("GameManager spawné côté client : " + IsClient + " IsServer : " + IsServer);
        if (swapCoroutine == null)
        {
            swapCoroutine = StartCoroutine(SwapCoroutine());
        }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoin;
    }

    private void OnClientJoin(ulong clientId)
    {
        StartCoroutine(WaitForPlayerSpawn(clientId));
    }

    private IEnumerator WaitForPlayerSpawn(ulong clientId)
    {
        NetworkObject playerObj = null;
        while (playerObj == null)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                playerObj = client.PlayerObject;
            yield return null;
        }
        var role = playerObj.GetComponent<PlayerRole>();
        var netTransform = playerObj.GetComponent<Unity.Netcode.Components.NetworkTransform>(); 
        var clients = new List<NetworkClient>(NetworkManager.Singleton.ConnectedClientsList);
        clients.Sort((a, b) => a.ClientId.CompareTo(b.ClientId));
        int index = clients.FindIndex(c => c.ClientId == clientId); 
        if (netTransform != null && netTransform.IsOwner)
        {
            netTransform.enabled = false; 
        }
        Vector3 targetPosition;
        if (index % 2 == 0)
        {
            role.currentRole.Value = PlayerRole.Role.Hider;
            targetPosition = posHider;
        }
        else
        {
            role.currentRole.Value = PlayerRole.Role.Seeker;
            targetPosition = posSeeker;
        }
        playerObj.transform.position = targetPosition; 
        yield return null; 
        yield return null; 
        if (netTransform != null && netTransform.IsOwner)
        {
            netTransform.enabled = true; 
        }
        Debug.Log($"Player {clientId} rejoint : rôle = {role.currentRole.Value}, position = {playerObj.transform.position}");
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





