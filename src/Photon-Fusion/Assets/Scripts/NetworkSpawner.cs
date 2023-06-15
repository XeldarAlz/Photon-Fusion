using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// The NetworkSpawner is responsible for managing network operations for the game including player spawning and game state management.
public class NetworkSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    #region Singleton
    // Singleton pattern: instance field and property.
    private static NetworkSpawner _instance;
    public static NetworkSpawner Instance
    {
        get
        {
            // Check if the instance is null.
            if (_instance == null)
            {
                // Try to find an existing LevelController object in the scene.
                _instance = FindObjectOfType<NetworkSpawner>();

                // If no LevelController object exists, create a new one.
                if (_instance == null)
                {
                    var singletonObject = new GameObject("NetworkSpawner");
                    _instance = singletonObject.AddComponent<NetworkSpawner>();
                }

                // Ensure the singleton object persists across scenes.
                DontDestroyOnLoad(_instance.gameObject);
            }

            // Return the instance.
            return _instance;
        }
    }
    #endregion

    [Networked] private int RestartCall { get; set; }
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private bool canPlaySolo;

    private readonly Vector3 _spawnPosition = new Vector3(0, -4.45f, 0);
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _networkSceneManager;
    
    public bool Initialized { get; private set; }
    private bool _gameOver;
    private bool _canStart;
    private bool _networked;
    private int _restartRequest;

    private int GetActivePlayerObjects()
    {
        return _spawnedPlayers.Count(pair => pair.Value.gameObject.activeSelf);
    }
    
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

    // Event handler for player elimination
    private void OnPlayerEliminated()
    {
        var activePlayerCount = GetActivePlayerObjects();
        
        if (activePlayerCount == 1)
        {
            Initialized = false;
            _gameOver = true;
        }
    }
    
    // Display GUI elements
    private void OnGUI()
    {
        // Display host/join buttons if the game isn't networked
        if (!_networked)
        {
            if (GUI.Button(new Rect(0, 0, 200, 80), "Host"))
            {
                StartGame(GameMode.Host);
            }

            if (GUI.Button(new Rect(0, 80, 200, 80), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }

        // Display start/restart buttons if the runner is a server and the game can be started or restarted
        if (_runner.IsServer)
        {
            if (_canStart)
            {
                if (GUI.Button(new Rect(0, 0, 200, 80), "Start"))
                {
                    InitializeGame(2);
                }
            }
        }
        
        if (_gameOver)
        {
            if (GUI.Button(new Rect(0, 0, 200, 80), "Restart"))
            {
                RestartRequestCall();
            }
        }
    }

    // Start the game in a given mode
    private async void StartGame(GameMode mode)
    {
        _runner.ProvideInput = true;

        // Create start game arguments
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "PlayRoom",
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = _networkSceneManager
        };

        // Start the game
        await _runner.StartGame(startGameArgs);
        _networked = true;
    }

    private void RestartRequestCall()
    {
        _gameOver = false;

        if (_runner.IsServer)
        {
            RestartCall++;

            if (RestartCall == _runner.ActivePlayers.Count())
            {
                RestartCall = 0;
                RestartGame();
            }
        }
    }
    
    // Restart the game
    private void RestartGame()
    {
        foreach (var player in _spawnedPlayers)
        {
            if (!player.Value.isActiveAndEnabled)
            {
                player.Value.gameObject.SetActive(true);
            }
        }
        _gameOver = false;

        if (_runner.IsServer)
        {
           InitializeGame(2);
        }
    }

    // Initialize the game after a certain delay
    private void InitializeGame(int delay)
    {
        _canStart = false;
        Invoke(nameof(InvokeInitialized), delay);
    }
    
    // When a player joins the game, spawn a character for them
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (_runner.IsServer)
        {
            // Spawn player and add to the list of spawned characters
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, _spawnPosition, Quaternion.identity, player);
            _spawnedPlayers.Add(player, networkPlayerObject);
          
            // Initialize the spawned network player object.
            networkPlayerObject.GetComponent<Player>().RPC_Init();
        }
        
        UpdateGameState();
    }
    
    // When a player leaves the game, despawn their character and remove from the list of spawned characters
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedPlayers.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedPlayers.Remove(player);
        }
        
        UpdateGameState();
    }

    // Handle input from the players
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Create a new input data
        var data = new NetworkInputData();

        // Set the direction based on player input
        if (Input.GetKey(KeyCode.A))
        {
            data.Direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            data.Direction += Vector3.right;
        }

        // Set the input data
        input.Set(data);
    }

    // Updates the game state based on the number of spawned players.
    private void UpdateGameState()
    {
        // Check if playing solo is allowed.
        if (canPlaySolo)
        {
            // In solo play mode, set the canStart flag to true since solo play can always start.
            _canStart = true;
        }
        else
        {
            // Check if there are more than one spawned players.
            if (_spawnedPlayers.Count > 1)
            {
                // Check if the game has been initialized.
                if (Initialized)
                {
                    // Reset the initialization flag and game over flag.    
                    Initialized = false;
                    _gameOver = false;
                }

                // Set the canStart flag to true, indicating the game can be started.
                _canStart = true;
            }
            else
            {
                // Set the canStart flag to false since there is not enough players to start the game.
                _canStart = false;
            }
        }
    }
    
    // Mark the game as initialized
    private void InvokeInitialized()
    {
        Initialized = true;
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}