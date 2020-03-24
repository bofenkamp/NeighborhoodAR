using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CitizenController : MonoBehaviour
{
    //neighborhood related
    [HideInInspector] public HouseManager myHouse;
    private HouseManager destinationHouse;

    //walking
    private float defaultYVal;
    [SerializeField] private float walkSpeed = 1;
    [SerializeField] private Vector2 destination;
    private bool reachedDestination;
    private bool destinationIsHome = true;

    //detecting player closeness
    private Transform player;
    private bool PlayerCloseBy {
        get
        {
            float distFromPlayer = (rightShoulder.position - player.position).magnitude;
            return distFromPlayer <= playerDistForWaving;
        }
    }
    [SerializeField] private float playerDistForWaving = .33f;

    //animation
    public enum Animation { walking, turning, waving, idle, backToBusiness};
    public Animation currAnimation = Animation.idle;
    public Transform leftShoulder;
    public Transform rightShoulder;

    //walking animation
    [SerializeField] private float walkAnimSpeed = .5f; //time per swing
    [SerializeField] private float walkArmSwingAngle = 45f;
    [SerializeField] private float walkBodySwingAngle = 7f;
    [SerializeField] private float walkBounceAmount = .1f;

    //turning animation
    [SerializeField] private float turnSpeed = 360f; //degrees per second
    [SerializeField] private float turnArmRaiseAngle = 20f;

    //waving animation
    [SerializeField] private float waveSpeed = .2f; //time per wave
    [SerializeField] private float waveArmHighAngle = 70f;
    [SerializeField] private float waveArmLowAngle = 40f;
    [SerializeField] private float waveBodyTiltAngle = 3f;

    //idle animation
    [SerializeField] private float minVisitTime = 5f;
    [SerializeField] private float maxVisitTime = 30f;

    public void Initialize()
    {
        defaultYVal = transform.position.y;
        player = Camera.main.transform;
        StartCoroutine(AnimateIdle());
    }

    void AssignNewDestination()
    {
        //if not home, go home. If at home, go somewhere else
        destinationIsHome = !destinationIsHome;

        if (destinationIsHome) //return home
            destination = new Vector2(myHouse.house.position.x, myHouse.house.position.z);
        else //visit neighbor's house
        {
            List<HouseManager> viableHouses = GetNearbyHouses();
            if (viableHouses.Count > 0)
            {
                destinationHouse = viableHouses[Random.Range(0, viableHouses.Count)];
                destination = new Vector2 (destinationHouse.house.position.x, destinationHouse.house.position.z);
            }
            else
            {
                StartCoroutine(AnimateIdle());
                return;
            }
        }

        if (destination != new Vector2(transform.position.x, transform.position.z))
        {
            StartCoroutine(WalkToDestination());
            StartCoroutine(AnimateWalking());
        }
        else
        {
            StartCoroutine(AnimateIdle());
        }
    }

    //find houses that are on the same plane
    //and don't have another house between them and this citizen's house
    List<HouseManager> GetNearbyHouses()
    {
        List<HouseManager> viableHouses = new List<HouseManager>();
        foreach (HouseManager houseManager in FindObjectsOfType<HouseManager>())
        {
            if (houseManager == myHouse)
                continue; //don't add this citizen's house
            if (Mathf.Abs(houseManager.house.position.y - myHouse.house.position.y) > 0.15f)
                continue; //not on this plane

            Ray toHouse = new Ray(myHouse.house.position, houseManager.house.position - myHouse.house.position);
            RaycastHit hit;
            if (Physics.Raycast(toHouse, out hit))
            {
                if (hit.collider.transform.parent.gameObject == houseManager.gameObject)
                    viableHouses.Add(houseManager);
            }
        }

        return viableHouses;
    }

    IEnumerator WalkToDestination()
    {
        while (true)
        {
            //only move forward if walking
            if (currAnimation == Animation.walking)
            {
                float distanceMoved = Time.deltaTime * walkSpeed * transform.localScale.y;
                Vector2 pos = new Vector2(transform.position.x, transform.position.z);
                float distanceRemaining = (pos - destination).magnitude;
                if (distanceMoved >= distanceRemaining) //reached destination
                {
                    transform.position = new Vector3(destination.x, transform.position.y, destination.y);
                    reachedDestination = true;
                    break;
                }
                else
                {
                    Vector2 direction = (destination - pos).normalized;
                    Vector3 deltaPos = new Vector3(direction.x * distanceMoved, 0, direction.y * distanceMoved);
                    transform.position += deltaPos;
                }
            }
            yield return null;
        }

        StartCoroutine(AnimateIdle());

    }

    IEnumerator AnimateWalking()
    {
        currAnimation = Animation.walking;
        
        float startTime = Time.time;
        bool progGreaterThanZero = true;

        reachedDestination = new Vector2(transform.position.x, transform.position.z) == destination;

        if (!reachedDestination)
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x,
                GetAngleXZPlane(transform.position + Vector3.forward, transform.position,
                new Vector3(destination.x, 0, destination.y)) * Mathf.Rad2Deg,
                transform.eulerAngles.z);
        }

        while (!reachedDestination)
        {
            //animate
            float prog = Mathf.Sin((Time.time - startTime) * Mathf.PI / walkAnimSpeed);
            rightShoulder.localEulerAngles = Vector3.left * prog * walkArmSwingAngle;
            leftShoulder.localEulerAngles = Vector3.right * prog * walkArmSwingAngle;
            transform.eulerAngles = new Vector3(0f, transform.eulerAngles.y, -prog * walkBodySwingAngle);
            transform.position = new Vector3(transform.position.x,
                defaultYVal + Mathf.Abs(prog) * walkBounceAmount * transform.localScale.y,
                transform.position.z);

            //reached end of cycle, so we can check if the player is close enough to wave to them
            if (prog == 0 || (progGreaterThanZero && prog < 0) || (!progGreaterThanZero && prog > 0))
            {
                if (PlayerCloseBy)
                    break;
                else //player not close enough to wave, reset cycle end check later
                {
                    if (prog > 0)
                        progGreaterThanZero = true;
                    else if (prog < 0)
                        progGreaterThanZero = false;
                }
            }

            yield return null;
        }

        //reset position
        rightShoulder.eulerAngles = Vector3.zero;
        leftShoulder.eulerAngles = Vector3.zero;
        transform.eulerAngles = Vector3.zero;
        transform.position = new Vector3(transform.position.x, defaultYVal, transform.position.z);

        if (!reachedDestination)
            StartCoroutine(AnimateTurning());
        yield return null;
    }

    IEnumerator AnimateTurning()
    {
        currAnimation = Animation.turning;

        float startRotation = transform.eulerAngles.y;

        while (true)
        {
            if (transform.position == player.position) //can't turn to face player if they occupy the same space
                break;
            float targetRotation = GetAngleXZPlane(transform.position + Vector3.forward, transform.position, player.position)
                * Mathf.Rad2Deg;
            if (targetRotation == startRotation) //already rotated currectly, no need to turn
                break;
            float currRotation = transform.eulerAngles.y;
            //are we further away from the target than when we started?
            if ((currRotation < startRotation && startRotation < targetRotation) ||
                (currRotation > startRotation && startRotation > targetRotation))
                startRotation = transform.eulerAngles.y;

            //animate
            float linearProg = Mathf.Abs(currRotation - startRotation) / Mathf.Abs(targetRotation - startRotation);
            float prog = Mathf.Sin(linearProg * Mathf.PI);
            if (float.IsNaN(linearProg))
            {
                Debug.Log("LINEAR PROG NOT A NUMBER");
                Debug.Log("start rotation = " + startRotation);
                Debug.Log("current rotation = " + currRotation);
                Debug.Log("target rotation = " + targetRotation);
            }
            rightShoulder.localEulerAngles = Vector3.forward * prog * turnArmRaiseAngle;
            leftShoulder.localEulerAngles = Vector3.back * prog * turnArmRaiseAngle;

            //move towards camera
            float turnAmount = Time.deltaTime * turnSpeed;
            if (turnAmount >= Mathf.Abs(targetRotation - currRotation)) //are we facing the player?
            {
                transform.eulerAngles = Vector3.up * targetRotation;
                break;
            }
            else
            {
                if (targetRotation < currRotation)
                    transform.Rotate(Vector3.up, -turnAmount, Space.World);
                else
                    transform.Rotate(Vector3.up, turnAmount, Space.World);
            }

            yield return null;
        }

        //reset position
        rightShoulder.localEulerAngles = Vector3.zero;
        leftShoulder.localEulerAngles = Vector3.zero;

        StartCoroutine(AnimateWaving());

        yield return null;
    }

    IEnumerator AnimateWaving()
    {
        currAnimation = Animation.waving;
        float startTime = Time.time;
        float endOfFirstWave = startTime + waveSpeed / 2f;

        while (PlayerCloseBy)
        {
            if (transform.position != player.position)
            {
                //keep facing player if they aren't in the same place
                transform.eulerAngles = new Vector3(transform.eulerAngles.x,
                    GetAngleXZPlane(transform.position + Vector3.forward,
                    transform.position, player.position) * Mathf.Rad2Deg,
                    transform.eulerAngles.z);
            }

            //animate
            bool firstWaveDone = Time.time > endOfFirstWave;
            float prog = Mathf.Sin((Time.time - startTime) * Mathf.PI / waveSpeed);
            if (firstWaveDone) //arm already raised, body already tilted
            {
                if (player.position != rightShoulder.position)
                    transform.eulerAngles = new Vector3(GetVerticalAngle(rightShoulder.position, player.position),
                        transform.eulerAngles.y, transform.eulerAngles.z);
                float midpoint = (waveArmHighAngle + waveArmLowAngle) / 2f;
                float scalar = waveArmHighAngle - midpoint;
                float waveAngle = midpoint + prog * scalar;
                rightShoulder.localEulerAngles = Vector3.forward * waveAngle;
            }
            else //raising arm and tilting body
            {
                if (player.position != rightShoulder.position)
                    transform.eulerAngles = new Vector3(GetVerticalAngle(rightShoulder.position, player.position) * prog,
                        transform.eulerAngles.y, transform.eulerAngles.z);
                rightShoulder.localEulerAngles = Vector3.forward * prog * waveArmHighAngle;
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, prog * waveBodyTiltAngle);
            }

            yield return null;
        }

        StartCoroutine(AnimateBackToBusiness());
        yield return null;
    }

    IEnumerator AnimateIdle()
    {
        currAnimation = Animation.idle;

        yield return new WaitForSeconds(Random.Range(minVisitTime, maxVisitTime));
        
        AssignNewDestination();

        yield return null;
    }

    //turn away from player and continue going to the destination
    IEnumerator AnimateBackToBusiness()
    {
        currAnimation = Animation.backToBusiness;

        //only applicable if the destination isn't already reached
        if (new Vector2(transform.position.x, transform.position.z) != destination)
        {

            Vector3 startBodyAngle = transform.eulerAngles;
            float startShouderZAngle = rightShoulder.localEulerAngles.z;
            float targetAngle = GetAngleXZPlane(transform.position + Vector3.forward, transform.position,
                new Vector3(destination.x, 0, destination.y)) * Mathf.Rad2Deg;

            if (startBodyAngle.y != targetAngle)
            {
                while (true)
                {
                    //animate
                    float prog = Mathf.Abs(transform.eulerAngles.y - startBodyAngle.y) / Mathf.Abs(targetAngle - startBodyAngle.y);
                    rightShoulder.localEulerAngles = Vector3.forward * (startShouderZAngle * (1 - prog));
                    //transform.eulerAngles = new Vector3(startBodyAngle.x * (1 - prog), transform.eulerAngles.y, startBodyAngle.z * (1 - prog));
                    transform.position = new Vector3(transform.position.x,
                        defaultYVal + walkBounceAmount * Mathf.Sin(prog * Mathf.PI) * transform.localScale.y,
                        transform.position.z);

                    //turn
                    float turnAmount = Time.deltaTime * turnSpeed;
                    float remainingTurn = Mathf.Abs(targetAngle - transform.eulerAngles.y);
                    if (remainingTurn <= turnAmount) //see if we're done turning yet
                        break;
                    else //physically turn
                    {
                        if (transform.eulerAngles.y < targetAngle)
                            transform.eulerAngles += Vector3.up * turnAmount;
                        else
                            transform.eulerAngles += Vector3.down * turnAmount;
                    }

                    yield return null;
                }
            }
        }

        StartCoroutine(AnimateWalking());
        yield return null;
    }

    float GetAngleXZPlane(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        //convert to two dimensions, since height is irrelevant here
        pointA = new Vector3(pointA.x, 0, pointA.z);
        pointB = new Vector3(pointB.x, 0, pointB.z);
        pointC = new Vector3(pointC.x, 0, pointC.z);

        float angle = GetAngle(pointA, pointB, pointC);

        if (float.IsNaN(angle))
        {
            Debug.Log("PAY ATTENTION TO THIS:");
            Debug.Log(pointA);
            Debug.Log(pointB);
            Debug.Log(pointC);
        }

        if (pointC.x < pointB.x)
            return angle + Mathf.PI;
        else if (pointC.x > pointB.x)
            return Mathf.PI - angle;
        else
            return angle;
    }

    float GetVerticalAngle(Vector3 citizenPos, Vector3 cameraPos)
    {
        Vector3 pointA = new Vector3(cameraPos.x, citizenPos.y, cameraPos.z);
        Vector3 pointB = citizenPos;
        Vector3 pointC = cameraPos;

        float angle = GetAngle(pointA, pointB, pointC);
        angle = angle * Mathf.Rad2Deg;
        if (cameraPos.y > citizenPos.y)
            return angle - 180f;
        else if (cameraPos.y == citizenPos.y)
            return 0f;
        else
            return 180f - angle;
    }

    float GetAngle(Vector3 pointA, Vector3 pointB, Vector3 pointC)
    {
        Vector3 A = pointB - pointA;
        Vector3 B = pointC - pointB;
        float dotProduct = A.x * B.x + A.z * B.z;
        float magnitudeProduct = A.magnitude * B.magnitude;
        return Mathf.Acos(dotProduct / magnitudeProduct);
    }
}
