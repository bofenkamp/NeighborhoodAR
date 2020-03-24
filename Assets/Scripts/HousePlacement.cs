using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class HousePlacement : MonoBehaviour
{
    //external
    private Camera cam;

    //objects
    public GameObject housePrefab;
    private GameObject currHouseObj; //house being placed in world
    private List<GameObject> existingHouseObjs;

    //creation values
    [SerializeField] private float bounceForce = 150f;

    //attributes of neighborhood
    [SerializeField] private int maxHouses = 10;
    public HouseShape[] houseShapes;
    [SerializeField] private float minDistBetweenHouses = 1.5f; //units in terms of house size

    //attributes of houses
    private House currHouse;
    private HouseShape selectedShape;
    public HouseColor[] houseColors;
    [SerializeField] private float houseSize = 0.2f;

    //interface
    private TextMeshProUGUI messageText;

    private void Start()
    {
        cam = FindObjectOfType<Camera>();
        existingHouseObjs = new List<GameObject>();

#if UNITY_EDITOR
        selectedShape = houseShapes[Random.Range(0, houseShapes.Length)];
        PlaceHouse(Vector3.zero);
        selectedShape = houseShapes[Random.Range(0, houseShapes.Length)];
        PlaceHouse(new Vector3(1, 0, 0));
        selectedShape = houseShapes[Random.Range(0, houseShapes.Length)];
        PlaceHouse(new Vector3(2, 0, 0));
        selectedShape = houseShapes[Random.Range(0, houseShapes.Length)];
        PlaceHouse(new Vector3(0, 0, 1));
        selectedShape = houseShapes[Random.Range(0, houseShapes.Length)];
        PlaceHouse(new Vector3(-.8f, 0, -.8f));
        selectedShape = null;
#endif
        messageText = FindObjectOfType<TextMeshProUGUI>();
        //StartCoroutine(DropBalls());
    }

    private void Update()
    {
        //get touches
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                //get touches that were just tapped
                if (touch.phase == TouchPhase.Began)
                {
                    //make sure touch position is on ground
                    Vector3? housePos = GetLocationTapped(touch.position);
                    if (housePos.HasValue)
                    {
                        //make sure house location is valid
                        if (IsValidHouseLocation(housePos.Value))
                        {
                            PlaceHouse(housePos.Value);
                        }
                    }
                }
            }
        }
    }

    private Vector3? GetLocationTapped(Vector2 touchPosition)
    {
        if (Physics.Raycast(cam.ScreenPointToRay(touchPosition), out RaycastHit hitObj))
        {
            if(hitObj.transform.gameObject.name.Contains("ARPlane"))
                return hitObj.point;
        }

        //ground was not tapped
        return null;
    }

    private bool IsValidHouseLocation(Vector3 position)
    {
        //no more than 10 houses per neighborhood
        //overcrowding is not quaint
        if (existingHouseObjs.Count >= maxHouses)
        {
            messageText.text = "More houses would make this neighborhood too crowded. Just enjoy what you've already created!";
            return false;
        }

        //make sure the house isn't too close to any other houses
        if (TooCloseToOtherHouses(position))
        {
            messageText.text = "That spot's already someone's property! Please build somewhere else.";
            return false;
        }

        return true;
    }

    private bool TooCloseToOtherHouses(Vector3 position)
    {
        if (existingHouseObjs == null || existingHouseObjs.Count == 0)
            return false;

        float minDist = houseSize * minDistBetweenHouses;
        foreach(GameObject house in existingHouseObjs)
        {
            float dist = (house.GetComponent<HouseManager>().house.position - position).magnitude;
            if (dist < minDist)
                return true;
        }

        return false;
    }

    private void PlaceHouse(Vector3 position)
    {
        if (selectedShape == null)
            messageText.text = "Please select the shape of the new house!";
        else
        {
            currHouse = new House(selectedShape, RandomlySelectColor(), position);
            currHouseObj = CreateNewHouse(currHouse);
        }
    }

    private HouseColor RandomlySelectColor()
    {
        int i = Random.Range(0, houseColors.Length);
        return houseColors[i];
    }

    private GameObject CreateNewHouse(House house)
    {
        GameObject newHouseObj = Instantiate(housePrefab,
            house.position + (Vector3.up * house.shape.offset * houseSize), Quaternion.identity);
        newHouseObj.name = "House " + existingHouseObjs.Count;
        HouseManager newHouseManager = newHouseObj.GetComponent<HouseManager>();

        newHouseObj.GetComponent<MeshFilter>().mesh = house.shape.shape;
        newHouseObj.GetComponent<MeshRenderer>().material.color = house.color.color;
        newHouseManager.house = house;
        newHouseObj.transform.localScale = Vector3.one * houseSize;
        newHouseObj.transform.GetChild(0).localPosition = Vector3.up * (-house.shape.offset + 0.25f) ; //position collider
        newHouseObj.GetComponent<Rigidbody>().AddForce(Vector3.up * bounceForce);
        newHouseManager.InitializeHouse();

        existingHouseObjs.Add(newHouseObj);
        UpdateHeaderText();
        return newHouseObj;
    }

    private void UpdateHeaderText()
    {
        if (messageText == null)
            messageText = FindObjectOfType<TextMeshProUGUI>();

        if (existingHouseObjs.Count == 1)
            messageText.text = "You built a home! Build more so the people who live there can make new friends.";
        else if (existingHouseObjs.Count == 2)
            messageText.text = "Now you have a neighborhood! You should see people walking around soon. Move your camera close to them to say hi.";
        else
            messageText.text = "";
    }

    public void SelectHouseShape(int i)
    {
        selectedShape = houseShapes[i];
    }
}