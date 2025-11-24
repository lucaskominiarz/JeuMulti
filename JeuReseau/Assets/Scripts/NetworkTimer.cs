using System;
using System.Collections;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkTimer : NetworkBehaviour
{
    [SerializeField] private float timeToSwap = 60f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float updateSpeed = 0.1f;
    [SerializeField] private float baseValue = 240f;

    private float actualTimerValue = 0f;
    private bool timerIsActive = true;
    private Coroutine timerCoroutine;

    private void Awake()
    {
        actualTimerValue = baseValue;
        RestartTimer();
    }

    IEnumerator TimerCoroutine()
    {
        while (timerIsActive)
        {
            yield return new WaitForSecondsRealtime(updateSpeed);
            actualTimerValue -= updateSpeed;
            actualTimerValue = (float)Math.Round(actualTimerValue,2);
            UpdateUi();
        }
    }

    private void UpdateUi()
    {
        string newText = actualTimerValue.ToString();
        if (actualTimerValue % 1 == 0 )
        {
            newText += ",0";
        }
        timerText.text = newText;
    }

    #region Timer Functions

    public void StopTimer()
    {
        StopCoroutine(timerCoroutine);
    }

    public void RestartTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            
        }
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    public void ResetTimer()
    {
        actualTimerValue = 0f;
    }
    
    #endregion

}
