using Mirror;
// using Mirror.Examples.Benchmark;
using System.Collections;
using System.Collections.Generic;
using THP.Network;
using UnityEngine;

public class MultiplayerPlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefabTypeOne;
    [SerializeField] private GameObject playerPrefabTypeTwo;

    public override void OnStartServer()
    {
        base.OnStartServer();
        THF_NetworkManager.OnServerReadied += SpawnPlayer;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        THF_NetworkManager.OnServerReadied -= SpawnPlayer;
    }

    private void OnDestroy()
    {
        THF_NetworkManager.OnServerReadied -= SpawnPlayer;
    }

    [Server]
    public void SpawnPlayer(NetworkConnection conn)
    {
        // Get the Network_LobbyPlayer for this connection
        var lobbyPlayer = conn.identity != null
            ? conn.identity.GetComponent<Network_LobbyPlayer>()
            : null;

        GameObject playerInstance = null;
        PlayerMovement playerMovement = null;

        if (lobbyPlayer != null)
        {
            // Use the playerNumber to decide which prefab to spawn
            switch (lobbyPlayer.playerNumber)
            {
                case PlayerNumber.Player1:
                    playerInstance = Instantiate(playerPrefabTypeOne, Vector3.zero, Quaternion.identity);
                    break;
                case PlayerNumber.Player2:
                    playerInstance = Instantiate(playerPrefabTypeTwo, Vector3.zero, Quaternion.identity);
                    break;
                default:
                    // Fallback or error handling
                    playerInstance = Instantiate(playerPrefabTypeOne, Vector3.zero, Quaternion.identity);
                    break;
            }
        }
        else
        {
            // Fallback if lobbyPlayer is not found
            playerInstance = Instantiate(playerPrefabTypeOne, Vector3.zero, Quaternion.identity);
        }

        if (playerInstance != null)
        {
            // Assign to networkPlayerOne or networkPlayerTwo
            playerMovement = playerInstance.GetComponent<PlayerMovement>();
            playerMovement.SetPlayerNumber(lobbyPlayer.playerNumber);
            var thfManager = NetworkManager.singleton as THF_NetworkManager;
            if (playerMovement != null && thfManager != null)
            {
                if (lobbyPlayer != null)
                {
                    if (lobbyPlayer.playerNumber == PlayerNumber.Player1)
                        thfManager.networkPlayerOne = playerMovement;
                    else if (lobbyPlayer.playerNumber == PlayerNumber.Player2)
                        thfManager.networkPlayerTwo = playerMovement;
                }
            }

            NetworkServer.Spawn(playerInstance, conn.identity.gameObject);

        }

    }
}
