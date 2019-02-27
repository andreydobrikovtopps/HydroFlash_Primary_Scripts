using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using GameSparks.Core;
using System.Linq;

/// <summary>
/// Component responsinble for Photon connection
/// </summary>
public class serverConnector : MonoBehaviourPunCallbacks {

    [Tooltip("The objects not to be destroyed")]
    [SerializeField]
    private GameObject[] dontDestroyObjects;

    private int numAIPlayers;
    private string mapName;
    private mapHandler mapHandlerObj;
    private Sprite sprite;
    public bool destroyed;
    private bool disconnected;
    private bool manuallyDiconnect;
    public string versionName = "0.2";
    private int playersInRoom;
    //A backup variable to be used if we don't recieve confirmations, 
    //so that the game still starts
    private float timeToStart = 15f;
    //Using this to know when to begin to see if it's time to start anyway
    private bool checkingTimeToStart;
    //Has the game begun and everything allowed to be instantiated
    private bool gameActuallyBegun;

    private ExitGames.Client.Photon.Hashtable mapHash;
    //both used so that the creator of room instantiates the AIs
    //if that host leaves, it can be passed
    private bool hostOfRoom;
    private bool gameBegun;
    private bool inRoom;

    //Used to check if you should load game normally or wait for lobby
    private bool isLobbyGame;
    private string[] lobbyPlayers;
    private bool waitingForLobbyPlayers;
    Dictionary<int, Player> roomPlayers;
    ICollection<int> roomKeys;
    float nextPlayerCheck;
    int numMatched;

    private int maxLobbyTime = 10;
    private float endTimer;

    public int serverNum;

    private gameNetworkController gnc;

    //An event thrown whenever connected
    public UnityEvent connectEvent;

    //Event thrown to reset the scene on a reload
    public UnityEvent resetSceneStillConnected;

    //Event thrown to update the mapHandler
    public UnityEvent connectedToRoom;

    //An event to be thrown 
    //public UnityEvent suddenDisconnectionEvent;

    public void connectToPhoton(){
        if (!PhotonNetwork.IsConnected)
        {

            //PhotonNetwork.GameVersion = versionName;
            PhotonNetwork.AuthValues = new AuthenticationValues();
            PhotonNetwork.AuthValues.UserId = PlayerPrefs.GetString("gamesparks.userid");
            PhotonNetwork.ConnectUsingSettings();

        }
        else{
            Debug.Log("We're trying to connect but are already connected?");
        }
        //else{
        //    connectEvent.Invoke();
        //}
    }

    private void Start()
    {
        //DontDestroyOnLoad(gameObject);
        serverNum = Random.Range(0, 1000);
        destroyed = false;
        GameObject[] testDestroy = GameObject.FindGameObjectsWithTag("serverConnector");
        foreach(GameObject go in testDestroy)
        {
            if (go.GetComponent<serverConnector>().destroyed)
            {
                Destroy(go);
            }
        }

    }
    public void disconnectFromPhoton(){
        manuallyDiconnect = true;
        PhotonNetwork.Disconnect();
    }

    public override void OnConnectedToMaster(){
        Debug.Log("connected to Photon!");
        connectEvent.Invoke();
        //PhotonNetwork.AuthValues.UserId = PlayerPrefs.GetString("gamesparks.userid");
        Debug.Log("ID is " + PhotonNetwork.AuthValues.UserId);
        //PhotonNetwork.AutomaticallySyncScene = true;
    }


    //Rooms are the same as games
    public override void OnJoinedRoom()
    {
       // if (!isLobbyGame)
       // {
            PhotonNetwork.LocalPlayer.CustomProperties.Clear();
            inRoom = true;
            connectedToRoom.Invoke();
            //PhotonNetwork.AutomaticallySyncScene = true;
            Debug.Log("joined room " + PhotonNetwork.CurrentRoom.Name);
        if(isLobbyGame && hostOfRoom)
        {
            GameObject.FindGameObjectWithTag("chatManager").GetComponent<chatManager>().tellLobbyToJoin();
        }
        // }

    }

    private void OnFailedToConnectToPhoton()
    {
        Debug.Log("Failed to connect to Photon");
        
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {

            int mapInt = mapHandlerObj.getMapIndex();

            mapHash = new ExitGames.Client.Photon.Hashtable();

            //Short non descriptive name to transmit less info and improve speed
            mapHash.Add("MI", mapInt);

            //test code
            hostOfRoom = true;
            RoomOptions roomOptions = new RoomOptions()
            {
                IsVisible = true,
                IsOpen = true,
                MaxPlayers = 10,
                CustomRoomProperties = mapHash,
                PublishUserId = true
            };
        if (!isLobbyGame)
        {
            PhotonNetwork.CreateRoom(PlayerPrefs.GetString("gamesparks.userid"), roomOptions, null);
            endTimer = Time.time + maxLobbyTime;

        }
        else
        {
            PhotonNetwork.CreateRoom(PlayerPrefs.GetString("gamesparks.userid"), roomOptions, null, lobbyPlayers);
            nextPlayerCheck = Time.time + 1f;
            waitingForLobbyPlayers = true;
        }
        //actual code
        //PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 10}, null);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (!manuallyDiconnect)
        {
            for (int i = 0; i < dontDestroyObjects.Length; i++)
            {
                if (dontDestroyObjects[i].tag.Equals("gsh"))
                {
                    dontDestroyObjects[i].GetComponent<GameSparksHandler>().destroyMe();
                }
                else
                {
                    Destroy(dontDestroyObjects[i]);
                }
            }
            Destroy(GetComponent<PhotonView>());
            SceneManager.LoadScene(0);
            destroyed = true;
            disconnected = true;
            Debug.Log("Disconnected " + cause.ToString());
        }
    }

    /*
     * This method should probably be changed in the future
     * to pair based on skill level. For now, it suffices to connect players    
     */
    public void joinRandomGame()
    {
        Debug.Log("joining random room");
        hostOfRoom = false;
        PhotonNetwork.JoinRandomRoom();

    }

    /// <summary>
    /// Starts a game with the local lobby.
    /// </summary>
    public void startLobbyGame()
    {
        bool isLead = GameObject.FindGameObjectWithTag("chatManager").GetComponent<chatManager>().getIsLobbyLeader();
        if (isLead)
        {
            isLobbyGame = true;
            Debug.Log("Starting lobby game, joining random room");
            hostOfRoom = false;
            lobbyPlayers = GameObject.FindGameObjectWithTag("chatManager").GetComponent<chatManager>().getPlayersInLobby();
            PhotonNetwork.JoinRandomRoom(null, 0, 0, null, null, lobbyPlayers);
        }
    }
    // Use this for initialization
    void Awake () {
        PhotonNetwork.AutomaticallySyncScene = true;
        DontDestroyOnLoad(gameObject);
        numAIPlayers = 0;
        hostOfRoom = false;
        gameBegun = false;
        inRoom = false;
        mapHandlerObj = GameObject.FindGameObjectWithTag("mapHandler").GetComponent<mapHandler>();
//        Debug.Log("new server connector");
	}
	
	// Update is called once per frame
	void Update () {
        if (!waitingForLobbyPlayers)
        {
            if (hostOfRoom && !gameBegun && inRoom)
            {
                //start Game
                if ((Time.time > endTimer || PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers) && PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("starting game");
                    gameBegun = true;
                    PhotonNetwork.CurrentRoom.IsOpen = false;
                    PhotonNetwork.QuickResends = 3;
                    PhotonNetwork.IsMessageQueueRunning = false;
                    PhotonNetwork.LoadLevel(1);
                }
            }
            if (gameBegun && PhotonNetwork.IsMasterClient)
            {
                if (!gameActuallyBegun)
                {
                    if (playersInRoom >= PhotonNetwork.CurrentRoom.PlayerCount)
                    {
                        Debug.Log("All players have joined the game, let's start!");
                        GetComponent<PhotonView>().RPC("beginGame", RpcTarget.AllBufferedViaServer, null);
                        gameActuallyBegun = true;
                    }
                    else if (checkingTimeToStart && Time.time > timeToStart)
                    {
                        Debug.Log("Startiing game because we're not waiting anymore!");
                        checkingTimeToStart = false;
                        GetComponent<PhotonView>().RPC("beginGame", RpcTarget.AllBufferedViaServer, null);
                        gameActuallyBegun = true;
                    }
                }
            }
        }
        else
        {
            if (Time.time > nextPlayerCheck) {
                nextPlayerCheck = Time.time + 1f;
                roomPlayers = PhotonNetwork.CurrentRoom.Players;
                roomKeys = roomPlayers.Keys;
                numMatched = 0;
                for (int i = 0; i < roomPlayers.Count; i++)
                {
                    if (lobbyPlayers.Contains(roomPlayers[roomKeys.ElementAt(i)].UserId))
                    {
                        numMatched++;
                    }
                }
                Debug.Log("Matched " + numMatched + " Players in room of " + lobbyPlayers.Length);
                if(numMatched == lobbyPlayers.Length)
                {
                    endTimer = Time.time + 5;
                    waitingForLobbyPlayers = false;
                }

            }
            
        }
    }

    // called first
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void turnOffDisconnect()
    {
        disconnected = false;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (destroyed)
        {
            Destroy(gameObject);
            Debug.Log("DESTROY ME");
        }
        Debug.Log("finished loading scene " + scene.name);
        if (!destroyed)
        {
            if (scene.buildIndex > 0)
            {
                PhotonNetwork.IsMessageQueueRunning = true;
                GetComponent<PhotonView>().RPC("tellMCIJoined", RpcTarget.MasterClient, null);
                if (PhotonNetwork.IsMasterClient)
                {
                    timeToStart += Time.time;
                    checkingTimeToStart = true;
                }
               
            }
        }
        else if(disconnected && scene.buildIndex == 0)
        {
            GameObject.FindGameObjectWithTag("menuManager").GetComponent<MenuManager>().userDisconnected();
            GameObject[] serverConnects = GameObject.FindGameObjectsWithTag("serverConnector");
            for(int i = 0; i < serverConnects.Length; i++)
            {
                if (!serverConnects[i].gameObject.Equals(gameObject))
                {
                    serverConnects[i].GetComponent<serverConnector>().turnOffDisconnect();
                }
            }
            Destroy(gameObject);
        }
        else if(scene.buildIndex > 0)
        {
            Debug.Log("Successfully started game");
        }
       
        //if(scene.name.Equals("mainMenu")){
        //    GameObject[] serverObjs = GameObject.FindGameObjectsWithTag("serverConnector");
        //    for (int i = 0; i < serverObjs.Length; i++){
        //        if(serverObjs[i] != gameObject){
        //            PhotonNetwork.Destroy(serverObjs[i]);
        //        }
        //    }
        //}
    }

    void addAIPlayer(){
        numAIPlayers++;
    }

    public void leaveRoom(){
        if(hostOfRoom && !gameBegun){
            PhotonNetwork.CurrentRoom.IsOpen = false;
            GetComponent<PhotonView>().RPC("everyoneLeaveRoom", RpcTarget.OthersBuffered, null);
        }
        PhotonNetwork.LeaveRoom();
    }

    private void OnDestroy()
    {
        Debug.Log("Destroying... " + serverNum);


    }
    public void exitToMainMenu(){
        Debug.Log("exiting to main");
        PhotonNetwork.LeaveRoom();
        for (int i = 0; i < dontDestroyObjects.Length; i++)
        {
            if (dontDestroyObjects[i].tag.Equals("gsh"))
            {
                dontDestroyObjects[i].GetComponent<GameSparksHandler>().destroyMe();
            }
            else
            {
                Destroy(dontDestroyObjects[i]);
            }
        }
        destroyed = true;
        //GS.Reset();
        GS.Disconnect();
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene(0);
        PhotonNetwork.Destroy(gameObject);


        //PhotonNetwork.Destroy(gameObject.GetComponent<PhotonView>());

        //PhotonNetwork.LoadLevel(0);
        //numAIPlayers = 0;
        //resetSceneStillConnected.Invoke();
        //hostOfRoom = false;
        //gameBegun = false;
        //inRoom = false;
    }



    [PunRPC]
    public void everyoneLeaveRoom(){
        leaveRoom();
    }

    [PunRPC]
    public void tellMCIJoined()
    {
        playersInRoom++;
       
    }
    [PunRPC]
    public void beginGame()
    {
        gnc = GameObject.FindWithTag("GNC").GetComponent<gameNetworkController>();
        gnc.spawnMe();
    }

    private void setMapSettings(){
        
    }

    //when a player disconnects
    public override void OnLeftRoom(){
        Debug.Log("Leaving room");
        inRoom = false;
        //when you exit from a lobby, takes you back to the menu
        if(SceneManager.GetActiveScene().name.Equals("mainMenu")){
            //Debug.Log("now we here baby cakes");
            resetSceneStillConnected.Invoke();
        }
        //reloads the mainMenu
        //PhotonNetwork.Disconnect();
        //SceneManager.LoadScene(0);
        //Destroy(this.gameObject);
        //PhotonNetwork.Destroy(GetComponent<PhotonView>());
    }

    public void joinLeaderRoom(string roomToJoin)
    {
        Debug.Log("hopefully joining room");
        PhotonNetwork.JoinRoom(roomToJoin);
    }
}
