using System;
using DG.Tweening;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private string message;
     
    
    private NetworkVariable<int> randomNumber = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<PlayerData> playerData = new(
        new PlayerData
        {
            life = 100, stunt = false
        }, NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner); 
    
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private bool test;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float fallSpeed = 10f;
    [SerializeField] private CameraScript cameraReference;
    [SerializeField] private float crouchSpeed = 8f;
    [SerializeField] private GameObject cubeCrouch;
    [SerializeField][Range(0f, 100f)] private float sprintMultiplier;
    [SerializeField] private float tweenDuration = 0.2f;
    
    private Transform cameraTransform;
    private bool canJump = true;
    private Vector3 direction;
    private bool sprinting = false;
    private bool crouching = false;
    private float baseMoveSpeed;
    
    private void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

    public override void OnNetworkSpawn()
    {
        
        /*randomNumber.OnValueChanged += (int previousValue, int newValue) =>
        {
            Debug.Log(OwnerClientId + " Random Number " + newValue);
        };*/
        playerData.OnValueChanged += (PlayerData previousValue, PlayerData newValue) =>
        {
            Debug.Log(OwnerClientId + " life " + newValue.life + " stunt " + newValue.stunt + " message " + newValue.message );
        };
        if (IsOwner)
        {
            cameraReference = GameManager.INSTANCE.CameraLookMe(transform);
            cameraTransform = cameraReference.transform;
        }
    }

    private void Update() 
    {
        if (!IsOwner)
        {
            return;
        }
        
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 camForward = cameraTransform.forward;
        Vector3 deplacement = verticalInput * camForward + horizontalInput * cameraTransform.right;
        Vector3 deplacementFinal = Vector3.ClampMagnitude(deplacement,1) * moveSpeed;
        
        if (Vector3.Distance(deplacementFinal, Vector3.zero)<= 5f) 
        {
            sprinting = false;
            cameraReference.ChangeFov(80f, tweenDuration);
        }
        
        if (Input.GetButtonDown("Sprint"))
        {
            sprinting = !sprinting;
            if (sprinting && !crouching)
            {
                cameraReference.ChangeFov(100f, tweenDuration);
            }
            else
            {
                cameraReference.ChangeFov(80f, tweenDuration);
            }
        }
        if (sprinting && !crouching)
        {
            moveSpeed = baseMoveSpeed + baseMoveSpeed * (sprintMultiplier / 100);
        }
        else if (!crouching)
        {
            moveSpeed = baseMoveSpeed;
        }
        
        
        if (Input.GetButtonDown("Jump") && canJump)
        {
            rb.linearVelocity = new Vector3(deplacementFinal.x, rb.linearVelocity.y +jumpForce, deplacementFinal.z);
            canJump = false;
        }
        else
        {
            rb.linearVelocity = new Vector3(deplacementFinal.x, rb.linearVelocity.y - fallSpeed * Time.deltaTime, deplacementFinal.z);
        }

        
        

        if (Input.GetButtonDown("Crouch"))
        {
            crouching = !crouching;
            if (crouching)
            {
                cubeCrouch.SetActive(false);
                DOTween.To(() => cameraReference.hauteur, x => cameraReference.hauteur = x, cameraReference.hauteur / 2, tweenDuration);
                moveSpeed = crouchSpeed;
                cameraReference.ChangeFov(80f, tweenDuration);
            }
            else
            {
                cubeCrouch.SetActive(true);
                DOTween.To(() => cameraReference.hauteur, x => cameraReference.hauteur = x, cameraReference.hauteur * 2, tweenDuration);
                moveSpeed = baseMoveSpeed;
            }
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            canJump = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            canJump = false;
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
        spawnedObjectTransform = Instantiate(prefab,transform.position + new Vector3(0,Random.Range(2f,8f),0), Quaternion.identity).transform;
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);
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