using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Moves between the start screen's menus: the first menu (Campaign / Battle),
// the battle settings, and the player select. B on a controller or Esc goes back one screen.
public class StartScreenNavigator : MonoBehaviour
{
    [SerializeField] private GameObject modeScreen;
    [SerializeField] private GameObject battleSettingsScreen;
    [SerializeField] private GameObject playerSelectScreen;

    [Header("Selected first when each screen opens (for controllers)")]
    [SerializeField] private GameObject modeFirstButton;
    [SerializeField] private GameObject battleSettingsFirstButton;
    [SerializeField] private GameObject playerSelectFirstButton;

    private void Start()
    {
        ShowMode();
    }

    private void Update()
    {
        bool back = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        foreach (Gamepad pad in Gamepad.all)
        {
            if (pad.buttonEast.wasPressedThisFrame) back = true;
        }
        if (back)
        {
            Back();
        }
    }

    // The button a controller should land on for whichever screen is showing.
    public GameObject FirstButton
    {
        get
        {
            if (playerSelectScreen != null && playerSelectScreen.activeInHierarchy) return playerSelectFirstButton;
            if (battleSettingsScreen != null && battleSettingsScreen.activeInHierarchy) return battleSettingsFirstButton;
            return modeFirstButton;
        }
    }

    public void ShowMode()
    {
        Show(modeScreen);
    }

    public void ShowBattleSettings()
    {
        Show(battleSettingsScreen);
    }

    public void ShowPlayerSelect()
    {
        Show(playerSelectScreen);
    }

    public void Back()
    {
        if (playerSelectScreen != null && playerSelectScreen.activeInHierarchy)
        {
            ShowBattleSettings();
        }
        else if (battleSettingsScreen != null && battleSettingsScreen.activeInHierarchy)
        {
            ShowMode();
        }
    }

    private void Show(GameObject screen)
    {
        if (modeScreen != null) modeScreen.SetActive(screen == modeScreen);
        if (battleSettingsScreen != null) battleSettingsScreen.SetActive(screen == battleSettingsScreen);
        if (playerSelectScreen != null) playerSelectScreen.SetActive(screen == playerSelectScreen);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(FirstButton);
        }
    }
}
