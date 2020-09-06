using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EventTextType
{
    Normal, Combat, Loot, Skill
}

public class TextEventGeneration : MonoBehaviour
{
    public static float TextFadeDuration = 3.5f;
    //Text area for events
    public GameObject canvas;

    //Text object used to display every actions
    public GameObject textEvent;

    //Track added texts
    public List<GameObject> texts = new List<GameObject>();

    //Add a text gameobject displaying the event
    public void AddTextEvent(string t, EventTextType tp)
    {
        Text g = Instantiate(textEvent, canvas.transform).GetComponent<Text>();
        //Set the event text
        g.text = t;
        //Set the text color
        switch (tp)
        {
            case EventTextType.Combat: g.color = Color.red; break;
            case EventTextType.Loot: g.color = Color.yellow; break;
            case EventTextType.Skill: g.color = Color.magenta; break;
        }
        //Set text position
        g.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -(10 + (texts.Count * 20)));
        texts.Add(g.gameObject);
        StartCoroutine(RemoveTextGameobject(g.gameObject));
    }

    IEnumerator RemoveTextGameobject(GameObject t)
    {
        yield return new WaitForSeconds(TextFadeDuration);
        //Update texts positions
        for (int i = 0; i < texts.Count; i++)
            texts[i].gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -(10 + ((i-1) * 20)));
        //Remove the text
        texts.Remove(t);
        Destroy(t);
    }
}
