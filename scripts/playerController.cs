using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameSparks.Api.Requests;
using GameSparks.Core;
using TMPro;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine.SceneManagement;

/// <summary>
/// The class that controls player movement
/// </summary>
public class playerController : MonoBehaviour, PlayerInterface {

    [SerializeField]
    private Image outOfBoundsImage;
    [SerializeField]
    private TextMeshProUGUI outOfBoundsText;
    [SerializeField]
    private GameObject projectileOrigin;
    [SerializeField]
    private Text teamText;
    [SerializeField]
    private Image bubbleImg;
    [SerializeField]
    private Image hurtImg;
    //a link to the players hand to be used for setting up the Gun
    [SerializeField]
    private GameObject hand;
    [SerializeField]
    private playerBlasterController pbc;
    [SerializeField]
    private Image blueScoreBar;
    [SerializeField]
    private Image redScoreBar;
    [SerializeField]
    private Text redScoreText;
    [SerializeField]
    private Text blueScoreText;
    [SerializeField]
    private skinSelector skins;
    [SerializeField]
    private Image crossHairHit;
    [SerializeField]
    private TextMeshProUGUI goldBlasterText;
    [SerializeField]
    private tutorialController tutorialObj;
    [SerializeField]
    private Text blastText;
    [SerializeField]
    private Text deathText;
    [SerializeField]
    private Canvas canvas;

    //Movement floats
    public float speed;// = 6.0F;
    public float jumpSpeed;// = 8.0F;
    public float gravity;// = 20.0F;
	public float sprintMod = 1.5f;
    private float defaultSpeed;
    private float defaultJumpSpeed;
    //The time to get rid of the hit effect
    float bubbleEndTime;
    float hurtEndTime;
    private float crossHitEndTime;
    private float crossHitDuration = .6f;

    private int xpEarned;
    private int tutorialNum;
    private int redScore;
    private int blueScore;
    private int hp;
    private int blasts;
    private int deaths;
    private int incorrectQuestions;
    private int correctQuestions;
    private int questionScore = 0;


    private bool dead;
    //Red Team or Blue Team
    private bool redTeam;
    private bool gameOver;
    private bool gameStarted;
    //has the countdown been initiated
    private bool countdown;
    //is the bubble image displayed
    bool bubbleImageShown;
    bool hurtImgShown;
    private bool hasGoldBlast;
    private bool showTutorial;
    private bool crossHitShown;

    private bool outOfBounds;
    private float outOfBoundsTimer;
    private float outOfBoundsTimerLength = 6.5f;

	public GameObject sprintObj;
	private bool sprinting;

    private Blaster goldObj;

    //controllers
	public Animator ac;
	public CharacterController controller;
    public GameController gc;
    private gameNetworkController gnc;
    private PhotonView pView;

	public Vector3 moveDirection = Vector3.zero;

    //The properties of the overarching player
    private ExitGames.Client.Photon.Hashtable playerProp;

    private double gameTimer;

    //used to slowly give more hp over time 5 hp every 5 seconds, 5 seconds after last attack
    //this means that health doesn't start regenerating until 5 seconds after you've taken damage
    private float nextHP;
    private float hpTime = .66f;
    private int hpAmount = 1;
    private bool damaged;
    //the amount of time you have to wait for health to replinish while damaged
    private float damageWait = 5f;
    private float startTime;
    private float elapsedTime;

    public Image hpBar;
	public Image crossHair;
	private Color chColor;

	//When puffs of smoke appear
	private float runTime;
	private float nextRun;

    //is the gold health bar enabled
    private bool goldHealth;

	//the main Camera
	public GameObject cam;

	private Vector3 newVect;

	//have the crosshairs changed colors
	private bool crossHairChange;

    [SerializeField]
    private Text waterPackText;
    private int waterPacks;

    private string playerNum;


    [SerializeField]
    private Canvas namePlateCanvas;
    [SerializeField]
    private Text usernameText;

    [SerializeField]
    private GameObject gameoverObject;

    [SerializeField]
    private TextMeshProUGUI victoryText;

    [SerializeField]
    private TextMeshProUGUI defeatText;

    private string username;

    //can the user exit the game
    private bool canExit;

    private int oldBlasts;
    private int oldDeaths;
    private int oldWins;
    private int oldLosses;
    private int oldXP;

    private int shotsFired;
    private int shotsLanded;

    private int autoWP;
    private int[] settingData;

    private int mathXP;
    private int vocabXP;
    private int spellXP;

    private bool falling;
    private float startY;
    private float spawnEndTime;

    [SerializeField]
    private GameObject playerBlastImage;

    private bool playerBlastImageShown;

    private float playerBlastImgEndTime;

    private GameSparksHandler handler;


    void Start()
    {
        //sets the active skin on other players' devices
        if (!pView.IsMine)
        {
            return;
        }
        else{
            pView.RPC("setSkinAndStuff", RpcTarget.OthersBuffered, null);

            new LogEventRequest().SetEventKey("loadPlayerData").Send((response) => {
                if (!response.HasErrors)
                {
                    Debug.Log("Received Player Data From GameSparks...");
                    GSData data = response.ScriptData.GetGSData("Game_Data");
                    oldWins = (int) data.GetInt("HydroFlash_WINS");
                    oldLosses = (int)data.GetInt("HydroFlash_LOSSES");
                    oldBlasts = (int)data.GetInt("HydroFlash_BLASTS");
                    oldDeaths = (int)data.GetInt("HydroFlash_DEATHS");
                    oldXP = (int)data.GetInt("HydroFlash_XP");
                    shotsFired = (int)data.GetInt("HydroFlash_FIRED");
                    shotsLanded = (int)data.GetInt("HydroFlash_LANDED");
                    incorrectQuestions = (int)data.GetInt("HydroFlash_IQ");
                    correctQuestions = (int)data.GetInt("HydroFlash_CQ");
                    elapsedTime = (long)data.GetLong("HydroFlash_TIME");
                    tutorialNum = (int) data.GetInt("HydroFlash_TUTNUM");
                    if (tutorialNum < 11)
                    {
                        showTutorial = true;
                        tutorialObj.changeTutNum(tutorialNum);
                        tutorialObj.gameObject.SetActive(true);
                    }
                    else{
                        showTutorial = false;
                        tutorialObj.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.Log("Error Loading Player Data...");
                }
            });


            settingData = handler.getSettings();
            autoWP = settingData[5];

        }
    }
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        pView = GetComponent<PhotonView>();
        if (!pView.IsMine)
        {
            return;
        }
        xpEarned = 0;
        gameOver = false;
        canExit = false;
        outOfBounds = false;
        damaged = false;
        goldHealth = false;
        falling = false;
        handler = GameObject.FindWithTag("gsh").GetComponent<GameSparksHandler>();
        username = handler.getName();
        usernameText.text = username;
        pView.RPC("setUsername", RpcTarget.OthersBuffered, username);
        playerProp = new ExitGames.Client.Photon.Hashtable();
        int[] skinMatVals = skins.setMySkin(handler.getProfNum());
        playerProp.Add("skin", skinMatVals[0]);
        playerProp.Add("mat", skinMatVals[1]);
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProp, null, null);
        redScore = 0;
        blueScore = 0;
        countdown = false;
        gameStarted = false;
        blasts = 0;
        deaths = 0;
        blastText.text = "" + 0;
        deathText.text = "" + 0;
        gnc = GameObject.FindGameObjectWithTag("GNC").GetComponent<gameNetworkController>();
        defaultSpeed = speed;
        defaultJumpSpeed = jumpSpeed;
        canvas.gameObject.SetActive(true);
        gc.gameObject.SetActive(true);
        goldBlasterText.gameObject.SetActive(false);
        cam = GameObject.FindGameObjectWithTag("cam");
        if(cam == null){
            Debug.Log("null cam found");
        }
        cam.gameObject.GetComponent<SmoothMouseLook>().setTheParent(transform);
		hp = 100;
		sprinting = false;
		runTime = .4f;
		crossHair.color = Color.black;
		chColor = crossHair.color;
		crossHairChange = false;
        dead = false;
        cam.GetComponent<SmoothMouseLook>().enabled = true;
        waterPacks = 0;
        waterPackText.text = "Water Packs: " + waterPacks;
        bubbleImg.gameObject.SetActive(false);
        bubbleImageShown = false;
        crossHitShown = false;
        crossHairHit.gameObject.SetActive(false);
	}
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            Debug.Log("Destroying Player because scene switched");
            PhotonNetwork.Destroy(gameObject);
        }
    }
    void Update()
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (!gameOver)
        {
            redScore = gnc.getScore(true);
            blueScore = gnc.getScore(false);

            redScoreBar.fillAmount = redScore / 20f;
            blueScoreBar.fillAmount = blueScore / 20f;
            redScoreText.text = "" + redScore;
            blueScoreText.text = "" + blueScore;

            if (gameStarted)
            {
                if (!dead)
                {
                    if(showTutorial){
                        if(Input.GetKeyUp(KeyCode.Return)){
                            Debug.Log("Enter pressed");
                            tutorialNum++;
                            if (tutorialNum < 11)
                            {
                                showTutorial = false;
                                tutorialObj.gameObject.SetActive(false);
                                StartCoroutine(showNextTutorial());
                            }
                            else{
                                showTutorial = false;
                                tutorialObj.gameObject.SetActive(false);
                            }
                        }
                    }
                    if (autoWP == 1)
                    {
                        if (Input.GetKeyUp(KeyCode.E))
                        {
                            if (pbc.canUseWaterPack() && waterPacks > 0)
                            {
                                pbc.applyWaterPack();
                                waterPacks--;
                                waterPackText.text = "Water Packs: " + waterPacks;
                                pbc.setAmmoText();

                            }
                        }
                    }
                    if (outOfBounds)
                    {
                        if (Time.time < outOfBoundsTimer)
                        {
                            outOfBoundsText.text = "Return to Playing Field! " +
                                (int)(outOfBoundsTimer - Time.time);
                        }
                        else
                        {
                            ac.SetBool("dead", true);
                            deaths++;
                            deathText.text = "" + deaths;
                            //pbc.dropAllBlasters();
                            dead = true;
                            StartCoroutine(die(-1));
                        }
                    }
                    if (crossHitShown)
                    {
                        if (Time.time > crossHitEndTime)
                        {
                            crossHitShown = false;
                            crossHairHit.gameObject.SetActive(false);
                        }
                    }
                    if(playerBlastImageShown){
                        if(Time.time > playerBlastImgEndTime){
                            playerBlastImage.SetActive(false);
                            playerBlastImageShown = false;
                        }
                    }
                    if (damaged)
                    {
                        if (Time.time > nextHP)
                        {
                            //add health
                            if (hp <= 100 - hpAmount)
                            {
                                hp += hpAmount;
                            }
                            //if health was almost 100
                            else
                            {
                                hp = 100;
                            }
                            nextHP = Time.time + hpTime;
                        }
                        if (hp >= 100)
                        {
                            damaged = false;
                        }
                    }
                    if (hp >= 100)
                    {
                        hpBar.fillAmount = 1f;
                    }
                    else
                    {
                        hpBar.fillAmount = (float)hp / 100;
                    }
                    if (bubbleImageShown)
                    {
                        if (bubbleEndTime < Time.time)
                        {
                            bubbleImg.gameObject.SetActive(false);
                            bubbleImageShown = false;
                        }
                    }
                    if(hurtImgShown){
                        if(hurtEndTime < Time.time){
                            hurtImg.gameObject.SetActive(false);
                            hurtImgShown = false;
                        }
                    }
                    if (!gc.getQuestionMode())
                    {
                        if (pbc.hasGun())
                        {
                            if (autoWP == 0)
                            {
                                if (pbc.canUseWaterPack() && waterPacks > 0)
                                {
                                    pbc.applyWaterPack();
                                    waterPacks--;
                                    waterPackText.text = "Water Packs: " + waterPacks;
                                    pbc.setAmmoText();

                                }
                            }
                        }
                        if (pbc.isReloading())
                        {
                            if (!ac.GetBool("reloading"))
                            {

                                ac.SetBool("Dancing", false);
                                ac.SetBool("reloading", true);
                            }
                        }
                        else
                        {
                            ac.SetBool("reloading", false);
                            if (Input.GetKeyDown("r"))
                            {
                                if (pbc.canReload())
                                {
                                    pbc.reload();
                                }
                            }
                        }

                        if (sprinting && Time.time > nextRun)
                        {
                            newVect = transform.position;
                            newVect.y = 1.5f;

                            ///LOCAL INSTANTIATION NOT NETWORK//////////////////////
                            //Instantiate(sprintObj, newVect, transform.rotation);
                            PhotonNetwork.Instantiate(sprintObj.name, newVect, transform.rotation, 0);
                            nextRun = Time.time + runTime;
                        }
                        if (Input.GetKeyDown(KeyCode.LeftShift))
                        {
                            nextRun = Time.time + runTime;
                        }
                        if (Input.GetKey(KeyCode.LeftShift))
                        {
                            sprinting = true;
                        }
                        else
                        {
                            sprinting = false;
                        }

                        moveDirection.x = Input.GetAxis("Horizontal");
                        // moveDirection.y -= gravity * Time.deltaTime;
                        moveDirection.z = Input.GetAxis("Vertical");
                        moveDirection = transform.TransformDirection(moveDirection);

                        if (!Mathf.Approximately(Input.GetAxis("Horizontal"), 0f) || !Mathf.Approximately(Input.GetAxis("Vertical"), 0f))
                        {
                            ac.SetBool("Dancing", false);
                        }
                        if (sprinting)
                        {
                            moveDirection.x *= speed * sprintMod;
                            moveDirection.z *= speed * sprintMod;
                        }
                        else
                        {
                            moveDirection.x *= speed;
                            moveDirection.z *= speed;
                        }

                        if (controller.isGrounded)
                        {
                            moveDirection.x = Input.GetAxis("Horizontal");
                            moveDirection.y = 0f;
                            moveDirection.z = Input.GetAxis("Vertical");
                            moveDirection = transform.TransformDirection(moveDirection);

                            if (sprinting)
                            {
                                moveDirection *= speed * sprintMod;
                            }
                            else
                            {
                                moveDirection *= speed;
                            }

                            ac.SetBool("jumping", false);
                            if (Input.GetButton("Jump"))
                            {
                                moveDirection.y = jumpSpeed;
                                ac.SetBool("jumping", true);
                            }
                            if (falling)
                            {

                                if (Time.time > spawnEndTime)
                                {

                                    //Debug.Log("fall dist: " + (int)(startY - transform.position.y));
                                    if((int)(startY - transform.position.y) > 15){
                                        hurtImg.gameObject.SetActive(true);
                                        hurtEndTime = Time.time + .25f;
                                        hurtImgShown = true;
                                        hp -= (int)(startY - transform.position.y);
                                        damaged = true;
                                        nextHP += damageWait;

                                        if(hp <= 0){
                                            ac.SetBool("dead", true);
                                            deaths++;
                                            deathText.text = "" + deaths;
                                            //pbc.dropAllBlasters();
                                            dead = true;
                                            StartCoroutine(die(-1));
                                        }
                                    }
                                }

                                falling = false;
                            }
                        }

                        else
                        {
                            moveDirection.y -= gravity * Time.deltaTime;
                            if (!falling)
                            {
                                if (Time.time > spawnEndTime)
                                {
                                    if (transform.position.y > 20)
                                    {
                                        falling = true;
                                        startY = transform.position.y;
                                    }
                                }
                            }
                        }
                        //moveDirection = Quaternion.AngleAxis(-30, Vector3.up) * moveDirection;
                        //controller.Move(moveDirection * Time.deltaTime);

                        if (Mathf.Approximately(moveDirection.x, 0) && Mathf.Approximately(moveDirection.z, 0))
                        {
                            ac.SetBool("running", false);
                        }

                        else
                        {
                            ac.SetBool("running", true);
                        }
                        if (Input.GetKeyDown(KeyCode.Alpha2))
                        {
                            if (pbc.checkForBlaster(1))
                            {
                                pbc.swapItems(1);
                            }
                            else
                            {
                                //do nothing?
                            }
                        }
                        if (Input.GetKeyDown(KeyCode.Alpha3))
                        {
                            if (pbc.checkForBlaster(2))
                            {
                                pbc.swapItems(2);
                            }
                            else
                            {
                                //do nothing?
                            }
                        }
                        if (Input.GetKeyDown(KeyCode.Alpha4))
                        {
                            if (pbc.checkForBlaster(3))
                            {
                                pbc.swapItems(3);
                            }
                            else
                            {
                                //do nothing?
                            }
                        }
                        if (Input.GetKeyDown(KeyCode.Alpha5))
                        {
                            if (pbc.checkForBlaster(4))
                            {
                                pbc.swapItems(4);
                            }
                            else
                            {
                                //do nothing?
                            }
                        }
                        if (Input.GetKeyDown(KeyCode.Alpha0))
                        {
                            pbc.dropBlast1(3 * transform.forward + transform.position);
                        }
                        if (Input.GetKeyDown(KeyCode.Alpha9))
                        {
                            ac.SetBool("Dancing", true);
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
                        teamText.gameObject.SetActive(false);
                        startTime = Time.time;
                        spawnEndTime = Time.time + 5f;
                        gc.setGameOver(false);
                        namePlateCanvas.gameObject.SetActive(false);
                        pView.RPC("setNamePlate", RpcTarget.OthersBuffered, null);

                        //gnc.initializeAISLocally();
                    }
                    else
                    {
                        teamText.text = "Game starts in: " + (int)(gameTimer - PhotonNetwork.Time);
                    }
                }
                else
                {

                }
            }
        }
        if(canExit){
            //exit to the main menu
            if(Input.anyKeyDown){
                canExit = false;
                GameObject.FindWithTag("serverConnector").GetComponent<serverConnector>().exitToMainMenu();
                handler.destroyMe();
            }
        }
	}

    public void addHealth(int healthAmount){
        if (!goldHealth)
        {
            hp += healthAmount;
            if (hp > 125)
            {
                hp = 125;
            }
        }
    }

    public void addGoldHealth(int healthAmount)
    {
        hp += healthAmount;
        if(hp > 200){
            hp = 200;
        }
        goldHealth = true;
        hpBar.color = new Color(255f, 214f, 0f);
    }

    public void exitToMainMenu(){
        GameObject.FindWithTag("serverConnector").GetComponent<serverConnector>().exitToMainMenu();

    }

    public void dropGoldBlast(){
        hasGoldBlast = false;
    }

    private void LateUpdate()
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (!dead)
        {

            if (!gc.getQuestionMode())
            {
                if (Input.GetMouseButton(0))
                {
                    if (pbc.canShoot())
                    {
                        shotsFired++;
                        //if you want the effect instantiated at a point
                        if (pbc.getBlasterType() == 1)
                        {
                            cam.gameObject.GetComponent<SmoothMouseLook>().shoot1(pbc.getName() + "effect", pbc.getDamage());
                            pbc.incrementAmmo();
                            pbc.shooting();
                            ac.SetBool("shooting", true);

                        }
                        //if you want a trail to be shot
                        else if (pbc.getBlasterType() == 2 || pbc.getBlasterType() == 3)
                        {
                            cam.gameObject.GetComponent<SmoothMouseLook>().shoot2(pbc.getName() + "effect", pbc.getDamage(), 
                                                                                  (transform.forward * 2 + transform.up * 3.8f + transform.right * - 2.5f + transform.position));//pbc.getGunTipPosition());
                            pbc.incrementAmmo();
                            pbc.shooting();
                            ac.SetBool("shooting", true);
                        }
                        else if(pbc.getBlasterType() == 4){
                            cam.gameObject.GetComponent<SmoothMouseLook>().shoot3(pbc.getEffect().name,pbc.getGunTipPosition());
                            pbc.incrementAmmo();
                            pbc.shooting();
                            ac.SetBool("shooting", true);
                        }

                    }
                    else
                    {
                        //if you're not shooting
                        if (!pbc.isShooting())
                        {
                            ac.SetBool("shooting", false);
                        }
                        //Make empty clip sound
                    }
                }
                else
                {
                    if (!pbc.isShooting())
                    {
                        ac.SetBool("shooting", false);
                    }
                }
            }
        }
        if(gc.getQuestionMode() && !controller.isGrounded){
            moveDirection.x = 0f;
            moveDirection.z = 0f;
            moveDirection.y -= gravity * Time.deltaTime;
        }
        if (!gameOver)
        {
            if (!(dead && controller.isGrounded))
            {
                moveDirection = Quaternion.AngleAxis(-30, Vector3.up) * moveDirection;
                controller.Move(moveDirection * Time.deltaTime);
            }
        }

    }
    [PunRPC]
    public void setNamePlate(){
        if (PhotonNetwork.LocalPlayer.GetTeam().Equals(pView.Owner.GetTeam()))
        {
            //usernameText.text = playerName;
            namePlateCanvas.gameObject.SetActive(true);
            namePlateCanvas.GetComponent<cameraBillboard>()
                           .setCam(GameObject.FindGameObjectWithTag("cam")
                                   .GetComponent<Camera>());
        }
        else
        {
            namePlateCanvas.gameObject.SetActive(false);
        }
    }
    [PunRPC]
    public void setSkinAndStuff()
    {
        if (!pView.IsMine)
        {
            object obj;
            object mat;
            pView.Owner.CustomProperties.TryGetValue("skin", out obj);
            pView.Owner.CustomProperties.TryGetValue("mat", out mat);
            skins.setOthersSkin((int)obj, (int)mat);
            namePlateCanvas.gameObject.SetActive(true);
            namePlateCanvas.GetComponent<cameraBillboard>().setCam(GameObject.FindGameObjectWithTag("cam").GetComponent<Camera>());
        }
    }
    public void goIdle() {
		ac.SetBool ("running", false);
		ac.SetBool ("jumping", false);
	}

	public void foundPlayer() {
//		Debug.Log ("player");
		crossHairChange = true;
		crossHair.color = Color.red;
	}
	public void foundObject() {
//		Debug.Log ("object");
		crossHairChange = true;
		crossHair.color = Color.blue;
	}

	public void foundNothing() {
//		Debug.Log ("resetting");
		crossHairChange = false;
		crossHair.color = chColor;
	}
	public bool isCrossHairChanged() {
		return crossHairChange;
	}

    public void hasBlaster(bool hasBlast) {
        ac.SetBool("hasGun", hasBlast);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (other.tag.Equals("item") && !dead && !gc.getQuestionMode())
        {
            if (!other.GetComponent<Blaster>().getIsOwned())
            {
                if (other.GetComponent<Blaster>().getBlasterType() == 4)
                {
                    goldObj = other.GetComponent<Blaster>();
                    gc.upperLevelQuestion();
                }
                else
                {
                    if (pbc.getEmptySlots() != 0)
                    {
                        pbc.addBlaster(other.GetComponent<Blaster>());
                    }
                    else
                    {
                        Physics.IgnoreCollision(GetComponent<Collider>(), other);
                    }
                }
            }
        }
        if(other.tag.Equals("outOfBounds")){
            outOfBounds = true;
            outOfBoundsTimer = Time.time + outOfBoundsTimerLength;
            outOfBoundsImage.gameObject.SetActive(true);
        }
        if(other.tag.Equals("itemSpawn")){
            Physics.IgnoreCollision(GetComponent<Collider>(), other);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (other.gameObject.layer == 4)
        {
            speed = defaultSpeed / 2f;
            jumpSpeed = defaultJumpSpeed * .6f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!pView.IsMine)
        {
            return;
        }

        if(other.tag.Equals("outOfBounds")){
            outOfBounds = false;
            outOfBoundsImage.gameObject.SetActive(false);
        }

        if (other.gameObject.layer == 4)
        {
            speed = defaultSpeed;
            jumpSpeed = defaultJumpSpeed;
        }
    }

    public void setGameTimer(double endTime){
        gameTimer = endTime;
        countdown = true;
    }

    public void setBlasterPosition(GameObject theBlaster) {
        int blastNum = theBlaster.GetComponent<PhotonView>().ViewID;
        if (theBlaster.GetComponent<Blaster>().getBlasterType() == 1)
        {
            pView.RPC("setBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 1);
            //theBlaster.transform.SetParent(hand.transform);
            //theBlaster.transform.localPosition = new Vector3(.5f, .05f, -.01f);
            //theBlaster.transform.localRotation = Quaternion.Euler(0f, -25f, -90f);
        }
        else if(theBlaster.GetComponent<Blaster>().getBlasterType() == 2){
            pView.RPC("setBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 2);
            //theBlaster.transform.SetParent(hand.transform);
            //theBlaster.transform.localPosition = new Vector3(.2f, .1f, 0);
            //theBlaster.transform.localRotation = Quaternion.Euler(-90f, 180f, -110f);
        }
        else if (theBlaster.GetComponent<Blaster>().getBlasterType() == 3)
        {
            pView.RPC("setBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 3);
            //theBlaster.transform.SetParent(hand.transform);
            //theBlaster.transform.localPosition = new Vector3(.176f, .1f, -.05f);
            //theBlaster.transform.localRotation = Quaternion.Euler(-180, -20, 70);
        }
        else if(theBlaster.GetComponent<Blaster>().getBlasterType() == 4){
            pView.RPC("setBlasterOnOthers", RpcTarget.AllBufferedViaServer, blastNum, pView.ViewID, 4);
        }
    }
    [PunRPC]
    public void setBlasterOnOthers(int blasterID, int playerID, int gunType){
        GameObject tempHand = PhotonView.Find(playerID).GetComponent<playerController>().getHand();
        GameObject theBlaster = PhotonView.Find(blasterID).gameObject;
        theBlaster.transform.SetParent(tempHand.transform);

        if(gunType == 1){
            theBlaster.transform.localPosition = new Vector3(.5f, .05f, -.01f);
            theBlaster.transform.localRotation = Quaternion.Euler(0f, -25f, -90f);
        }
        else if(gunType == 2){
            theBlaster.transform.localPosition = new Vector3(.2f, .1f, 0);
            theBlaster.transform.localRotation = Quaternion.Euler(-90f, 180f, -110f);
        }
        else if(gunType == 3){
            theBlaster.transform.localPosition = new Vector3(.176f, .1f, -.05f);
            theBlaster.transform.localRotation = Quaternion.Euler(-180, -20, 70);
        }
        else if(gunType == 4){
            theBlaster.transform.localPosition = new Vector3(.527f, .048f, .155f);
            theBlaster.transform.localRotation = Quaternion.Euler(8.6f, 66f, -70f);
            Debug.Log("equipped?");
        }
    }
    public void loseHealth(int damage, int attackerPV) {
        if (!pView.IsMine)
        {
            return;
        }
        hp -= damage;
        damaged = true;
        nextHP = Time.time + damageWait;
        if (hp < 100)
        {
            if(goldHealth){
                goldHealth = false;
                hpBar.color = Color.green;
            }
            hpBar.fillAmount = (float)hp / 100;
        }

        bubbleImg.gameObject.SetActive(true);
        bubbleImageShown = true;
        bubbleEndTime = Time.time + 1.5f;
        if (attackerPV != -1)
        { 
            GameObject attacker = PhotonView.Find(attackerPV).gameObject;
            if (attacker.tag.Equals("Player"))
            {
                attacker.GetComponent<PhotonView>().RPC("changeHitCrosshair", RpcTarget.AllBufferedViaServer, null);
            }

            if (hp <= 0 && !dead)
            {
                dead = true;

                //increment the player's kills
                if (attacker.tag.Equals("Player"))
                {
                    attacker.GetComponent<PhotonView>().RPC("addBlastScore", RpcTarget.AllBufferedViaServer, null);
                }
                //increment AI kill
                else
                {

                }
                ac.SetBool("dead", true);
                deaths++;
                deathText.text = "" + deaths;
                //pbc.dropAllBlasters();
                if (hasGoldBlast)
                {
                    pbc.dropGoldBlast();
                    hasGoldBlast = false;
                }
                StartCoroutine(die(attackerPV));
            }
            else
            {
                //cause hit animation
            }
        }
        else{
            if (hp <= 0 && !dead)
            {
                dead = true;

                ac.SetBool("dead", true);
                deaths++;
                deathText.text = "" + deaths;
                if (hasGoldBlast)
                {
                    pbc.dropGoldBlast();
                    hasGoldBlast = false;
                }
                StartCoroutine(die(-1));
            }
        }

    }

    public IEnumerator die(int attackerPV){
        yield return new WaitForSeconds(3f);
        resetPlayer(attackerPV);
    }


    public void addWaterPack(bool isGold){
        if (isGold)
        {
            if (!goldObj.getIsOwned())
            {
                pbc.addBlaster(goldObj);
                hasGoldBlast = true;
            }
        }
        else
        {
            waterPacks++;
            waterPackText.text = "Water Packs: " + waterPacks;
        }
        questionScore++;
    }


    [PunRPC]
    public void applyDamage(int damage, int attackerPV, bool isRed){
        if (!pView.IsMine)
        {
            return;
        }
        if (isRed != redTeam)
        {
            loseHealth(damage, attackerPV);
        }
        else if(attackerPV == pView.ViewID){
            loseHealth(damage, -1);
        }
    }
    //checks if player is on red team
    public bool checkIfRedTeam(){
        return pView.Owner.GetTeam().Equals(PunTeams.Team.red);
    }

    public void setTeam(){
        if (pView.IsMine)
        {
            if (PhotonNetwork.LocalPlayer.GetTeam().Equals(PunTeams.Team.red))
            {
                redTeam = true;
                teamText.text = "Red Team";
                teamText.color = Color.red;
            }
            else
            {
                redTeam = false;
                teamText.text = "Blue Team";
                teamText.color = Color.blue;
            }
        }
    }

    //resets the player to defaults
    public void resetPlayer(int attackerPV){
//        Debug.Log("reset Player");
        gnc.respawnMe(pView, redTeam, attackerPV);
        hp = 100;
        sprinting = false;
        crossHair.color = Color.black;
        crossHairChange = false;
        dead = false;
        ac.SetBool("dead", false);
        cam.GetComponent<SmoothMouseLook>().enabled = true;
        waterPacks = 0;
        waterPackText.text = "Water Packs: " + waterPacks;
        bubbleImg.gameObject.SetActive(false);
        bubbleImageShown = false;
        spawnEndTime = Time.time + 5f;
        falling = false;

    }

    //Gets the player's hand for use with gun positioning
    public GameObject getHand(){
        return hand;
    }

    [PunRPC]
    public void addBlastScore(){
        if(!pView.IsMine){
            return;
        }
        playerBlastImage.SetActive(true);
        playerBlastImageShown = true;
        playerBlastImgEndTime = Time.time + .4f;
        blasts++;
        blastText.text = "" + blasts;
    }


    //makes a small red crosshair appear when hitting someone
    [PunRPC]
    public void changeHitCrosshair(){
        shotsLanded++;
        crossHitShown = true;
        crossHitEndTime = Time.time + crossHitDuration;
        crossHairHit.gameObject.SetActive(true);
    }

    public string getName(){
        return username;
    }

    [PunRPC]
    public void setUsername(string name){
        username = name;
        usernameText.text = username;

    }

    public ScoreOrder[] getOrderAndPlaces(){
        return gnc.getScoreOrders();
    }

    public Vector3 getProjectileOrigin(){
        return projectileOrigin.transform.position;
    }


    public void notifyOfGoldBlaster(){
        StartCoroutine(goldBlasterSpawned());
    }

    private IEnumerator goldBlasterSpawned(){
        goldBlasterText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        goldBlasterText.gameObject.SetActive(false);
    }

    public void endGame(bool winningTeam){
        if (!gameOver)
        {
            gameOver = true;
            gameoverObject.SetActive(true);
            cam.gameObject.GetComponent<SmoothMouseLook>().endGame();
            gc.endGame();

            if (elapsedTime.AlmostEquals(-1f,.01f))
            {
                elapsedTime += Time.time - startTime;
            }
            else{
                elapsedTime += Time.time - startTime + 1;
            }

            //If victory
            if (winningTeam == redTeam)
            {
                Debug.Log("We won!");
                victoryText.gameObject.SetActive(true);
                defeatText.gameObject.SetActive(false);
            }
            //If a loss
            else
            {
                Debug.Log("We lost!");
                victoryText.gameObject.SetActive(false);
                defeatText.gameObject.SetActive(true);
            }
            gameOver = true;
            StartCoroutine(waitThenShowXP(winningTeam == redTeam));
        }
    }

    //holds the player at the screen then allows them to push a button to exit
    private IEnumerator waitThenShowXP(bool victory)
    {
        //show game text for 1 sec
        yield return new WaitForSeconds(1.7f);
        victoryText.gameObject.SetActive(false);
        defeatText.gameObject.SetActive(false);

        //show blast bonus
        if(blasts > 0){
            xpEarned += (blasts * 75 + 100);
            victoryText.text = "Blast Bonus " + xpEarned + " XP";
        }
        else{
            victoryText.text = "Blast Bonus 0 XP";
            victoryText.gameObject.SetActive(true);
        }

        victoryText.gameObject.SetActive(true);
        //show blast bonus text for 1 sec
        yield return new WaitForSeconds(1.5f);

        //no deaths bonus
        if(deaths == 0){
            xpEarned += 500;
            victoryText.text = "Invincible Bonus 500 XP";
            yield return new WaitForSeconds(1.5f);
        }

        //victory bonus
        if(victory){
            oldWins++;
            xpEarned += 250;
            victoryText.text = "Victory Bonus 250 XP";
            yield return new WaitForSeconds(1.5f);
        }
        else{
            oldLosses++;
            xpEarned += 100;
            victoryText.text = "Gameplay Bonus 100 XP";
            yield return new WaitForSeconds(1.5f);
        }

        //kd bonus
        if(blasts > deaths){
            if(deaths == 0){
                xpEarned += 50 * blasts;
                victoryText.text = "B/K Bonus " + (50 * blasts) + " XP";
            }
            else{
                xpEarned += 50 * Mathf.RoundToInt((float)(blasts) / (float)deaths);
                victoryText.text = "B/K Bonus " +
                    50 * Mathf.RoundToInt((float)(blasts) / (float)deaths) +
                    " XP";
            }
            yield return new WaitForSeconds(1.5f);
        }

        if (questionScore >= 20)
        {
            xpEarned += 1000;
            victoryText.text = "Question Emperor Bonus 1000 XP";
            yield return new WaitForSeconds(1.5f);
        }

        string[] ids = handler.getIDS();
        int[] correctQ = gc.getCorrectAnswers();
        for (int i = 0; i < correctQ.Length; i++){
            //if(correctQ[i] != null){
                correctQuestions += correctQ[i];
           // }
        }
        int[] inCorrectQ = gc.getIncorrectAnswers();
        for (int i = 0; i < inCorrectQ.Length; i++)
        {
            //if(correctQ[i] != null){
            incorrectQuestions += inCorrectQ[i];
            // }
        }
        int[] prevXPs = handler.getPopLearnXP();

        for (int i = 0; i < 10; i++){
            if (ids[i] != null)
            {
                int totXP = prevXPs[i];
                Debug.Log("correct: " + correctQ[i]);
                if (totXP < 20)
                {
                    totXP += correctQ[i];
                }
                else
                {
                    totXP += correctQ[i];
                    Debug.Log("penalty: " + (totXP % 20) * inCorrectQ[i]);
                    totXP -= ((totXP % 20) * inCorrectQ[i]);
                    if(totXP <= 0){
                        totXP = 0;
                    }
                }
                Debug.Log("new total: " + totXP);
                new LogEventRequest()
                    .SetEventKey("setStudyListXP")
                    .SetEventAttribute("name", ids[i])
                    .SetEventAttribute("xpAmount", totXP)
                    .Send((response) =>
                    {
                        if (!response.HasErrors)
                        {
                        
                        }
                    else{
                            Debug.Log("problem with entry " + i);
                    }
                    });

            }
        }
        int[][] studyXP = gc.getStudyXP();

        for (int i = 0; i < ids.Length; i++){
            if (ids[i] != null)
            {
                List<int> tempData = new List<int>(studyXP[i]);
                new LogEventRequest().SetEventKey("setScoreArray")
                                     .SetEventAttribute("listName", ids[i])
                                     .SetEventAttribute("data", tempData)
                                     .Send((response) =>
                                     {
                                         if (!response.HasErrors)
                                         {

                                         }

                                         else
                                         {
                                             Debug.Log("Problem setting array data on server!");
                                         }

                                     });
            }
        }

        new LogEventRequest()
            .SetEventKey("updateStats")
            .SetEventAttribute("wins", oldWins)
            .SetEventAttribute("losses", oldLosses)
            .SetEventAttribute("blasts", oldBlasts + blasts)
            .SetEventAttribute("deaths", oldDeaths + deaths)
            .SetEventAttribute("xp", oldXP + xpEarned)
            .SetEventAttribute("time", (long) elapsedTime)
            .SetEventAttribute("correctQuestions", correctQuestions)
            .SetEventAttribute("incorrectQuestions", incorrectQuestions)
            //Sets the game value to a non zero number
            .SetEventAttribute("games", 1)
            .SetEventAttribute("shotsFired", shotsFired)
            .SetEventAttribute("shotsLanded", shotsLanded)
            .SetEventAttribute("tutorialNum", tutorialNum)
            .Send((response) =>
                      {
                          if (!response.HasErrors)
                          {
                              Debug.Log("Player Saved To GameSparks...");
                              StartCoroutine(endEndGame());
                              
                          }
                          else
                          {
                              Debug.Log("Error Saving Player Data...");
                          }
                      });


        //canExit = true;
    }

    private IEnumerator endEndGame(){
        yield return new WaitForSeconds(.5f);
        victoryText.fontStyle = FontStyles.Italic;
        victoryText.text = "Press any button to exit";
        canExit = true;

    }

    private IEnumerator showNextTutorial(){
        yield return new WaitForSeconds(.7f);
        showTutorial = true;
        tutorialObj.changeTutNum(tutorialNum);
        tutorialObj.gameObject.SetActive(true);
    }


    public void setAWP(int newAWP){
        autoWP = newAWP;
    }

    //0 Math, 1 Vocab, 2, Spelling
    public void updateCorrect(int subject){
        if(subject == 1){
            mathXP++;
        }
        else if(subject == 2){
            vocabXP++;
        }
        else if(subject == 3){
            spellXP++;
        }
    }

}
