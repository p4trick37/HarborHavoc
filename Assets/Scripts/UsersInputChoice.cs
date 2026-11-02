using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
public class UsersInputChoice : MonoBehaviour
{
    [SerializeField] private GameObject campaignButton;
    [SerializeField] private GameObject battleButton;
    [SerializeField] private GameObject startBattleButton;

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
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(campaignButton);
    }

    private void Update()
    {
        
        AllowController();
        AllowMouse();

        if(mouseUsed == false && EventSystem.current.currentSelectedGameObject == null && startBattleButton.activeInHierarchy == true)
        {
            EventSystem.current.SetSelectedGameObject(startBattleButton);
        }
    }

    public void Switch()
    {
        if(mouseUsed == false)
        {
            EventSystem.current.SetSelectedGameObject(startBattleButton);
        }
    }

    


    private void AllowController()
    {
        if(move != Vector2.zero && chosenInput == false && mouseUsed == true)
        {
            if(campaignButton.activeInHierarchy == true)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(campaignButton);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(startBattleButton);
            }
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
