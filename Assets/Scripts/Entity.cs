using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public enum EntityType
{
    Player, Mob, Item
}

public enum Direction
{
    Up, Down, Left, Right
}

public class Entity : MonoBehaviour
{
    public int posX; 
    public int posY;
    public string entityName;
    public EntityType entityType;
    public List<Action> waitingActionsList = new List<Action>();

    public TurnManager turnManager;
    public MapGenerator mapGenerator;
    public TextEventGeneration textEventGen;
    public AudioSource audioSource;

    public void Awake()
    {
        mapGenerator = GameObject.Find("Map Manager").GetComponent<MapGenerator>();
        textEventGen = GameObject.Find("Text Generator").GetComponent<TextEventGeneration>();
        turnManager = GameObject.Find("Turn Manager").GetComponent<TurnManager>();
        audioSource = GetComponent<AudioSource>();
    }

    //Do entity action
    public void ResolveTurn()
    {
        foreach (Action action in waitingActionsList)
            action();
    }
}
