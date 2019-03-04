using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine.SceneManagement;

//A script basically duplicating the playerController, but with autonomous 
//controls
public class aiPlayerController : MonoBehaviour, PlayerInterface {


    //movement controls
    public float speed = 11.0F;
    public float jumpSpeed = 8.0F;
    public float gravity = 20.0F;
    public float sprintMod = 1.5f;


    //Used to limit certian calls
    private int checkNum;
    private Vector3 newVect;

    public GameObject sprintObj;
    private bool sprinting;

    //Controllers
    public Animator ac;
    public CharacterController controller;
    public Vector3 moveDirection = Vector3.zero;

    private bool dead;
    private PhotonView pView;

    private PunTeams.Team team;

    //Used to reset speed after water
    private float defaultSpeed;
    private float defaultJumpSpeed;

    //Red Team or Blue Team
    private bool redTeam;
    private gameNetworkController gnc;

    //The properties of the overarching player
    private ExitGames.Client.Photon.Hashtable playerProp;
    private ExitGames.Client.Photon.Hashtable currentProp;

    private bool gameStarted;

    //has the countdown been initiated
    private bool countdown = false;

    private double gameTimer;

    [SerializeField]
    private NavMeshAgent navAgent;

    //a link to the players hand to be used for setting up the Gun
    [SerializeField]
    private GameObject hand;

    [SerializeField]
    private aiBlasterController abc;

    [SerializeField]
    private skinSelector skins;

    [SerializeField]
    private AIFinder aif;

    public Vector3 targetPosition = Vector3.zero;

    private int redScore;
    private int blueScore;
    /// <summary>
    /// ///////////////temporarily public
    /// </summary>
    public int hp;

    //When puffs of smoke appear
    private float runTime;
    private float nextRun;

    private int waterPacks;

    public GameObject target;
    public bool isTargetPlayer;

    //A skill level 1-10 that determines certain factors
    private int skillLevel;

    //used in place of a camera
    [SerializeField]
    private GameObject followObj;

    [SerializeField]
    private string playerName;

    private string[] possibleNames = new string[]{"Danny", "Chelsea", "HotDogMan",
    "Sophia", "Sarah", "Aaron", "Wizard", "Chight", "BbyGirl", "Liam", "Olivia",
    "Ava", "Emma", "TheDarkLord", "Logan", "CrayonBoy", "Unicorn", "uCntTUcHDis",
    "gnome", "BigLose", "goodBad", "nobody", "nbody", "gator", "Crocodile",
    "SpongeRob", "noob", "TomAndGary", "PickleRick", "RickSanchez", "TenTitnsNO",
    "Chillary", "yeet", "postmanaAA", "WdsdyMyDuds", "SquidDab", "AvcadoThx",
        "FreeShavcado", "whutRthose", "Eagle", "Crocs", "tclkmElmo", "randumb", 
    "kenprincss", "firestar", "saxOfone", "lordluvr","sPonGeBuB", "poohBear", "doggie",
    "Candance"};

    [SerializeField]
    private Canvas namePlateCanvas;
    [SerializeField]
    private Text usernameText;

    //Used to gauge if the AI is stuck on something. If paused for more than a second
    //Jump up and forward
    private Vector3 prevPosition;
    private float nextPosCheck;
    [SerializeField]
    private bool jumping;

    //A complex series of steps in an attempt to rotate the aiPlayer towards the target when shooting
    //checks if the angle to the target is between 20 and -20 degrees
    private bool angleClose;
    private bool isShooting;

    public int aiNumber;

    private bool debugText;
    public float distance;

    private Vector3 movementVector;

    public bool pathPending;

    //Everything to be done on all players
    void Start()
    {
        //newly added condition in hopes of preventing error in finding gnc
        if (!pView.IsMine)
        {
            return;
        }
        gameStarted = false;
        gnc = GameObject.FindGameObjectWithTag("GNC").GetComponent<gameNetworkController>();
        defaultSpeed = speed;
        defaultJumpSpeed = jumpSpeed;
        dead = false;

        hp = 100;
        sprinting = false;
        runTime = .4f;
        waterPacks = 0;
        nextPosCheck = 0f;
        //random skill level 1-10
        skillLevel = Random.Range(1, 11);
    }

    public void initializeSkins(){
        playerProp = PhotonNetwork.MasterClient.CustomProperties;
        if (playerProp == null)
        {
            Debug.Log("Initializing properties");
            playerProp = new ExitGames.Client.Photon.Hashtable();
        }
        int[] skinMatVals = skins.setMySkin(-1);
        playerProp.Add("skin" + aiNumber, skinMatVals[0]);
        playerProp.Add("mat" + aiNumber, skinMatVals[1]);
        playerProp.Add("username" + aiNumber, playerName);

        PhotonNetwork.MasterClient.SetCustomProperties(playerProp); 

        if (team.Equals(PhotonNetwork.LocalPlayer.GetTeam()))
        {
            usernameText.text = playerName;
            namePlateCanvas.GetComponent<cameraBillboard>()
                           .setCam(GameObject.FindGameObjectWithTag("cam")
                           .GetComponent<Camera>());
        }
        else
        {
            namePlateCanvas.gameObject.SetActive(false);
        }
    }
    //To be done on only the player who created this AI
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        debugText = false;
        pView = GetComponent<PhotonView>();
        if (!pView.IsMine)
        {
            return;
        }
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.buildIndex == 0)
        {
            Debug.Log("Destroying AI because scene switched");
            PhotonNetwork.Destroy(gameObject);
        }
    }
    private void FixedUpdate()
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (gameStarted)
        {
            if (!dead)
            {
                //Once the Player is grounded
                if (controller.isGrounded)
                {
                    if (navAgent.isActiveAndEnabled)
                    {
                        if (navAgent.isOnNavMesh)
                        {

                            targetPosition = aif.getTarget();
                            if (!targetPosition.AlmostEquals(navAgent.destination, 15f)
                                && !navAgent.pathPending
                                && Vector3.Distance(transform.position, targetPosition) > 10f)
                            {
                                findNewTarget();
                            }
                            else if (navAgent.pathPending && !transform.position.AlmostEquals(navAgent.destination, 15f))
                            {
                                pathPending = true;
                                if (aiNumber == 4)
                                {
                                    Debug.Log("Manually moving " + playerName);
                                }
                                movePlayerForward();

                            }
                            else
                            {
                                pathPending = false;
                                if (aiNumber == 4)
                                {
                                    Debug.Log("Auto move enabled " + playerName);
                                }
                            }
                            //turns towards the player it's attacking
                            if (isTargetPlayer && navAgent.remainingDistance < 15f)
                            {
                                if (aiNumber == 4)
                                {
                                    Debug.Log("Rotating Playa " + playerName);
                                }
                                movementVector = (targetPosition - transform.position).normalized;
                                rotateToTarget(5f);
                            }
                        }
                        else
                        {
                            if (aiNumber == 4)
                            {
                                Debug.Log("jumping because I'm off the navmesh -" + playerName);
                            }
                            //Debug.Log(playerName + " jumping because I'm off the navmesh");
                            jumpForwards(15f);
                        }
                    }
                    else
                    {
                        //Debug.Log("Enabling Nav Agent " + playerName);
                        navAgent.enabled = true;
                        if (navAgent.isOnNavMesh 
                            && !navAgent.pathPending 
                            && !targetPosition.AlmostEquals(navAgent.destination, 15f))
                        {

                            //Debug.Log("setting dest on " + playerName);
                            findNewTarget();
                           // if (!targetPosition.AlmostEquals(navAgent.destination, 5f))
                          //  {
                          //      navAgent.SetDestination(targetPosition);
                                //Debug.Log(playerName + " moving to " + navAgent.destination.ToString() + " from " + targetPosition.ToString());
                           // }
                          //  else
                          //  {
                                //Debug.Log(playerName + " is basically at my destination " + navAgent.destination.ToString() + " from " + targetPosition.ToString());
                         //   }
                            /* }*/
                        }
                        /*Debug.Log(playerName + " is grounded apparently with navagent off");
                        navAgent.enabled = true;
                        jumping = false;
                        targetPosition = aif.getTarget();
                        if (navAgent.isOnNavMesh)
                        {
                            if (!targetPosition.AlmostEquals(navAgent.destination, 5f))
                            {
                                navAgent.SetDestination(targetPosition);
                            }
                        }*/
                    }

                    if (jumping)
                    {
                        if (aiNumber == 4)
                        {
                            Debug.Log("landed! -" + playerName);
                        }
                        //next jump randomly between .6 and 1 so they're not jumping in unison
                        // nextPosCheck += Random.Range(.6f, 3.5f);//was 1.2
                        // moveDirection = Vector3.zero;
                        //Debug.Log(playerName + " finished jump");
                        navAgent.enabled = true;
                        jumping = false;

                        if (isTargetPlayer)
                        {
                            nextPosCheck += Random.Range(.05f, 2f);
                        }
                        else
                        {
                            nextPosCheck += Random.Range(.5f, 5f);
                        }
                        if (navAgent.isActiveAndEnabled)
                        {
                            //distance = navAgent.remainingDistance;
                            if (navAgent.isOnNavMesh)
                            {
                                if (!targetPosition.AlmostEquals(navAgent.destination, 5f) && !navAgent.pathPending)
                                //if (!targetPosition.AlmostEquals(navAgent.destination, 5f))
                                {
                                    //Debug.Log("Resetting position post jump for " + playerName);
                                    findNewTarget();
                                }
                                /* }*/
                            }
                        }
                            // targetPosition = aif.getTarget();
                            /*if (navAgent.isOnNavMesh)
                            {
                                if (!targetPosition.AlmostEquals(navAgent.destination, 5f))
                               // if (!transform.position.AlmostEquals(navAgent.destination, 5f))
                                {*/
                            //if (navAgent.isOnNavMesh)
                            //{
                            //    navAgent.SetDestination(targetPosition);
                            //}
                            /*}
                        }*/

                        }
                    //This code was going to be used to see if facing correctly

                    //if(isShooting){
                    //    float angle = Vector3.Angle(transform.position, target.transform.position);
                    //    //Debug.Log("Angle: " + angle);
                    //    if(angle > -20f && angle < 20f) {
                    //        angleClose = true;
                    //    }
                    //    else{
                    //        angleClose = false;
                    //    }

                    //}

                    ac.SetBool("jumping", false);

                    if (!(navAgent.velocity.magnitude > 0f) && !pathPending)
                    {
                        ac.SetBool("running", false);
                    }
                    else
                    {
                        ac.SetBool("running", true);
                    }
                    if (navAgent.isOnNavMesh)
                    {
                        if (navAgent.remainingDistance > 10f)
                        {
                            //nextRun = Time.time + runTime;
                            sprinting = true;
                            navAgent.speed = 17f;
                        }
                        else
                        {
                            sprinting = false;
                            navAgent.speed = 11f;
                        }
                    }

                    //int randJump = Random.Range(0, 1000);
                    //if(skillLevel * 10f > randJump){
                    //    moveDirection = transform.forward * 2;
                    //    moveDirection.y = jumpSpeed;
                    //    ac.SetBool("jumping", true);
                    //    navAgent.enabled = false;
                    //}
                    //else{
                    //    ac.SetBool("jumping", false);
                    //}

                }
                //If the player isn't grounded move down
                else
                {
                    //navAgent.enabled = false;
                   // Debug.Log(playerName + " I'm floating");
                    moveDirection.y -= gravity * Time.fixedDeltaTime;
                    controller.Move(moveDirection * Time.fixedDeltaTime);

                }
                //if (jumping)
                //{
                   
                //    // moveDirection.y -= gravity * Time.deltaTime;
                //    // controller.Move(moveDirection * Time.deltaTime);
                //}

                //It's jumping time if we're not moving!
                //Space out next pos checks over time instead of frames to 
                //give a feel that AI isn't pausing
                if (nextPosCheck < Time.time)
                {
                    nextPosCheck = Time.time + Random.Range(.5f, 2.5f);

                    //If we haven't moved, and the navAgent's path is ready
                    if (prevPosition.AlmostEquals(transform.position, 1f) && !jumping)// && !navAgent.pathPending)
                   // if (prevPosition.AlmostEquals(transform.position, 1f) && !jumping)
                    {
                        jumpForwards(15f);
                        controller.Move(moveDirection * Time.fixedDeltaTime);
                    }
                    //We have moved since last check
                    else
                    {
                        if (navAgent.pathPending && navAgent.isOnNavMesh)
                        {
                            //If we're close to a player and not close to 
                            if (navAgent.remainingDistance <= 17 && isTargetPlayer)
                            {
                                if (transform.position.magnitude < 250f)
                                {
                                   
                                    //chance to jump to emulate a good player who shoots while jumping
                                    int randAttack = Random.Range(0, skillLevel);
                                    if (randAttack > 5)
                                    {
                                        //Jumpas randomly
                                        jumpRandomDir(15f);
                                    }

                                }
                                else
                                {
                                    //chance to jump to emulate a good player who shoots while jumping
                                    int randAttack = Random.Range(0, skillLevel);
                                    if (randAttack > 5)
                                    {
                                        jumpForwards(Random.Range(0f, 15f));
                                    }
                                }
                            }
                            //Not targeting player or close so one in 5 chance of
                            //jumping towards target. 
                            else
                            {
                                int randomNum = Random.Range(0, 5);
                                if(randomNum == 5)
                                {
                                    //Debug.Log("oops, jumoing " + playerName);
                                    jumpForwards(15f);
                                }
                            }
                        }
                        else
                        {
                            if (navAgent.pathPending)
                            {
                                jumpRandomDir(Random.Range(0f, 15f));
                                //Debug.Log("check back later homie, still pending my path " + playerName);
                            }
                            else
                            {
                                //Debug.Log("No jump cuz not on navmesh");
                            }
                        }
                    }

                    prevPosition = transform.position;
                }
            }
        }
    }

    void Update()
    {
        if (!pView.IsMine)
        {
            return;
        }

        //redScore = gnc.getScore(true);
        //blueScore = gnc.getScore(false);
        //Debug.Log("Update functional");
        if (gameStarted)
        {
            //Debug.Log("Game started");
            if (!dead)
            {
                if (abc.hasGun())
                {
                    if (abc.canUseWaterPack() && waterPacks > 0)
                    {
                        abc.applyWaterPack();
                        waterPacks--;
                    }
                    if (abc.isReloading())
                    {
                        if (!ac.GetBool("reloading"))
                        {
                            ac.SetBool("reloading", true);
                        }
                    }
                    else
                    {
                        ac.SetBool("reloading", false);
                        if (!abc.hasAmmoInClip())
                        {
                            if (abc.canReload())
                            {
                                abc.reload();
                            }
                        }
                    }
                }
                if (sprinting && Time.time > nextRun)
                {
                    newVect = transform.position;
                    newVect.y = 1.5f;
                    //Instantiate(sprintObj, newVect, transform.rotation);
                    PhotonNetwork.Instantiate(sprintObj.name, newVect, transform.rotation, 0);
                    nextRun = Time.time + runTime;
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
                    if (navAgent.isOnNavMesh)
                    {
                        navAgent.SetDestination(targetPosition);
                    }
                    aif.startGame();
                }
            }
        }
    }

    public PunTeams.Team getTeam(){
        return team;
    }

    public void setTeamAndTimer(PunTeams.Team newTeam, double endTime, int aiNum, bool newChar, string name){
        team = newTeam;
        redTeam = team.Equals(PunTeams.Team.red);
        gameTimer = endTime;
        nextPosCheck = Time.time + 20f;
        countdown = true;
        aiNumber = aiNum;
        if (newChar)
        {
            generateName();
        }
        else
        {
            playerName = name;
            pView.RPC("setName", RpcTarget.AllBufferedViaServer, playerName);
            initializeSkins();
        }
    }
   
    public void initializeLocalAIIDontOwn(int numAI, bool isRed){
        if (!pView.IsMine)
        {
            if(isRed){
               team = PunTeams.Team.red;
            }
            else{
                team = PunTeams.Team.blue;
            }
            aiNumber = numAI;
            object obj;
            object mat;
            object plrNam;
            if(!PhotonNetwork.MasterClient.CustomProperties.TryGetValue("skin" + aiNumber, out obj)){
                Debug.Log("can't find skin");
            }
            if(!PhotonNetwork.MasterClient.CustomProperties.TryGetValue("mat" + aiNumber, out mat)){
                Debug.Log("Can't find material");
            }
            if (!PhotonNetwork.MasterClient.CustomProperties.TryGetValue("username" + aiNumber, out plrNam))
            {
                Debug.Log("Can't find name");
            }
            playerName = (string) plrNam;
            //Debug.Log("skin " + (int)obj + " mat " + (int)mat + " name " + playerName);
            skins.setOthersSkin((int)obj, (int)mat);

            if (team.Equals(PhotonNetwork.LocalPlayer.GetTeam()))
            {
                usernameText.text = playerName;
                namePlateCanvas.GetComponent<cameraBillboard>().setCam(GameObject.FindGameObjectWithTag("cam").GetComponent<Camera>());
            }
            else
            {
                namePlateCanvas.gameObject.SetActive(false);
            }
        }
        else{
            Debug.Log("You own the ai PhotonView");
        }
    }

    public void addHealth(int healthAmount)
    {
        hp += healthAmount;
    }

    private void LateUpdate()
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (dead && jumping && !controller.isGrounded && gameStarted)
        {
            moveDirection.y -= gravity * Time.deltaTime;
            controller.Move(moveDirection * Time.deltaTime);
        }
       
    }
    public void goIdle()
    {
        ac.SetBool("running", false);
        ac.SetBool("jumping", false);
    }

    public void hasBlaster(bool hasBlast)
    {
        ac.SetBool("hasGun", hasBlast);
    }



    private void OnTriggerEnter(Collider other)
    {
        if (!pView.IsMine)
        {
            return;
        }


        if (other.tag.Equals("item"))
        {
            if(!other.GetComponent<Blaster>().getIsOwned() && other.GetComponent<Blaster>().getBlasterType() != 4){
                abc.addBlaster(other.GetComponent<Blaster>());
            }
            //Physics.IgnoreCollision(GetComponent<Collider>(), other);
        }
        if (other.tag.Equals("itemSpawn"))
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), other);
        }


    }
    private void OnTriggerStay(Collider other)
    {
        if (!pView.IsMine)
        {
            return;
        }

        checkNum++;
        if (checkNum == 30)
        {
            checkNum = 0;

            if (other.gameObject.layer == 4)
            {
                speed = defaultSpeed / 2f;
                jumpSpeed = defaultJumpSpeed * .6f;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (other.gameObject.layer == 4)
        {
            speed = defaultSpeed;
            jumpSpeed = defaultJumpSpeed;
        }
    }



    public void setBlasterPosition(GameObject theBlaster)
    {
        int blastNum = theBlaster.GetComponent<PhotonView>().ViewID;
        if (theBlaster.GetComponent<Blaster>().getBlasterType() == 1)
        {
            pView.RPC("setAIBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 1);
        }
        else if (theBlaster.GetComponent<Blaster>().getBlasterType() == 2)
        {
            pView.RPC("setAIBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 2);
        }
        else if (theBlaster.GetComponent<Blaster>().getBlasterType() == 3)
        {
            pView.RPC("setAIBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 3);
        }
    }
    [PunRPC]
    public void setAIBlasterOnOthers(int blasterID, int playerID, int gunType)
    {
        GameObject tempHand = PhotonView.Find(playerID).GetComponent<aiPlayerController>().getHand();
        GameObject theBlaster = PhotonView.Find(blasterID).gameObject;
        theBlaster.transform.SetParent(tempHand.transform);

        if (gunType == 1)
        {
            theBlaster.transform.localPosition = new Vector3(.5f, .05f, -.01f);
            theBlaster.transform.localRotation = Quaternion.Euler(0f, -25f, -90f);
        }
        else if (gunType == 2)
        {
            theBlaster.transform.localPosition = new Vector3(.2f, .1f, 0);
            theBlaster.transform.localRotation = Quaternion.Euler(-90f, 180f, -110f);
        }
        else
        {
            theBlaster.transform.localPosition = new Vector3(.176f, .1f, -.05f);
            theBlaster.transform.localRotation = Quaternion.Euler(-180, -20, 70);
        }
    }
    public void loseHealth(int damage, int viewID)
    {
        hp -= damage;
        //This is deliberateley redundant so that the ai dies the moment it's dead
        if (hp <= 0 && !dead){
            ac.SetBool("dead", true);
        }
        GameObject attacker = PhotonView.Find(viewID).gameObject;
        if (attacker.tag.Equals("Player"))
        {
            attacker.GetComponent<PhotonView>().RPC("changeHitCrosshair", RpcTarget.AllBufferedViaServer, null);
        }
        if (hp <= 0 && !dead)
        {
            navAgent.enabled = false;
            controller.enabled = false;
            //increment the player's kills

            if (attacker.tag.Equals("Player"))
            {
                //attacker.GetComponent<playerController>().addBlastScore();
                attacker.GetComponent<PhotonView>().RPC("addBlastScore", RpcTarget.AllBufferedViaServer, null);
            }
            //increment AI kill
            else
            {
                attacker.GetComponentInChildren<AIFinder>().resetTargetAndRunRandom();
            }
            dead = true;
            aif.setDead(true);
            StartCoroutine(die(viewID));
        }
        else
        {
            //cause hit animation
        }


    }

    public IEnumerator die(int attackerPV)
    {
        //Debug.Log("die");
        yield return new WaitForSeconds(3f);
        resetPlayer(attackerPV);
    }


    public void addWaterPack()
    {
        waterPacks++;
    }



    [PunRPC]
    public void applyDamage(int damage, int attackerPV, bool isRed)
    {
        if (!pView.IsMine)
        {
            return;
        }
        if (isRed != redTeam)
        {
            loseHealth(damage, attackerPV);
        }
    }
    //checks if player is on red team
    public bool checkIfRedTeam()
    {
        return redTeam;
    }


    //resets the player to defaults
    public void resetPlayer(int attackerPV)
    {
        //Debug.Log("reset Player");
        gnc.respawnMe(pView, redTeam, attackerPV);
        moveDirection = Vector3.zero;
        aif.justGotBlasted();
        controller.enabled = true;
        controller.Move(moveDirection);

        //navAgent.enabled = true;
        hp = 100;
        sprinting = false;
        dead = false;
        aif.setDead(false);
        ac.SetBool("dead", false);
        waterPacks = 0;

    }

    //Gets the player's hand for use with gun positioning
    public GameObject getHand()
    {
        return hand;
    }

    public void setShooting(bool isShooting){
        ac.SetBool("shooting", isShooting);
        this.isShooting = isShooting;
    }

    //Used to set the targetted object true if given target is player
    public void setTarget(GameObject tarObj, bool isPlayer){
        target = tarObj;
        isTargetPlayer = isPlayer;
        if (isPlayer)
        {
            navAgent.stoppingDistance = 15f;
            //Debug.Log("chasing player");
        }
        else{
            navAgent.stoppingDistance = .5f;
        }
    }

    private void generateName()
    {
        int rand1 = Random.Range(0, possibleNames.Length);
        int rand2 = Random.Range(0, 999);
        int rand3 = Random.Range(0, 15);
        if (rand3 < 1)
        {
            playerName += possibleNames[rand1];
        }
        else if (rand3 < 10)
        {
            playerName += possibleNames[rand1];
            playerName += rand2;
        }
        else
        {
            playerName += rand2;
            playerName += possibleNames[rand1];
            playerName += Random.Range(1950, 2030);
        }
        pView.RPC("setName", RpcTarget.AllBufferedViaServer, playerName);
    }
    [PunRPC]
    public void setName(string nameSet){
        playerName = nameSet;
    }
    public int getLevel(){
        return skillLevel;
    }

    public int getAINum(){
        return aiNumber;
    }

    public string getName(){
        return playerName;
    }

    public void endGame(){
        gameStarted = false;
    }

    private void findNewTarget()
    {
        if (aiNumber == 4)
        {
            Debug.Log("Finding new target " + playerName);
        }
        // Debug.Log("Oh boy, new target for " + playerName);
        targetPosition = aif.getTarget();
        navAgent.SetDestination(targetPosition);
    }

    //The problem with this is that once a path has been found, even if it's incomplete
    //the player will begin to follow it kinda
    private void movePlayerForward()
    {
       
            movementVector = (targetPosition - transform.position).normalized;

        // Quaternion lookRotation = Quaternion.LookRotation(movementVector);
        //if (Quaternion.Angle(lookRotation, transform.rotation) > 15f)
        //{
        //    rotateToTarget(10f);
        //}
        //else
        //{
        navAgent.Move(movementVector * Time.fixedDeltaTime * 10f);
       // controller.Move(Vector3.forward * Time.fixedDeltaTime * 10f);
        //    if(aiNumber == 4)
        //{
        //    Debug.Log("moving to " + targetPosition.ToString() + " by moving " + movementVector.ToString() + " from " + transform.position.ToString());
        //}
        // }
        //sprinting = true;
        //ac.SetBool("jumping", false);
        //ac.SetBool("running", true);
    }

    private void rotateToTarget(float rotateMultiplier)
    {
        Quaternion lookRotation = Quaternion.LookRotation(movementVector);
        if (Quaternion.Angle(lookRotation, transform.rotation) > 15f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.fixedDeltaTime * rotateMultiplier);
            //Debug.Log(playerName + " turning amount to turn " + Quaternion.Angle(lookRotation, transform.rotation).ToString() + " to " + targetPosition.ToString());
        }
    }

    private void jumpForwards(float jumpDist)
    {
        if (aiNumber == 4)
        {
            Debug.Log("Forwards jumping " + playerName);
        }
        navAgent.enabled = false;
        moveDirection = Vector3.zero;
        moveDirection = targetPosition - transform.position;
        moveDirection.Normalize();
        moveDirection.x *= jumpDist;
        moveDirection.z *= jumpDist;
        moveDirection.y = jumpSpeed;
        controller.Move(moveDirection * Time.fixedDeltaTime);

        jumping = true;
        ac.SetBool("jumping", true);
    }

    private void jumpRandomDir(float jumpDist)
    {
        if (aiNumber == 4)
        {
            Debug.Log("Randomly jumping " + playerName);
        }
        navAgent.enabled = false;
        moveDirection = Vector3.zero;
        moveDirection = new Vector3(Random.Range(-10.0f, 10.0f), 0, Random.Range(-10.0f, 10.0f));
        moveDirection.Normalize();
        moveDirection.x *= jumpDist;
        moveDirection.z *= jumpDist;
        moveDirection.y = jumpSpeed;
        controller.Move(moveDirection * Time.fixedDeltaTime);

        jumping = true;
        ac.SetBool("jumping", true);

    }


}
