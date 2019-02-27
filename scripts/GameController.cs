using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

/// <summary>
/// The component that controls in game functionality such as question handling etc.
/// </summary>
public class GameController : MonoBehaviour
{

    //The different grade levels imported from the site

    private int answerIndex;

    private int chosenIndex;
    //the word and definition pairs
    //private string[] phrases;
    //an array of booleans to keep track of the number of questions asked
    private bool[][] donePhrases = new bool[10][];

    //The answer to the question pair
    private string questionAnswer;

    //the parent gameObject holding the question texts
    public GameObject imageHolder;

    //The Image objects for the answers and questions
    [SerializeField]
    private TextMeshProUGUI questionText;
    [SerializeField]
    private TextMeshProUGUI answer1;
    [SerializeField]
    private TextMeshProUGUI answer2;
    [SerializeField]
    private TextMeshProUGUI answer3;
    [SerializeField]
    private TextMeshProUGUI answer4;

    //the color changing buttons that function as answers
    public Button[] buttons;

    //the file being read from
    //private TextAsset csvFile; // Reference of CSV file

    private char lineSeperater = '\n'; // It defines line seperate character
    private char fieldSeperator = ','; // It defines field seperate chracter

    //a link to the current user
    public playerController pc;

    //The Photon view of the player
    private PhotonView pView;

    //are we answering a gold blaster question
    private bool goldBlasterQuestion;


    [SerializeField]
    private GameObject pauseMenu;
    [SerializeField]
    private GameObject settingsMenu;
    [SerializeField]
    private Text[] usernames;
    [SerializeField]
    private Text[] blasts;
    [SerializeField]
    private Text[] deaths;
    [SerializeField]
    private Image[] usernameHolder;

    private int userSlot;

    [SerializeField]
    private Text[] ratios;


    [SerializeField]
    private Text[] endUsernames;
    [SerializeField]
    private Text[] endBlasts;
    [SerializeField]
    private Text[] endDeaths;
    [SerializeField]
    private Image[] endUsernameHolder;

    //private int userSlot;

    [SerializeField]
    private Text[] endRatios;

    /* Game mode
	 * 1 Math
	 * 2 Vocab
	 * 3 Spelling
	 * 4 Math Vocab
	 * 5 Math Spelling
	 * 6 Spelling Vocab
	 * 7 Math Vocab Spelling
	 */
    private int gameMode;
    //are you currently answering a question?
    public bool questionMode;
    public bool paused;

    private bool gameOver;

    private StudentController sc;

    private GameSparksHandler gameSparksHandler;





    private int[] settings;

    private string[][] words = new string[10][];
    private string[][] defs = new string[10][];
    private string[][] defs2 = new string[10][];
    private string[][] misspell1 = new string[10][];
    private string[][] misspell2 = new string[10][];
    private string[][] misspell3 = new string[10][];
    private int[][] studyXP = new int[10][];

    private int[] correctAnswers = new int[10];
    private int[] incorrectAnswers = new int[10];

    private string[] listID = new string[10];
    private int[] type = new int[10];

    private bool[] listEmpty = new bool[10];

    private List<int> indexes = new List<int>();

    private int numSets;
    private int questionIndex;

    private int correctStreak;

    // Use this for initialization
    void Start()
    {
        pView = pc.GetComponent<PhotonView>();
        if (!pView.IsMine)
        {
            return;
        }
        correctStreak = 0;
        gameOver = true;
        questionMode = false;
        Cursor.visible = false;
        paused = false;

        imageHolder.SetActive(false);
        pauseMenu.SetActive(false);

        gameSparksHandler = GameObject.FindWithTag("gsh").GetComponent<GameSparksHandler>();
        words = gameSparksHandler.getWords();
        defs = gameSparksHandler.getDefs();
        defs2 = gameSparksHandler.getDefs2();
        misspell1 = gameSparksHandler.getMisspell();
        misspell2 = gameSparksHandler.getMisspell2();
        misspell3 = gameSparksHandler.getMisspell3();
        listID = gameSparksHandler.getIDS();
        type = gameSparksHandler.getTypes();
        listEmpty = gameSparksHandler.getListEmptys();
        studyXP = gameSparksHandler.getStudyXPS();



        settings = gameSparksHandler.getSettings();
      
        numSets = 0;
        for (int i = 0; i < listID.Length; i++)
        {
//            Debug.Log("List Empty " + i + " val " + listEmpty[i]);
            if (!listEmpty[i])
            {
                indexes.Add(i);
                Debug.Log("id: " + listID[i]);
                numSets++;
            }
        }
        for (int i = 0; i < 10; i++)
        {
            if (!listEmpty[i])
            {
                donePhrases[i] = new bool[words[i].Length];
            }
        }
        //numSets = words.Length;
        //Debug.Log("sets: " + numSets);
    }

    // Update is called once per frame
    void Update()
    {
        if (!pView.IsMine)
        {
            return;
        }

        if (!gameOver)
        {
            //The q key initializes  a question
            if (Input.GetKey("q") && !questionMode && !paused)
            {
                goldBlasterQuestion = false;
                questionMode = true;
                Cursor.visible = true;
                questionIndex = indexes[Random.Range(0, indexes.Count)];
                setTexts(query());

            }
            if (paused)
            {
                if (Input.GetKeyUp("p"))
                {
                    Debug.Log("pressed p");
                    usernameHolder[userSlot].color = Color.white;
                    questionMode = false;
                    Cursor.visible = false;
                    paused = false;
                    settingsMenu.GetComponent<settingSetter>().setGSHSettings();
                    pauseMenu.SetActive(false);
                }
            }
            else if (Input.GetKeyUp("p") && !questionMode)
            {
                questionMode = true;
                Cursor.visible = true;
                paused = true;
                ScoreOrder[] gameScores = pc.getOrderAndPlaces();
                for (int i = 0; i < gameScores.Length; i++)
                {
                    if (gameScores[i].getPlayerID() == pView.ViewID)
                    {
                        //Debug.Log("issa match");
                        userSlot = i;
                        usernameHolder[userSlot].color = Color.yellow;
                    }
                    usernames[i].text = gameScores[i].getName();
                    blasts[i].text = "" + gameScores[i].getScore();
                    deaths[i].text = "" + gameScores[i].getDeaths();
                    ratios[i].text = "" + gameScores[i].getBDRatio().ToString("F1");
                    //Debug.Log(i + 1 + " " + gameScores[i].getName() + " " +
                    //gameScores[i].getBDRatio() + " ratio " +
                    //gameScores[i].getScore() + " kills "
                    //+ gameScores[i].getDeaths() + " deaths");
                }
                pauseMenu.SetActive(true);
            }
        }
    }


    public void upperLevelQuestion()
    {
        questionMode = true;
        Cursor.visible = true;
        goldBlasterQuestion = true;
        setTexts(query());





    }

    //Returns an array in the following order [0] Question, [1] answer, [2] answer, [3] answer [4] answer randomly chosen
    public string[] query()
    {
        //the answer to be returned in the given format
        string[] answerReturn = new string[5];

        //Gets 3 random indexes, tries to find the lowest score and ask a question you're bad at
        //With preference given to index 1 (if equal)
        int index1 = Random.Range(0, words[questionIndex].Length);
        int index2 = Random.Range(0, words[questionIndex].Length);
        int index3 = Random.Range(0, words[questionIndex].Length);

        int num1 = studyXP[questionIndex][index1];
        int num2 = studyXP[questionIndex][index2];
        int num3 = studyXP[questionIndex][index3];

        //1 < 2 && 1 < 3
        if(num1 <= num2 && num1 <= num3){
            chosenIndex = index1;
        }
        else if(num2 <= num3){
            chosenIndex = index2;
        }
        else{
            chosenIndex = index3;
        }


        questionAnswer = words[questionIndex][chosenIndex];
        if (type[questionIndex] == 2)
        {
            int defChooser = Random.Range(0, 2);
            if(defChooser == 0){
                answerReturn[0] = defs[questionIndex][chosenIndex];
            }
            else{
                //Debug.Log("2 def " + words[questionIndex][chosenIndex] + " snswer " +
                          //defs2[questionIndex][chosenIndex] + " qi "
                          //+ questionIndex + " ci " +chosenIndex);
                answerReturn[0] = defs2[questionIndex][chosenIndex];
            }
        }
        else
        {
            answerReturn[0] = defs[questionIndex][chosenIndex];
        }
        //gets the random index for the answer
        int rand = Random.Range(1, 5);
        answerIndex = rand;
        //the random words to go with the definition
        int rand2 = Random.Range(0, words[questionIndex].Length);
        //the actual word being used
        string item;
        //this part attempts to assign a random word to each index, one of which will be overwritten by the actual word
        for (int i = 1; i <= 4; i++)
        {
            bool retry = true;
            int newCount = 0;
            while (retry && newCount < 20)
            {
                //Debug.Log("rand " + rand2 + "p " + p);
                rand2 = Random.Range(0, words[questionIndex].Length);
                while (rand2 == chosenIndex)
                {
                    rand2 = Random.Range(0, words[questionIndex].Length);
                }
                item = words[questionIndex][rand2];
                if (System.Array.IndexOf(answerReturn, item) == -1)
                {
                    answerReturn[i] = item;
                    retry = false;
                    break;
                }
                newCount++;
            }
            //Debug.Log("newCount: " + newCount);
        }
        answerReturn[answerIndex] = questionAnswer;
        //Debug.Log ("p = " + p + " " + answerReturn [0] + " ans1: " + answerReturn [1] + " ans2: " + answerReturn [2] + " ans3: " + answerReturn [3] + " ans4: " + answerReturn [4]);
        return answerReturn;
    }
    public void setGameOver(bool over)
    {
        gameOver = false;
    }
    private void setTexts(string[] ansArr)
    {
        pc.goIdle();
        imageHolder.SetActive(true);
        questionText.text = ansArr[0];
        answer1.text = ansArr[1];
        answer2.text = ansArr[2];
        answer3.text = ansArr[3];
        answer4.text = ansArr[4];

    }

    public void endQuestion(bool correct)
    {
        if (correct)
        {
            correctStreak++;
            correctAnswers[questionIndex]++;
            imageHolder.SetActive(false);
            //questionMode = false;
            Cursor.visible = false;

            pc.addWaterPack(goldBlasterQuestion);

            int curXP = studyXP[questionIndex][chosenIndex];

            if(curXP < 3){
                curXP += 5;
                if (correctStreak > 3)
                {
                    curXP++;
                }
            }
            else if(curXP < 5){
                curXP += 3;
                if (correctStreak > 3)
                {
                    curXP++;
                }
            }
            else if(curXP < 6){
                curXP += 2;
                if (correctStreak > 3)
                {
                    curXP++;
                }
            }
            else{
                if (correctStreak > 3)
                {
                    if (correctStreak < 15)
                    {
                        curXP += correctStreak % 3;
                    }
                    else
                    {
                        curXP += 4;
                    }
                }
                else
                {
                    curXP++;
                }
            }

            if(curXP > 10){
                curXP = 10;
            }

            studyXP[questionIndex][chosenIndex] = curXP;

            StartCoroutine(endCorrectQuestion());
        }
        else
        {
            correctStreak = 0;
            int curXP = studyXP[questionIndex][chosenIndex];

            if (curXP > -1)
            {
                curXP -= 5;
            }
            else if (curXP > -5)
            {
                curXP = -5;
            }
            else
            {
                curXP--;
            }

            if (curXP < -10)
            {
                curXP = -10;
            }

            studyXP[questionIndex][chosenIndex] = curXP;

            incorrectAnswers[questionIndex]++;
            StartCoroutine(showCorrectAnswer());
        }
    }

    //returns whether the user is currently answering a question
    public bool getQuestionMode()
    {
        return questionMode;
    }


    public int getCorrectAnswer()
    {
        return answerIndex;
    }

    //This function exists to make a slight pause at the end of answering a question so that blasters dont fire
    private IEnumerator endCorrectQuestion()
    {
        yield return new WaitForSeconds(.1f);
        questionMode = false;

    }

    private IEnumerator showCorrectAnswer()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
            ColorBlock newColors = buttons[i].colors;
            if (i == answerIndex - 1)
            {
                newColors.disabledColor = new Color(0, 255, 0);
                buttons[answerIndex - 1].colors = newColors;
            }
            else
            {
                newColors.disabledColor = new Color(255, 0, 0);
                buttons[i].colors = newColors;
            }
        }

        yield return new WaitForSeconds(2.5f);
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = true;
        }
        imageHolder.SetActive(false);
        questionMode = false;
        Cursor.visible = false;
    }

    public void endGame()
    {
        gameOver = true;
        Cursor.visible = true;

        ScoreOrder[] gameScores = pc.getOrderAndPlaces();

        for (int i = 0; i < gameScores.Length; i++)
        {
            if (gameScores[i].getPlayerID() == pView.ViewID)
            {
                //Debug.Log("issa match");
                userSlot = i;
                endUsernameHolder[userSlot].color = Color.yellow;
            }
            endUsernames[i].text = gameScores[i].getName();
            endBlasts[i].text = "" + gameScores[i].getScore();
            endDeaths[i].text = "" + gameScores[i].getDeaths();
            endRatios[i].text = "" + gameScores[i].getBDRatio().ToString("F1");
        }


    }

    public int[] getCorrectAnswers(){
        return correctAnswers;
    }

    public int[] getIncorrectAnswers()
    {
        return incorrectAnswers;
    }

    public int[][] getStudyXP(){
        return studyXP;
    }

}
