using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VInspector;


public class IDAssigner : MonoBehaviour
{

    //private void OnEnable()
    //{
    //    GenerateGuid(); 
    //}
    [Button]
    void AssignLivableObjectID()
    {
        LivableObject[] livables = FindObjectsOfType<LivableObject>();
        foreach (LivableObject livable in livables)
        {
            if (!livable.gameObject.GetComponent<ObjectID>())
            {
                livable.gameObject.AddComponent<ObjectID>();
                livable.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                livable.gameObject.GetComponent<ObjectID>().GenerateGuid();
                //Debug.Log("1");
            }
        }
    }

    [Button]
    void AssignNPCID()
    {
        NPCControl[] characters = FindObjectsOfType<NPCControl>();
        foreach (NPCControl character in characters)
        {
            if (!character.gameObject.GetComponent<ObjectID>()) 
            {
                character.gameObject.AddComponent<ObjectID>();
                character.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                character.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
        }
    }

    [Button]
    void AssignGroupMasterID()
    {
        GroupMaster[] controllers = FindObjectsOfType<GroupMaster>();
        foreach (GroupMaster controller in controllers)
        {
            if (!controller.gameObject.GetComponent<ObjectID>())
            {
                controller.gameObject.AddComponent<ObjectID>();
                controller.gameObject.GetComponent<ObjectID>().GenerateGuid(); 
            }
            else
            {
                controller.gameObject.GetComponent<ObjectID>().GenerateGuid();
                //Debug.Log("1");
            }
        }
    }

    [Button]
    void AssignBuildingGroupControllerID()
    {
        BuildingGroupController[] controllers = FindObjectsOfType<BuildingGroupController>();
        foreach (BuildingGroupController controller in controllers)
        {
            if (!controller.gameObject.GetComponent<ObjectID>())
            {
                controller.gameObject.AddComponent<ObjectID>();
                controller.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                controller.gameObject.GetComponent<ObjectID>().GenerateGuid();
                //Debug.Log("1");
            }
        }
    }

    [Button]
    void AssignFloorTileID()
    {
        FloorTile[] tiles = FindObjectsOfType<FloorTile>();
        foreach (FloorTile tile in tiles)
        {
            if (!tile.gameObject.GetComponent<ObjectID>())
            {
                tile.gameObject.AddComponent<ObjectID>();
                tile.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                tile.gameObject.GetComponent<ObjectID>().GenerateGuid();
                //Debug.Log("1");
            }
        }
    }

    [Button]
    void AssignGroundDirtID()
    {
        GroundDirt[] dirts = FindObjectsOfType<GroundDirt>();
        foreach (GroundDirt dirt in dirts)
        {
            if (!dirt.gameObject.GetComponent<ObjectID>())
            {
                dirt.gameObject.AddComponent<ObjectID>();
                dirt.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                dirt.gameObject.GetComponent<ObjectID>().GenerateGuid();
                //Debug.Log("1"); 
            }
        }
    }
    [Button]
    void AssignPassiveActivationID()
    {
        PassiveActivation[] passives = FindObjectsOfType<PassiveActivation>();
        foreach (PassiveActivation passive in passives)
        {
            if (!passive.gameObject.GetComponent<ObjectID>())
            {
                passive.gameObject.AddComponent<ObjectID>();
                passive.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                passive.gameObject.GetComponent<ObjectID>().GenerateGuid(); 
                //Debug.Log("1"); 
            }
        }
    }

    [Button]
    void AssignTattooID()
    { 
        MindPalace tatSpace = FindObjectOfType<MindPalace>();
        CharacterTattooMenu[] charMenus = FindObjectsOfType<CharacterTattooMenu>();
        CharacterTattoo[] charTats = FindObjectsOfType<CharacterTattoo>();
        CharacterTattooMesh[] charMeshes = FindObjectsOfType<CharacterTattooMesh>();

        if (!tatSpace.gameObject.GetComponent<ObjectID>())
        {
            tatSpace.gameObject.AddComponent<ObjectID>();
            tatSpace.gameObject.GetComponent <ObjectID>().GenerateGuid();
        }
        else
        {
            tatSpace.gameObject.GetComponent<ObjectID>().GenerateGuid();
        }

        foreach(CharacterTattooMenu menu in charMenus)
        {
            if (!menu.gameObject.GetComponent<ObjectID>())
            {
                menu.gameObject.AddComponent<ObjectID>();
            }
            else
            {
                menu.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
        }

        foreach(CharacterTattoo tat in charTats)
        {
            if(!tat.gameObject.GetComponent<ObjectID>())
            {
                tat.gameObject.AddComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                tat.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
        }

        foreach(CharacterTattooMesh mesh in charMeshes)
        {
            if (!mesh.GetComponent<ObjectID>())
            {
                mesh.gameObject.AddComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                mesh.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
        }
    }
    [Button]
    void AssignVinylHolderID()
    {
        VinylHolder[] objs = FindObjectsOfType<VinylHolder>();
        foreach (VinylHolder obj in objs)
        {
            if (!obj.gameObject.GetComponent<ObjectID>())
            {
                obj.gameObject.AddComponent<ObjectID>();
                obj.gameObject.GetComponent<ObjectID>().GenerateGuid(); 
            }
            else
            {
                obj.gameObject.GetComponent<ObjectID>().GenerateGuid();
                //Debug.Log("1");  
            }
        } 
    }

    [Button]
    void AssignProgressDataID()
    {
        MainQuestState mainQuest = FindObjectOfType<MainQuestState>();
        if (!mainQuest.gameObject.GetComponent<ObjectID>())
        {
            mainQuest.gameObject.AddComponent<ObjectID>();
            mainQuest.gameObject.GetComponent <ObjectID>().GenerateGuid();  
        }
        else
        {
            mainQuest.gameObject.GetComponent<ObjectID>().GenerateGuid();
        }

        StartGame startData = FindAnyObjectByType<StartGame>();
        if (!startData.gameObject.GetComponent<ObjectID>())
        {
            startData.gameObject.AddComponent<ObjectID>();
            startData.gameObject.GetComponent<ObjectID>().GenerateGuid();
        }
        else
        {
            startData.gameObject.GetComponent<ObjectID>().GenerateGuid();
        }

    }

    [Button]
    void AssignConvoDataID()
    {
        DialogueSaver[] dialogues = FindObjectsOfType<DialogueSaver>();
        foreach (DialogueSaver obj in dialogues)
        {
            if (!obj.gameObject.GetComponent<ObjectID>())
            {
                obj.gameObject.AddComponent<ObjectID>();
                obj.gameObject.GetComponent<ObjectID>().GenerateGuid();
            }
            else
            {
                obj.gameObject.GetComponent<ObjectID>().GenerateGuid(); 
            }
        }
    }
}
