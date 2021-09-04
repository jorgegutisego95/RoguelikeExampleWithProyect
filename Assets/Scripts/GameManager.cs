using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    public BoardManager boardScript;
    public int playerFoodPoints = 100;
    [HideInInspector] public bool playersTurn = true;
    public float turnDelay = .1f;
    public float levelStartDelay = 2f;

    private int level = 0;
    private List<Enemy> enemies;
    private bool enemiesMoving;
    private Text levelText;
    private GameObject levelImage;
    private bool doingSetup;
    private float dbPeak = 0f;
    private float dbAvg = 0f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);

        enemies = new List<Enemy>();

        boardScript = GetComponent<BoardManager>();
    }

    void InitGame()
    {
        float dbPeakDif = 0f;
        float dbAvgDif = 0f;
        bool nota = false;

        doingSetup = true;
        levelImage = GameObject.Find("LevelImage");
        levelText = GameObject.Find("LevelText").GetComponent<Text>();
        //		levelText.text = "Day " + level;
        levelText.text = "Day " + NumberToWords(level);
        levelImage.SetActive(true);

        Invoke("HideLevelImage", levelStartDelay);

        enemies.Clear();

        if (level > 1)
        {
            Dictionary<string, float> paramsExtracted = MusicManager.instance.GetMusicData();

            if (paramsExtracted.Any())
            {
                foreach (KeyValuePair<string, float> kvp in paramsExtracted)
                {
                    if (kvp.Key.Equals("Pico de dB:"))
                    {
                        if (dbPeak < kvp.Value)
                        {
                            Debug.Log("Pico aumentado " + kvp.Value);
                            dbPeak = kvp.Value;
                            dbPeakDif = 1f;
                        }
                        else if (dbPeak > kvp.Value)
                        {
                            Debug.Log("Pico disminuido " + kvp.Value);
                            dbPeakDif = dbPeak - kvp.Value;
                            dbPeak = kvp.Value;
                        }
                        else
                        {
                            Debug.Log("Pico mantenido.");
                        }
                    }
                    else if (kvp.Key.Equals("Media aritmética de dB:"))
                    {
                        if (dbAvg < kvp.Value)
                        {
                            Debug.Log("Media Aumentada: " + kvp.Value);
                            dbAvg = kvp.Value;
                            dbAvgDif = 1f;
                        }
                        else if(dbAvg > kvp.Value)
                        {
                            Debug.Log("Media disminuida: " + kvp.Value);
                            dbAvgDif = -1f;
                            dbAvg = kvp.Value;
                            boardScript.SetupScene(level);
                        }
                        else
                        {
                            Debug.Log("Media mantenido.");
                        }
                    }
                    else
                    {
                        if(kvp.Key.Equals("sol3"))
                        {
                            Debug.Log("La nota más común es un sol3: " + kvp.Key);
                            nota = true;
                        }
                    }
                }

                if (nota)
                {
                    //Han aumentado el pico y la media.
                    boardScript.SetupSceneModifyingFoodAndEnemies(level, 5, 5);
                }
                else if (dbPeakDif < 0 && dbAvgDif == 0)
                {
                    //El pico ha disminuido
                    boardScript.SetupSceneModifyingEnemies(level, 4);
                }
                else if (dbPeakDif > 0 && dbAvgDif == 0)
                {
                    //El pico ha aumentado
                    boardScript.SetupSceneModifyingEnemies(level, 0);
                }
                else if (dbPeakDif == 0 && dbAvgDif > 0)
                {
                    //La media ha aumentado
                    boardScript.SetupSceneModifyingFood(level, 4);
                }
                else if (dbPeakDif == 0 && dbAvgDif < 0)
                {
                    //La media ha disminuido
                    boardScript.SetupSceneModifyingFood(level, 0);
                }
                else if (dbPeakDif > 0 && dbAvgDif > 0)
                {
                    //El pico ha aumentado y la media también
                    boardScript.SetupSceneModifyingFoodAndEnemies(level, 4, 0);
                }
                else if (dbPeakDif < 0 && dbAvgDif < 0)
                {
                    //El pico ha disminuido y la media también
                    boardScript.SetupSceneModifyingFoodAndEnemies(level, 0, 4);
                }
                else if (dbPeakDif > 0 && dbAvgDif < 0)
                {
                    //El pico ha aumentado y la media ha disminuido
                    boardScript.SetupSceneModifyingFoodAndEnemies(level, 0, 0);
                }
                else if (dbPeakDif < 0 && dbAvgDif > 0)
                {
                    //El pico ha disminuido y la media ha aumentado
                    boardScript.SetupSceneModifyingFoodAndEnemies(level, 4, 4);
                }
                else
                {
                    boardScript.SetupScene(level);
                }

                MusicManager.instance.CleanData();
            }
            else
            {
                boardScript.SetupScene(level);
            }
        }
        else
        {
            boardScript.SetupScene(level);
        }


    }

    private void HideLevelImage()
    {
        levelImage.SetActive(false);
        doingSetup = false;
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        level++;
        InitGame();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void Update()
    {
        if (playersTurn || enemiesMoving || doingSetup)
        {
            return;
        }

        StartCoroutine(MoveEnemies());
    }

    public void AddEnemyToList(Enemy script)
    {
        enemies.Add(script);
    }

    public void GameOver()
    {
        //		levelText.text = "After " + level + " days, you starved.";
        levelText.text = "After " + NumberToWords(level).ToLower() + " " + ((level <= 1) ? "day" : "days") + " you starved.";
        levelImage.SetActive(true);

        enabled = false;
    }

    IEnumerator MoveEnemies()
    {
        enemiesMoving = true;

        yield return new WaitForSeconds(turnDelay);

        if (enemies.Count == 0)
        {
            yield return new WaitForSeconds(turnDelay);
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].MoveEnemy();

            yield return new WaitForSeconds(enemies[i].moveTime);
        }

        enemiesMoving = false;

        playersTurn = true;
    }

    private static string NumberToWords(int number)
    {
        if (number == 0)
            return "Zero";

        if (number < 0)
            return "Minus " + NumberToWords(Math.Abs(number));

        string words = "";

        if ((number / 1000000) > 0)
        {
            words += NumberToWords(number / 1000000) + " Million ";
            number %= 1000000;
        }

        if ((number / 1000) > 0)
        {
            words += NumberToWords(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += NumberToWords(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "")
                words += "and ";

            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += "-" + unitsMap[number % 10];
            }
        }

        return words;
    }
}