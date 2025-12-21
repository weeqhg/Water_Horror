using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
public class RelayManager : NetworkBehaviour
{
    [SerializeField] private int maxPlayers = 4;
    
    private string joinCode;
    
    private async void Start()
    {
        await InitializeServices();
    }
    
    private async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously: " + AuthenticationService.Instance.PlayerId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize services: {e.Message}");
        }
    }
    
    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            
            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            Debug.Log($"Relay created with join code: {joinCode}");
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to create relay: {e.Message}");
            return null;
        }
    }
    
    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            
            Debug.Log($"Joined relay with code: {joinCode}");
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Failed to join relay: {e.Message}");
            return false;
        }
    }
    
    public string GetCurrentJoinCode()
    {
        return joinCode;
    }
}