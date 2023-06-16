using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// The NetworkSpawner class is responsible for managing network-related operations such as player spawning and synchronization, as well as controlling game state across a networked session.
public class NetworkSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Singleton
    // Singleton implementation to ensure only one instance of NetworkSpawner exists.
    // This allows for global access to its methods and properties across other scripts.
    private static NetworkSpawner _instance;
    public static NetworkSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkSpawner>();
                if (_instance == null)
                {
                    var singletonObject = new GameObject("NetworkSpawner");
                    _instance = singletonObject.AddComponent<NetworkSpawner>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }

            return _instance;
        }
    }
    #endregion

    [SerializeField] private NetworkPrefabRef playerPrefab; // A reference to the prefab used to spawn new player entities.
    [SerializeField] private bool canPlaySolo; // This variable controls whether a single player can play the game alone, without the need for other players.

    private readonly Vector3 _spawnPosition = new Vector3(0, -4.45f, 0); // The position where players are spawned when they join the game.
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new(); // Collection mapping player references to their corresponding network objects, allowing for easy access and management of players in the game.
    private readonly List<Player> _playerList = new(); // List to hold the players currently in the game.

    // References to network components needed for managing the game over the network.
    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _networkSceneManager;

    // Properties to keep track of the game state.
    public bool Initialized { get; private set; }
    private bool _gameOver;
    private bool _canStart;
    private bool _networked;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        _runner = GetComponent<NetworkRunner>();
        _networkSceneManager = GetComponent<NetworkSceneManagerDefault>();
    }
    
    private void OnEnable()
    {
        LevelController.Instance.OnPlayerEliminated += OnPlayerEliminated;
    }
    private void OnDisable()
    {
        LevelController.Instance.OnPlayerEliminated -= OnPlayerEliminated;
    }

    // Method for handling GUI events, primarily for buttons related to starting, joining and restarting the game.
    private void OnGUI()
    {
        if (!_networked)
        {
            if (GUI.Button(new Rect(0, 0, 200, 80), "Host")) StartGame(GameMode.Host);
            if (GUI.Button(new Rect(0, 80, 200, 80), "Join")) StartGame(GameMode.Client);
        }

        if (_runner.IsServer)
        {
            if (_gameOver && GUI.Button(new Rect(0, 0, 200, 80), "Restart")) RestartGame();
            if (_canStart && GUI.Button(new Rect(0, 0, 200, 80), "Start")) Invoke(nameof(InvokeInitialized), 2);
        }
    }

    // This method starts a networked game in a specified mode (either as a Host or Client).
    private async void StartGame(GameMode mode)
    {
        _runner.ProvideInput = true;

        var startGameArgs = new StartGameArgs() { GameMode = mode, SessionName = "PlayRoom", Scene = SceneManager.GetActiveScene().buildIndex, SceneManager = _networkSceneManager };
        await _runner.StartGame(startGameArgs);
        _networked = true;
    }

    // This method restarts the game and resets each player's data.
    private void RestartGame()
    {
        _gameOver = false;

        foreach (var player in _playerList)
        {
            player.RPC_ResetPlayerData();
        }

        if (_runner.IsServer)
        {
            Invoke(nameof(InvokeInitialized), 2);
        }
    }

    // This method spawns a player character when a player joins the game.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, _spawnPosition, Quaternion.identity, player);
            _spawnedPlayers.Add(player, networkPlayerObject);
            var spawnedPlayer = networkPlayerObject.GetComponent<Player>();
            _playerList.Add(spawnedPlayer);

            spawnedPlayer.RPC_Init();
        }

        UpdateGameState();
    }

    // This method despawns a player character and removes them from the list when a player leaves the game.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject networkObject))
        {
            var spawnedPlayer = networkObject.GetComponent<Player>();
            _playerList.Remove(spawnedPlayer);
            runner.Despawn(networkObject);
            _spawnedPlayers.Remove(player);
        }

        UpdateGameState();
    }

    // This method handles input from the players and sends it over the network.
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.A)) data.Direction += Vector3.left;
        if (Input.GetKey(KeyCode.D)) data.Direction += Vector3.right;

        input.Set(data);
    }

    // Method to update the game state based on the number of spawned players.
    private void UpdateGameState()
    {
        if (canPlaySolo)
        {
            _canStart = true;
        }
        else
        {
            if (_spawnedPlayers.Count > 1)
            {
                if (Initialized)
                {
                    Initialized = false;
                    _gameOver = false;
                }

                _canStart = true;
            }
            else
            {
                _canStart = false;
            }
        }
    }

    // Method to handle player elimination.
    private void OnPlayerEliminated()
    {
        if (_runner.IsServer && GetActivePlayerCount() <= 1)
        {
            Initialized = false;
            _gameOver = true;
        }
    }
    
    // Method to mark the game as initialized.
    private void InvokeInitialized()
    {
        Initialized = true;
        _canStart = false;
    }
    
    // Method that counts the number of active players in the game.
    private int GetActivePlayerCount()
    {
        return _playerList.Count(pair => pair.body.gameObject.activeSelf);
    }

    // Methods for handling various network events. These methods are currently empty but may be filled in as needed.
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) {}
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) {}
    public void OnConnectedToServer(NetworkRunner runner) {}
    public void OnDisconnectedFromServer(NetworkRunner runner) {}
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) {}
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) {}
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) {}
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {}
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) {}
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) {}
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) {}
    public void OnSceneLoadDone(NetworkRunner runner) {}
    public void OnSceneLoadStart(NetworkRunner runner) {}
}
