using System.Collections.Generic;
using HarborBattle;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerMenuManager : MonoBehaviour
{
    [SerializeField] private List<PlayerMenu> playersSelected;

    [SerializeField] private GameObject playerSelectScreen;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private string battleSceneName = "BattleArena";

    private PlayerInputManager inputManager;
    private SceneSwitcherForMenus sceneSwitcher;
    private bool starting;
    private ColorBlock startColors;
    private bool startColorsSaved;
    private bool startShownReady;

    private void Awake()
    {
        inputManager = FindAnyObjectByType<PlayerInputManager>();
        sceneSwitcher = FindAnyObjectByType<SceneSwitcherForMenus>();
    }

    private void Update()
    {
        playersSelected.RemoveAll(player => player == null);

        // Joining is handled here instead of by the Input Manager, so mouse clicks
        // never join and the campaign stays single player.
        if (inputManager != null && inputManager.joiningEnabled)
        {
            inputManager.DisableJoining();
        }
        bool showing = playerSelectScreen == null || playerSelectScreen.activeInHierarchy;
        if (showing && starting == false)
        {
            JoinPressedDevices();
        }

        bool ready = playersSelected.Count >= minPlayers && playersSelected.Count <= BattleLobby.MaxPlayers && starting == false;
        if (startButton != null)
        {
            // The button stays interactable so a controller can still see it highlighted;
            // it just looks greyed out and StartBattle does nothing until it's ready.
            if (startColorsSaved == false)
            {
                startColors = startButton.colors;
                startColorsSaved = true;
                startButton.interactable = true;
                startShownReady = !ready;
            }
            if (ready != startShownReady)
            {
                ColorBlock colors = startColors;
                if (ready == false)
                {
                    colors.normalColor = startColors.disabledColor;
                    colors.pressedColor = startColors.disabledColor;
                }
                startButton.colors = colors;
                startShownReady = ready;
            }
        }
        if (startButtonText != null)
        {
            if (playersSelected.Count < minPlayers)
            {
                startButtonText.text = "Waiting for players (" + playersSelected.Count + "/" + minPlayers + ")";
            }
            else if (playersSelected.Count > BattleLobby.MaxPlayers)
            {
                startButtonText.text = "Max " + BattleLobby.MaxPlayers + " players";
            }
            else
            {
                startButtonText.text = "Start";
            }
        }
    }

    // A controller joins with Start or A, the keyboard & mouse with any key.
    private void JoinPressedDevices()
    {
        if (playersSelected.Count >= BattleLobby.MaxPlayers) return;

        foreach (Gamepad pad in Gamepad.all)
        {
            bool pressed = pad.startButton.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame;
            if (pressed && HasJoined(pad) == false)
            {
                SpawnCard(pad);
                return;
            }
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame && HasJoined(keyboard) == false)
        {
            SpawnCard(keyboard);
        }
    }

    private bool HasJoined(InputDevice device)
    {
        foreach (PlayerMenu player in playersSelected)
        {
            if (player != null && player.Device == device) return true;
        }
        return false;
    }

    // Cards are created directly rather than through the Input Manager: the menu only
    // needs to know each player's device, and the battle scene sets up their controls.
    private void SpawnCard(InputDevice device)
    {
        GameObject prefab = inputManager != null ? inputManager.playerPrefab : null;
        if (prefab == null)
        {
            Debug.LogError("PlayerMenuManager needs the Input Manager's Player Prefab to make player cards.", this);
            return;
        }

        // Build the card inactive so its PlayerInput never grabs devices.
        GameObject staging = new GameObject("Player Card Staging");
        staging.SetActive(false);
        GameObject card = Instantiate(prefab, staging.transform);
        PlayerInput input = card.GetComponent<PlayerInput>();
        if (input != null)
        {
            DestroyImmediate(input);
        }
        card.GetComponent<PlayerMenu>().Device = device;
        card.transform.SetParent(null, false);
        Destroy(staging);
    }

    // Only one keyboard & mouse player, plus one player per controller.
    public bool CanJoin(PlayerMenu player)
    {
        if (player.Device == null) return false;
        foreach (PlayerMenu other in playersSelected)
        {
            if (other != null && other != player && other.Device == player.Device) return false;
        }
        return true;
    }

    public void AddPlayer(PlayerMenu player)
    {
        if (playersSelected.Contains(player) == false)
        {
            playersSelected.Add(player);
        }
        UpdatePlayerNums();
    }

    public void RemovePlayer(PlayerMenu player)
    {
        playersSelected.Remove(player);
        UpdatePlayerNums();
    }

    // Hook this up to the Start button's On Click.
    public void StartBattle()
    {
        playersSelected.RemoveAll(player => player == null);
        if (starting || playersSelected.Count < minPlayers || playersSelected.Count > BattleLobby.MaxPlayers) return;
        starting = true;

        List<Gamepad> devices = new List<Gamepad>();
        foreach (PlayerMenu player in playersSelected)
        {
            devices.Add(player.Gamepad);
        }
        BattleLobby.Set(devices);

        if (sceneSwitcher != null)
        {
            sceneSwitcher.LoadSceneByName(battleSceneName);
        }
        else
        {
            SceneManager.LoadScene(battleSceneName);
        }
    }

    private void UpdatePlayerNums()
    {
        playersSelected.RemoveAll(player => player == null);
        for(int i = 0; i < playersSelected.Count; i++)
        {
            int currentPlayer = i + 1;
            string text = "P" + currentPlayer;
            playersSelected[i].SetText(text);
        }
        UpdateNavigation();
    }

    // Controller navigation: left/right moves between players' Leave buttons,
    // down goes to Start, and up from Start goes back to the players.
    private void UpdateNavigation()
    {
        for (int i = 0; i < playersSelected.Count; i++)
        {
            Button leave = playersSelected[i].LeaveButton;
            if (leave == null) continue;
            Navigation navigation = new Navigation();
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = i > 0 ? playersSelected[i - 1].LeaveButton : null;
            navigation.selectOnRight = i < playersSelected.Count - 1 ? playersSelected[i + 1].LeaveButton : null;
            navigation.selectOnDown = startButton;
            leave.navigation = navigation;
        }

        if (startButton != null)
        {
            Navigation startNavigation = new Navigation();
            startNavigation.mode = Navigation.Mode.Explicit;
            startNavigation.selectOnUp = playersSelected.Count > 0 ? playersSelected[0].LeaveButton : null;
            startButton.navigation = startNavigation;
        }
    }
}
