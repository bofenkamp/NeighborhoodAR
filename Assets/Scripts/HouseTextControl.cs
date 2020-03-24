using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class HouseTextControl : MonoBehaviour
{
    //components
    private RectTransform rt;
    private TextMeshPro text;
    private GameObject player;

    //temporal values
    [SerializeField] private float revealSpeed; //time from creation to being fully visible
    [SerializeField] private float appearanceTime; //time in optimal reading state
    [SerializeField] private float disappearTime; //time to disappear

    //positional values
    [SerializeField] private float heightAboveHouse; //how far above the building does the text appear
    [SerializeField] private float characterSpacingOnDisappear;

    public IEnumerator RevealText(House house, float houseScale)
    {
        //initialize components
        rt = GetComponent<RectTransform>();
        text = GetComponent<TextMeshPro>();
        player = Camera.main.gameObject;
        StartCoroutine(FacePlayer(player.transform, rt));

        //initialize this object
        float houseHeight = houseScale * house.shape.offset * 2f;
        rt.localScale = Vector3.zero;
        text.text = "+1 " + house.color.colorName + " house!";
        text.color = house.color.color;
        rt.position = house.position;
        yield return null;

        //appear
        Vector3 startPos = rt.position;
        float riseAmount = houseHeight + heightAboveHouse;
        float startTime = Time.time;
        while(Time.time < startTime + revealSpeed)
        {
            float prog = (Time.time - startTime) / revealSpeed;
            rt.position = startPos + Vector3.up * prog * riseAmount;
            rt.localScale = Vector3.one * prog;
            yield return null;
        }

        //remain still for players to read
        rt.position = startPos + Vector3.up * riseAmount;
        rt.localScale = Vector3.one;
        yield return new WaitForSeconds(appearanceTime);

        //disappear
        startPos = rt.position;
        riseAmount = riseAmount * disappearTime / revealSpeed;
        float spaceAmount = characterSpacingOnDisappear - text.characterSpacing;
        float startSpace = text.characterSpacing;
        startTime = Time.time;
        while(Time.time < startTime + disappearTime)
        {
            float prog = (Time.time - startTime) / disappearTime;
            rt.position = startPos + Vector3.up * prog * riseAmount;
            text.characterSpacing = startSpace + spaceAmount * prog;
            text.color = new Color(text.color.r, text.color.g, text.color.b, 1 - prog);
            text.outlineColor = new Color(text.outlineColor.r, text.outlineColor.g, text.outlineColor.b, 1 - prog);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator FacePlayer(Transform player, RectTransform rt)
    {
        while (true)
        {
            rt.LookAt(player);
            rt.Rotate(Vector3.up, 180f, Space.Self);
            yield return null;
        }
    }
}
