using NesScripts.Controls.PathFind;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security;
using System.Transactions;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;

public enum PlayerType
{
    Archer, Mage, Rogue
}

public class Player : Entity
{
    //Player types and sprites
    public PlayerType playerType;
    public Sprite playerSpriteArcher;
    public Sprite playerSpriteMage;
    public Sprite playerSpriteRogue;

    //Camera
    GameObject playerCamera;

    //Audio
    public AudioClip audioClip_openDoor;
    public AudioClip audioClip_openTrapDoor;
    public AudioClip audioClip_getKey;
    public AudioClip audioClip_getLoot;

    //Stats
    public int health = 20;
    public bool hasFloorKey;
    public float delayBeforeAutoThresold = 1f;
    float delayBeforeAuto = 0;

    //Current selected tile or object
    GameObject selectedObject = null;

    // Start is called before the first frame update
    void Start()
    {
        //Entity init
        this.entityType = EntityType.Player;
        hasFloorKey = false;
        switch (playerType)
        {
            case PlayerType.Archer:
                gameObject.GetComponent<SpriteRenderer>().sprite = playerSpriteArcher;
                this.entityName = "Mr. Archer"; break;
            case PlayerType.Mage:
                gameObject.GetComponent<SpriteRenderer>().sprite = playerSpriteMage;
                this.entityName = "The Mage"; break;
            case PlayerType.Rogue:
                gameObject.GetComponent<SpriteRenderer>().sprite = playerSpriteRogue;
                this.entityName = "Roguy"; break;
        }
        playerCamera = GameObject.FindGameObjectWithTag("MainCamera");
        //Center camera on player
        playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }

    // Update is called once per frame
    void Update()
    {
        //Get player selection
        if (Input.GetMouseButtonDown((int)MouseButton.LeftMouse))
        {
            Vector3 ray = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(ray.x, ray.y), Vector2.zero, Mathf.Infinity);
            bool isSame = false;
            if (hit)
            {
                if (selectedObject != null)
                {
                    if (selectedObject.Equals(hit.collider.gameObject))
                        isSame = true;
                    selectedObject.GetComponent<SpriteRenderer>().color = Color.white;
                    selectedObject = null;

                }
                if (!isSame) switch (hit.collider.tag)
                {
                    case "Floor":
                    case "Door":
                    case "Trapdoor":
                        if (selectedObject != null)
                        {
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.white;
                            selectedObject = hit.collider.gameObject;
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.green;
                        }
                        else
                        {
                            selectedObject = hit.collider.gameObject;
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.green;
                        }
                        this.textEventGen.AddTextEvent("The floor.", EventTextType.Normal);
                        break;

                    case "Wall":
                        this.textEventGen.AddTextEvent("A wall.", EventTextType.Normal);
                        break;

                    case "Player":
                        this.textEventGen.AddTextEvent("That's " + hit.collider.gameObject.GetComponent<Player>().entityName
                            + ".", EventTextType.Normal);
                        break;

                    case "Mob":
                        if (selectedObject != null)
                        {
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.white;
                            selectedObject = hit.collider.gameObject;
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.red;
                        }
                        else
                        {
                            selectedObject = hit.collider.gameObject;
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.red;
                        }
                        this.textEventGen.AddTextEvent("An evil " + hit.collider.gameObject.GetComponent<Player>().entityName + ".", EventTextType.Combat);
                        break;

                    case "Item":
                        if (selectedObject != null)
                        {
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.white;
                            selectedObject = hit.collider.gameObject;
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.magenta;
                        }
                        else
                        {
                            selectedObject = hit.collider.gameObject;
                            selectedObject.GetComponent<SpriteRenderer>().color = Color.magenta;
                        }
                        this.textEventGen.AddTextEvent("An shiny " + hit.collider.gameObject.GetComponent<Item>().entityName + ".", EventTextType.Loot);
                        break;
                }
            }
        }
        //Player turn accept
        if (Input.GetMouseButtonDown((int)MouseButton.RightMouse))
        {
            if (selectedObject != null) switch (selectedObject.tag)
            {
                case "Floor":
                case "Trapdoor":
                case "Door":
                case "Item": this.waitingActionsList.Add(MoveToSelection); break;
            }
            //Finish the turn
            this.turnManager.Resolve();
        }

        //Auto turn
        if (Input.GetMouseButton((int)MouseButton.RightMouse))
            if (delayBeforeAuto < delayBeforeAutoThresold)
                delayBeforeAuto += Time.deltaTime;
            else
            {
                if (selectedObject != null) switch (selectedObject.tag)
                    {
                        case "Floor":
                        case "Trapdoor":
                        case "Door":
                        case "Item": this.waitingActionsList.Add(MoveToSelection); break;
                    }
                //Finish the turn
                this.turnManager.Resolve();
                delayBeforeAuto = 0;
            }
        else
            delayBeforeAuto = 0;
    }

    //Player collides with something
    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.transform.tag)
        {
            //Open the door
            case "Door":
                //Play sound
                audioSource.clip = audioClip_openDoor;
                audioSource.Play();
                //Remove door
                Destroy(other.gameObject);
                break;
            //Door to next level
            case "Trapdoor":
                //If the key was aquired
                if (hasFloorKey)
                {
                    //Play sound
                    audioSource.clip = audioClip_openTrapDoor;
                    audioSource.Play();
                    //Save player state
                    turnManager.SavePlayerState(this);
                    //Generate next level
                    turnManager.GoToNextLevel();
                }
                break;
            //Item
            case "Item":
                if (other.gameObject.GetComponent<Item>().type == ItemTypes.Key)
                {
                    //Play sound
                    audioSource.clip = audioClip_getKey;
                    audioSource.Play();
                    //Set key state
                    hasFloorKey = true;
                    //Change trapdoor sprite
                    mapGenerator.SetTrapDoorOpen();
                }
                //Remove item
                Destroy(other.gameObject);
                break;
        }
    }

    void MoveToSelection()
    {
        //Get player tile pos
        int px = (int)Math.Round(transform.position.x / MapGenerator.GRID_SIZE) + mapGenerator.currentGridOffsetX;
        int py = (int)Math.Round(transform.position.y / MapGenerator.GRID_SIZE) + mapGenerator.currentGridOffsetY;
        //Get selected object tile pos
        int tx = (int)Math.Round(selectedObject.transform.position.x / MapGenerator.GRID_SIZE) + mapGenerator.currentGridOffsetX;
        int ty = (int)Math.Round(selectedObject.transform.position.y / MapGenerator.GRID_SIZE) + mapGenerator.currentGridOffsetY;
        //Get path to target tile
        List<Point> tpath = Pathfinding.FindPath(this.mapGenerator.pathFindGrid,
            new Point(px, py), new Point(tx, ty));
        //Move toward the first point of the list
        transform.position = new Vector3((tpath[0].x - mapGenerator.currentGridOffsetX) * MapGenerator.GRID_SIZE,
            (tpath[0].y - mapGenerator.currentGridOffsetY) * MapGenerator.GRID_SIZE, 0);
        //Unselect tile if destination
        if (tpath.Count == 1)
        {
            selectedObject.GetComponent<SpriteRenderer>().color = Color.white;
            selectedObject = null;
        }
        //Center camera on player
        playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }
}
