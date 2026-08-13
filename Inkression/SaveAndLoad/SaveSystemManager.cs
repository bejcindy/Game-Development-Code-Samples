using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

public class SaveSystemManager : MonoBehaviour
{
    //[Header("File Storage Data Config")]
    public static string fileName = "save.test";
    public static bool forceNewGame;
    public static bool isNewGame;
    public static bool manualSave;
    public bool checkIsNewGame;
    public bool newGame;
    public bool demoSave;

    GameData gameData;
    List<ISaveSystem> saveSystemObjs;
    FileDataHandler dataHandler;

    public static SaveSystemManager instance { get; private set; }

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
    }

    private void Update()
    {
        checkIsNewGame = isNewGame;
    }

    private void OnApplicationQuit()
    {
        //SaveGame();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        saveSystemObjs = FindAllSaveSystemObjs();

#if UNITY_EDITOR
        if(newGame)
        {
            NewGame();            
            isNewGame = true;
            newGame = false;            
        }
        else if (demoSave)
        {
            DemoLoad();
        }
        //if (newGame || forceNewGame)
        //{
        //    NewGame();
        //    forceNewGame = false;
        //    isNewGame = true;
        //}
        //else
        //{
        //    LoadGame();
        //    isNewGame = false;
        //}

#endif

#if UNITY_STANDALONE && !UNITY_EDITOR
        if (demoSave)
        {
            DemoLoad();
        }
        //if(!forceNewGame)
        //    LoadGame();
        //else
        //{
        //    NewGame();
        //    forceNewGame = false;
        //    isNewGame = true;
        //}
#endif
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void NewGame()
    {
        gameData = new GameData();
        Debug.Log("New Game");
    }

    public void LoadGame()
    {
        //Load any saved data from a file using the data handler
        gameData = dataHandler.Load();
        Debug.Log("Load Game");
        //If there is no data to be loaded, initialize a new game
        if (gameData == null)
        {
            Debug.Log("No saves found, initializing a new game.");
            NewGame();
            isNewGame = true;
        }
        else
        {
            isNewGame = false;
        }
        //Push the loaded data to corresponding scripts
        foreach (ISaveSystem saveSystemObj in saveSystemObjs)
        {
            saveSystemObj.LoadData(gameData);
        }
        //Debug.Log("Loading player pos" + gameData.playerPosition);
    }
    public void SaveGame()
    {
        //Pass the data to other scripts so they can update it
        Debug.Log(gameData.ToString());
        foreach (ISaveSystem saveSystemObj in saveSystemObjs)
        {
            saveSystemObj.SaveData(ref gameData);
        }

        //Save the data to a file using the data handler
        dataHandler.Save(gameData);
        if (manualSave)
        {
            manualSave = false;
            PauseMenu.saveComplete = true;
        }
        Debug.Log("SaveGame");
    }

    List<ISaveSystem> FindAllSaveSystemObjs()
    {
        List<ISaveSystem> savedObjects = FindObjectsOfType(typeof(MonoBehaviour), true).OfType<ISaveSystem>().ToList();
        return savedObjects;
    }

    public void DemoLoad()
    {
        gameData = dataHandler.Load();
        Debug.Log("Load Demo");
        //If there is no data to be loaded, initialize a new game
        if (gameData == null)
        {
            Debug.Log("No saves found, initializing a new game.");
            NewGame();
            isNewGame = true;
        }
        else
        {
            StartMenu.playedThroughOnce = gameData.playedthroughOnce;
            isNewGame = false;
        }
    }
    public void DemoSave()
    {
        gameData.playedthroughOnce = true;
        dataHandler.Save(gameData);
    }
}
