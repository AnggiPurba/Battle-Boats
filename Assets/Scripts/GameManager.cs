using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Define game states for clear logic flow
    public enum GameState
    {
        MainMenu,
        Placement,
        InGame,
        Paused,
        GameOver
    }

    public GameState currentGameState;

    //–– LEVEL MANAGEMENT ––
    public static int currentLevel = 1;
    private static bool isInitialLoad = true;
    private const int BASE_SHIP_COUNT = 5;
    private const int TURN_TIME = 5;

    [Header("Ships")]
    public GameObject[] ships;
    public EnemyScript enemyScript;
    private ShipScript shipScript;
    private List<int[]> enemyShips;
    private int shipIndex = 0;
    public List<TileScript> allTileScripts;

    [Header("HUD")]
    public Button nextBtn;
    public Button rotateBtn;
    public Button replayBtn;
    public Text topText;
    public Text playerShipText;
    public Text enemyShipText;
    public Text TimerText;
    public Text TimerNumber;
    public Button Lvl2Btn;
    public Button Lvl3Btn;
    public Button ExitBtn;
    public Button ExitfromUIStartBtn;
    public Button StartfromUIStartBtn;
    public Text Title;
    public Text PlayerText;
    public Text EnemyText;
    public Text PauseResumeText;
    public Button PauseResumeBtn;
    public Text SpeedText;
    public Button SpeedBtn;
    public Text SpeedTextBtn;

    [Header("Objects")]
    public GameObject missilePrefab;
    public GameObject enemyMissilePrefab;
    public GameObject firePrefab;
    public GameObject woodDock;

    private bool setupComplete = false;
    private bool playerTurn = true;
    private Coroutine turnTimerCoroutine;
    private bool hasPausedThisLevel = false;

    private List<GameObject> playerFires = new List<GameObject>();
    private List<GameObject> enemyFires = new List<GameObject>();

    private int enemyShipCount;
    private int playerShipCount;

    private float normalMissileSpawnY = 15f;
    private float fastMissileSpawnY = 3f;
    private bool isFastMode = false;

    void Awake()
    {
        currentGameState = GameState.MainMenu;
    }

    void Start()
    {
        StartfromUIStartBtn.onClick.AddListener(StartGameClicked);
        ExitfromUIStartBtn.onClick.AddListener(ExitGameClicked);
        nextBtn.onClick.AddListener(NextShipClicked);
        rotateBtn.onClick.AddListener(RotateClicked);
        replayBtn.onClick.AddListener(ReplayClicked);
        Lvl2Btn.onClick.AddListener(() => StartLevel(2));
        Lvl3Btn.onClick.AddListener(() => StartLevel(3));
        ExitBtn.onClick.AddListener(ExitGameClicked);
        PauseResumeBtn.onClick.AddListener(TogglePauseResume);
        SpeedBtn.onClick.AddListener(ToggleSpeedMode);

        if (isInitialLoad)
        {
            SetUIForMainMenu();
            isInitialLoad = false;
        }
        else
        {
            PreparePlacementPhase();
        }
    }

    void SetUIForMainMenu()
    {
        Title.gameObject.SetActive(true);
        StartfromUIStartBtn.gameObject.SetActive(true);
        ExitfromUIStartBtn.gameObject.SetActive(true);

        nextBtn.gameObject.SetActive(false);
        // ... (bagian lain dari UI yang disembunyikan di MainMenu, tidak berubah)
        rotateBtn.gameObject.SetActive(false);
        replayBtn.gameObject.SetActive(false);
        Lvl2Btn.gameObject.SetActive(false);
        Lvl3Btn.gameObject.SetActive(false);
        topText.gameObject.SetActive(false);
        playerShipText.gameObject.SetActive(false);
        enemyShipText.gameObject.SetActive(false);
        TimerText.gameObject.SetActive(false);
        TimerNumber.gameObject.SetActive(false);
        ExitBtn.gameObject.SetActive(false);
        PlayerText.gameObject.SetActive(false);
        EnemyText.gameObject.SetActive(false);
        woodDock.SetActive(false);
        PauseResumeBtn.gameObject.SetActive(false);
        PauseResumeText.gameObject.SetActive(false);
        SpeedBtn.gameObject.SetActive(false);
        if (SpeedText != null) SpeedText.gameObject.SetActive(false);
        SpeedTextBtn.gameObject.SetActive(false);

        foreach (var s in ships) s.SetActive(false);
        SetTilesInteractable(false);
        currentGameState = GameState.MainMenu;
    }

    void StartGameClicked()
    {
        currentLevel = 1;
        PreparePlacementPhase();
    }

    void StartLevel(int level)
    {
        currentLevel = level;
        isInitialLoad = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void PreparePlacementPhase()
    {
        currentGameState = GameState.Placement;
        Time.timeScale = 1f;

        shipIndex = 0;
        if (enemyScript != null)
        {
            enemyShips = enemyScript.PlaceEnemyShips();
        }
        else
        {
            Debug.LogError("EnemyScript is not assigned in GameManager!");
            enemyShips = new List<int[]>();
        }
        enemyShipCount = BASE_SHIP_COUNT;
        playerShipCount = BASE_SHIP_COUNT;
        playerShipText.text = playerShipCount.ToString();
        enemyShipText.text = enemyShipCount.ToString();
        hasPausedThisLevel = false;
        isFastMode = false;
        SpeedTextBtn.text = "Normal";

        Title.gameObject.SetActive(false);
        StartfromUIStartBtn.gameObject.SetActive(false);
        ExitfromUIStartBtn.gameObject.SetActive(false);

        // Hide replay and level progression buttons during placement/gameplay
        replayBtn.gameObject.SetActive(false);
        Lvl2Btn.gameObject.SetActive(false);
        Lvl3Btn.gameObject.SetActive(false);

        topText.gameObject.SetActive(true);
        playerShipText.gameObject.SetActive(true);
        enemyShipText.gameObject.SetActive(true);
        PlayerText.gameObject.SetActive(true);
        EnemyText.gameObject.SetActive(true);
        PauseResumeBtn.gameObject.SetActive(true);
        PauseResumeText.gameObject.SetActive(true);
        PauseResumeText.text = "Pause";
        ExitBtn.gameObject.SetActive(true); // Exit button can remain during gameplay
        SpeedBtn.gameObject.SetActive(true);
        if (SpeedText != null) SpeedText.gameObject.SetActive(true);
        SpeedTextBtn.gameObject.SetActive(true);

        setupComplete = false;
        playerTurn = true;

        foreach (var s in ships)
            s.SetActive(true);

        if (ships.Length > 0 && ships[0] != null)
        {
            shipScript = ships[0].GetComponent<ShipScript>();
            if (shipScript != null)
            {
                shipScript.FlashColor(Color.yellow);
            }
            else
            {
                Debug.LogError("First ship in array does not have a ShipScript component!");
            }
        }
        else
        {
            Debug.LogError("Ships array is empty or the first ship is null!");
        }

        rotateBtn.gameObject.SetActive(true);
        nextBtn.gameObject.SetActive(true);
        woodDock.SetActive(true);

        SetTilesInteractable(true); // Tiles are interactable during placement
        topText.text = "Place your boats!";
    }

    void NextShipClicked()
    {
        if (shipScript == null || !shipScript.OnGameBoard())
        {
            if (shipScript != null) shipScript.FlashColor(Color.red);
            else Debug.LogError("shipScript is null in NextShipClicked");
            return;
        }

        if (shipIndex < ships.Length - 1)
        {
            shipIndex++;
            if (ships[shipIndex] != null)
            {
                shipScript = ships[shipIndex].GetComponent<ShipScript>();
                if (shipScript != null)
                {
                    shipScript.FlashColor(Color.yellow);
                }
                else
                {
                    Debug.LogError($"Ship at index {shipIndex} does not have a ShipScript component!");
                }
            }
            else
            {
                Debug.LogError($"Ship at index {shipIndex} is null!");
            }
        }
        else
        {
            rotateBtn.gameObject.SetActive(false);
            nextBtn.gameObject.SetActive(false);
            woodDock.SetActive(false);
            setupComplete = true;
            topText.text = "Guess an enemy tile.";

            foreach (var s in ships)
                s.SetActive(false);

            currentGameState = GameState.InGame;

            if (currentLevel >= 2)
                StartTimer();
        }
    }

    public void TileClicked(GameObject tile)
    {
        // Prevent interaction if paused
        if (currentGameState == GameState.Paused) return;

        if (currentGameState == GameState.Placement)
        {
            if (shipScript != null && tile != null)
            {
                shipScript.ClearTileList();
                shipScript.SetClickedTile(tile);
                Vector3 newPos = shipScript.GetOffsetVec(tile.transform.position);
                if (shipIndex < ships.Length && ships[shipIndex] != null)
                {
                    ships[shipIndex].transform.localPosition = newPos;
                }
                else
                {
                    Debug.LogError($"Attempting to access ship at index {shipIndex}, which is out of bounds or null.");
                }
            }
            else
            {
                Debug.LogError("shipScript or tile is null in TileClicked (Placement)");
            }
        }
        // Restrict player tile interaction to a single click per turn in InGame state
        else if (currentGameState == GameState.InGame && playerTurn)
        {
            StopAndHideTimer();
            if (tile != null)
            {
                Vector3 p = tile.transform.position;
                p.y += isFastMode ? fastMissileSpawnY : normalMissileSpawnY;
                
                // **** PENTING: Menonaktifkan interaksi tile di sini, saat misil ditembakkan ****
                SetTilesInteractable(false); // Disable tile interaction immediately after click
                playerTurn = false; // Player turn ends after one click

                if (missilePrefab != null)
                {
                    Instantiate(missilePrefab, p, missilePrefab.transform.rotation);
                }
                else
                {
                    Debug.LogError("missilePrefab is not assigned in GameManager!");
                }
            }
            else
            {
                Debug.LogError("tile is null in TileClicked (InGame)");
            }
        }
    }

    void RotateClicked()
    {
        if (currentGameState == GameState.Paused) return;
        if (shipScript != null)
        {
            shipScript.RotateShip();
        }
        else
        {
            Debug.LogError("shipScript is null in RotateClicked");
        }
    }

    public void CheckHit(GameObject tile)
    {
        if (tile == null)
        {
            Debug.LogError("CheckHit received a null tile GameObject!");
            Invoke(nameof(EndPlayerTurn), 1f);
            return;
        }

        TileScript tileScriptComp = tile.GetComponent<TileScript>();
        if (tileScriptComp == null)
        {
            // Error ini seharusnya sudah ditangani oleh pengecekan tag di MissileScript
            Debug.LogError("GameObject '" + tile.name + "' does not have a TileScript component!");
            Invoke(nameof(EndPlayerTurn), 1f);
            return;
        }

        int tileNum = 0;
        try
        {
            tileNum = int.Parse(Regex.Match(tile.name, @"\d+").Value);
        }
        catch (FormatException)
        {
            Debug.LogError($"Could not parse tile number from tile name: {tile.name}");
            Invoke(nameof(EndPlayerTurn), 1f);
            return;
        }

        bool anyHit = false;

        if (enemyShips == null)
        {
            Debug.LogError("enemyShips list is null in CheckHit!");
            Invoke(nameof(EndPlayerTurn), 1f);
            return;
        }

        foreach (var arr in enemyShips)
        {
            if (arr == null || !arr.Contains(tileNum)) continue;

            anyHit = true;
            int hits = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == tileNum) arr[i] = -5;
                if (arr[i] == -5) hits++;
            }

            if (hits == arr.Length)
            {
                enemyShipCount--;
                topText.text = "SUNK!!!!!!";
                if (firePrefab != null) enemyFires.Add(Instantiate(firePrefab, tile.transform.position, Quaternion.identity));
                else Debug.LogWarning("firePrefab is null, cannot instantiate fire.");
                tileScriptComp.SetTileColor(1, new Color32(68, 0, 0, 255));
            }
            else
            {
                topText.text = "HIT!!";
                tileScriptComp.SetTileColor(1, new Color32(255, 0, 0, 255));
            }
            tileScriptComp.SwitchColors(1);
            break;
        }

        if (!anyHit)
        {
            topText.text = "Missed!";
            tileScriptComp.SetTileColor(1, new Color32(38, 57, 76, 255));
            tileScriptComp.SwitchColors(1);
        }

        Invoke(nameof(EndPlayerTurn), 1f);
    }

    public void EnemyHitPlayer(Vector3 pos, int tileNum, GameObject hitObj)
    {
        // Pastikan tidak ada interaksi saat musuh menembak
        // SetTilesInteractable(false) sudah dipanggil di TileClicked atau EndPlayerTurn
        // jadi kita tidak perlu memanggilnya lagi di sini

        if (enemyScript == null || hitObj == null)
        {
            Debug.LogError("EnemyScript or hitObj is null in EnemyHitPlayer");
            Invoke(nameof(EndEnemyTurn), 2f);
            return;
        }

        enemyScript.MissileHit(tileNum);
        pos.y += 0.2f;
        if (firePrefab != null) playerFires.Add(Instantiate(firePrefab, pos, Quaternion.identity));
        else Debug.LogWarning("firePrefab is null, cannot instantiate fire for player hit.");

        ShipScript hitShipScript = hitObj.GetComponent<ShipScript>();
        if (hitShipScript != null && hitShipScript.HitCheckSank())
        {
            playerShipCount--;
            playerShipText.text = playerShipCount.ToString();
            enemyScript.SunkPlayer();
        }
        else if (hitShipScript == null)
        {
            Debug.LogError("hitObj in EnemyHitPlayer does not have a ShipScript component!");
        }

        Invoke(nameof(EndEnemyTurn), 2f);
    }

    void EndPlayerTurn()
    {
        foreach (var s in ships) if(s != null) s.SetActive(true);
        foreach (var f in playerFires) if(f != null) f.SetActive(true);
        foreach (var f in enemyFires) if(f != null) f.SetActive(false);

        enemyShipText.text = enemyShipCount.ToString();
        topText.text = "Enemy's turn";
        // ***** Pastikan tiles tidak dapat diklik selama giliran musuh *****
        SetTilesInteractable(false); 
        StopAndHideTimer();

        // Cek kondisi menang/kalah/draw setelah giliran pemain
        if (enemyShipCount < 1 && playerShipCount < 1)
        {
            GameOver("DRAW!!!"); // Jika keduanya 0, itu DRAW
            return; // Penting untuk return agar tidak melanjutkan ke giliran musuh
        }
        else if (enemyShipCount < 1)
        {
            GameOver("YOU WIN!!");
            return;
        }
        else if (playerShipCount < 1)
        {
            GameOver("ENEMY WINS!!!");
            return;
        }


        if (enemyScript != null)
        {
            enemyScript.NPCTurn();
            if (currentLevel == 3) enemyScript.NPCTurn();
        }
        else
        {
            Debug.LogError("EnemyScript is null, cannot perform enemy turn.");
            Invoke(nameof(EndEnemyTurn), 0.5f); // Call EndEnemyTurn to avoid freeze
        }

        ColorAllTiles(0);
    }

    public void EndEnemyTurn()
    {
        // Check if the game is already over from the previous player turn
        if (currentGameState == GameState.GameOver) return;

        foreach (var s in ships) if(s != null) s.SetActive(false);
        foreach (var f in playerFires) if(f != null) f.SetActive(false);
        foreach (var f in enemyFires) if(f != null) f.SetActive(true);

        playerShipText.text = playerShipCount.ToString();
        topText.text = "Select a tile";
        playerTurn = true;
        ColorAllTiles(1);
        // ***** Aktifkan kembali tiles untuk giliran pemain *****
        SetTilesInteractable(true);

        if (currentLevel >= 2)
            StartTimer();

        // Cek kondisi menang/kalah/draw setelah giliran musuh
        if (playerShipCount < 1 && enemyShipCount < 1)
        {
            GameOver("DRAW!!!"); // Jika keduanya 0, itu DRAW
            return;
        }
        else if (playerShipCount < 1)
        {
            GameOver("ENEMY WINS!!!");
            return;
        }
        else if (enemyShipCount < 1) // Check for win condition after enemy's turn
        {
            GameOver("YOU WIN!!");
            return;
        }
    }

    void StartTimer()
    {
        TimerText.gameObject.SetActive(true);
        TimerNumber.gameObject.SetActive(true);
        TimerNumber.text = TURN_TIME.ToString();

        if (turnTimerCoroutine != null)
            StopCoroutine(turnTimerCoroutine);

        turnTimerCoroutine = StartCoroutine(PlayerTurnTimer(TURN_TIME));
    }

    void StopAndHideTimer()
    {
        if (turnTimerCoroutine != null)
            StopCoroutine(turnTimerCoroutine);

        turnTimerCoroutine = null;
        TimerText.gameObject.SetActive(false);
        TimerNumber.gameObject.SetActive(false);
        if(TimerNumber != null) TimerNumber.text = TURN_TIME.ToString();
    }

    IEnumerator PlayerTurnTimer(int seconds)
    {
        int t = seconds;
        while (t > 0 && playerTurn && currentGameState == GameState.InGame)
        {
            if(TimerNumber != null) TimerNumber.text = t.ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }
        if (t <= 0 && playerTurn && currentGameState == GameState.InGame)
        {
            StopAndHideTimer();
            EndPlayerTurn();
        }
    }

    void ColorAllTiles(int idx)
    {
        foreach (var tile in allTileScripts)
        {
            if (tile != null) tile.SwitchColors(idx);
        }
    }

    void SetTilesInteractable(bool ok)
    {
        foreach (var tile in allTileScripts)
        {
            if (tile != null) tile.SetClickable(ok);
        }
    }

    /// <summary>
    /// Handles the end-game state, displaying appropriate buttons based on win/loss/draw and current level.
    /// </summary>
    /// <param name="outcomeMessage">A string indicating the game outcome ("YOU WIN!!", "ENEMY WINS!!!", or "DRAW!!!").</param>
    void GameOver(string outcomeMessage)
    {
        topText.text = "Game Over: " + outcomeMessage;
        playerTurn = false;
        SetTilesInteractable(false);
        StopAndHideTimer();
        PauseResumeBtn.gameObject.SetActive(false);
        PauseResumeText.gameObject.SetActive(false);
        SpeedBtn.gameObject.SetActive(false);
        if (SpeedText != null) SpeedText.gameObject.SetActive(false);
        SpeedTextBtn.gameObject.SetActive(false);
        currentGameState = GameState.GameOver;
        Time.timeScale = 1f;

        // Default hide all level progression buttons
        replayBtn.gameObject.SetActive(false);
        Lvl2Btn.gameObject.SetActive(false);
        Lvl3Btn.gameObject.SetActive(false);

        if (outcomeMessage.Contains("YOU WIN"))
        {
            replayBtn.gameObject.SetActive(true); // Replay button (to Level 1) always appears if you win

            if (currentLevel == 1)
            {
                Lvl2Btn.gameObject.SetActive(true); // Unlock Level 2
            }
            else if (currentLevel == 2)
            {
                Lvl2Btn.gameObject.SetActive(true); // Button to replay Level 2
                Lvl3Btn.gameObject.SetActive(true); // Unlock Level 3
            }
            else if (currentLevel == 3)
            {
                // Won Level 3, show all progression buttons
                Lvl2Btn.gameObject.SetActive(true); // Button to play Level 2
                Lvl3Btn.gameObject.SetActive(true); // Button to replay Level 3
            }
        }
        else if (outcomeMessage.Contains("ENEMY WINS")) // ENEMY WINS
        {
            replayBtn.gameObject.SetActive(true); // Always show replay to Level 1 if lost

            if (currentLevel == 2)
            {
                Lvl2Btn.gameObject.SetActive(true); // If lost on Level 2, show Lvl2Btn to retry
            }
            else if (currentLevel == 3)
            {
                Lvl2Btn.gameObject.SetActive(true); // If lost on Level 3, show Lvl2Btn
                Lvl3Btn.gameObject.SetActive(true); // If lost on Level 3, show Lvl3Btn to retry
            }
            // If lost on Level 1, only replayBtn (to Level 1) is active
        }
        else if (outcomeMessage.Contains("DRAW")) // Ini adalah logika baru untuk DRAW
        {
            replayBtn.gameObject.SetActive(true); // Selalu munculkan replay ke Level 1

            if (currentLevel == 2)
            {
                Lvl2Btn.gameObject.SetActive(true); // Munculkan Lvl2Btn jika draw di Level 2
            }
            else if (currentLevel == 3)
            {
                Lvl2Btn.gameObject.SetActive(true); // Munculkan Lvl2Btn jika draw di Level 3
                Lvl3Btn.gameObject.SetActive(true); // Munculkan Lvl3Btn jika draw di Level 3
            }
            // Jika draw di Level 1, hanya replayBtn yang aktif
        }
        ExitBtn.gameObject.SetActive(true); // Exit button always appears on Game Over
    }

    void ReplayClicked()
    {
        currentLevel = 1; // Replay always returns to Level 1 as per this implementation
        isInitialLoad = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ExitGameClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    void TogglePauseResume()
    {
        if ((currentGameState == GameState.Placement || currentGameState == GameState.InGame) && !hasPausedThisLevel)
        {
            Time.timeScale = 0f;
            currentGameState = GameState.Paused;
            PauseResumeText.text = "Resume";
            SetTilesInteractable(false); // Disable tiles when paused
            if (setupComplete && currentLevel >=2 && playerTurn) StopAndHideTimer(); // Only stop timer if active during player's turn
            SpeedBtn.gameObject.SetActive(false);
            if (SpeedText != null) SpeedText.gameObject.SetActive(false);
            SpeedTextBtn.gameObject.SetActive(false);
        }
        else if (currentGameState == GameState.Paused)
        {
            Time.timeScale = 1f;
            hasPausedThisLevel = true; // Mark that pause has been used

            if (setupComplete) // If placement is complete, means we're in InGame
            {
                currentGameState = GameState.InGame;
                if (playerTurn) // If it's the player's turn
                {
                    SetTilesInteractable(true); // Re-enable tiles
                    if (currentLevel >= 2) StartTimer(); // Resume timer if needed
                }
                // If it's not the player's turn, no need to do anything with tiles/timer
            }
            else // If placement is not complete, return to Placement
            {
                currentGameState = GameState.Placement;
                SetTilesInteractable(true);
            }

            // Hide Pause/Resume button after resuming
            PauseResumeBtn.gameObject.SetActive(false);
            PauseResumeText.gameObject.SetActive(false);

            // Show speed button again
            SpeedBtn.gameObject.SetActive(true);
            if (SpeedText != null) SpeedText.gameObject.SetActive(true);
            SpeedTextBtn.gameObject.SetActive(true);
        }
    }

    void ToggleSpeedMode()
    {
        isFastMode = !isFastMode;
        SpeedTextBtn.text = isFastMode ? "Fast" : "Normal";
    }

    public float GetMissileSpawnY()
    {
        return isFastMode ? fastMissileSpawnY : normalMissileSpawnY;
    }
}