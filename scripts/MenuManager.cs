using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSparks.Api;
using GameSparks.Api.Requests;
using GameSparks.Api.Responses;
using GameSparks.Core;
using GameSparks.Platforms.WebGL;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using UnityEditor;
using System.Linq;
using System;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// The script used to manage the transitions on the main menu. Still porting methods
/// over from gsh
/// </summary>
public class MenuManager : MonoBehaviourPunCallbacks
{
    private IDictionary<string, object> friends;
    private ICollection<string> friendsIDS;
    private ICollection<string> activeFriendsIDS;
    private IDictionary<string, object> requests;
    private ICollection<string> requestsIDS;
    private List<GSData> denied;
    private Dictionary<string, FriendInfo> photonFriendList = new Dictionary<string, FriendInfo>();

    private string currentActiveID;

    [SerializeField]
    private TextMeshProUGUI numRequests;

    [SerializeField]
    private Image requestsBackground;

    [SerializeField]
    private GameObject friendObj;

    [SerializeField]
    private GameObject requestObject;

    [SerializeField]
    private GameObject friendContent;

    [SerializeField]
    private GameObject requestContent;

    [SerializeField]
    private Image blastLeaderImg;

    [SerializeField]
    private TextMeshProUGUI blastLeaderText;

    [SerializeField]
    private Image winsLeaderImg;

    [SerializeField]
    private TextMeshProUGUI winsLeaderText;

    [SerializeField]
    private Image bkImg;

    [SerializeField]
    private TextMeshProUGUI bkText;

    [SerializeField]
    Sprite[] profImages;

    [SerializeField]
    private GameObject activeFriendsHolder;

    [SerializeField]
    private GameObject activeFriendsObject;

    [SerializeField]
    private GameObject disconnectObj;
    /// <summary>
    /// The friend stat data object that will show all of their stats.
    /// </summary>
    [SerializeField]
    private GameObject friendStatDataObj;
    /// <summary>
    /// The friend stat data holder, that will eventually hold all of their stat elements.
    /// </summary>
    [SerializeField]
    private GameObject friendStatDataHolder;
    /// <summary>
    /// The friend stat element to be instantiated.
    /// </summary>
    [SerializeField]
    private GameObject friendStatElement;

    [SerializeField]
    private TextMeshProUGUI friendDataUsername;

    [SerializeField]
    private Image friendDataImage;

    [SerializeField]
    private GameObject activeFriendData;

    [SerializeField]
    private GameSparksHandler gsh;

    [SerializeField]
    private chatManager chatManagerComp;

    [SerializeField]
    private GameObject lobbyInvitesContainer;

    [SerializeField]
    private Button teamBlastBttn;

    [SerializeField]
    private Button squadGameBttn;

    //the user id for the leader of each category if it's blank, you're the default leader
    private string blastLeader = "";
    private string winLeader = "";
    private string bkLeader = "";

    private int blastLeaderIndex;
    private int winLeaderIndex;
    private int bkLeaderIndex;

    //the amount needed to beat to have the most. By default, evrything is the user's
    private int blastLeadAmount;
    private int winLeadAmount;
    private float bkLeadAmount;

    // data formatted as 0 blasts, 1 deaths, 2 wins, 3 losses, 4 level,
    // 5 XP, 6 XPToNext, 7 CQ, 8 IQ, 9 Shots fired, 10 Shots Landed
    private int[] playerStats;

    //A double array of the stats of friends 0 blasts 1 kos 2 wins 3 losses 4 image num
    private int[,] friendsData;

    //the object holding all of the friend's stat info
    private GSData friendsStats;

    //the data returned from "getactivefriends"
    private GSData activeFriends;

    private string displayName;

    private bool inLobby;
    private bool lobbyLeader;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //set friends data (called froom gamesparks handler) which then gets all friends records and such
    public void setfriendData(GSData gsDat)
    {
       
        friends = gsDat.GetGSData("friends").BaseData;
        friendsIDS = friends.Keys;
        requests = gsDat.GetGSData("requests").BaseData;
        requestsIDS = requests.Keys;
        friendsData = new int[friendsIDS.Count, 6];
        gsh = GameObject.FindGameObjectWithTag("gsh").GetComponent<GameSparksHandler>();
        //sets the number of friend requests on the main screem
        numRequests.text = "" + requests.Count;
        if(requests.Count > 0)
        {
            requestsBackground.gameObject.SetActive(true);
        }
        else
        {
            requestsBackground.gameObject.SetActive(false);
        }
        playerStats = gsh.getData();
        displayName = gsh.getName();
        blastLeadAmount = playerStats[0];
        winLeadAmount = playerStats[2];
        if (playerStats[1] != 0) {
            bkLeadAmount = (float) playerStats[0] / playerStats[1];
                }
        else
        {
            bkLeadAmount = (float)playerStats[0];
        }
        getFriendsStats();

    }
    /// <summary>
    /// Gets the friends stats from GS.
    /// </summary>
    private void getFriendsStats()
    {
        new LogEventRequest().SetEventKey("getPlayerFriendsStats").Send((response) =>
        {
            if (!response.HasErrors)
            {
                Debug.Log("Received Friends' stats From GameSparks...");
                friendsStats = response.ScriptData.GetGSData("friendsStats");
                setTopFriends();

            }
            else
            {
                Debug.Log("Problem Receiving Friends' stats From GameSparks...");

            }

        });
    }
    /// <summary>
    /// Sets the top friends on the player friends section.
    /// </summary>
    private void setTopFriends()
    {
        //determine who the top players are
        for(int i = 0; i < friendsIDS.Count; i++)
        {
            friendsData[i, 0] = (int)friendsStats.GetGSData(friendsIDS.ElementAt(i)).GetInt("HydroFlash_BLASTS");
            friendsData[i, 1] = (int)friendsStats.GetGSData(friendsIDS.ElementAt(i)).GetInt("HydroFlash_DEATHS");
            friendsData[i, 2] = (int)friendsStats.GetGSData(friendsIDS.ElementAt(i)).GetInt("HydroFlash_WINS");
            friendsData[i, 3] = (int)friendsStats.GetGSData(friendsIDS.ElementAt(i)).GetInt("HydroFlash_LOSSES");
            friendsData[i, 4] = (int)((GSData)friends[friendsIDS.ElementAt(i)]).GetInt("profNum");
            friendsData[i, 5] = (int)((GSData)friends[friendsIDS.ElementAt(i)]).GetInt("level");

            if (friendsData[i, 0] > blastLeadAmount)
            {
                blastLeader = friendsIDS.ElementAt(i);
                blastLeadAmount = friendsData[i, 0];
                blastLeaderIndex = i;
            }
            if (friendsData[i, 2] > winLeadAmount)
            {
                winLeader = friendsIDS.ElementAt(i);
                winLeadAmount = friendsData[i, 2];
                winLeaderIndex = i;
            }
            float bkNum;
            if(friendsData[i,1] > 0)
            {
                bkNum = (float)friendsData[i, 0] / friendsData[i, 1];
            }
            else
            {
                bkNum = friendsData[i, 0];
            }
            if(bkNum > bkLeadAmount)
            {
                bkLeader = friendsIDS.ElementAt(i);
                bkLeadAmount = bkNum;
                bkLeaderIndex = i;
            }
        }

        //set top players
        if (blastLeader.Equals(""))
        {
            blastLeaderText.text = displayName + ": " + blastLeadAmount;
            blastLeaderImg.sprite = gsh.getProfImage();
        }
        else
        {
            blastLeaderText.text = (string)((GSData)friends[blastLeader]).GetString("displayName") + ": " + blastLeadAmount;
            blastLeaderImg.sprite = profImages[friendsData[blastLeaderIndex, 4]];
        }

        if (winLeader.Equals(""))
        {
            winsLeaderText.text = displayName + ": " + winLeadAmount;
            winsLeaderImg.sprite = gsh.getProfImage();
        }
        else
        {
            winsLeaderText.text = (string)((GSData)friends[winLeader]).GetString("displayName") + ": " + winLeadAmount;
            winsLeaderImg.sprite = profImages[friendsData[winLeaderIndex, 4]];
        }

        if (bkLeader.Equals(""))
        {
            bkText.text = displayName + ": " + bkLeadAmount.ToString("0.00");
            bkImg.sprite = gsh.getProfImage();
        }
        else
        {
            bkText.text = (string)((GSData)friends[bkLeader]).GetString("displayName") + ": " + bkLeadAmount.ToString("0.00");
            bkImg.sprite = profImages[friendsData[bkLeaderIndex, 4]];
        }

        setFriendsAndRequests();

    }

    private void setFriendsAndRequests()
    {
        for (int i = friendContent.transform.childCount - 1; i >= 0; i--)
        {
            //Debug.Log("child " + scrollViewObj.transform.GetChild(0).name);
            Destroy(friendContent.transform.GetChild(i).gameObject);
        }
        for (int i = requestContent.transform.childCount - 1; i >= 0; i--)
        {
            //Debug.Log("child " + scrollViewObj.transform.GetChild(0).name);
            Destroy(requestContent.transform.GetChild(i).gameObject);
        }

        friendContent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float) 100 * friendsIDS.Count);
        //build friend list
        for (int i = 0; i < friendsIDS.Count; i++)
        {
            GameObject tempObj = Instantiate(friendObj);
            tempObj.transform.SetParent(friendContent.transform, false);
            playerFriend fp = tempObj.GetComponent<playerFriend>();
            fp.setUserAndPicID((string)((GSData)friends[friendsIDS.ElementAt(i)]).GetString("displayName"),
             profImages[friendsData[i, 4]], friendsIDS.ElementAt(i), friendsData[i, 5]);
            fp.setOtherData(friendsData[i, 2], friendsData[i, 3], friendsData[i, 0], friendsData[i, 1]);
        }
        requestContent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float) 90 * requestsIDS.Count);
        //build request list
        for (int i = 0; i < requestsIDS.Count; i++)
        {
            GameObject tempObj = Instantiate(requestObject);
            tempObj.transform.SetParent(requestContent.transform, false);
            playerFriend fp = tempObj.GetComponent<playerFriend>();
            fp.setUserAndPicID((string)((GSData)requests[requestsIDS.ElementAt(i)]).GetString("displayName"),
                profImages[(int)((GSData)requests[requestsIDS.ElementAt(i)]).GetInt("profNum")]
             , requestsIDS.ElementAt(i),
                 (int)((GSData)requests[requestsIDS.ElementAt(i)]).GetInt("level"));
        }
    }

    public void getActiveFriends()
    {
        new LogEventRequest().SetEventKey("getActiveFriends").Send((response) =>
        {
            if (!response.HasErrors)
            {
                Debug.Log("Received Friends' stats From GameSparks...");
                activeFriends = response.ScriptData.GetGSData("activeList");
                activeFriendsIDS = activeFriends.BaseData.Keys;
                setActiveFriends();


            }
            else
            {
                Debug.Log("Problem Receiving Friends' stats From GameSparks...");

            }

        });
    }

    private void setActiveFriends()
    {
        deleteAllActiveFriendChildren();
        activeFriendsHolder.GetComponent<RectTransform>().sizeDelta = new Vector2((float) 275 * activeFriendsIDS.Count, 100);
        for (int i = 0; i < activeFriendsIDS.Count; i++)
        {
            GameObject tempObj = Instantiate(activeFriendsObject);
            tempObj.transform.SetParent(activeFriendsHolder.transform, false);
            activePlayer fp = tempObj.GetComponent<activePlayer>();
            GSData tempData = activeFriends.GetGSData(activeFriendsIDS.ElementAt(i));
            fp.setActivePlayerDetails((string)tempData.GetString("displayName"),
             (int)tempData.GetInt("level"), profImages[(int)tempData.GetInt("profNum")], activeFriendsIDS.ElementAt(i));
        }
    }

    public void userDisconnected()
    {
        disconnectObj.SetActive(true);
    }

    public void showPlayerData(string playerID)
    {
        deleteAllFriendDataChildren();
        int index = -1;
        for(int i = 0; i < friendsIDS.Count; i++)
        {
            if (friendsIDS.ElementAt(i).Equals(playerID))
            {
                index = i;
                break;
            }
        }
        GameObject tempObj = Instantiate(friendStatElement, friendStatDataHolder.transform, false);
       // tempObj.transform.SetParent(friendStatDataObj.transform, false);
        tempObj.GetComponent<friendStatData>()
            .setStatData(playerStats[0].ToString(),
            friendsData[index, 0].ToString(),
             "Blasts");

        GameObject tempObj2 = Instantiate(friendStatElement, friendStatDataHolder.transform, false);
        //tempObj2.transform.SetParent(friendStatDataObj.transform, false);
        tempObj2.GetComponent<friendStatData>()
            .setStatData(playerStats[1].ToString(),
            friendsData[index, 1].ToString(),
             "Knockouts");

        GameObject tempObj3 = Instantiate(friendStatElement, friendStatDataHolder.transform, false);
        //tempObj3.transform.SetParent(friendStatDataHolder.transform, false);
        float bkr;
        float friendBKR;
        if(playerStats[1] > 0)
        {
            bkr = playerStats[0] / (float) playerStats[1];
        }
        else
        {
            bkr = playerStats[0];
        }

        if (friendsData[index, 1] > 0)
        {
            friendBKR = friendsData[index, 0] / (float)friendsData[index, 1];
        }
        else
        {
            friendBKR = friendsData[index, 0];
        }
        tempObj3.GetComponent<friendStatData>()
            .setStatData(bkr.ToString("0.00"),
            friendBKR.ToString("0.00"),
             "B/K ratio");

        GameObject tempObj4 = Instantiate(friendStatElement, friendStatDataHolder.transform, false);
        //tempObj4.transform.SetParent(friendStatDataHolder.transform, false);
        tempObj4.GetComponent<friendStatData>()
            .setStatData(playerStats[2].ToString(),
            friendsData[index, 2].ToString(),
             "Wins");

        GameObject tempObj5 = Instantiate(friendStatElement, friendStatDataHolder.transform, false);
        //tempObj5.transform.SetParent(friendStatDataHolder.transform, false);
        tempObj5.GetComponent<friendStatData>()
            .setStatData(playerStats[3].ToString(),
            friendsData[index, 3].ToString(),
             "Losses");
       
        friendDataImage.sprite = profImages[(int)((GSData)friends[friendsIDS.ElementAt(index)]).GetInt("profNum")];
        friendDataUsername.text = (string)((GSData)friends[playerID]).GetString("displayName");
        friendStatDataObj.SetActive(true);
    }

    private void deleteAllFriendDataChildren()
    {
        for (int i = friendStatDataHolder.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(friendStatDataHolder.transform.GetChild(i).gameObject);
        }
    }
    private void deleteAllActiveFriendChildren()
    {
        for (int i = activeFriendsHolder.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(activeFriendsHolder.transform.GetChild(i).gameObject);
        }
    }

    public void showActivePlayer(string playerID)
    {
        currentActiveID = playerID;
        activeFriendData.SetActive(true);
        string playerUsername = (string)((GSData)friends[playerID]).GetString("displayName");
        Sprite playerImage = profImages[(int)((GSData)friends[playerID]).GetInt("profNum")];
        bool roomStatus = photonFriendList[playerID].IsInRoom;
        activeFriendData.GetComponent<activeFriendDataObject>().setActiveFriendInfo(playerUsername, roomStatus, playerImage);

    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.FindFriends(friendsIDS.ToArray<string>());
        Debug.Log("FRIEND ARRAY SET");
    }
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }
    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
       Debug.Log("ON FRIEND UPDATE CALLED");
        for (int i = 0; i < friendList.Count; i++)
        {
            FriendInfo friend = friendList[i];
            if (!photonFriendList.ContainsKey(friend.UserId))
            {
                photonFriendList.Add(friend.UserId, friend);
            }
           // Debug.LogFormat("{0}", friend);
           // Debug.Log("Online var " + friend.IsOnline);
           // Debug.Log("Is in room " + friend.IsInRoom.ToString());
        }
    }

    public void sendLobbyInvite()
    {
        chatManagerComp.sendLobbyInvite(currentActiveID);
    }

    public void putMessageOnScreen()
    {

    }

    /// <summary>
    /// When you join a lobby, disables some menu items.
    /// </summary>
    public void joinedLobby(bool isLeader)
    {
        lobbyLeader = isLeader;
        inLobby = true;

        lobbyInvitesContainer.SetActive(false);
        teamBlastBttn.interactable = false;
        squadGameBttn.interactable = true;

    }

    public string getUsernameFromID(string playerID)
    {
        if (playerID.Equals(PlayerPrefs.GetString("gamesparks.userid")))
        {
            return displayName;
        }
            return (string)((GSData)friends[playerID]).GetString("displayName");

    }

    public Sprite getSpriteFromID(string playerID)
    {
        if (playerID.Equals(PlayerPrefs.GetString("gamesparks.userid")))
        {
            return gsh.getProfImage();
        }
            return profImages[(int)((GSData)friends[playerID]).GetInt("profNum")];
    }

    public void joinLeaderRoom(string leaderID)
    {
        Debug.Log("menu manager joining room " + leaderID);
        GameObject.FindGameObjectWithTag("serverConnector").GetComponent<serverConnector>().joinLeaderRoom(leaderID);
    }

}
