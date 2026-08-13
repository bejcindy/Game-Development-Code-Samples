using Mirror;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SceneNetworkManager : MonoBehaviour
{
    public static SceneNetworkManager Instance { get; private set; }

    private void Awake()
    {
        // Implement singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // Destroy duplicate instances
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Check current scene on start
        CheckAndEnableNetworkObjects();
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene events when destroyed
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check and enable objects whenever a new scene is loaded
        CheckAndEnableNetworkObjects();
    }

    private void CheckAndEnableNetworkObjects()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // Enable network objects if:
        // 1. Scene starts with "LEVEL" AND
        // 2. Game mode is NOT online multiplayer
        if (GameLogic.Instance.GetGameMode() != GameMode.Online_Multiplayer)
        {
            EnableNetworkObjectsForSingleplayer();
        }
    }

    private void EnableNetworkObjectsForSingleplayer()
    {
        // Find all NetworkBehaviour objects in the scene
        NetworkBehaviour[] networkObjects = FindObjectsOfType<NetworkBehaviour>(true);

        foreach (NetworkBehaviour netObj in networkObjects)
        {
            // Skip the NetworkManager itself
            if (netObj.GetComponent<NetworkManager>() != null)
                continue;

            // Make sure the GameObject is enabled
            if (!netObj.gameObject.activeSelf)
                netObj.gameObject.SetActive(true);

            // Enable all components that might be disabled by Mirror
            foreach (Behaviour component in netObj.GetComponents<Behaviour>())
            {
                component.enabled = true;
            }
        }

        // Reinitialize object references for LEVEL scenes
        if (SceneManager.GetActiveScene().name.StartsWith("LEVEL"))
            ObjectFinder.Instance?.ReinitializeReferences();
    }
}