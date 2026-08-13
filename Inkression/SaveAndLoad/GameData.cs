using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public bool glassChangeLayer;
    public bool katieDollDiaDone;

    public bool soccerDumpsterOn;
    public bool trashScoreBoardEnabled;
    public bool trashThrowDiaDone;
    public bool trashGameCompleteDiaDone;
    public int soccerScore;
    
    public bool groceryBoxQuestAccepted;
    public bool groceryBoxGameSucceeded;

    public Vector3 playerPosition;
    public Quaternion playerRotation;

    public SerializableDictionary<string, LivableValues> livableDict;
    public SerializableDictionary<string, bool> doorDict;
    public SerializableDictionary<string, bool> lidDict;
    public SerializableDictionary<string, bool> windowDict;
    public SerializableDictionary<string, bool> benchDict;

    public SerializableDictionary<string, bool> lookingDict;

    public SerializableDictionary<string, PickUpValues> pickupDict;
    public SerializableDictionary<string, GroceryBoxValues> groceryBoxDict;
    public SerializableDictionary<string, bool> pizzaDict;
    public SerializableDictionary<string, int> cigDict;

    public SerializableDictionary<string, bool> groupMasterDict;
    public SerializableDictionary<string, bool> buildingGroupControllerDict;

    //NPC
    public SerializableDictionary<string, NPCData> npcStage;
    public SerializableDictionary<string, bool> onOffState;

    public SerializableDictionary<string, bool> floorTileDict;
    public SerializableDictionary<string, bool> groundDirtDict;

    //Tattoo
    public SerializableDictionary<string, bool> tatSpaceDict;
    public CharacterTattooMenu currentMenu;
    public SerializableDictionary<string, TattooData> tattooDict;
    public SerializableDictionary<string, TattooMenuData> tattooMenuDict;
    public SerializableDictionary<string, TatCharData> tatCharDict;

    public SerializableDictionary<string, bool> passiveActivationDict;

    public GameObject vinylOnRecordPlayer;
    public bool recordPlayerOn;
    public SerializableDictionary<string, GameObject> vinylHolderDict;
    public SerializableDictionary<string, bool> vinylDict;


    //Progress
    public SerializableDictionary<string, ProgressData> progressDict;

    public SerializableDictionary<string, int> gameProgressDict;

    //Dialogues
    public SerializableDictionary<string, DialogueData> convoDict;

    public bool playedthroughOnce;

    //Default values when there is no saved data
    public GameData()
    {
        playerPosition = new Vector3(-35.5f, 7.5f, 18.5f);
        playerRotation = new Quaternion(0, 0.866f, 0, 0.5f);

        livableDict = new SerializableDictionary<string, LivableValues>();
        doorDict = new SerializableDictionary<string, bool>();
        lidDict = new SerializableDictionary<string, bool>();
        windowDict = new SerializableDictionary<string, bool>();
        benchDict = new SerializableDictionary<string, bool>();

        lookingDict = new SerializableDictionary<string, bool>();

        pickupDict = new SerializableDictionary<string, PickUpValues>();
        pizzaDict = new SerializableDictionary<string, bool>();
        cigDict = new SerializableDictionary<string, int>();

        groupMasterDict = new SerializableDictionary<string, bool>();
        buildingGroupControllerDict = new SerializableDictionary<string, bool>();
        doorDict = new SerializableDictionary<string, bool>();
        pizzaDict = new SerializableDictionary<string, bool>();

        
        npcStage = new SerializableDictionary<string, NPCData>();
        
        floorTileDict = new SerializableDictionary<string, bool>();
        groundDirtDict = new SerializableDictionary<string, bool>();

        passiveActivationDict = new SerializableDictionary<string, bool>();

        npcStage = new();
        onOffState = new();
        tatSpaceDict = new();
        tattooDict = new();
        tattooMenuDict = new();
        tatCharDict = new();
        progressDict = new();
        gameProgressDict = new();
        convoDict = new();
        groceryBoxDict = new();
        vinylHolderDict = new SerializableDictionary<string, GameObject>();
        vinylDict = new SerializableDictionary<string, bool>();
    }
    
}

[System.Serializable]
public class LivableValues
{
    public bool activated;
    public bool isActive;
    public bool isEnabled;
    public float minDist;
    public Transform parent;
    public LivableValues(bool _activated, bool _isActive, bool _isEnabled, float _minDist, Transform _parent)
    {
        activated = _activated;
        isActive = _isActive;
        isEnabled = _isEnabled;
        minDist = _minDist;
        parent = _parent;
    }
}

[System.Serializable]
public class TattooData
{
    public bool isDiscovered;
    public bool isActivated;
    public TattooData(bool _isDiscovered, bool _isActivated)
    {
        isDiscovered = _isDiscovered;
        isActivated = _isActivated;
    }
}

[System.Serializable]
public class TattooMenuData
{
    public bool isDiscovered;
    public bool isFinished;

    public TattooMenuData(bool _isDiscovered, bool _isFinished)
    {
        isDiscovered= _isDiscovered;
        isFinished= _isFinished;
    }
}

[System.Serializable]
public class TatCharData
{
    public int stage;
    public bool stageColored;

    public TatCharData(int stage, bool stageColored)
    {
        this.stage = stage;
        this.stageColored = stageColored;
    }
}

[System.Serializable]
public class PickUpValues
{
    public Vector3 pos;
    public Quaternion rot;
    public bool dumped;
    public PhysicMaterial physicMat;
    public PickUpValues(Vector3 position, Quaternion rotation, bool _dumped, PhysicMaterial _physicMat)
    {
        pos = position;
        rot = rotation;
        dumped = _dumped;
        physicMat = _physicMat;
    }
}

[System.Serializable]
public class GroceryBoxValues
{
    public GroceryBox aboveBox;
    public GroceryBox belowBox;

    public GroceryBoxValues(GroceryBox _aboveBox, GroceryBox _belowBox)
    {
        this.aboveBox = _aboveBox;
        this.belowBox = _belowBox;
    }
}

[System.Serializable]
public class ProgressData
{
    public bool mechUnlocked;
    public int demoProgress;
    public int totalFloorCleaned;

    public ProgressData(bool cutsceneFinished, int demoProgress, int totalFloorCleaned)
    {
        this.mechUnlocked = cutsceneFinished;
        this.demoProgress = demoProgress;
        this.totalFloorCleaned = totalFloorCleaned;
    }
}

[System.Serializable]
public class GroceryBoxGameData
{
    public bool _questAccepted;
    public bool _gameStarted;
    public bool _questSucceeded;

}



