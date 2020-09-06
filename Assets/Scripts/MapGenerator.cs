using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Windows;
using UnityEngine.WSA;

//Types of map tiles
public enum Types
{
    Floor, Wall, Door, Trapdoor
}

public class MapGenerator : MonoBehaviour
{
    //Maps utils
    public static float GRID_SIZE = 0.32f;
    public static int[] numberOfDungeonsPerLevel = { 6, 3, 1 };
    public int currentLevel = 0;

    //Floor prefab
    public GameObject floor_wood;
    public GameObject floor_trapdoor_close;
    public Sprite floor_trapdoor_open;
    //Walls prefab
    public GameObject wall_stone;
    public GameObject wall_door;
    //Player prefab
    public GameObject player;
    //Items prefabs
    public GameObject item_key;

    //JSON data of the map
    GeneratedMapJSONContent JSONMap;
    GameObject currentMap;
    //Lists of generated gameobjects
    List<GameObject> gameobjectTilesFloors = new List<GameObject>();
    List<GameObject> gameobjectTilesWalls = new List<GameObject>();
    GameObject gameobjectTilePlayer = null;
    GameObject gameobjectTileTrapdoor = null;
    List<GameObject> gameobjectTilesMobs = new List<GameObject>();
    List<GameObject> gameobjectTilesItems = new List<GameObject>();
    List<GameObject> gameobjectTilesDoors = new List<GameObject>();

    //List of the spawned entities
    public List<Entity> currentEntities = new List<Entity>();

    //Lists of the generated tiles
    List<Tile> tilesFloors = new List<Tile>();
    List<Tile> tilesWalls = new List<Tile>();

    //Pathfinding grid and vars
    bool[,] tilesmap;
    public int currentGridOffsetX = 0, currentGridOffsetY = 0;
    public NesScripts.Controls.PathFind.Grid pathFindGrid;

    //Generate a random level
    public void GenerateLevel(int level)
    {
        level = 1;
        currentMap = new GameObject("Map");
        //Get a random dungeon file and read it
        int rand = (int)(UnityEngine.Random.value * (numberOfDungeonsPerLevel[level]));
        string path = "Assets\\Dungeons\\level" + level + "\\d" + rand + ".json";
        using (StreamReader r = new StreamReader(path))
        {
            string json = r.ReadToEnd();
            JSONMap = JsonUtility.FromJson<GeneratedMapJSONContent>(json);
        }
        //Add the rectangles tiles
        foreach (TileRect tile in JSONMap.rects)
        {
            for (int i = 0; i < tile.w; i++)
                for (int j = 0; j < tile.h; j++)
                    AddTileFloor((tile.x + i), (tile.y + j));
        }
        ///Add the doors tiles
        foreach (Tile tile in JSONMap.doors)
            if (UnityEngine.Random.value < 0.5f)
                AddTileDoor(tile.x, tile.y);
        //Add walls tiles : a base layer and then two more
        AddTilesWallsToMap(tilesFloors);
        for (int i = 0; i < 2; i++)
            AddTilesWallsToMap(tilesWalls);
        //Add trapdoor tile
        AddTileTrapDoor();
        //Add player
        AddTilePlayer();
        //Add the key
        AddTileKey();
        //Add mobs
        //Add items
        //Make bool tilemap, convert negative numbers tiles positions to an array
        //Calculate the size of the map and the negative offset
        //The offset is the minimal number
        currentGridOffsetX = currentGridOffsetY = 0;
        int maxX = 0, maxY = 0;
        foreach (Tile t in tilesWalls)
        {
            if (t.x < currentGridOffsetX)
                currentGridOffsetX = t.x;
            if (t.y < currentGridOffsetY)
                currentGridOffsetY = t.y;
            if (t.x > maxX)
                maxX = t.x;
            if (t.y > maxY)
                maxY = t.y;
        }
        currentGridOffsetX = Math.Abs(currentGridOffsetX);
        currentGridOffsetY = Math.Abs(currentGridOffsetY);
        //Create tilemap with size
        tilesmap = new bool[(maxX + currentGridOffsetX) + 1, (maxY + currentGridOffsetY) + 1];
        //Assign values
        foreach (Tile t in tilesFloors)
            tilesmap[(t.x + currentGridOffsetX), (t.y + currentGridOffsetY)] = true;
        foreach (Tile t in tilesWalls)
            tilesmap[(t.x + currentGridOffsetX), (t.y + currentGridOffsetY)] = false;
        //Create grid
        pathFindGrid = new NesScripts.Controls.PathFind.Grid(tilesmap);
    }

    //Add walls all around the generated tiles----------------------------------
    void AddTilesWallsToMap(List<Tile> target)
    {
        //Check the sides of all the floor tiles and put a wall where there isn't anything
        int cnt = target.Count;
        for (int i = 0; i < cnt; i++)
        {
            Tile tile = target[i];
            bool W = false, E = false, N = false, S = false;
            foreach (Tile neib in tilesFloors)
            {
                //Check every directions
                if ((tile.x + 1) == neib.x && tile.y == neib.y)
                    W = true;
                if ((tile.x - 1) == neib.x && tile.y == neib.y)
                    E = true;
                if ((tile.y + 1) == neib.y && tile.x == neib.x)
                    N = true;
                if ((tile.y - 1) == neib.y && tile.x == neib.x)
                    S = true;
            }
            foreach (Tile neib in tilesWalls)
            {
                //Check every directions
                if ((tile.x + 1) == neib.x && tile.y == neib.y)
                    W = true;
                if ((tile.x - 1) == neib.x && tile.y == neib.y)
                    E = true;
                if ((tile.y + 1) == neib.y && tile.x == neib.x)
                    N = true;
                if ((tile.y - 1) == neib.y && tile.x == neib.x)
                    S = true;
            }
            if (!W)
                AddTileWall((tile.x + 1), tile.y);
            if (!E)
                AddTileWall((tile.x - 1), tile.y);
            if (!N)
                AddTileWall(tile.x, (tile.y + 1));
            if (!S)
                AddTileWall(tile.x, (tile.y - 1));
            }
        }

    //Tiles adding function--------------------------------
    void AddTileWall(int x, int y)
    {
        gameobjectTilesWalls.Add(Instantiate(wall_stone, new Vector3(x * GRID_SIZE, y * GRID_SIZE, 0),
            Quaternion.identity, currentMap.transform));
        tilesWalls.Add(new Tile(x, y, Types.Wall));
    }
    void AddTileFloor(int x, int y)
    {
        gameobjectTilesFloors.Add(Instantiate(floor_wood, new Vector3(x * GRID_SIZE, y * GRID_SIZE, 0),
            Quaternion.identity, currentMap.transform));
        tilesFloors.Add(new Tile(x, y, Types.Floor));
    }
    void AddTileDoor(int x, int y)
    {
        gameobjectTilesDoors.Add(Instantiate(wall_door, new Vector3(x * GRID_SIZE, y * GRID_SIZE, 0),
            Quaternion.identity, currentMap.transform));
        tilesFloors.Add(new Tile(x, y, Types.Door));
    }
    void AddTileTrapDoor()
    {
        int trgt = (int)(UnityEngine.Random.value * tilesFloors.Count);
        gameobjectTileTrapdoor = Instantiate(floor_trapdoor_close, new Vector3(tilesFloors[trgt].x * GRID_SIZE, tilesFloors[trgt].y * GRID_SIZE, 0),
            Quaternion.identity, currentMap.transform);
        tilesFloors.Add(new Tile(tilesFloors[trgt].x, tilesFloors[trgt].y, Types.Trapdoor));
    }
    void AddTilePlayer()
    {
        int pl = (int)(UnityEngine.Random.value * tilesFloors.Count);
        gameobjectTilePlayer = Instantiate(player, new Vector3(tilesFloors[pl].x * GRID_SIZE, tilesFloors[pl].y * GRID_SIZE, 0),
            Quaternion.identity, currentMap.transform);
        currentEntities.Add((Entity)gameobjectTilePlayer.GetComponent<Player>());
    }
    void AddTileKey()
    {
        int ke = (int)((UnityEngine.Random.value * tilesFloors.Count) / 2) + (tilesFloors.Count / 2);
        GameObject lekey = Instantiate(item_key, new Vector3(tilesFloors[ke].x * GRID_SIZE, tilesFloors[ke].y * GRID_SIZE, 0),
            Quaternion.identity, currentMap.transform);
        currentEntities.Add((Entity)lekey.GetComponent<Item>());
    }

    //Set the trapdoor sprite open
    public void SetTrapDoorOpen()
    {
        gameobjectTileTrapdoor.GetComponent<SpriteRenderer>().sprite = floor_trapdoor_open;
    }

    //Utils function
    public Vector2 WorldPositionToTileGridPos(int x, int y)
    {
        return new Vector2();
    }

    //Clear all the variables used to generate the map
    public void ClearCurrentMap()
    {
        //Destroy all generated gameobjects
        Destroy(currentMap);
        //Clear others
        JSONMap = null;
        gameobjectTilesFloors = new List<GameObject>();
        gameobjectTilesWalls = new List<GameObject>();
        gameobjectTilePlayer = gameobjectTileTrapdoor = null;
        gameobjectTilesMobs = new List<GameObject>();
        gameobjectTilesItems = new List<GameObject>();
        gameobjectTilesDoors = new List<GameObject>();
        currentEntities = new List<Entity>();
        tilesFloors = new List<Tile>();
        tilesWalls = new List<Tile>();
    }
}

[System.Serializable]
public class GeneratedMapJSONContent
{   
    public List<TileRect> rects;
    public List<Tile> doors;
    public List<Tile> columns;
    public List<Tile> water;
}

[System.Serializable]
public class TileRect
{
    public int x;
    public int y;
    public int w;
    public int h;
}

[System.Serializable]
public class Tile
{
    public int x;
    public int y;
    public Types type;

    public Tile(int x, int y, Types t)
    {
        this.x = x;
        this.y = y;
        this.type = t;
    }
}