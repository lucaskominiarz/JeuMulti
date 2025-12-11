using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private string message;


    private NetworkVariable<int> randomNumber =
        new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<PlayerData> playerData = new(
        new PlayerData
        {
            life = 100, stunt = false
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private bool test;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private CameraScript cameraReference;
    [SerializeField] [Range(0f, 100f)] private float sprintMultiplier;
    [SerializeField] private float tweenDuration = 0.2f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField]private float wallCheckDistance = 0.5f;
    [SerializeField]float horizontalPush = 7f;
    [SerializeField] float verticalPush = 8f;
    [SerializeField] private TMP_Text endGameText;
    [SerializeField] private TMP_Text roleText;

    
    private Vector3 wallNormal;
    private Transform cameraTransform;
    private bool canJump = true;
    private Vector3 direction;
    private bool sprinting = false;
    private float baseMoveSpeed;
    private bool isTouchingRightWall = false;
    private bool isTouchingLeftWall = false;
    private int currentTiltDirection = 0;

    public NetworkVariable<bool> isEliminated = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);


    private void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

    IEnumerator CheckRole()
    {
        while(true)
        {
            yield return new WaitForSeconds(1f);
            PlayerRole myRole = GetComponent<PlayerRole>();
            roleText.text = myRole.currentRole.Value == PlayerRole.Role.Hider ? "Hider" : "Seeker";
            roleText.color = myRole.currentRole.Value == PlayerRole.Role.Hider ? Color.blue : Color.red;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            StartCoroutine(CheckRole());
            cameraReference = GameManager.INSTANCE.CameraLookMe(transform);
            cameraTransform = cameraReference.transform;
            isEliminated.OnValueChanged += (oldValue, newValue) =>
            {
                if (newValue)
                {
                    rb.isKinematic = true;
                }
            };
        }
    }


    private void Update()
    {
        if (!IsOwner || isEliminated.Value)
            return;

        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 camForward = cameraTransform.forward;
        Vector3 deplacement = verticalInput * camForward + horizontalInput * cameraTransform.right;
        Vector3 deplacementFinal = Vector3.ClampMagnitude(deplacement, 1) * moveSpeed;
        transform.rotation = new Quaternion(transform.rotation.x, Quaternion.LookRotation(camForward).y,transform.rotation.z, transform.rotation.w);

        if (Vector3.Distance(deplacementFinal, Vector3.zero) <= 5f)
        {
            sprinting = false;
            cameraReference.ChangeFov(80f, tweenDuration);
        }

        if (Input.GetButtonDown("Sprint"))
        {
            sprinting = !sprinting;
            if (sprinting)
            {
                cameraReference.ChangeFov(100f, tweenDuration);
            }
            else
            {
                cameraReference.ChangeFov(80f, tweenDuration);
            }
        }

        if (sprinting)
        {
            moveSpeed = baseMoveSpeed + baseMoveSpeed * (sprintMultiplier / 100);
        }
        else
        {
            moveSpeed = baseMoveSpeed;
        }
        
        CheckWall();
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float radius = 0.3f;
        canJump = Physics.SphereCast(origin, radius, Vector3.down, out _, groundCheckDistance, groundLayer);
        if (Input.GetButtonDown("Jump"))
        {
            if (canJump)
            {
                rb.linearVelocity = new Vector3(deplacementFinal.x, rb.linearVelocity.y + jumpForce, deplacementFinal.z);
            }
            else if (isTouchingLeftWall)
            {
                rb.linearVelocity = new Vector3(horizontalPush, verticalPush, rb.linearVelocity.z);
            }
            else if (isTouchingRightWall)
            {
                
                rb.linearVelocity = new Vector3(-horizontalPush, verticalPush, rb.linearVelocity.z);
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(deplacementFinal.x, rb.linearVelocity.y - fallSpeed * Time.deltaTime,
                deplacementFinal.z);
        }
        

        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //randomNumber.Value = Random.Range(0, 100);
            playerData.Value = new PlayerData()
            {
                life = Random.Range(0, 100),
                stunt = playerData.Value.stunt,
                message = "Praise the sun"
            };
            //TestRpc(new RpcParams());
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            DestroyObjectRpc();
        }
        */

    }

    private void CheckWall()
    {
        isTouchingLeftWall = Physics.Raycast(transform.position, -transform.right, wallCheckDistance, groundLayer);
        isTouchingRightWall = Physics.Raycast(transform.position, transform.right, wallCheckDistance, groundLayer);
        
        if (isTouchingLeftWall)
        {
            cameraReference.TiltCamera(-1);
        }
        else if (isTouchingRightWall)
        {
            cameraReference.TiltCamera(1);
        }
        else
        {
            cameraReference.TiltCamera(0);
        }
        
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isEliminated.Value) return;
        if (!collision.gameObject.CompareTag("Player")) return;
        if (!collision.gameObject.TryGetComponent<PlayerRole>(out var otherRole)) return;
        PlayerRole myRole = GetComponent<PlayerRole>();
        if (myRole.currentRole.Value == PlayerRole.Role.Seeker &&
            otherRole.currentRole.Value == PlayerRole.Role.Hider)
        {

            EliminatePlayerServerRpc(collision.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    private Transform spawnedObjectTransform;

    [Rpc(SendTo.Server)]
    void DestroyObjectRpc()
    {
        spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(true);
    }

    [Rpc(SendTo.Server)]
    void TestRpc(RpcParams rpcParams)
    {
        spawnedObjectTransform = Instantiate(prefab, transform.position + new Vector3(0, Random.Range(2f, 8f), 0),
            Quaternion.identity).transform;
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EliminatePlayerServerRpc(ulong targetId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj))
            return;
        PlayerNetwork targetPlayer = targetObj.GetComponent<PlayerNetwork>();
        if (targetPlayer.isEliminated.Value) return;
        targetPlayer.isEliminated.Value = true;
        DisablePlayerForEveryoneClientRpc(targetObj.GetComponent<NetworkObject>().NetworkObjectId);
        CheckVictory();
    }

    [ClientRpc]
    private void DisablePlayerForEveryoneClientRpc(ulong targetId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out var targetObj))
            return;

        foreach (var col in targetObj.GetComponentsInChildren<Collider>())
            col.enabled = false;

        foreach (var rend in targetObj.GetComponentsInChildren<Renderer>())
            rend.enabled = false;

        if (targetObj.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;
    }
    
    public void CheckVictory()
    {
        if (!IsServer) return;

        bool hidersAlive = false;
        bool seekersAlive = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (!client.PlayerObject) continue;

            var playerNet = client.PlayerObject.GetComponent<PlayerNetwork>();
            var role = client.PlayerObject.GetComponent<PlayerRole>();

            if (playerNet == null || role == null) continue;
            if (playerNet.isEliminated.Value) continue;

            if (role.currentRole.Value == PlayerRole.Role.Hider) hidersAlive = true;
            else if (role.currentRole.Value == PlayerRole.Role.Seeker) seekersAlive = true;
        }
        if (!hidersAlive || !seekersAlive)
        {
            ReloadAllClientsClientRpc();
        }
    }

    [ClientRpc]
    private void ReloadAllClientsClientRpc()
    {
        StartCoroutine(ReloadWithTextCoroutine());
    }

    private IEnumerator ReloadWithTextCoroutine()
    {
        if (endGameText != null)
        {
            endGameText.gameObject.SetActive(true);
            endGameText.text = "Les seekers gagnent";
        }
        yield return new WaitForSeconds(3f);
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    [ClientRpc]
    public void HidersWinClientRpc()
    {
        StartCoroutine(HidersWinCoroutine());
    }

    private IEnumerator HidersWinCoroutine()
    {
        if (endGameText != null)
        {
            endGameText.gameObject.SetActive(true);
            endGameText.text = "Les Hiders gagnent";
        }
        
        yield return new WaitForSeconds(3f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        if (IsServer)
        {
            yield return new WaitForSeconds(0.5f);
            NetworkManager.Singleton.Shutdown();
        }
    }
    


}

public struct PlayerData : INetworkSerializable
{
    public int life;
    public bool stunt;
    public FixedString128Bytes message;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref life);
        serializer.SerializeValue(ref stunt);
        serializer.SerializeValue(ref message);
    }
}