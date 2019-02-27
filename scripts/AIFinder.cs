using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

//Class basically used for finding enemies and guns, and acts as the brains
//for the AI. Basically, the "SmoothMouseLook" script
public class AIFinder : MonoBehaviour {
    
    public GameObject parent;
    [SerializeField]
    private aiPlayerController apc;
    [SerializeField]
    private aiBlasterController abc;

    private float interactLength = 30f;

    Quaternion originalRotation;

    private float gunDist;

    //A mask used to allow bullets to go through blasters, I hardcoded an int but if
    //more layers are ever added, this will need to change
    private int layerMask = 511;

    private List<GameObject> touchingObjs = new List<GameObject>();
    private GameObject targetObj;

    private List<GameObject> touchingPlayers = new List<GameObject>();

    //Not to be confused with hasBlater, this checks if we own a blaster
    public bool ownsBlaster;

    //are we locked in and finding a blaster
    public bool hasBlaster;
    //do we have something we're chasing either player or blaster
    public bool hasTarget;
    //are we chasing a player
    public bool hasPlayer;

    //are we running towards the center, or are we running to a point
    public bool runToCenter;

    private bool dead;

    public bool shooting;

    //Following variables used to change the frequency of certain calls
    private int targetCheck;
    private const int TARGET_CHECK_FRAME_COUNT = 10;

    private int playerTargetCheck;
    private const int PLAYER_TARGET_FRAME_COUNT = 3;

    //the point we're running to
    public Vector3 targetPoint;

    private int circleLength = 105625;
    //Miscounted it actually spans from -300x, -300z to 400x, 400z 350 distance instead of 300
    //The points to run to when within the 10 ft circle of the center
    private Vector3[] runPoints = { new Vector3(350, 1, 0), new Vector3(-250, 1, 0)
        ,new Vector3(0,1,350), new Vector3(0,1,-250), new Vector3 (350,1,310),
        new Vector3 (-210,1,-250), new Vector3 (350,5, -250), new Vector3 (-210,1,350), new Vector3 (60,1,10)};

    //used to manage the start of games
    private bool gameStarted;
    private bool countdown;

    private double gameTimer;

    private Blaster targetBlaster;

    private bool questionReloading;

    private int checkNum;

    void Update()
    {
        if (gameStarted)
        {
            if (!dead)
            {
                //if has a gun, find player or nearby guns as long as not chasing a player
                if (ownsBlaster)
                {
                    //If you don't currently have a target, check once every ___ frames
                    if (!hasTarget)
                    {
                        targetCheck++;
                        if (targetCheck == TARGET_CHECK_FRAME_COUNT)
                        {
                            targetCheck = 0;
                            float distance = 500f;
                            float tempDist;
                            int foundIndex = 0;
                            for (int i = 0; i < touchingPlayers.Count; i++)
                            {
                                if (touchingPlayers[i].tag.Equals("Player") || touchingPlayers[i].tag.Equals("AI"))
                                {

                                    tempDist = Vector3.Distance(transform.position,
                                                                touchingPlayers[i].transform.position);
                                    if (tempDist < distance)
                                    {
                                        distance = tempDist;
                                        foundIndex = i;
                                        hasPlayer = true;
                                        hasTarget = true;
                                    }

                                }
                               


                            }
                            //keep following that player unless another one comes closer
                            if (hasPlayer && hasTarget)
                            {
                                targetObj = touchingPlayers[foundIndex];
                                targetPoint = touchingPlayers[foundIndex].transform.position;
                                apc.setTarget(targetObj, true);
                            }
                            //if no players are contained in the area
                            else
                            {
                                if (Vector3.Distance(targetPoint, parent.transform.position) < 25f)
                                {
                                    int rand = Random.Range(0, runPoints.Length);
                                    targetPoint = runPoints[rand];
                                    //Debug.Log("Running to center");
                                    //if close to the origin point, run away in a direction
                                    //if (transform.position.sqrMagnitude < 500f)
                                    //{
                                    //Debug.Log("Running away from the center");

                                    //runToCenter = false;

                                    //}
                                }
                                //check if far away from origin and return if so
                                //else
                                //{
                                //    if (transform.position.sqrMagnitude > 80000f)
                                //    {
                                //        //Debug.Log("Going back to old Nassau");
                                //        runToCenter = true;
                                //        targetPoint = Vector3.zero;
                                //    }
                                //}
                            }
                        }
                    }

                    //if you have a target, lock on to it's position,
                    //if it's a player and they're in range and you can shoot
                    //blast 'em
                    else
                    {
                        playerTargetCheck++;
                        if (playerTargetCheck == PLAYER_TARGET_FRAME_COUNT)
                        {
                            playerTargetCheck = 0;
                            targetPoint = targetObj.transform.position;

                            //if targeting player
                            if (hasPlayer)
                            {
                                if (Vector3.Distance(targetPoint, transform.position) <= gunDist)
                                {
                                    if (!shooting)
                                    {
                                        shooting = true;
                                    }
                                }
                                else
                                {
                                    shooting = false;
                                }
                                if ((Vector3.Distance(targetPoint, transform.position) <= gunDist)
                                   && abc.canShoot())
                                {
                                    questionReloading = false;
                                    //if you want the effect instantiated at a point
                                    if (abc.getBlasterType() == 1)
                                    {
                                        shoot1(abc.getName() + "effect", abc.getDamage(), targetObj);
                                        abc.incrementAmmo();
                                        abc.shooting();
                                        apc.setShooting(true);

                                    }
                                    //if you want a trail to be shot
                                    if (abc.getBlasterType() == 2 || abc.getBlasterType() == 3)
                                    {
                                        shoot2(abc.getName() + "effect", abc.getDamage(), abc.getGunTipPosition(), targetObj);
                                        abc.incrementAmmo();
                                        abc.shooting();
                                        apc.setShooting(true);
                                    }
                                }
                                else
                                {
                                    if (!abc.isShooting())
                                    {
                                        apc.setShooting(false);
                                    }
                                    if (!abc.hasAmmo() && !questionReloading)
                                    {
                                        abc.startAnswerFakeQuestion();
                                        questionReloading = true;
                                    }
                                    //if out of your gun's range, pursue but act as if you don't have a player target
                                    //to prevent shooting
                                    if ((Vector3.Distance(targetPoint, transform.position) > gunDist))
                                    {
                                        hasPlayer = false;
                                    }
                                }
                            }
                        }
                    }
                }
                //if you don't have a gun, priority number one is finding one. Advance towards
                //the origin
                else
                {
                    //Are we already chasing a gun?
                    if (hasBlaster)
                    {
                        if (targetBlaster.getIsOwned())
                        {
                            hasBlaster = false;
                            hasTarget = false;
                            int rand = Random.Range(0, runPoints.Length);
                            targetPoint = runPoints[rand];
                        }
                    }
                    //we don't have a gun and we aren't chasing one. Is one contained in 
                    //our collider?
                    else
                    {
                        //Check if we have a blaster in our current list of contained objs
                        //made float really high so it is larger than the distance to an item
                        float distance = 500f;
                        float tempDist;
                        int foundIndex = 0;
                        for (int i = 0; i < touchingObjs.Count; i++)
                        {
                            tempDist = Vector3.Distance(transform.position,
                                                        touchingObjs[i].transform.position);
                            if (tempDist < distance)
                            {
                                distance = tempDist;
                                foundIndex = i;
                                hasBlaster = true;
                                hasTarget = true;
                            }
                        }

                        //Checks if a blaster has been successfully located
                        //if so, update travel location
                        if (hasBlaster && hasTarget)
                        {
                            targetObj = touchingObjs[foundIndex];
                            targetPoint = targetObj.transform.position;
                            targetBlaster = targetObj.GetComponent<Blaster>();
                            apc.setTarget(targetObj, false);
                        }
                        //if moving towards the center and not within 10 ft, 
                        else
                        {  
                            if (Vector3.Distance(targetPoint, parent.transform.position) < 25f)
                            {
                                int rand = Random.Range(0, runPoints.Length);
                                targetPoint = runPoints[rand];
                            }

                        }

                    }

                }

            }
        }
        else
        {
            if (countdown)
            {
                if (PhotonNetwork.Time > gameTimer)
                {
                    countdown = false;
                    gameStarted = true;
                    int rand = Random.Range(0, runPoints.Length);
                    targetPoint = runPoints[rand];
                    Debug.Log("target point set");
                }
            }
        }
    }


    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.freezeRotation = true;
        originalRotation = transform.localRotation;
        gunDist = interactLength;
        originalRotation = transform.localRotation;
        parent = transform.parent.gameObject;
        hasBlaster = false;
        hasPlayer = false;
        hasTarget = false;
        ownsBlaster = false;
        //runToCenter = true;
        gameStarted = false;
        countdown = false;
        dead = false;
        questionReloading = false;
        int rand = Random.Range(0, runPoints.Length);
        targetPoint = runPoints[rand];
        checkNum = 0;
    }

    public void setDead(bool isDead){
        dead = isDead;
    }

    public void justGotBlasted(){
        //hasBlaster = false;
        hasPlayer = false;
        hasTarget = false;
        //ownsBlaster = false;
        int rand = Random.Range(0, runPoints.Length);
        targetPoint = runPoints[rand];
    }



    //used to say that you own the blaster
    public void setOwnsBlaster(bool hasBlast){
        ownsBlaster = hasBlast;
        if (hasBlast)
        {
            hasTarget = false;
            hasBlaster = false;
            int rand = Random.Range(0, runPoints.Length);
            targetPoint = runPoints[rand];
        }
    }
    public void setGunDistance(float distance)
    {
        gunDist = distance;
    }
    //the first kind of shooting where an effect is instantiated on a spot used for BigBabyBlaster
    // public void shoot1(GameObject effect, int damage){
    //chooses a random number between 1 and 20, adds the level multiplier and if the 
    //total is greater than 17
    public void shoot1(string effect, int damage, GameObject playerTarget)
    {
        //was constantly shooting at the ground so correct byaiming for half height
        Vector3 targetDirYFix = playerTarget.transform.position;
        targetDirYFix.y += 1;
        Vector3 directionPlayer = targetDirYFix - transform.position;

        //Vector3 directionPlayer = playerTarget.transform.position - transform.position;
        directionPlayer.Normalize();
       // directionPlayer.y += 2f;



        RaycastHit worldHit;
        //if something is hit
        if (Physics.Raycast(transform.position, directionPlayer , out worldHit, gunDist))
        {
            //Debug.DrawRay(transform.position, directionPlayer * worldHit.distance, Color.red, 10f);
            //Debug.Log("dist: " + worldHit.distance);
            if (worldHit.collider.tag.Equals("Player") || worldHit.collider.tag.Equals("AI") || worldHit.collider.tag.Equals("item"))
            {
                //if (worldHit.collider.tag.Equals("AI"))
                //{
                //    Debug.Log(apc.getName() + " shooting at " + worldHit.collider.gameObject.GetComponent<aiPlayerController>().getName());
                //}
                //Debug.Log("we live");
                //Debug.Log("point y: " + worldHit.point.y);

                int rand = Random.Range(1, 21);
                rand += apc.getLevel();
                //used to make closer shots more accurate
                int dist = (int) Vector3.Distance(parent.transform.position, targetPoint);
                if(dist < 20){
                    rand += 5;
                }
                //y=-x/6  + 25/3 equation to make at 50,0 and at 20, 5
                else if(dist < 50){
                    rand += Mathf.RoundToInt((-1 / 6) * dist + (25 / 3));
                }
                //Debug.Log("dist: " + dist + "rand: " + rand);
                if (rand > 17)
                {
                    //Debug.Log("Gotcha");
                    bool isRed;
                    isRed = apc.checkIfRedTeam();
                    playerTarget.GetComponent<PhotonView>().RPC("applyDamage", RpcTarget.AllBufferedViaServer, damage, parent.GetComponent<PhotonView>().ViewID, isRed);
                    PhotonNetwork.Instantiate(effect, playerTarget.transform.position, Quaternion.identity, 0);
                }
                else
                {
                    int randMiss1 = Random.Range(0, 21);
                    int randMiss2 = Random.Range(0, 21);
                    Vector3 splatPoint = playerTarget.transform.position;
                    splatPoint.x += 10 - randMiss1;
                    splatPoint.z += 10 - randMiss2;
                    splatPoint.y += 1;
                    PhotonNetwork.Instantiate(effect, splatPoint, Quaternion.identity, 0);
                }
            }
            else{
                //Debug.Log("miss but hit something");
                //Debug.Log("point y: " + worldHit.point.y);
//                Debug.Log("collider was " + worldHit.collider.tag);
                PhotonNetwork.Instantiate(effect, worldHit.point, Quaternion.identity, 0);
            }
        }

    }




    //the second kind of shooting where an effect is shot 
    //public void shoot2(GameObject effect, int damage, Vector3 gunTip) {
    public void shoot2(string effect, int damage, Vector3 gunTip, GameObject playerTarget)
    {
        Vector3 targetDirYFix = playerTarget.transform.position;
        targetDirYFix.y += 1;
        Vector3 directionPlayer = targetDirYFix - transform.position;
        directionPlayer.Normalize();
        //directionPlayer.y += 2f;


        RaycastHit worldHit;
        //if something is hit
        if (Physics.Raycast(gunTip, directionPlayer, out worldHit, gunDist))
        {
            //Debug.DrawRay(transform.position * worldHit.distance, directionPlayer, Color.red, 10f);
            //Debug.Log("dist: " + worldHit.distance);
            //Debug.Log("point y: " + worldHit.point.y);
            if (worldHit.collider.tag.Equals("Player") || worldHit.collider.tag.Equals("AI"))
            {
                //Debug.Log("we live2");
                int rand = Random.Range(1, 21);
                rand += apc.getLevel();
                //used to make closer shots more accurate
                int dist = (int)Vector3.Distance(parent.transform.position, targetPoint);
                if (dist < 20)
                {
                    rand += 5;
                }
                //y=-x/6  + 25/3 equation to make at 50,0 and at 20, 5
                else if (dist < 50)
                {
                    rand += Mathf.RoundToInt((-1 / 6) * dist + (25 / 3));
                }
                if (rand > 17)
                {
                    //Debug.Log("gotcha long range");
                    bool isRed;
                    isRed = apc.checkIfRedTeam();
                    playerTarget.GetComponent<PhotonView>().RPC("applyDamage", RpcTarget.AllBufferedViaServer, damage, parent.GetComponent<PhotonView>().ViewID, isRed);
                    GameObject tempEff = PhotonNetwork.Instantiate(effect, gunTip, transform.rotation, 0);
                    //tempEff.GetComponent<ParticleSystem>().velocityOverLifetime.
                    //tempEff.GetComponent<moveToPoint>().move(playerTarget.transform.position);
                }
                else
                {
                    int randMiss1 = Random.Range(0, 21);
                    int randMiss2 = Random.Range(0, 21);
                    Vector3 splatPoint = playerTarget.transform.position;
                    splatPoint.x += 10 - randMiss1;
                    splatPoint.z += 10 - randMiss2;
                    GameObject tempEff = PhotonNetwork.Instantiate(effect, gunTip, transform.rotation, 0);
                    //tempEff.GetComponent<moveToPoint>().move(splatPoint);
                }
            }
            else{
                //Debug.Log("Blocked but hit somewhere");
                //Debug.Log("point y: " + worldHit.point.y);
                GameObject tempEff = PhotonNetwork.Instantiate(effect, gunTip, transform.rotation, 0);
                //tempEff.GetComponent<moveToPoint>().move(worldHit.point);
            }
        }

    }
    /// <summary>
    /// If the object within the collider is an item, check to see if it's owned,
    /// if it is, remove it from touching objs if it was contained. If not, add it
    /// changing this so it checks once every 10 times
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        checkNum++;
        if (checkNum == 20)
        {
            checkNum = 0;
            if (gameStarted)
            {
                if (other.tag.Equals("item"))
                {
                    if (other.GetComponent<Blaster>().getBlasterType() != 4)
                    {
                        if (!touchingObjs.Contains(other.gameObject))
                        {

                            if (!other.GetComponent<Blaster>().getIsOwned())
                            {
                                touchingObjs.Add(other.gameObject);
                            }
                            else
                            {
                                touchingObjs.Remove(other.gameObject);
                            }

                        }
                        else
                        {
                            if (other.GetComponent<Blaster>().getIsOwned())
                            {
                                touchingObjs.Remove(other.gameObject);
                            }
                        }
                    }

                }
                else if (other.tag.Equals("AI"))
                {
                    if (!touchingPlayers.Contains(other.gameObject))
                    {
                        if (other.GetComponent<aiPlayerController>().checkIfRedTeam() !=
                           apc.checkIfRedTeam())
                        {
                            touchingPlayers.Add(other.gameObject);
                        }
                    }
                }
                else if (other.tag.Equals("Player"))
                {
                    if (!touchingPlayers.Contains(other.gameObject))
                    {
                        if (other.GetComponent<playerController>().checkIfRedTeam() !=
                            apc.checkIfRedTeam())
                        {
                            touchingPlayers.Add(other.gameObject);
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (gameStarted)
        {
            if (other.tag.Equals("item"))
            {
                touchingObjs.Remove(other.gameObject);
            }
            else if(other.tag.Equals("Player") || other.tag.Equals("AI")){
                touchingPlayers.Remove(other.gameObject);
            }
        }
        if(other.gameObject.Equals(targetObj)){
            hasPlayer = false;
            hasTarget = false;
            hasBlaster = false;
            int rand = Random.Range(0, runPoints.Length);
            targetPoint = runPoints[rand];
        }
    }
    public Vector3 getTarget(){
        return targetPoint;
    }
    public void startGame(){
        gameStarted = true;
    }
    public void setGameTimer(double endTime)
    {
        gameTimer = endTime;
        countdown = true;
    }

    //Actually runs randomly now
    public void resetTargetAndRunRandom(){
        //runToCenter = true;
        hasTarget = false;
        hasPlayer = false;
        int rand = Random.Range(0, runPoints.Length);
        targetPoint = runPoints[rand];
        //targetPoint = Vector3.zero;
    }
    //Generates a random player name 3 forms:
    // name 10%
    // name + number 40%
    // number + name + number 50%

}
