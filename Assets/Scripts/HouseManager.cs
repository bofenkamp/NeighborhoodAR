using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HouseManager : MonoBehaviour
{
    //components
    public House house;
    private Rigidbody rb;

    //occupants
    public GameObject occupantPrefab;
    [SerializeField] private int minOccupantCount = 1;
    [SerializeField] private int maxOccupantCount = 5;

    //UI
    public GameObject houseTextPrefab;

    public void InitializeHouse()
    {
        rb = GetComponent<Rigidbody>();

        StartCoroutine(Expand());
    }

    private IEnumerator Expand()
    {
        float finalSize = transform.localScale.x;
        transform.localScale = Vector3.zero;

        yield return null;

        float startVelocity = rb.velocity.y;
        while (rb.velocity.y > 0)
        {
            float prog = 1f - (rb.velocity.y / startVelocity);
            prog = Mathf.Clamp(prog, 0f, 1f);
            transform.localScale = Vector3.one * finalSize * prog;
            yield return null;
        }
        transform.localScale = Vector3.one * finalSize;
        yield return null;
    }

    private void GenerateOccupants()
    {
        int occupantCount = Random.Range(minOccupantCount, maxOccupantCount + 1);
        for (int i = 0; i < occupantCount; i++)
        {
            //change size and color
            GameObject newOccupant = Instantiate(occupantPrefab, house.position, Quaternion.identity);
            newOccupant.transform.localScale = transform.localScale;
            newOccupant.transform.GetChild(0).GetComponent<MeshRenderer>().material.color = house.color.color;
            newOccupant.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().material.color = house.color.color;
            newOccupant.transform.GetChild(2).GetChild(0).GetComponent<MeshRenderer>().material.color = house.color.color;
            CitizenController citizenControl = newOccupant.GetComponent<CitizenController>();
            citizenControl.myHouse = this;
            citizenControl.Initialize();
        }
    }

    //make sure houses are fixed when they land
    private void OnCollisionEnter(Collision collision)
    {
        rb.isKinematic = true;
        transform.eulerAngles = Vector3.zero;
        GenerateOccupants();
        HouseTextControl houseText = Instantiate(houseTextPrefab).GetComponent<HouseTextControl>();
        StartCoroutine(houseText.RevealText(house, transform.localScale.y));
    }
}
