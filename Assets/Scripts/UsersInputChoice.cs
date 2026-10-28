using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
public class UsersInputChoice : MonoBehaviour
{
    [SerializeField] private GameObject keyboardButton;
    [SerializeField] private GameObject controllerButton;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject endButton;

    private static bool chosenInput = false;

    private ControllerInput controls;
    private Vector2 move;

    private Vector2 lastMousePosition;

    private bool mouseUsed = true;

    private void Awake()
    {
        controls = new ControllerInput();

        controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => move = Vector2.zero;
    }

    private void Start()
    {
        if(chosenInput == true)
        {
            LoadStart();
            Debug.Log("True");
        }
        else
        {
            keyboardButton.SetActive(true);
            controllerButton.SetActive(true);
            startButton.SetActive(false);
            endButton.SetActive(false);
        }
    }

    private void Update()
    {
        
        AllowController();
        AllowMouse();
        Debug.Log(mouseUsed);
    }

    public void UseController()
    {
        PlayerController.usingController = true;
        chosenInput = true;
        Cursor.lockState = CursorLockMode.Locked;
        LoadStart(); 
    }

    public void UseKeyboard()
    {
        PlayerController.usingController = false;
        chosenInput = true;
        LoadStart();
    }

    private void LoadStart()
    {
        keyboardButton.SetActive(false);
        controllerButton.SetActive(false);
        startButton.SetActive(true);
        endButton.SetActive(true);
        if(PlayerController.usingController == true)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(startButton);
        }
    }

    private void AllowController()
    {
        if(move != Vector2.zero && chosenInput == false && mouseUsed == true)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(controllerButton);
            mouseUsed = false;
        }
    }

    private void AllowMouse()
    {
        Vector2 mousePosition = Input.mousePosition;
        if (lastMousePosition != mousePosition && chosenInput == false)
        {
            EventSystem.current.SetSelectedGameObject(null);
            mouseUsed = true;
        }
        lastMousePosition = mousePosition;
    }
    

    private void OnEnable()
    {
        controls.Player.Enable();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
}
