using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public GameObject mapManager;
    MapGenerator mapGenerator;

    int currentLevel = 0;
    int currentTurn = 0;

    PlayerState playerState;

    // Start is called before the first frame update
    void Start()
    {
        mapGenerator = mapManager.GetComponent<MapGenerator>();
        mapGenerator.GenerateLevel(currentLevel);

        playerState = new PlayerState();
    }
    
    //Called when the player got the key and is on the trapdoor
    public void GoToNextLevel()
    {
        currentLevel++;
        mapGenerator.ClearCurrentMap();
        mapGenerator.GenerateLevel(currentLevel);
    }

    //Save player state between levels
    public void SavePlayerState(Player player)
    {
        playerState.health = player.health;
    }

    //Resolve actions list of all the entities
    public void Resolve()
    {
        //Foreach entity on the map
        foreach (Entity g in mapGenerator.currentEntities)
        {
            //Resolve actions
            g.ResolveTurn();
            //Clear actions
            g.waitingActionsList.Clear();
        }
        currentTurn++;
    }
}

public class PlayerState
{
    public int health;
}
