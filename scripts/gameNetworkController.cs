using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Linq;

/// <summary>
/// Used to control the in game functionality. Either exists as a master, or client
/// </summary>
public class gameNetworkController : MonoBehaviourPunCallbacks
{
    //The first 10 spawn points are team blue and the last ten are red
    //

    /// <summary>
    /// Important!!!! add ais to teams so that they're taken into account for 
    /// enemy locations. Also, so that you can locally initialize AIs from each player
    /// </summary>
    [SerializeField]
    private GameObject[] playerSpawnPoints;
    //a list of each player's gnc's view ID
    public List<int> players = new List<int>();
    public List<int> aiIDS = new List<int>();

    private Hashtable redScores = new Hashtable();
    private Hashtable blueScores = new Hashtable();

    private Hashtable names = new Hashtable();
    private Hashtable deaths = new Hashtable();

    private ScoreOrder[] blueScoreOrders = new ScoreOrder[5];
    private ScoreOrder[] redScoreOrders = new ScoreOrder[5];

    public int numPlayers;
    public int playerNum;

    private List<int> redTeam = new List<int>();
    private List<int> blueTeam = new List<int>();

    private List<Vector3> enemyLocations = new List<Vector3>();

    public int redScore;
    public int blueScore;

    //only used for the initial spawn
    private List<Vector3> playerSpawnPointsUnusedRed = new List<Vector3>();
    private List<Vector3> playerSpawnPointsUnusedBlue = new List<Vector3>();

    private List<GameObject> aiObjects = new List<GameObject>();
    public List<GameObject> objectSpawnPoints;
    private List<GameObject> totalObjectSpawnPoints = new List<GameObject>();
    private bool gameStarted;
    private bool allPlayersInGame;



    private string[] blasterNames = new string[] { "BigBabyBlaster1", "BigBabyBlaster2",
    "Bubbler1", "Bubbler2", "WaterGun1", "WaterGun2"};
    
    //used to calculate best possible spawns
    List<Vector3> possibleSpawns;
    private double endCountdownTimer;

    //link to player controller
    private GameObject pcLink;

    //the view id of the player controller
    private int idView;

    private bool gameOver;

    //private PhotonView photonView;

    private void Awake()
    {
        
        gameOver = false;
        numPlayers = 0;
        redScore = 0;
        blueScore = 0;
        //photonView = gameObject.GetComponent<PhotonView>();
        for (int i = 0; i < 10; i++)
        {
            playerSpawnPointsUnusedBlue.Add(playerSpawnPoints[i].transform.position);
            playerSpawnPointsUnusedRed.Add(playerSpawnPoints[i + 10].transform.position);
        }
    }
    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PhotonNetwork.IsMasterClient && !gameOver)
        {
            if (!gameStarted)
            {
                if (endCountdownTimer > 0)
                {
                    if (endCountdownTimer < PhotonNetwork.Time)
                    {
                        gameStarted = true;
                    }
                }
            }
            else
            {
                if (allPlayersInGame)
                {
                    if (PhotonNetwork.CurrentRoom.PlayerCount != players.Count)
                    {
                        Debug.Log("Player Count Uneven! Expected " + players.Count + " got " + PhotonNetwork.CurrentRoom.PlayerCount);
                        //Dictionary<int, Player> curRoom = PhotonNetwork.CurrentRoom.Players;
                        for (int i = 0; i < players.Count; i++)
                        {

                            if (PhotonView.Find(players[i]) == null)
                            {
                                int oldID = players[i];
                                players.RemoveAt(i);
                                //Spawn a new AI in their place
                                int idViewAI;
                                bool isOnRed = redTeam.Contains(oldID);
                                GameObject tempSpawnObj;
                                int rand = Random.Range(0, playerSpawnPoints.Length);
                                tempSpawnObj = PhotonNetwork.InstantiateSceneObject("AIPlayer",
                                                             playerSpawnPoints[rand].transform.position,
                                                             Quaternion.identity,
                                                             0);
                                if (isOnRed)
                                {
                                    tempSpawnObj.GetComponent<aiPlayerController>().setTeamAndTimer(PunTeams.Team.red,
                                                                                                endCountdownTimer, 10 - PhotonNetwork.PlayerList.Length, false, (string)names[oldID]);
                                }
                                else
                                {
                                    tempSpawnObj.GetComponent<aiPlayerController>().setTeamAndTimer(PunTeams.Team.blue,
                                                                                                    endCountdownTimer, 10 - PhotonNetwork.PlayerList.Length, false, (string)names[oldID]);
                                }
                                idViewAI = tempSpawnObj.GetComponent<PhotonView>().ViewID;
                                this.photonView.RPC("removePlayerAddAI", RpcTarget.AllBufferedViaServer, idViewAI, isOnRed, oldID);
                                //I'm really unsure if this equates to the correct user
                                if (aiObjects[i] == null)
                                {
                                    aiObjects.RemoveAt(i);
                                }
                                aiObjects.Add(tempSpawnObj);
                                break;
                            }
                            else
                            {
                                Debug.Log("Room contains user " + players[i]);
                            }
                        }
                    }
                }
            }
        }
    }

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    //Spawns _ chests and _ Blasters on the map
    public void spawnObjects()
    {
        totalObjectSpawnPoints = new List<GameObject>(objectSpawnPoints);
        Debug.Log("List size: " + objectSpawnPoints.Count);
        int rand;
        int randBlast;
        for (int i = 0; i < 19; i++)
        {
            if (objectSpawnPoints.Count > 0)
            {
                rand = Random.Range(0, objectSpawnPoints.Count);
                randBlast = Random.Range(0, blasterNames.Length);
                if (randBlast <= 3)
                {
                    PhotonNetwork.InstantiateSceneObject(blasterNames[randBlast],
                                              objectSpawnPoints[rand].transform.position,
                                              Quaternion.Euler(90, 0, 0),
                                              0);
                }
                else
                {
                    PhotonNetwork.InstantiateSceneObject(blasterNames[randBlast],
                                              objectSpawnPoints[rand].transform.position,
                                              Quaternion.Euler(180, 0, -90),
                                              0);
                }
                objectSpawnPoints.RemoveAt(rand);
            }
        }
    }

    //Spawns the leftover CPUs
    private void spawnAIS(){
        int numAIS = 10 - PhotonNetwork.PlayerList.Length;

        int idViewAI;
        //Start spawning in reverse order from slot 10 down
        for (int i = numAIS; i > 0; i--){
            GameObject tempSpawnObj;
            //random number either 1 or 0 to add to the spawn place 0: 0,1 1:2,3 2:4,5 3:6,7 4:8,9 x*2 +rand
            //1,2,3,4
            int rand = Random.Range(0, 2);
            //Spawn Blue
            if(i % 2 == 0){
                tempSpawnObj = PhotonNetwork.InstantiateSceneObject("AIPlayer",
                                                         playerSpawnPointsUnusedBlue[i + rand],
                                                         Quaternion.identity,
                                                         0);
                tempSpawnObj.GetComponent<aiPlayerController>().setTeamAndTimer(PunTeams.Team.blue,
                                                                                endCountdownTimer, i, true, null);
                idViewAI = tempSpawnObj.GetComponent<PhotonView>().ViewID;
                this.photonView.RPC("updateNumPlayers", RpcTarget.AllBufferedViaServer, idViewAI, false, true);
                //blueTeam.Add(idViewAI);
                //blueScores.Add(idViewAI, 0);
            }
            //Spawn Red
            else{
                tempSpawnObj = PhotonNetwork.InstantiateSceneObject("AIPlayer",
                                                         playerSpawnPointsUnusedRed[i - rand],
                                                         Quaternion.identity,
                                                         0);
                tempSpawnObj.GetComponent<aiPlayerController>().setTeamAndTimer(PunTeams.Team.red,
                                                                                endCountdownTimer, i, true, null);
                idViewAI = tempSpawnObj.GetComponent<PhotonView>().ViewID;
                this.photonView.RPC("updateNumPlayers", RpcTarget.AllBufferedViaServer, idViewAI, true, true);
                //redTeam.Add(idViewAI);
                //redScores.Add(idViewAI, 0);
            }

            aiObjects.Add(tempSpawnObj);

        }
        Debug.Log("There are " + aiObjects.Count + " AIs");
        //this is done to ensue that the team and number have been set beforehand
        for (int i = 0; i < aiObjects.Count; i++){
            aiObjects[i].GetComponent<aiPlayerController>().initializeSkins();
        }
        //initialize ais on other players
        photonView.RPC("initializeAISLocally", RpcTarget.OthersBuffered, null);

        //if (i % 2 == 0)
        //{
        //    this.photonView.RPC("updateNumPlayers", PhotonTargets.AllBufferedViaServer, idViewAI, false, true);
        //}
        //else{
        //    this.photonView.RPC("updateNumPlayers", PhotonTargets.AllBufferedViaServer, idViewAI, true, true);
        //}

    }

    //team =true if red 
    public void respawnMe(PhotonView photonView, bool team, int attackerPV)
    {
        possibleSpawns = new List<Vector3>();
        float distanceToPlayer;
        float tempDist;
        //the position being examined
        Vector3 currentPos;
        int pvID = photonView.ViewID;
        Vector3[] enemyPositions = new Vector3[5];
        enemyLocations.Clear();
        //respawn away from blue team
        if (team)
        {
            //Debug.Log("respawning Blue");
            this.photonView.RPC("updateBlast", RpcTarget.AllBufferedViaServer, false, attackerPV, pvID);

            for (int k = 0; k < blueTeam.Count; k++){
                enemyPositions[k] = PhotonView.Find(blueTeam[k]).transform.position;
            }
            //for each possible spawn point, calculate the distance from each 
            //opposing player
            for (int i = 0; i < playerSpawnPoints.Length; i++)
            {
                distanceToPlayer = 101f;
                currentPos = playerSpawnPoints[i].transform.position;
                //for eachplayer, calc distance and if > 50 add spawn point
                for (int j = 0; j < blueTeam.Count; j++)
                {
                    tempDist = Vector3.Distance(enemyPositions[j], currentPos);
                    if (tempDist < distanceToPlayer)
                        distanceToPlayer = tempDist;
                }
                if (distanceToPlayer >= 100f)
                {
                    possibleSpawns.Add(currentPos);
                }
            }
            if (possibleSpawns.Count > 0)
            {
                //Debug.Log("possible spawns" + possibleSpawns.Count);
                int rand = Random.Range(0, possibleSpawns.Count);
                photonView.gameObject.transform.position = possibleSpawns[rand];
            }else{
                int rand = Random.Range(0, playerSpawnPoints.Length);
                photonView.gameObject.transform.position = playerSpawnPoints[rand].transform.position;
            }
        }
        //respawn away from red team
        else
        {
            //Debug.Log("respawning red");
            this.photonView.RPC("updateBlast", RpcTarget.AllBufferedViaServer, true, attackerPV, pvID);

            for (int k = 0; k < redTeam.Count; k++)
            {
                enemyPositions[k] = PhotonView.Find(redTeam[k]).transform.position;
            }
            //for each possible spawn point, calculate the distance from each 
            //opposing player
            for (int i = 0; i < playerSpawnPoints.Length; i++)
            {
                distanceToPlayer = 101f;
                currentPos = playerSpawnPoints[i].transform.position;
                //for eachplayer, calc distance and if > 50 add spawn point
                for (int j = 0; j < redTeam.Count; j++)
                {
                    tempDist = Vector3.Distance(enemyPositions[j], currentPos);
                    if (tempDist < distanceToPlayer)
                        distanceToPlayer = tempDist;
                }
                if (distanceToPlayer >= 100f)
                {
                    possibleSpawns.Add(currentPos);
                }
            }
            if (possibleSpawns.Count > 0)
            {
                //Debug.Log("possible spawns" + possibleSpawns.Count);
                int rand = Random.Range(0, possibleSpawns.Count);
                photonView.gameObject.transform.position = possibleSpawns[rand];
            }
            else
            {
                int rand = Random.Range(0, playerSpawnPoints.Length);
                photonView.gameObject.transform.position = playerSpawnPoints[rand].transform.position;
            }
        }
    }

    //spawns the player and adds them to a team red = true blue = false
    //Even number players join team red, odd number players join blue
    // There are 10 spots each player could spawn at marked as indexes
    //However, there are only 5 players per team so each player can spawn
    //at either it's index or index + 1 (-1 if on the odd team
    public void spawnMe()
    {
        allPlayersInGame = true;
        Debug.Log("spawning player");
        int rand;
        //The number of player IDs lower than the player's. A way to artificially sort
        //the playerList
        int numLower = 0;
        //PhotonView photonView;
        playerNum = PhotonNetwork.LocalPlayer.ActorNumber;
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            if (PhotonNetwork.PlayerList[i].ActorNumber < playerNum)
            {
                numLower++;
            }
        }
        playerNum = numLower;
        //0 is even so sets to blue if first player
        if (playerNum == 0)
        {
            PhotonNetwork.LocalPlayer.SetTeam(PunTeams.Team.blue);
            //this.photonView.RPC("p1", PhotonTargets.Others, null);
            //numPlayers++;
            //rand = Random.Range(0, playerSpawnPointsUnusedBlue.Count);
            //I want either 0 or 1
            rand = Random.Range(0, 2);
            Debug.Log("calling spawn objs");
            spawnObjects();
            //spawn a player at either index or index + 1 since we're only using
            //every other one
            pcLink = PhotonNetwork.Instantiate("player",
                                      playerSpawnPointsUnusedBlue[playerNum * 2 + rand],
                                      Quaternion.identity,
                                      0);
            //this.photonView.RPC("updateNumPlayers", PhotonTargets.Others, photonView.viewID);
            //photonView.gameObject.GetComponent<playerController>().setTeam(false);
            //blueTeam.Add(photonView.viewID);
            //playerSpawnPointsUnusedBlue.RemoveAt(rand); 0: 0,1 1:2,3 2:4,5 3:6,7 4:8,9 x*2 +rand
            endCountdownTimer = PhotonNetwork.Time + 15;
            spawnAIS();
            StartCoroutine(wait5SecThenSendTime());

        }
        else
        {
            //red team
            if (playerNum % 2 == 1)
            {
                PhotonNetwork.LocalPlayer.SetTeam(PunTeams.Team.red);
                //numPlayers++;
                //rand = Random.Range(0, playerSpawnPointsUnusedRed.Count);
                rand = Random.Range(0, 2);
                pcLink = PhotonNetwork.Instantiate("player", playerSpawnPointsUnusedRed[playerNum * 2 + rand],
                                          Quaternion.identity, 0);
                
                //this.photonView.RPC("updateNumPlayers", PhotonTargets.Others, photonView.viewID);
                //photonView.gameObject.GetComponent<playerController>().setTeam(true);
                //redTeam.Add(photonView.viewID);
                //playerSpawnPointsUnusedRed.RemoveAt(rand);

            }
            //blue team
            else
            {
                PhotonNetwork.LocalPlayer.SetTeam(PunTeams.Team.blue);
                //rand = Random.Range(0, playerSpawnPointsUnusedBlue.Count);
                rand = Random.Range(0, 2);
                pcLink = PhotonNetwork.Instantiate("player", playerSpawnPointsUnusedBlue[playerNum * 2 + rand],
                                          Quaternion.identity, 0);
                
                //this.photonView.RPC("updateNumPlayers", PhotonTargets.Others, photonView.viewID);
                //photonView.gameObject.GetComponent<playerController>().setTeam(false);
                //blueTeam.Add(photonView.viewID);
                //playerSpawnPointsUnusedBlue.RemoveAt(rand);
            }
        }
        idView = pcLink.GetPhotonView().ViewID;
        this.photonView.RPC("updateNumPlayers", RpcTarget.AllBufferedViaServer, idView, playerNum % 2 == 1, false);
        //pcLink.GetComponent<playerController>().setNum(testStr);
        //players.Add(photonView.viewID);
        //photonView.gameObject.GetComponent<playerController>().testText.text = "" + PhotonNetwork.time;
        //PhotonNetwork.RaiseEvent(0, photonView.viewID, true, null);

    }

    [PunRPC]
    public void initializeAISLocally(){
        Debug.Log("initializing the ais");
        GameObject tempAIObj;
        for (int i = aiIDS.Count; i > 0; i--){
            tempAIObj = PhotonView.Find(aiIDS[i - 1]).gameObject;
            aiObjects.Add(tempAIObj);
            bool isOnRed = redTeam.Contains(aiIDS[i - 1]);
            //used to put them in reversed order
            int test = Mathf.Abs(aiIDS.Count - i + 1);
            Debug.Log("ai number " + test);
            tempAIObj.GetComponent<aiPlayerController>().initializeLocalAIIDontOwn(test, isOnRed);
        }
    }

    //Get the score of either red or blue
    public int getScore(bool red)
    {
        if (red)
        {
            return redScore;
        }
        else
        {
            return blueScore;
        }
    }

    [PunRPC]
    public void updateBlast(bool red, int attackerPV, int deathPV)
    {

        //        Debug.Log("update blast");
        if (!gameOver)
        {
            int currentScore;
            if (attackerPV != -1)
            {
                if (red)
                {
                    redScore++;
                    currentScore = (int)redScores[attackerPV];
                    currentScore++;
                    redScores[attackerPV] = currentScore;
                    Debug.Log((string)names[attackerPV] + " with kill number " + (int)redScores[attackerPV]);

                }
                else
                {
                    blueScore++;
                    currentScore = (int)blueScores[attackerPV];
                    currentScore++;
                    blueScores[attackerPV] = currentScore;
                    Debug.Log((string)names[attackerPV] + " with kill number " + (int)blueScores[attackerPV]);

                }
                if (redScore + blueScore == 15 && PhotonNetwork.IsMasterClient)
                //if (redScore + blueScore == 3 && playerNum == 0)
                {
                    //////////////////////Enable this on non-demo
                    spawnGoldBlaster();
                    Debug.Log("spawning gold blaster!");
                }
                if ((redScore + blueScore) % 7 == 0 && PhotonNetwork.IsMasterClient)
                //if ((redScore + blueScore) % 3 == 0 && playerNum == 0)
                {
                    spawnHealthPacks();

                }
                if ((redScore + blueScore) % 5 == 0 && PhotonNetwork.IsMasterClient)
                //if ((redScore + blueScore) % 3 == 0 && playerNum == 0)
                {
                    spawnMoreBlasters(3);

                }
                //if someone has scored 30, end the game
                if ((redScore == 20 || blueScore == 20) && PhotonNetwork.IsMasterClient)
                {
                    if (redScore == 20)
                    {
                //if ((redScore == 5 || blueScore == 5) && playerNum == 0)
                //{
                    //if (redScore == 5)
                    //{

                        this.photonView.RPC("endGame", RpcTarget.AllBufferedViaServer, true);
                    }
                    else
                    {
                        this.photonView.RPC("endGame", RpcTarget.AllBufferedViaServer, false);
                    }
                    for (int i = 0; i < aiObjects.Count; i++)
                    {
                        aiObjects[i].GetComponent<aiPlayerController>().endGame();
                    }
                }
            }
            //reusung current score as a temp variable to increment death count
            currentScore = (int)deaths[deathPV];
            currentScore++;
            deaths[deathPV] = currentScore;
        }
    }
    [PunRPC]
    private void notifyPCOfGold()
    {
        pcLink.GetComponent<playerController>().notifyOfGoldBlaster();
    }
    //ends the game if true, red won
    [PunRPC]
    public void endGame(bool winningTeam){
        gameOver = true;
        pcLink.GetComponent<playerController>().endGame(winningTeam);
    }

    private void spawnHealthPacks(){
        objectSpawnPoints.Clear();
        for (int i = 0; i < totalObjectSpawnPoints.Count; i++)
        {
            if (!totalObjectSpawnPoints[i].GetComponent<itemChecker>().isTouchingSomething())
            {
                objectSpawnPoints.Add(totalObjectSpawnPoints[i]);
            }
        }
        //spawns 1-2 health packs and 0-2 gold health packs
        if (objectSpawnPoints.Count > 0)
        {
            int randG = Random.Range(1, 3);
            int randSpawn;
            for (int i = 0; i < randG; i++){
                if (objectSpawnPoints.Count > 0)
                {
                    randSpawn = Random.Range(0, objectSpawnPoints.Count);
                    PhotonNetwork.InstantiateSceneObject("health",
                                      objectSpawnPoints[randSpawn].transform.position,
                                      Quaternion.identity,
                                      0);
                    objectSpawnPoints.RemoveAt(randSpawn);
                }
            }
            randG = Random.Range(0, 3);
            for (int i = 0; i < randG; i++)
            {
                if (objectSpawnPoints.Count > 0)
                {
                    randSpawn = Random.Range(0, objectSpawnPoints.Count);
                    PhotonNetwork.InstantiateSceneObject("goldHealth",
                                              objectSpawnPoints[randSpawn].transform.position,
                                              Quaternion.identity,
                                              0);
                    objectSpawnPoints.RemoveAt(randSpawn);
                }
            }
        }
       // spawnMoreBlasters(3);
    }

    private void spawnMoreBlasters(int numBlasters){
        objectSpawnPoints.Clear();
        int randSpawn;
        int randBlast;
        for (int i = 0; i < totalObjectSpawnPoints.Count; i++)
        {
            if (!totalObjectSpawnPoints[i].GetComponent<itemChecker>().isTouchingSomething())
            {
                objectSpawnPoints.Add(totalObjectSpawnPoints[i]);
            }
        }
        for (int i = 0; i < numBlasters; i++)
        {
            if (objectSpawnPoints.Count > 0)
            {
                randSpawn = Random.Range(0, objectSpawnPoints.Count);
                randBlast = Random.Range(0, blasterNames.Length);
                if (randBlast <= 3)
                {
                    PhotonNetwork.InstantiateSceneObject(blasterNames[randBlast],
                                          objectSpawnPoints[randSpawn].transform.position,
                                          Quaternion.Euler(90, 0, 0),
                                          0);
                }
                else
                {
                    PhotonNetwork.InstantiateSceneObject(blasterNames[randBlast],
                                              objectSpawnPoints[randSpawn].transform.position,
                                              Quaternion.Euler(180, 0, -90),
                                              0);
                }
                objectSpawnPoints.RemoveAt(randSpawn);
            }
        }
        

    }
    //spawns a gold blaster in an availible position if none are availible, 
    //nothing happens
    private void spawnGoldBlaster(){
        objectSpawnPoints.Clear();
        for (int i = 0; i < totalObjectSpawnPoints.Count; i++){
            if(!totalObjectSpawnPoints[i].GetComponent<itemChecker>().isTouchingSomething()){
                objectSpawnPoints.Add(totalObjectSpawnPoints[i]);
            }
        }
        if(objectSpawnPoints.Count > 0){
            int rand = Random.Range(0, objectSpawnPoints.Count);
            PhotonNetwork.InstantiateSceneObject("waterBalloonLauncher",
                                      objectSpawnPoints[rand].transform.position,
                                      Quaternion.identity,
                                      0);
           photonView.RPC("notifyPCOfGold", RpcTarget.AllBufferedViaServer, null);
            
        }
        else{
            Debug.Log("Couldn't find any suitable spawn points for gold blaster");
        }
    }
    //void OnEvent(byte eventcode, object content, int senderid)
    //{
    //    //update Num Players
    //    if(eventcode == 0){
            
    //    }
    //}
    //[PunRPC]
    //public void p1(){
    //    numPlayers++;
    //}
    //Team true = red
    [PunRPC]
    public void updateNumPlayers(int viewID, bool team, bool isAI){
        if (isAI)
        {
            aiIDS.Add(viewID);
            names.Add(viewID, PhotonView.Find(viewID).GetComponent<aiPlayerController>().getName());
            //Debug.Log("Adding name " + PhotonView.Find(viewID).GetComponent<aiPlayerController>().getName());
            //Debug.Log("received name " + names[viewID]);
        }
        else
        {
            players.Add(viewID);
            string nam = PhotonView.Find(viewID).GetComponent<playerController>().getName();
            names.Add(viewID, PhotonView.Find(viewID).GetComponent<playerController>().getName());
            Debug.Log("The added user was " + viewID + " named " + nam);
        }
        Debug.Log("nameGiven: " + names[viewID]);
        Debug.Log("ID: " + viewID);
        if(team){
            redTeam.Add(viewID);
            redScores.Add(viewID, 0);
            for (int i = 0; i < redScoreOrders.Length; i++)
            {
                if (redScoreOrders[i] == null)
                {
                    redScoreOrders[i] = new ScoreOrder(viewID, (string)names[viewID], true, 0, 0);
                    break;
                }
            }
        }
        else{
            blueTeam.Add(viewID);
            blueScores.Add(viewID, 0);
            for (int i = 0; i < blueScoreOrders.Length; i++)
            {
                if (blueScoreOrders[i] == null)
                {
                    blueScoreOrders[i] = new ScoreOrder(viewID, (string)names[viewID], false, 0, 0);
                    break;
                }
            }
        }
        deaths.Add(viewID, 0);

    }

    [PunRPC]
    public void removePlayerAddAI(int viewID, bool team, int oldID)
    {
        aiIDS.Remove(oldID);
        aiIDS.Add(viewID);
        names.Remove(oldID);
        names.Add(viewID, PhotonView.Find(viewID).GetComponent<aiPlayerController>().getName());


        if (team)
        {
            redTeam.Remove(oldID);
            redTeam.Add(viewID);
            redScores.Remove(oldID);
            redScores.Add(viewID, 0);
            for (int i = 0; i < redScoreOrders.Length; i++)
            {
                if (redScoreOrders[i] != null)
                {
                    if (redScoreOrders[i].getPlayerID() == oldID)
                    {
                        redScoreOrders[i].setPlayerID(viewID);
                        break;
                    }
                }
            }
        }
        else
        {
            blueTeam.Remove(oldID);
            blueTeam.Add(viewID);
            blueScores.Remove(oldID);
            blueScores.Add(viewID, 0);
            for (int i = 0; i < blueScoreOrders.Length; i++)
            {
                if (blueScoreOrders[i] != null)
                {
                    if (blueScoreOrders[i].getPlayerID() == oldID)
                    {
                        blueScoreOrders[i] = new ScoreOrder(viewID, (string)names[viewID], false, 0, 0);
                        break;
                    }
                }
            }
        }
        deaths.Remove(oldID);
        deaths.Add(viewID, 0);

    }



    [PunRPC]
    public void setTimer(double endTime){
        pcLink.GetComponent<playerController>().setTeam();
        endCountdownTimer = endTime;
        pcLink.GetComponent<playerController>().setGameTimer(endCountdownTimer);


    }

    [PunRPC]
    public void setTeam(){
        pcLink.GetComponent<playerController>().setTeam();
    }

    //Used to allow everyone to load the scene and then to start the game countdown timer
    private IEnumerator wait5SecThenSendTime(){
        this.photonView.RPC("setTeam", RpcTarget.AllBufferedViaServer, null);
        yield return new WaitForSeconds(5f);
        this.photonView.RPC("setTimer", RpcTarget.AllBufferedViaServer, endCountdownTimer);
    }
    private void updateScoreData(){
        ScoreOrder tempSO;
        int idNum;

        for (int i = 0; i < blueScoreOrders.Length; i++){
            tempSO = blueScoreOrders[i];
            idNum = tempSO.getPlayerID();
            tempSO.setScore((int)blueScores[idNum]);
            tempSO.setDeaths((int)deaths[idNum]);
            Debug.Log("name: " + names[idNum] + " score: " + (int)blueScores[idNum]);

            tempSO = redScoreOrders[i];
            idNum = tempSO.getPlayerID();
            tempSO.setScore((int)redScores[idNum]);
            tempSO.setDeaths((int)deaths[idNum]);
            Debug.Log("name: " + names[idNum] + " score: " + (int)redScores[idNum]);
        }
    }

    public ScoreOrder[] getScoreOrders(){
        updateScoreData();
        //puts initial values in 
        ScoreOrder[] bothTeamOrder = new ScoreOrder[10];
        bothTeamOrder[0] = blueScoreOrders[0];
        bothTeamOrder[5] = redScoreOrders[0];

        //adds all of the elements to the array puts blue in index 0-4 red in 5-9
        for (int i = 1; i < blueScoreOrders.Length; i++){
            //Sorts blue elements
            int testIndex = i - 1;
            ScoreOrder temp = bothTeamOrder[testIndex];
            float BDRatio = blueScoreOrders[i].getBDRatio();
            float tempBDRatio = bothTeamOrder[testIndex].getBDRatio();

            while (testIndex >= 0 && BDRatio > tempBDRatio)
            {
                bothTeamOrder[testIndex + 1] = bothTeamOrder[testIndex];
                testIndex--;
                if (testIndex >= 0)
                {
                    tempBDRatio = bothTeamOrder[testIndex].getBDRatio();
                }
                //Debug.Log("conditions met " + testIndex);
            }
//            Debug.Log("BLUE: " + BDRatio + " < " + tempBDRatio + ". Put in " + (testIndex + 1));
            bothTeamOrder[testIndex + 1] = blueScoreOrders[i];

            //Sorts red elements
            //+5 - 1
            testIndex = i + 4;
            temp = bothTeamOrder[testIndex];
            BDRatio = redScoreOrders[i].getBDRatio();
            tempBDRatio = bothTeamOrder[testIndex].getBDRatio();

            while (testIndex >= 5 && BDRatio > tempBDRatio)
            {
                bothTeamOrder[testIndex + 1] = bothTeamOrder[testIndex];
                testIndex--;
                if (testIndex >= 5)
                {
                    tempBDRatio = bothTeamOrder[testIndex].getBDRatio();
                }
            }
            bothTeamOrder[testIndex + 1] = redScoreOrders[i];
        }
        return bothTeamOrder;
    }
}
