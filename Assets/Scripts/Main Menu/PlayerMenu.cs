using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMenu : MonoBehaviour
{
    private PlayerMenuManager playerManager;
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private Image inputImage;
    [SerializeField] private Sprite keyboardIcon;
    [SerializeField] private Sprite controllerIcon;

    // The device this player joined with: a controller, or the keyboard (which also uses the mouse).
    // Set by PlayerMenuManager before the card is switched on.
    public InputDevice Device { get; set; }

    // The controller this player joined with, or null for keyboard & mouse.
    public Gamepad Gamepad => Device as Gamepad;

    public Button LeaveButton { get; private set; }

    private void Awake()
    {
        LeaveButton = GetComponentInChildren<Button>(true);
        playerManager = FindAnyObjectByType<PlayerMenuManager>();
        Transform canvasUI = GameObject.FindGameObjectWithTag("UI").transform;
        Transform menu = canvasUI.Find("PlayerSelect/Menu");
        transform.SetParent(menu, false);

        if (playerManager.CanJoin(this) == false)
        {
            Destroy(gameObject);
            return;
        }
        playerManager.AddPlayer(this);

        // Show which input this player joined with.
        if (inputImage != null)
        {
            inputImage.sprite = Gamepad != null ? controllerIcon : keyboardIcon;
            inputImage.preserveAspect = true;
        }
    }

    private void OnDestroy()
    {
        if (playerManager != null)
        {
            playerManager.RemovePlayer(this);
        }
    }

    public void Leave()
    {
        Destroy(gameObject);
    }

    public void SetText(string text)
    {
        playerText.text = text;
    }

    public void SetColor(Color color)
    {
        playerText.color = color;
    }
}

