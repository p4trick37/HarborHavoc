using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HarborBattle
{
    // Shown a moment after a match ends: the result plus Rematch and Main Menu buttons.
    // Built as a uGUI canvas (not OnGUI) so controllers can navigate it.
    public class BattleEndMenu : MonoBehaviour
    {
        private GameObject root;
        private TMP_Text title, score;
        private TMP_Text rematchLabel;
        private Button rematch, mainMenu;
        private float timer;
        private bool showing, chosen;

        private void Update()
        {
            var arena=BattleArena.Instance;
            if(arena==null) return;
            if(arena.Result==null)
            {
                if(showing) Hide();
                timer=0;
                return;
            }
            if(!showing)
            {
                if((timer+=Time.unscaledDeltaTime)>=arena.endMenuDelay) Show(arena);
                return;
            }
            // Keep a button selected so a controller can always navigate after a mouse click elsewhere.
            if(EventSystem.current!=null && EventSystem.current.currentSelectedGameObject==null)
                EventSystem.current.SetSelectedGameObject(rematch.gameObject);
        }

        private void Show(BattleArena arena)
        {
            if(root==null) Build(arena);
            title.text=arena.Result;
            title.color=arena.WinnerIndex>=0 ? BattleLobby.PlayerColor(arena.WinnerIndex) : Color.white;
            // In a series, show the score and keep going until someone has won it.
            score.text=BattleLobby.IsSeries ? BattleLobby.Format.Label+":  "+arena.SeriesScore : "";
            rematchLabel.text=BattleLobby.IsSeries && !BattleLobby.SeriesOver ? "Next Game" : "Rematch";
            rematch.interactable=mainMenu.interactable=true;
            chosen=false;
            root.SetActive(true);
            showing=true;
            Cursor.visible=true;
            Cursor.lockState=CursorLockMode.None;
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(rematch.gameObject);
        }

        private void Hide()
        {
            if(root!=null) root.SetActive(false);
            showing=false;
        }

        private void Choose(bool restart)
        {
            if(chosen) return;
            chosen=true;
            rematch.interactable=mainMenu.interactable=false;
            if(restart) BattleArena.Instance.Rematch();
            else BattleArena.Instance.QuitToStartScreen();
        }

        private void Build(BattleArena arena)
        {
            if(EventSystem.current==null)
            {
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            root=new GameObject("Battle End Menu",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);
            scaler.matchWidthOrHeight=.5f;

            var dim=Panel("Dim",root.transform,Vector2.zero,Vector2.zero,new Color(0,0,0,.55f));
            dim.anchorMin=Vector2.zero; dim.anchorMax=Vector2.one;
            var panel=Panel("Panel",root.transform,Vector2.zero,new Vector2(640,440),new Color(.05f,.12f,.18f,.95f));

            title=Label("Result",panel,new Vector2(0,135),new Vector2(600,90),56,arena.menuFont);
            score=Label("Score",panel,new Vector2(0,70),new Vector2(600,50),30,arena.menuFont);
            rematch=MakeButton("Rematch",panel,new Vector2(0,-15),arena.menuFont);
            rematchLabel=rematch.GetComponentInChildren<TMP_Text>();
            mainMenu=MakeButton("Main Menu",panel,new Vector2(0,-125),arena.menuFont);
            rematch.onClick.AddListener(()=>Choose(true));
            mainMenu.onClick.AddListener(()=>Choose(false));

            var down=new Navigation { mode=Navigation.Mode.Explicit, selectOnDown=mainMenu.GetComponent<Selectable>() };
            var up=new Navigation { mode=Navigation.Mode.Explicit, selectOnUp=rematch.GetComponent<Selectable>() };
            rematch.navigation=down;
            mainMenu.navigation=up;
        }

        private static RectTransform Panel(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));
            var rect=obj.GetComponent<RectTransform>();
            rect.SetParent(parent,false);
            rect.anchoredPosition=position; rect.sizeDelta=size;
            obj.GetComponent<Image>().color=color;
            return rect;
        }

        private static TMP_Text Label(string text,Transform parent,Vector2 position,Vector2 size,float fontSize,TMP_FontAsset font)
        {
            var obj=new GameObject(text,typeof(RectTransform),typeof(TextMeshProUGUI));
            var rect=obj.GetComponent<RectTransform>();
            rect.SetParent(parent,false);
            rect.anchoredPosition=position; rect.sizeDelta=size;
            var label=obj.GetComponent<TextMeshProUGUI>();
            if(font!=null) label.font=font;
            label.text=text; label.fontSize=fontSize;
            label.alignment=TextAlignmentOptions.Center;
            label.raycastTarget=false;
            return label;
        }

        private static Button MakeButton(string text,Transform parent,Vector2 position,TMP_FontAsset font)
        {
            var rect=Panel(text,parent,position,new Vector2(380,85),Color.white);
            var button=rect.gameObject.AddComponent<Button>();
            var colors=button.colors;
            colors.normalColor=new Color(0,.56f,1);
            colors.highlightedColor=new Color(.42f,.68f,.89f);
            colors.selectedColor=new Color(1,.8f,.25f);
            colors.pressedColor=new Color(.5f,.92f,.53f);
            colors.disabledColor=new Color(.5f,.5f,.5f,.6f);
            button.colors=colors;
            var label=Label(text,rect,Vector2.zero,Vector2.zero,40,font);
            label.rectTransform.anchorMin=Vector2.zero; label.rectTransform.anchorMax=Vector2.one;
            return button;
        }
    }
}
