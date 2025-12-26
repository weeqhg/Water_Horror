using SimpleVoiceChat;
using Unity.Netcode;
using UnityEngine;

public class NetworkVoicePlayer : NetworkBehaviour
{
    [SerializeField] private Speaker localSpeaker;

    void Start()
    {
        // Подписываемся на событие отправки аудио 
        Recorder.OnSendDataToNetwork += SendVoiceToServer;

        RegisterInGame();
    }

    private void RegisterInGame()
    {
        if (VoiceSettingManager.Instance == null) return;
        ulong myOwnerId = OwnerClientId;
        Debug.Log($"I am owned by client: {myOwnerId}");
        if (NetworkManager.Singleton.LocalClientId == myOwnerId)
            return;
        VoiceSettingManager.Instance.Register(OwnerClientId, localSpeaker);
    }

    // Отправка голоса на сервер
    private void SendVoiceToServer(byte[] voiceData)
    {
        if (IsClient && IsOwner)
        {
            SendVoiceDataServerRpc(voiceData);
        }
    }

    // RPC для отправки данных на сервер
    [ServerRpc(RequireOwnership = false)]
    private void SendVoiceDataServerRpc(byte[] voiceData, ServerRpcParams rpcParams = default)
    {
        // Рассылаем всем клиентам, кроме отправителя
        ulong senderId = rpcParams.Receive.SenderClientId;
        ReceiveVoiceDataClientRpc(voiceData, senderId);
    }

    // RPC для получения данных всеми клиентами
    [ClientRpc]
    private void ReceiveVoiceDataClientRpc(byte[] voiceData, ulong senderId)
    {
        // Не воспроизводим свой собственный голос
        if (NetworkManager.Singleton.LocalClientId == senderId)
            return;

        // Передаем данные в Speaker
        localSpeaker.ProcessVoiceData(voiceData);
    }

    public override void OnNetworkDespawn()
    {
        Recorder.OnSendDataToNetwork -= SendVoiceToServer;
    }
}
