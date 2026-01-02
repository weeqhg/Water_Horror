using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject authorPanel;

    [Header("Join Room References")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button joinButton;

    [Header("Lobby References")]
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private Button settingBackButton;

    [Header("Scene Names")]
    //[SerializeField] private string menuSceneName = "MenuScene";
    [SerializeField] private string gameSceneName = "GameScene";

    private string currentRoomCode;
    private bool isAttemptingConnection = false;
    private RelayManager relayManager;

    private void Start()
    {
        relayManager = FindObjectOfType<RelayManager>();
        if (relayManager == null)
        {
            GameObject obj = new GameObject("RelayManager");
            relayManager = obj.AddComponent<RelayManager>();
        }

        ShowMainMenu();

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        joinButton.onClick.AddListener(JoinRoom);
        startGameButton.onClick.AddListener(StartGame);
        copyCodeButton.onClick.AddListener(CopyRoomCode);
        settingBackButton.onClick.AddListener(ShowMainMenu);

        isAttemptingConnection = false;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    #region UI Navigation
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        joinRoomPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingPanel.SetActive(false);
        authorPanel.SetActive(false);
    }

    public void ShowCreateRoom()
    {
        mainMenuPanel.SetActive(false);
    }

    public void ShowJoinRoom()
    {
        mainMenuPanel.SetActive(false);
        settingPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
        authorPanel.SetActive(false);
    }

    public void ShowLobby()
    {
        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        settingPanel.SetActive(false);
        authorPanel.SetActive(false);
    }

    public void ShowSetting()
    {
        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingPanel.SetActive(true);
        authorPanel.SetActive(false);
    }

    public void ShowAuthor()
    {
        authorPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        settingPanel.SetActive(false);
    }
    #endregion

    

    #region Network Methods
    public async void CreateRoom()
    {
        if (isAttemptingConnection) return;

        isAttemptingConnection = true;

        try
        {
            // Создаем Relay и получаем код комнаты
            string relayCode = await relayManager.CreateRelay();

            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogError("Failed to create relay");
                isAttemptingConnection = false;
                return;
            }

            currentRoomCode = relayCode;

            // Запускаем хост
            if (NetworkManager.Singleton.StartHost())
            {
                UpdateLobbyUI();
                ShowLobby();
                Debug.Log($"Room created with code: {currentRoomCode}");
            }
            else
            {
                Debug.LogError("Failed to start host!");
                isAttemptingConnection = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating room: {e.Message}");
            isAttemptingConnection = false;
        }
    }

    public async void JoinRoom()
    {
        if (isAttemptingConnection) return;

        if (joinCodeInput.text == "") return;

        string code = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("Room code cannot be empty!");
            return;
        }

        isAttemptingConnection = true;

        try
        {
            // Подключаемся к Relay
            bool joinSuccess = await relayManager.JoinRelay(code);

            if (!joinSuccess)
            {
                Debug.LogError("Failed to join relay");
                isAttemptingConnection = false;
                return;
            }

            currentRoomCode = code;

            // Запускаем клиент
            if (NetworkManager.Singleton.StartClient())
            {
                ShowLobby();
                UpdateLobbyUI();
                Debug.Log($"Joining room with code: {currentRoomCode}");
            }
            else
            {
                Debug.LogError("Failed to start client!");
                isAttemptingConnection = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error joining room: {e.Message}");
            isAttemptingConnection = false;
        }
    }

    public void LeaveLobby()
    {
        if (IsNetworkManagerRunning())
        {
            NetworkManager.Singleton.Shutdown();
        }

        isAttemptingConnection = false;
        ShowMainMenu();
    }

    public void StartGame()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private bool IsNetworkManagerRunning()
    {
        return NetworkManager.Singleton != null &&
               (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient);
    }
    #endregion

    #region UI Updates
    private void UpdateLobbyUI()
    {
        lobbyCodeText.text = $"Room Code: {currentRoomCode}";
        UpdatePlayersCount();

        startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
    }

    private void UpdatePlayersCount()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            playersCountText.text = $"Players: {playerCount}/4";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            playersCountText.text = "Players: Connected";
        }
        else
        {
            playersCountText.text = "Players: 0/4";
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        isAttemptingConnection = false;

        if (NetworkManager.Singleton.IsServer)
        {
            UpdatePlayersCount();
        }
        else
        {
            UpdatePlayersCount();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        isAttemptingConnection = false;

        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                UpdatePlayersCount();
            }
            else
            {
                Debug.Log("Disconnected from host");
                NetworkManager.Singleton.Shutdown();
                ShowMainMenu();
            }
        }
        else
        {
            ShowMainMenu();
        }
    }

    private void CopyRoomCode()
    {
        if (!string.IsNullOrEmpty(currentRoomCode))
        {
            GUIUtility.systemCopyBuffer = currentRoomCode;
            Debug.Log("Room code copied to clipboard: " + currentRoomCode);

            // Можно показать всплывающее сообщение
            // ShowNotification("Room code copied!");
        }
    }
    #endregion
}