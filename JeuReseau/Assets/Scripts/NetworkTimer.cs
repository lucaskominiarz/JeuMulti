using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    [SerializeField] private float timeToSwap = 60f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float baseValue = 240f;

    private NetworkVariable<float> timerValue = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Coroutine timerCoroutine;

    public override void OnNetworkSpawn()
    {
        timerValue.OnValueChanged += (_, newValue) =>
        {
            UpdateUI(newValue);
        };

        if (IsServer)
        {
            timerValue.Value = baseValue;
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }
    }

    IEnumerator TimerCoroutine()
    {
        while (timerValue.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            timerValue.Value -= 1f;

            if (Mathf.Abs(timerValue.Value % timeToSwap) < 0.1f)
            {
                GameManager.INSTANCE.SwapRolesServerRpc();
            }
        }
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                client.PlayerObject.GetComponent<PlayerNetwork>().HidersWinClientRpc();
                break;
            }
        }
        

    }

    private void UpdateUI(float value)
    {
        timerText.text = $"{value:0}";
    }
}