using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Rendering;

/// <summary>
/// Фаза старта игры
/// </summary>
public class StartGame : NetworkBehaviour
{
    [SerializeField] private float durationLoad = 3f;

    [SerializeField] private CreateWorld createWorld;
    [SerializeField] private SubmarineMain submarineMain;

    [SerializeField] private LocalizeStringEvent depthValue;
    [SerializeField] private EventManager beforeStartGame;
    [SerializeField] private EventManager afterStartGame;

    
    public void StartCurrentGame(int indexId)
    {
        createWorld.CreateWorldOnServer(indexId);
        StartForClientRpc();
    }



    [ClientRpc]
    private void StartForClientRpc()
    {
        submarineMain.StartDropSubmarine();

        StartCoroutine(StartGameCoroutine());
    }


    private IEnumerator StartGameCoroutine()
    {
        beforeStartGame.StartEventManager();

        yield return new WaitForSeconds(durationLoad);

        string depth = createWorld.GetDepth();
        depthValue.StringReference.Arguments = new object[] { depth };
        depthValue.RefreshString();

        submarineMain.EndDropSubmarine();

        afterStartGame.StartEventManager();
    }
}
