using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// The NetworkSpawner is responsible for managing network operations for the game including player spawning and game state management.
public class NetworkSpawner : NetworkBehaviour, INetworkRunnerCallbacks
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
    
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private List<Color> playerColorList;
   
    private readonly Vector3 _spawnPosition = new Vector3(0, -4.45f, 0);

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner _runner;
    private NetworkSceneManagerDefault _networkSceneManager;
    
    public bool initialized;
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
        LevelController.Instance.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        LevelController.Instance.OnGameOver -= OnGameOver;
    }

    // Event handler for when the game is over
    private void OnGameOver()
    {
        initialized = false;
        _gameOver = true;
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
            
            if (_gameOver)
            {
                if (GUI.Button(new Rect(0, 0, 200, 80), "Restart"))
                {
                    RestartGame();
                }
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

    // Restart the game
    private void RestartGame()
    {
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
        if (runner.IsServer)
        {
            // Spawn player and add to the list of spawned characters
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, _spawnPosition, Quaternion.identity, player);
            _spawnedCharacters.Add(player, networkPlayerObject);
            
            // Initialize the spawned network player object.
            networkPlayerObject.GetComponent<Player>().RPC_Init();

            // If there are more than one players, game can start
            if (_spawnedCharacters.Count > 1)
            {
                if (initialized)
                {
                    initialized = false;
                    _gameOver = false;
                }

                _canStart = true;
            }
        }
    }

    // Mark the game as initialized
    private void InvokeInitialized()
    {
        initialized = true;
    }

    // When a player leaves the game, despawn their character and remove from the list of spawned characters
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
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