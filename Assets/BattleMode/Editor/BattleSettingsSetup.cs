using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HarborBattle.Editor
{
    // One-click StartScreen setup for the battle settings screen. Adds UI/BattleSettings
    // (lives picker, Continue, Back), a Back button on PlayerSelect and a StartScreenNavigator,
    // and points the Battle button at the settings screen. Leaves Start Battle untouched.
    // Running it again rebuilds only the objects it made.
    public static class BattleSettingsSetup
    {
        private const string ScreenName="BattleSettings";
        private const string PlayerSelectBackName="Back";

        [MenuItem("Battle Mode/Add Battle Settings Screen")]
        public static void Setup()
        {
            var canvas=GameObject.FindGameObjectWithTag("UI");
            var ui=canvas!=null ? canvas.transform : null;
            var mode=ui!=null ? ui.Find("Mode") : null;
            var playerSelect=ui!=null ? ui.Find("PlayerSelect") : null;
            var battle=mode!=null ? mode.Find("Battle")?.GetComponent<Button>() : null;
            var campaign=mode!=null ? mode.Find("Campaign") : null;
            var startBattle=playerSelect!=null ? playerSelect.Find("Start Battle")?.GetComponent<Button>() : null;
            if(mode==null || playerSelect==null || battle==null)
            {
                EditorUtility.DisplayDialog("Add Battle Settings Screen","Open StartScreen first. It needs a UI canvas (tagged UI) with Mode/Battle and PlayerSelect.","OK");
                return;
            }

            foreach(var old in new[] {ui.Find(ScreenName),playerSelect.Find(PlayerSelectBackName)})
                if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);

            // Settings screen, laid out on the 800x600 reference canvas.
            var screen=Rect(ScreenName,ui,Vector2.zero,new Vector2(500,400));
            Undo.RegisterCreatedObjectUndo(screen.gameObject,"Add Battle Settings Screen");
            screen.SetSiblingIndex(playerSelect.GetSiblingIndex());
            var font=battle.GetComponentInChildren<TMP_Text>(true)?.font;
            Label("Header",screen,"Battle Settings",new Vector2(0,110),new Vector2(400,50),34,font);

            // The Lives row starts as a Button copy for its look; swap the Button for a LivesStepper.
            var livesButton=Copy(battle,"Lives",screen,"Lives: 3",new Vector2(0,30),new Vector2(260,45));
            var livesRow=livesButton.gameObject;
            var colors=livesButton.colors;
            var target=livesButton.targetGraphic;
            Object.DestroyImmediate(livesButton);
            var stepper=livesRow.AddComponent<LivesStepper>();
            stepper.colors=colors;stepper.targetGraphic=target;
            var stepperSo=new SerializedObject(stepper);
            stepperSo.FindProperty("valueText").objectReferenceValue=livesRow.GetComponentInChildren<TMP_Text>(true);
            stepperSo.ApplyModifiedPropertiesWithoutUndo();
            var less=Copy(battle,"Fewer Lives",livesRow.transform,"<",new Vector2(-160,0),new Vector2(45,45));
            var more=Copy(battle,"More Lives",livesRow.transform,">",new Vector2(160,0),new Vector2(45,45));
            UnityEventTools.AddPersistentListener(less.onClick,new UnityAction(stepper.Decrease));
            UnityEventTools.AddPersistentListener(more.onClick,new UnityAction(stepper.Increase));
            less.navigation=more.navigation=new Navigation {mode=Navigation.Mode.None};

            var next=Copy(battle,"Continue",screen,"Continue",new Vector2(0,-60),new Vector2(220,45));
            var back=Copy(battle,"Back",screen,"Back",new Vector2(0,-125),new Vector2(160,38));
            var playerBack=Copy(battle,PlayerSelectBackName,playerSelect,"Back",new Vector2(0,-215),new Vector2(160,35));

            // Navigator on the canvas, wired to all three screens.
            var navigator=ui.GetComponent<StartScreenNavigator>();
            if(navigator==null) navigator=Undo.AddComponent<StartScreenNavigator>(ui.gameObject);
            var so=new SerializedObject(navigator);
            so.FindProperty("modeScreen").objectReferenceValue=mode.gameObject;
            so.FindProperty("battleSettingsScreen").objectReferenceValue=screen.gameObject;
            so.FindProperty("playerSelectScreen").objectReferenceValue=playerSelect.gameObject;
            so.FindProperty("modeFirstButton").objectReferenceValue=campaign!=null ? campaign.gameObject : battle.gameObject;
            so.FindProperty("battleSettingsFirstButton").objectReferenceValue=livesRow.gameObject;
            so.FindProperty("playerSelectFirstButton").objectReferenceValue=startBattle!=null ? startBattle.gameObject : playerBack.gameObject;
            so.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(next.onClick,new UnityAction(navigator.ShowPlayerSelect));
            UnityEventTools.AddPersistentListener(back.onClick,new UnityAction(navigator.ShowMode));
            UnityEventTools.AddPersistentListener(playerBack.onClick,new UnityAction(navigator.ShowBattleSettings));
            var manager=Object.FindAnyObjectByType<PlayerMenuManager>(FindObjectsInactive.Include);
            if(manager!=null)
            {
                var managerSo=new SerializedObject(manager);
                managerSo.FindProperty("backButton").objectReferenceValue=playerBack;
                managerSo.ApplyModifiedProperties();
            }

            Undo.RecordObject(battle,"Add Battle Settings Screen");
            battle.onClick=new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(battle.onClick,new UnityAction(navigator.ShowBattleSettings));

            // Up/down between the settings rows; left/right on Lives changes the value.
            stepper.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnDown=next};
            next.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=stepper,selectOnDown=back};
            back.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=next};

            AddMatchRow(screen);
            screen.gameObject.SetActive(false);
            Selection.activeGameObject=screen.gameObject;
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
        }

        // Adds a "Match: Single Game / First to N / Best of N" row under Lives on an existing
        // BattleSettings screen, keeping any restyling done to it. Running it again replaces the row.
        [MenuItem("Battle Mode/Add Match Format Setting")]
        public static void AddMatchFormatSetting()
        {
            var canvas=GameObject.FindGameObjectWithTag("UI");
            var screen=canvas!=null ? canvas.transform.Find(ScreenName) : null;
            if(screen==null || screen.GetComponentInChildren<LivesStepper>(true)==null)
            {
                EditorUtility.DisplayDialog("Add Match Format Setting","Open StartScreen and run Battle Mode > Add Battle Settings Screen first.","OK");
                return;
            }
            AddMatchRow(screen);
            Selection.activeGameObject=screen.Find("Match").gameObject;
            EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
        }

        private static void AddMatchRow(Transform screen)
        {
            var lives=screen.GetComponentInChildren<LivesStepper>(true);
            var next=screen.Find("Continue")?.GetComponent<Button>();
            var back=screen.Find("Back")?.GetComponent<Button>();
            var old=screen.Find("Match");
            if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);

            // Clone the Lives row (and its arrows), then swap in a MatchFormatStepper.
            var livesRect=(RectTransform)lives.transform;
            var row=Object.Instantiate(lives.gameObject,screen,false);
            Undo.RegisterCreatedObjectUndo(row,"Add Match Format Setting");
            row.name="Match";
            row.transform.SetSiblingIndex(lives.transform.GetSiblingIndex()+1);
            var rowRect=(RectTransform)row.transform;
            const float spacing=55;
            rowRect.anchoredPosition=livesRect.anchoredPosition-new Vector2(0,spacing);
            var copied=row.GetComponent<LivesStepper>();
            var colors=copied.colors;var target=copied.targetGraphic;
            Object.DestroyImmediate(copied);
            var stepper=row.AddComponent<MatchFormatStepper>();
            stepper.colors=colors;stepper.targetGraphic=target;
            var label=row.GetComponentInChildren<TMP_Text>(true);
            if(label!=null) label.text="Match: Single Game";
            var so=new SerializedObject(stepper);
            so.FindProperty("valueText").objectReferenceValue=label;
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var arrow in row.GetComponentsInChildren<Button>(true))
            {
                bool fewer=arrow.name.StartsWith("Fewer");
                arrow.name=fewer ? "Previous Match" : "Next Match";
                arrow.onClick=new Button.ButtonClickedEvent();
                UnityAction action=fewer ? new UnityAction(stepper.Decrease) : new UnityAction(stepper.Increase);
                UnityEventTools.AddPersistentListener(arrow.onClick,action);
            }

            // Make room: if Continue would overlap the new row, Continue and Back both move down one row.
            bool crowded=next!=null && ((RectTransform)next.transform).anchoredPosition.y>rowRect.anchoredPosition.y-spacing;
            foreach(var button in new[] {next,back})
            {
                if(!crowded || button==null) continue;
                var rect=(RectTransform)button.transform;
                Undo.RecordObject(rect,"Add Match Format Setting");
                rect.anchoredPosition-=new Vector2(0,spacing);
            }

            // Up/down: Lives -> Match -> Continue -> Back.
            Undo.RecordObject(lives,"Add Match Format Setting");
            var livesNav=lives.navigation;livesNav.mode=Navigation.Mode.Explicit;livesNav.selectOnDown=stepper;lives.navigation=livesNav;
            stepper.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=lives,selectOnDown=next};
            if(next!=null)
            {
                Undo.RecordObject(next,"Add Match Format Setting");
                var nav=next.navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnUp=stepper;next.navigation=nav;
            }
        }

        private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var obj=new GameObject(name,typeof(RectTransform));
            obj.layer=parent.gameObject.layer;
            var rect=obj.GetComponent<RectTransform>();
            rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=size;
            return rect;
        }

        private static void Label(string name,Transform parent,string text,Vector2 position,Vector2 size,float fontSize,TMP_FontAsset font)
        {
            var label=Rect(name,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();
            if(font!=null) label.font=font;
            label.text=text;label.fontSize=fontSize;label.color=Color.white;
            label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
        }

        // Copies the Battle button so new buttons match the menu's style.
        private static Button Copy(Button template,string name,Transform parent,string text,Vector2 position,Vector2 size)
        {
            var obj=Object.Instantiate(template.gameObject,parent,false);
            obj.name=name;obj.SetActive(true);
            var button=obj.GetComponent<Button>();
            button.onClick=new Button.ButtonClickedEvent();
            var rect=(RectTransform)obj.transform;
            rect.localScale=Vector3.one;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=size;
            var label=obj.GetComponentInChildren<TMP_Text>(true);
            if(label!=null)
            {
                label.text=text;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
                label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            }
            return button;
        }
    }
}
