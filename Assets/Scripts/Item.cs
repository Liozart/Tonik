using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemTypes
{
    Key
}

public class Item : Entity
{
    public ItemTypes type;

    // Start is called before the first frame update
    void Start()
    {
        switch (type)
        {
            case ItemTypes.Key:
                this.entityName = "Trapdoor Key";
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
