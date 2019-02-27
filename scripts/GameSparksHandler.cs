using System.Collections.Generic;
using UnityEngine;
using GameSparks.Api.Requests;
using GameSparks.Core;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

/// <summary>
/// Game sparks handler. Used to connect GameSparks functionality to the game, previously also managed the menus
/// Slowly transitioning the menu functionality to a menu manager.
///  The way I handled a lot of things in this script is based 
/// on the success of the previous method. This should be fine, but be aware of this
/// What I mean by this is first it checks that it's connected---on success--> next thing--->
/// </summary>
public class GameSparksHandler : MonoBehaviour
{

    //used to prevent things from being called while destroying this object
    private bool destroyed = false;

    //Where the user types in their username
    [SerializeField]
    private TMP_InputField username;

    //Where the user types in their password
    [SerializeField]
    private TMP_InputField password;

    [SerializeField]
    private Image connectingImage;

    [SerializeField]
    private TextMeshProUGUI connectingImageText;

    [SerializeField]
    private Button loginButton;

    [SerializeField]
    private Button findGameButton;

    [SerializeField]
    private Button studyListsButton;

    [SerializeField]
    private Button myProfileButton;

    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private TextMeshProUGUI loggedInText;

    [SerializeField]
    private TextMeshProUGUI logoutText;

    [SerializeField]
    private TextMeshProUGUI errorText;

    //The object that holds all of the login stuff
    [SerializeField]
    private GameObject loginObject;

    [SerializeField]
    private Image xpBar;

    [SerializeField]
    private TextMeshProUGUI levelText;

    [SerializeField]
    private Sprite[] profImages;

    [SerializeField]
    private Image profPic;

    [SerializeField]
    private GameObject friendOnListObj;

    [SerializeField]
    private GameObject scrollViewObj;

    [SerializeField]
    private GameObject addStudyListScroll;

    [SerializeField]
    private GameObject addStudyListObj;

    [SerializeField]
    private GameObject addStudyListContentHolder;

    [SerializeField]
    private TMP_InputField searchUsername;

    [SerializeField]
    private TMP_InputField searchDisplayName;

    [SerializeField]
    private TMP_InputField searchSchool;

    [SerializeField]
    private TMP_InputField searchEmail;

    [SerializeField]
    private AudioSource audioSource;

    private string displayName;

    private int XP;
    private int lastXP;
    private int XPToNext;

    private int level;

    private int wins;
    private int losses;
    private int blasts;
    private int deaths;
    private int profileNum;
    private int correctQuestions;
    private int incorrectQuestions;
    private int shotsFired;
    private int shotsLanded;

    //Game settings
    private int qualityLev;
    private int inGameVol;
    private int sfxVol;
    private int xSense;
    private int ysense;

    private long gameplayTime;


    private float blastsPG;


    //used to prevent multiple handlers
    private bool mainHandler;

    //is the user account Gold
    private bool isGold;

    private int autoWP;

    private string[][] words = new string[50][];
    private string[][] defs = new string[50][];
    private string[][] defs2 = new string[50][];
    private string[][] misspell1 = new string[50][];
    private string[][] misspell2 = new string[50][];
    private string[][] misspell3 = new string[50][];
    private int[][] studyXP = new int[30][];

    //private string[][] studyList0 = new string[7][];
    //private string[][] studyList1 = new string[7][];
    //private string[][] studyList2 = new string[7][];
    //private string[][] studyList3 = new string[7][];
    //private string[][] studyList4 = new string[7][];
    //private string[][] studyList5 = new string[7][];
    //private string[][] studyList6 = new string[7][];
    //private string[][] studyList7 = new string[7][];
    //private string[][] studyList8 = new string[7][];
    //private string[][] studyList9 = new string[7][];

    private string[] titles = new string[30];
    private string[] authors = new string[30];
    private string[] listID = new string[30];
    private bool[] listEmpty = new bool[30];

    /// <summary>
    /// The type.
    /// 0 - CC Vocab
    /// 1 - plain list (answer [word], question [definition])
    /// 2 - 
    /// </summary>
    private int[] type = new int[30];

    [SerializeField]
    private TextMeshProUGUI DEMOTEXT;

    [SerializeField]
    private GameObject studyListPrefab;

    [SerializeField]
    private GameObject studyListHolder;

    [SerializeField]
    private GameObject wordDefPrefab;

    [SerializeField]
    private GameObject wordDefHolder;

    [SerializeField]
    private GameObject studyListMenu;

    [SerializeField]
    private GameObject wordDefMenu;

    [SerializeField]
    private TextMeshProUGUI questionTitle;
    /// <summary>
    /// The number of the current sl.
    /// </summary>
    private int currentSL;

    [SerializeField]
    private GameObject setInactiveError;

    [SerializeField]
    private GameObject activeObject;

    [SerializeField]
    private Sprite greenSprite;

    [SerializeField]
    private Sprite blueSprite;

    [SerializeField]
    private GameObject savingImage;

    private GSData xpAmounts;

    private MenuManager menuManager;

    // Import the JSLib as following. Make sure the
    // names match with the JSLib file we've just created.

    [DllImport("__Internal")]
    private static extern void requestLoginInfo();

    private void Awake()
    {
        Resources.UnloadUnusedAssets();
        //        Debug.Log("Quality" + QualitySettings.GetQualityLevel());
        SceneManager.sceneLoaded += OnSceneLoaded;
        for (int i = 0; i < type.Length; i++)
        {
            type[i] = -1;
        }
        //        Debug.Log("GSH is awake");
    }

    // Use this for initialization
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        GS.GameSparksAvailable += handleGameSparksAvailible;
        if (!GS.Available)
        {
            connectingImage.gameObject.SetActive(true);
            connectingImageText.text = "Connecting to the server... (taking a long time, huh?)";
        }
    }

    //When returning to the main menu, automatically do the GS stuff
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!destroyed)
        {
            if (scene.buildIndex == 0)
            {
                Cursor.visible = true;
                menuManager = GameObject.FindGameObjectWithTag("menuManager").GetComponent<MenuManager>();
            }
        }
    }

    public void resetAuthToken(string token)
    {
        PlayerPrefs.SetString("gamesparks.authtoken", token);

    }
    public void resetUserId(string userID)
    {
        PlayerPrefs.SetString("gamesparks.userid", userID);
    }
    public void attemptToLogin()
    {
        new AuthenticationRequest().SetUserName(username.text)
                                   .SetPassword(password.text)
                                   .Send((response) =>
                                   {
                                       if (!response.HasErrors)
                                       {
                                           loginObject.SetActive(false);
                                           loginButton.interactable = false;
                                           errorText.gameObject.SetActive(false);
                                           loadData();
                                           //setLevel();
                                           //setLoggedInScreen();
                                           Debug.Log("Player Authenticated...");
                                           Debug.Log("user Id " + PlayerPrefs.GetString("gamesparks.userid"));
                                       }
                                       else
                                       {
                                           Debug.Log("Error Authenticating Player...");
                                           DEMOTEXT.gameObject.SetActive(true);
                                           errorText.gameObject.SetActive(true);
                                           username.text = "";
                                           password.text = "";
                                       }
                                   });
    }

    /// <summary>
    /// Sets the main menu buttons to either enabled or disabled.
    /// </summary>
    /// <param name="buttonActive">If set to <c>true</c> button active.</param>
    private void setButtons(bool buttonActive)
    {
        findGameButton.interactable = buttonActive;
        studyListsButton.interactable = buttonActive;
        myProfileButton.interactable = buttonActive;
        settingsButton.interactable = buttonActive;
    }

    private void handleGameSparksAvailible(bool isAvailible)
    {
        //*****Comment this out to test with Unity only!!!******
        //trigger the imported function
        Debug.Log("Sending info from Unity ->");
        //requestLoginInfo();
        

        if (!destroyed && SceneManager.GetActiveScene().name.Equals("mainMenu"))
        {
            if (isAvailible)
            {
                //Debug.Log("We're connected now!");
                connectingImageText.text = "Retrieving Player Data...";
                //connectingImage.gameObject.SetActive(false);
                if (GS.Authenticated)
                {
                    Debug.Log("logged in!");
                    //Debug.Log("authtoken " + PlayerPrefs.GetString("gamesparks.authtoken"));
                    Debug.Log("user Id " + PlayerPrefs.GetString("gamesparks.userid"));
                    
                    loginButton.interactable = false;
                    setButtons(true);
                    loadData();
                }
                else
                {
                    setButtons(false);
                    audioSource.Play();
                    connectingImage.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.Log("Looks like we disconnected?");
                connectingImage.gameObject.SetActive(true);
                //connectingImage.gameObject.GetComponentInChildren<TextMeshProUGUI>().text =
                //"Can't connect to the game, try refreshing the page?";
            }
        }
    }
    //Loads the player data from gamesparks
    private void loadData()
    {
        new LogEventRequest().SetEventKey("loadPlayerData").Send((response) =>
        {
            if (!response.HasErrors)
            {
                //Debug.Log("Received Player Data From GameSparks...");
                GSData data = response.ScriptData.GetGSData("Game_Data");
                wins = (int)data.GetInt("HydroFlash_WINS");
                losses = (int)data.GetInt("HydroFlash_LOSSES");
                blasts = (int)data.GetInt("HydroFlash_BLASTS");
                deaths = (int)data.GetInt("HydroFlash_DEATHS");
                XP = (int)data.GetInt("HydroFlash_XP");
                level = (int)data.GetInt("HydroFlash_LEVEL");
                gameplayTime = (long)data.GetLong("HydroFlash_TIME");
                correctQuestions = (int)data.GetInt("HydroFlash_CQ");
                incorrectQuestions = (int)data.GetInt("HydroFlash_IQ");
                shotsFired = (int)data.GetInt("HydroFlash_FIRED");
                shotsLanded = (int)data.GetInt("HydroFlash_LANDED");

                float tempXP = ((float)XP + 23000f) / 21959f;
                tempXP = Mathf.Log(tempXP) / .079f;
                Debug.Log("level check " + Mathf.FloorToInt(tempXP));
                if (level != Mathf.FloorToInt(tempXP) + 1)
                {
                    level = Mathf.FloorToInt(tempXP) + 1;
                    setLevel();
                }
                else
                {
                    setLoggedInScreen();
                }
            }
            else
            {
                Debug.Log("Error Loading Player Data...");
            }
        });

    }

    //updates the player's level
    private void setLevel()
    {
        if (XP < 764)
        {
            level = 1;
        }
        Debug.Log("level: " + level);
        Debug.Log("XP: " + XP);
        //updates the level
        new LogEventRequest()
            .SetEventKey("updateStats")
            .SetEventAttribute("level", level)
            .Send((response) =>
            {
                if (!response.HasErrors)
                {
                    Debug.Log("Player Saved To GameSparks...");
                    setLoggedInScreen();

                }
                else
                {
                    Debug.Log("Error Saving Player Data...");
                }
            });
    }
    //Sets all of the data and info on the main menu
    private void setLoggedInScreen()
    {
        Debug.Log("Logging in!");
        XPToNext = Mathf.RoundToInt(Mathf.Exp((float)(level) * .079f) * 21959 - 23000);
        Debug.Log("Level " + level + " with " + XP + " XP " + XPToNext + " to next!");
        if (level > 1)
        {
            float prevXP = Mathf.Exp((float)(level - 1) * .079f) * 21959 - 23000;
            xpBar.fillAmount = (((float)XP) - prevXP) / ((float)(XPToNext) - prevXP);
        }
        else
        {
            xpBar.fillAmount = (float)XP / (float)XPToNext;
        }
        logoutText.gameObject.SetActive(true);
        DEMOTEXT.gameObject.SetActive(false);
        levelText.text = "Level " + level;
        connectingImage.gameObject.SetActive(false);
        connectingImageText.text = "Refreshing Data...";
        setButtons(true);

        new AccountDetailsRequest()
        .Send((response) =>
        {
            if (!response.HasErrors)
            {
                displayName = response.DisplayName;

                onReceivedResponse();
            }
            else
            {
                Debug.Log("had a problem getting data");
            }
        });

    }
    //Sets profile data
    private void onReceivedResponse()
    {
        loggedInText.text = "Welcome back, " + displayName;
        new LogEventRequest().SetEventKey("getProfImage")
                             .Send((response) =>
        {
            if (!response.HasErrors)
            {
                Debug.Log("Prof pic info received from GameSparks...");
                GSData data = response.ScriptData.GetGSData("Prof_Data");
                profileNum = (int)data.GetInt("profNum");
                profPic.sprite = profImages[profileNum];
                getPlayerFriends();
                setSettings();
                //audioSource.Play();
            }
            else
            {
                Debug.Log("Error Loading Prof pic Data...");
            }
        });

    }

    private void checkSubscription()
    {
        new LogEventRequest().SetEventKey("getSubscription")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     Debug.Log("Subscription info received from GameSparks...");

                                     GSData data = response.ScriptData.GetGSData("Sub_Data");
                                     isGold = (int)data.GetInt("isGold") == 1;
                                 }
                                 else
                                 {
                                     Debug.Log("Error Loading Prof pic Data...");
                                 }
                             });
    }
    public string getName()
    {
        return displayName;
    }
    //logs the player out, called from the logout text
    // Also, I added making the loginButton interactable in two lines because it wasn't working
    public void logout()
    {
        PlayerPrefs.DeleteKey("gamesparks.authtoken");
        PlayerPrefs.DeleteKey("gamesparks.userid");
        GameObject.FindGameObjectWithTag("serverConnector").GetComponent<serverConnector>().exitToMainMenu();
        //GS.Reset();
       
        //SceneManager.LoadScene(0);
        //destroyMe();
        //GS.Disconnect();
        //new EndSessionRequest().Send((response) =>
        //{
        //    if (!response.HasErrors)
        //    {
        //        Debug.Log("Successfully logged out");

        //    }
        //    else
        //    {
        //        Debug.Log("had a problem logging out");
        //    }
        //});
        //loggedInText.text = "Login to save your data!";
        //logoutText.gameObject.SetActive(false);

    }

    //public void setUsername(string username){
    //    this.username = username;
    //}

    //public void setPassword(string password){
    //    this.password = password;
    //}

    void Update()
    {

    }
    //returns data formatted as 0 blasts, 1 deaths, 2 wins, 3 losses, 4 level,
    //5 XP, 6 XPToNext, 7 CQ, 8 IQ, 9 Shots fired, 10 Shots Landed
    public int[] getData()
    {
        int[] playerData = new int[] { blasts, deaths, wins, losses, level,
            XP, XPToNext, correctQuestions, incorrectQuestions, shotsFired, shotsLanded};

        return playerData;
    }

    public long getGameplayTime()
    {
        return gameplayTime;
    }

    public Sprite getProfImage()
    {
        return profImages[profileNum];
    }

    public int getProfNum()
    {
        return profileNum;
    }
    //checks if user has a Gold account
    public bool getIsGold()
    {
        return isGold;
    }

    public void resetProfPic()
    {
        new LogEventRequest().SetEventKey("getProfImage")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     Debug.Log("Prof pic info received from GameSparks...");
                                     GSData data = response.ScriptData.GetGSData("Prof_Data");
                                     profileNum = (int)data.GetInt("profNum");
                                     profPic.sprite = profImages[profileNum];
                                 }
                                 else
                                 {
                                     Debug.Log("Error Loading Prof pic Data...");
                                 }
                             });
    }

    public void setSettings()
    {
        new LogEventRequest().SetEventKey("loadSettings")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     Debug.Log("Settings received from GameSparks...");
                                     GSData data = response.ScriptData.GetGSData("Setting_Data");
                                     qualityLev = (int)data.GetInt("HydroFlash_QUALLEV");
                                     QualitySettings.SetQualityLevel(qualityLev);
                                     Debug.Log("qual: " + qualityLev);
                                     inGameVol = (int)data.GetInt("HydroFlash_GAMEVOL");
                                     audioSource.GetComponent<audioManager>().setInGameVolume((float)inGameVol / 20f);
                                     audioSource.Play();
                                     sfxVol = (int)data.GetInt("HydroFlash_SFXVOL");
                                     xSense = (int)data.GetInt("HydroFlash_XSENSE");
                                     ysense = (int)data.GetInt("HydroFlash_YSENSE");
                                     autoWP = (int)data.GetInt("HydroFlash_AUTOWP");
                                     loadQuestionData();
                                 }
                                 else
                                 {
                                     Debug.Log("Error Loading Setting Data...");
                                 }
                             });

    }

    //Sends settings 0 QualLev 1 inGameVol 2 SFXVOL 3 xsense 4 ysense to GameSparks 5 auto Water Pack 
    public void setSettingsToGS(int QL, int IGV, int SFXV, int XSENSE, int YSENSE, int aWP)
    {
        qualityLev = QL;
        inGameVol = IGV;
        audioSource.GetComponent<audioManager>().setInGameVolume(inGameVol / 20f);
        sfxVol = SFXV;
        xSense = XSENSE;
        ysense = YSENSE;
        autoWP = aWP;

        if (SceneManager.GetActiveScene().buildIndex == 1)
        {
            GameObject.FindWithTag("cam").GetComponent<SmoothMouseLook>().setAgainSettings(xSense, ysense);

        }

        new LogEventRequest()
            .SetEventKey("setSettings")
            .SetEventAttribute("xSensitivity", XSENSE)
            .SetEventAttribute("ySensitivity", YSENSE)
            .SetEventAttribute("gameVolume", IGV)
            .SetEventAttribute("SFXVolume", SFXV)
            .SetEventAttribute("qualityLevel", QL)
            .SetEventAttribute("autoWP", aWP)
            .Send((response) =>
            {
                if (!response.HasErrors)
                {
                    Debug.Log("Settings Saved To GameSparks...");


                }
                else
                {
                    Debug.Log("Error Saving Settings Data...");
                }
            });
    }



    private void loadQuestionData()
    {
        savingImage.gameObject.SetActive(true);
        new LogEventRequest().SetEventKey("getPlayerStudyLists")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     GSData xps = response.ScriptData.GetGSData("xpInfo");
                                     //Debug.Log("amount: " + (int)xps.GetInt("6Vocab"));
                                     GSData data = response.ScriptData.GetGSData("StudyListInfo_Data");
                                     List<GSData> studyListInfo = data.GetGSDataList("info");


                                     xpAmounts = response.ScriptData.GetGSData("xpInfo");
                                     //Debug.Log("test: " + (int)xpAmounts.GetGSData("6Vocab").GetInt("total"));

                                     //Debug.Log("length: " + studyListInfo.Count);
                                     //Debug.Log("list: " + studyListInfo[0].GetString("displayName"));
                                     for (int i = 0; i < 10; i++)
                                     {
                                         if (studyListInfo[i].GetString("null") == null)
                                         {
                                             listEmpty[i] = false;
                                             //Debug.Log("i " + i);
                                             titles[i] = (string)studyListInfo[i].GetString("displayName");
                                             type[i] = (int)studyListInfo[i].GetInt("type");
                                             authors[i] = (string)studyListInfo[i].GetString("creatorName");
                                             listID[i] = (string)studyListInfo[i].GetString("ID");
                                             //Debug.Log("i " + i + " name " + titles[i]);
                                         }
                                         else
                                         {
                                             listEmpty[i] = true;
                                             listID[i] = null;

                                         }
                                     }
                                 }
                                 else
                                 {
                                     Debug.Log("Error Loading Active PlayerList Data...");
                                 }
                             });
        new LogEventRequest().SetEventKey("getInactiveStudyLists")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     GSData data = response.ScriptData.GetGSData("Inactive_Info");
                                     List<GSData> studyListInfo = data.GetGSDataList("info");
                                     for (int i = 0; i < 20; i++)
                                     {
                                         if (studyListInfo[i].GetString("null") == null)
                                         {
                                             listEmpty[i + 10] = false;
                                             titles[i + 10] = (string)studyListInfo[i].GetString("displayName");
                                             type[i + 10] = (int)studyListInfo[i].GetInt("type");
                                             authors[i + 10] = (string)studyListInfo[i].GetString("creatorName");
                                             listID[i + 10] = (string)studyListInfo[i].GetString("ID");
                                         }
                                         else
                                         {
                                             listEmpty[i + 10] = true;
                                         }
                                     }
                                 }
                                 else
                                 {
                                     Debug.Log("Error Loading Inactive PlayerList Data...");
                                 }
                             });
        new LogEventRequest().SetEventKey("getInfoFromPlayerStudyList")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     Debug.Log("PopLearn Question Data received from GameSparks...");
                                     GSData data = response.ScriptData.GetGSData("studyInfo");
                                     List<GSData> studyListArr = data.GetGSDataList("info");
                                     for (int i = 0; i < 10; i++)
                                     {
                                         if (studyListArr[i].GetString("null") == null)
                                         {
                                             words[i] = studyListArr[i].GetStringList("word").ToArray();
                                             defs[i] = studyListArr[i].GetStringList("definition").ToArray();
                                             //listEmpty[i] = false;
                                             if (type[i] == 0)
                                             {
                                                 defs2[i] = studyListArr[i].GetStringList("definition2").ToArray();
                                                 misspell1[i] = studyListArr[i].GetStringList("misspell1").ToArray();
                                                 misspell2[i] = studyListArr[i].GetStringList("misspell2").ToArray();
                                                 misspell3[i] = studyListArr[i].GetStringList("misspell3").ToArray();
                                             }
                                             if (type[i] == 2)
                                             {
                                                 defs2[i] = studyListArr[i].GetStringList("definition2").ToArray();
                                             }
                                         }
                                         else
                                         {
                                             //listEmpty[i] = true;
                                         }
                                     }
                                     //destroyStudyListChildren();
                                     testLoadingArray();



                                     //studyList0[1] = studyListArr[0].GetStringList("definition").ToArray();


                                 }

                                 else
                                 {
                                     Debug.Log("Error Loading Question Data...");
                                 }
                             });


        savingImage.gameObject.SetActive(false);

    }

    private void testLoadingArray()
    {

        new LogEventRequest().SetEventKey("getScoreArray")
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     GSData data = response.ScriptData.GetGSData("ListArray_Data");
                                     //Used to be i < listID.Length but then you'd
                                     // get an error on "new int[words[i].Length];"
                                     // becaue if the list is inactive, you never fetch it's words array
                                     for (int i = 0; i < 10; i++)
                                     {
                                         if (listID[i] != null)
                                         {
                                             studyXP[i] = data.GetIntList(listID[i]).ToArray();
                                             if (studyXP[i].Length == 0)
                                             {
                                                 studyXP[i] = new int[words[i].Length];
                                                 Debug.Log("INITIALIZING VROOM VROOM");
                                                 //this used to be uncommented, but I don't see the point
                                                 //setArrayDataOnServer(listID[i], studyXP[i]);
                                             }

                                             //Debug.Log("List " + listID[i] + " length " + studyXP[i].Length);
                                         }
                                     }
                                     setStudyLists();
                                 }

                                 else
                                 {
                                     Debug.Log("Problem getting score array");
                                 }
                             });


    }

    private void setArrayDataOnServer(string idName, int[] dataToSend)
    {
        List<int> tempData = new List<int>(dataToSend);
        new LogEventRequest().SetEventKey("setScoreArray")
                             .SetEventAttribute("listName", idName)
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


    //returns an array of settings 0 QualLev 1 inGameVol 2 SFXVOL 3 xsense 4 ysense 5 autoWP 
    public int[] getSettings()
    {
        int[] settingsArr = new int[] { qualityLev, inGameVol, sfxVol, xSense, ysense, autoWP };
        return settingsArr;
    }

    public void destroyMe()
    {
        destroyed = true;
        Destroy(gameObject);
    }


    private void resetAllValues()
    {

        displayName = "Login to save your progress";

        XP = 0;
        lastXP = 0;
        XPToNext = 0;

        level = 0;

        wins = 0;
        losses = 0;
        blasts = 0;
        deaths = 0;
        profileNum = 0;
        correctQuestions = 0;
        incorrectQuestions = 0;
        shotsFired = 0;
        shotsLanded = 0;

    }

    //returns an array of arrays of strings 0 words 1 defs 2 defs2 3 misspell1 4 misspell2 5 misspell3
    public string[][] getQuestionData()
    {
        string[][] qData = new string[10][];//{ words, defs, defs2, misspell1,
                                            //misspell2, misspell3};
        return qData;
    }

    public string[][] getWords()
    {
        return words;
    }

    public string[][] getDefs()
    {
        return defs;
    }
    public string[][] getDefs2()
    {
        return defs2;
    }

    public string[][] getMisspell()
    {
        return misspell1;
    }
    public string[][] getMisspell2()
    {
        return misspell2;
    }

    public string[][] getMisspell3()
    {
        return misspell3;
    }

    public int[][] getStudyXPS()
    {
        return studyXP;
    }
    public int[] getTypes()
    {
        int[] newTypes = new int[10];
        for (int i = 0; i < newTypes.Length; i++)
        {
            newTypes[i] = type[i];
        }
        return newTypes;
    }

    public string[] getIDS()
    {
        string[] newIDs = new string[10];
        for (int i = 0; i < newIDs.Length; i++)
        {
            newIDs[i] = listID[i];
        }
        return newIDs;
    }
    public int[] getPopLearnXP()
    {
        int[] XPs = new int[10];
        for (int i = 0; i < 10; i++)
        {
            if (!listEmpty[i])
            {
                XPs[i] = (int)xpAmounts.GetGSData(listID[i]).GetInt("total");
            }
        }
        return XPs;
    }

    public bool[] getListEmptys()
    {
        bool[] listReturn = new bool[10];
        for (int i = 0; i < 10; i++)
        {
            listReturn[i] = listEmpty[i];
        }
        return listReturn;
    }

    public void searchForUser()
    {
        string usrnm = searchUsername.text;
        if (usrnm.Equals(""))
        {
            usrnm = "null";
        }

        string dispnm = searchDisplayName.text;
        if (dispnm.Equals(""))
        {
            dispnm = "null";
        }

        string schnm = searchSchool.text;
        if (schnm.Equals(""))
        {
            schnm = "null";
        }

        string mail = searchEmail.text;
        if (mail.Equals(""))
        {
            mail = "null";
        }
        new LogEventRequest().SetEventKey("findPlayers")
                             .SetEventAttribute("school", schnm)
                             .SetEventAttribute("username", usrnm)
                             .SetEventAttribute("displayName", dispnm)
                             .SetEventAttribute("email", mail)
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     List<GSData> data = response.ScriptData.GetGSDataList("playerList");
                                     int dataLength = data.Count;
                                     int newSize;
                                     newSize = dataLength * 130;

                                     if (newSize < 300)
                                     {
                                         newSize = 300;
                                     }

                                     scrollViewObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)newSize);

                                     for (int i = 0; i < dataLength; i++)
                                     {
                                         //Debug.Log(data[0].GetString("school"));
                                         GameObject tempObj = Instantiate(friendOnListObj);
                                         tempObj.transform.SetParent(scrollViewObj.transform, false);
                                         foundPlayer fp = tempObj.GetComponent<foundPlayer>();
                                         fp.setDisplayName(data[i].GetString("displayName"));
                                         fp.setUserName(data[i].GetString("username"));
                                         fp.setSchool(data[i].GetString("school"));
                                         fp.setPic(profImages[(int)data[i].GetInt("profNum")]);
                                         fp.setIdNum(data[i].GetString("Id"));



                                     }

                                 }
                                 else
                                 {
                                     Debug.Log("Some problem with loading data");
                                 }
                             });
    }

    public void findStudyLists(string searchNam)
    {
        destroyAddStudyListChildren();
        new LogEventRequest().SetEventKey("findStudyList")
                             .SetEventAttribute("name", searchNam)
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     List<GSData> data = response.ScriptData.GetGSDataList("questionList");
                                     int dataLength = data.Count;

                                     int newSize;
                                     newSize = dataLength * 110;

                                     if (newSize < 300)
                                     {
                                         newSize = 300;
                                     }
                                     addStudyListContentHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)newSize);

                                     for (int i = 0; i < dataLength; i++)
                                     {
                                         Debug.Log(data[i].GetString("displayName"));
                                         Debug.Log(data[i].GetString("creatorName"));
                                         GameObject tempObj = Instantiate(addStudyListObj);
                                         tempObj.transform.SetParent(addStudyListContentHolder.transform, false);
                                         studySetToBeAdded studySet = tempObj.GetComponent<studySetToBeAdded>();
                                         studySet.setInfo(data[i].GetString("displayName"), data[i].GetString("creatorName"), data[i].GetString("ID"));
                                     }
                                 }
                                 else
                                 {
                                     Debug.Log("problem finding list!");
                                 }
                             });
    }

    public void addStudyList(string listID)
    {
        new LogEventRequest().SetEventKey("addPlayerStudyList")
                             .SetEventAttribute("name", listID)
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     loadQuestionData();
                                 }

                                 else
                                 {
                                     Debug.Log("Problem adding list");
                                 }
                             });
    }

    public void destroyChildren()
    {
        for (int i = scrollViewObj.transform.childCount - 1; i >= 0; i--)
        {
            //Debug.Log("child " + scrollViewObj.transform.GetChild(0).name);
            Destroy(scrollViewObj.transform.GetChild(i).gameObject);
        }
        searchEmail.text = "";
        searchSchool.text = "";
        searchUsername.text = "";
        searchDisplayName.text = "";
    }
    public void destroyStudyListChildren()
    {
        for (int i = studyListHolder.transform.childCount - 1; i >= 0; i--)
        {
            //Debug.Log("child " + scrollViewObj.transform.GetChild(0).name);
            Destroy(studyListHolder.transform.GetChild(i).gameObject);
        }
    }

    public void destroyAddStudyListChildren()
    {
        for (int i = addStudyListContentHolder.transform.childCount - 1; i >= 0; i--)
        {
            //Debug.Log("child " + scrollViewObj.transform.GetChild(0).name);
            Destroy(addStudyListContentHolder.transform.GetChild(i).gameObject);
        }
    }

    public void make2(int num)
    {
        Debug.Log("called make 2 with " + num + " elements");
        int newSize;
        newSize = num * 130;
        if (newSize < 300)
        {
            newSize = 300;
        }

        scrollViewObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (float)newSize);

        for (int i = 0; i < num; i++)
        {
            GameObject tempObj = Instantiate(friendOnListObj);
            tempObj.transform.SetParent(scrollViewObj.transform, false);
        }

    }

    public void setStudyLists()
    {
        destroyStudyListChildren();
        int num = 0;
        for (int i = 0; i < 10; i++)
        {
            if (!listEmpty[i])
            {
                num++;
                //                Debug.Log("i: " + i);
                //                Debug.Log("result: " + (int)xpAmounts.GetInt(listID[i]));
                //float progress = 0f;
                //num++;
                GameObject temp;
                temp = Instantiate(studyListPrefab);
                temp.transform.SetParent(studyListHolder.transform);

                int progress = (int)xpAmounts.GetGSData(listID[i]).GetInt("total");
                //temp.transform.position = Vector3.zero;
                if (type[i] == 0)
                {

                    temp.GetComponent<studyListSetter>().setInfo(titles[i],
                                                                 authors[i],
                                                                 (progress / 100f), true, i);
                }
                else
                {
                    temp.GetComponent<studyListSetter>().setInfo(titles[i],
                                                                 authors[i],
                                                                 (progress / 100f), true, i);
                }
                temp.transform.localScale = new Vector3(1, 1, 1);
                temp.GetComponent<Button>().onClick
                    .AddListener(() => showWordDef(temp.GetComponent<studyListSetter>().getButtonNum()));
            }
        }
        for (int i = 0; i < 20; i++)
        {
            if (!listEmpty[i + 10])
            {
                //                Debug.Log("i is: " + i);
                num++;
                GameObject temp;
                temp = Instantiate(studyListPrefab);
                temp.transform.SetParent(studyListHolder.transform);
                //temp.transform.position = Vector3.zero;
                if (type[i + 10] == 0)
                {
                    temp.GetComponent<studyListSetter>().setInfo(titles[i + 10],
                                                                 authors[i + 10],

                                                                 (((int)xpAmounts.GetGSData(listID[i + 10]).GetInt("total")) / 200f), false, i + 10);
                }
                else
                {
                    temp.GetComponent<studyListSetter>().setInfo(titles[i + 10],
                                                                 authors[i + 10],
                                                                 (((int)xpAmounts.GetGSData(listID[i + 10]).GetInt("total")) / 200f), false, i + 10);
                }
                temp.transform.localScale = new Vector3(1, 1, 1);
                temp.GetComponent<Button>().onClick
                    .AddListener(() => showWordDef(temp.GetComponent<studyListSetter>().getButtonNum()));
            }
        }
        studyListHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (80f * num));
    }

    public void showWordDef(int i)
    {
        savingImage.gameObject.SetActive(true);
        studyListMenu.SetActive(false);
        wordDefMenu.SetActive(true);
        currentSL = i;
        questionTitle.text = titles[i];

        if (i < 10)
        {
            activeObject.GetComponentInChildren<Image>().sprite = greenSprite;
            activeObject.GetComponentInChildren<TextMeshProUGUI>().text = "Active";
            wordDefHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (140f * words[i].Length));
            // wordDefHolder.GetComponent<RectTransform>().position = new Vector3(25f, 140f * words[i].Length * -1f, 0f);
            for (int j = 0; j < words[i].Length; j++)
            {
                //if(words[i][j] == null){
                //    break;
                //}
                GameObject temp = Instantiate(wordDefPrefab);
                temp.transform.SetParent(wordDefHolder.transform);
                temp.transform.localScale = new Vector3(1, 1, 1);
                temp.GetComponent<wordDefSetter>().setWordDef(words[i][j], defs[i][j]);
            }
            savingImage.gameObject.SetActive(false);

        }
        else
        {
            new LogEventRequest().SetEventKey("getQuestionData")
                                 .SetEventAttribute("gradeType", listID[i])
                             .Send((response) =>
                             {
                                 if (!response.HasErrors)
                                 {
                                     activeObject.GetComponentInChildren<Image>().sprite = blueSprite;
                                     activeObject.GetComponentInChildren<TextMeshProUGUI>().text = "Inactive";
                                     GSData data = response.ScriptData.GetGSData("datasheet");
                                     GSData studyListInfo = data.GetGSData("data");

                                     string[] curWords = studyListInfo.GetStringList("word").ToArray();
                                     string[] curDefs = studyListInfo.GetStringList("definition").ToArray();
                                     wordDefHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (140f * curWords.Length));
                                     for (int j = 0; j < curWords.Length; j++)
                                     {
                                         //if(words[i][j] == null){
                                         //    break;
                                         //}
                                         GameObject temp = Instantiate(wordDefPrefab);
                                         temp.transform.SetParent(wordDefHolder.transform);
                                         temp.transform.localScale = new Vector3(1, 1, 1);
                                         temp.GetComponent<wordDefSetter>().setWordDef(curWords[j], curDefs[j]);
                                     }
                                     //Debug.Log("length: " + studyListInfo.Count);
                                     //Debug.Log("list: " + studyListInfo[0].GetString("displayName"));
                                     savingImage.gameObject.SetActive(false);

                                 }
                                 else
                                 {
                                     Debug.Log("Error Loading Active PlayerList Data...");
                                 }
                             });

        }
    }

    public void destroyWordDefChildren()
    {
        for (int i = wordDefHolder.transform.childCount - 1; i >= 0; i--)
        {
            //Debug.Log("child " + scrollViewObj.transform.GetChild(0).name);
            Destroy(wordDefHolder.transform.GetChild(i).gameObject);
        }
    }

    public void toggleActive()
    {
        //Debug.Log(listID[currentSL]);
        if (currentSL < 10)
        {
            new LogEventRequest().SetEventKey("makeActiveInactive")
                                 .SetEventAttribute("name", listID[currentSL])
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         activeObject.GetComponentInChildren<Image>().sprite = blueSprite;
                                         activeObject.GetComponentInChildren<TextMeshProUGUI>().text = "Inactive";
                                         loadQuestionData();
                                         //destroyStudyListChildren();
                                         setStudyLists();

                                     }
                                     else
                                     {
                                         setInactiveError.SetActive(true);
                                     }
                                 });
        }
        else
        {
            Debug.Log("list name: " + listID[currentSL]);
            new LogEventRequest().SetEventKey("makeInactiveActive")
                                 .SetEventAttribute("name", listID[currentSL])
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         activeObject.GetComponentInChildren<Image>().sprite = greenSprite;
                                         activeObject.GetComponentInChildren<TextMeshProUGUI>().text = "Active";
                                         loadQuestionData();
                                         //destroyStudyListChildren();
                                         setStudyLists();

                                     }
                                     else
                                     {
                                         setInactiveError.SetActive(true);
                                     }
                                 });
        }

    }

    public void removeStudySet()
    {
        savingImage.SetActive(true);
        new LogEventRequest().SetEventKey("removePlayerStudyList")
                                 .SetEventAttribute("name", listID[currentSL])
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         savingImage.SetActive(false);
                                         loadQuestionData();
                                         setStudyLists();

                                     }
                                     else
                                     {
                                         savingImage.SetActive(false);
                                         setInactiveError.SetActive(true);
                                     }
                                 });
    }

    public void sendFriendRequest(string playerIDNO)
    {
        new LogEventRequest().SetEventKey("friendRequest")
                                 .SetEventAttribute("player_id", playerIDNO)
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         Debug.Log("sent request!");

                                     }
                                     else
                                     {
                                         Debug.Log("problem sending request");
                                     }
                                 });
    }

    public void getPlayerFriends()
    {
        new LogEventRequest().SetEventKey("getPlayerFriends")
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         GSData data = response.ScriptData.GetGSData("friendsList");
                                         menuManager.setfriendData(data);
                                         Debug.Log("Recieved Player Friends");

                                     }
                                     else
                                     {
                                         Debug.Log("problem recieving friends");
                                     }
                                 });
    }

    public void acceptRequest(string playerID)
    {
        new LogEventRequest().SetEventKey("acceptFriendRequest")
            .SetEventAttribute("userId", playerID)
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         Debug.Log("Successfully accepted request");
                                         getPlayerFriends();
                                     }
                                     else
                                     {
                                         Debug.Log("Unable to accept request");
                                     }



                                 });
    }

    public void denyRequest(string playerID)
    {
        new LogEventRequest().SetEventKey("declineFriendRequest")
            .SetEventAttribute("userId", playerID)
                                 .Send((response) =>
                                 {
                                     if (!response.HasErrors)
                                     {
                                         Debug.Log("Successfully declined request");
                                         getPlayerFriends();
                                     }
                                     else
                                     {
                                         Debug.Log("Unable to decline request");
                                     }



                                 });
    }
}
