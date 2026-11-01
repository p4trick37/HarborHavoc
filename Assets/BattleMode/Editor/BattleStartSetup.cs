using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HarborBattle.Editor
{
    // One-click StartScreen setup: adds a Start button to PlayerSelect and makes the
    // Battle button open PlayerSelect instead of loading the arena directly.
    public static class BattleStartSetup
    {
        private const string StartName="Start Battle";

        [MenuItem("Battle Mode/Set Up Start Screen")]
        public static void Setup()
        {
            var canvas=GameObject.FindGameObjectWithTag("UI");
            var playerSelect=canvas!=null ? canvas.transform.Find("PlayerSelect") : null;
            var mode=canvas!=null ? canvas.transform.Find("Mode") : null;
            var battle=mode!=null ? mode.Find("Battle")?.GetComponent<Button>() : null;
            var manager=Object.FindAnyObjectByType<PlayerMenuManager>(FindObjectsInactive.Include);
            if(playerSelect==null || battle==null || manager==null)
            {
                EditorUtility.DisplayDialog("Set Up Start Screen","Open StartScreen first. It needs UI/PlayerSelect, UI/Mode/Battle and a PlayerMenuManager.","OK");
                return;
            }

            // Remove the earlier level-select build and any previous Start button.
            foreach(var name in new[] {"LevelSelect",StartName})
            {
                var old=playerSelect.Find(name);
                if(old!=null) Undo.DestroyObjectImmediate(old.gameObject);
            }

            // Copy the Battle button so Start matches the menu's style.
            var obj=Object.Instantiate(battle.gameObject,playerSelect,false);
            Undo.RegisterCreatedObjectUndo(obj,"Set Up Start Screen");
            obj.name=StartName;
            obj.SetActive(true);
            var rect=(RectTransform)obj.transform;
            rect.localScale=Vector3.one;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=new Vector2(0,-160);
            rect.sizeDelta=new Vector2(260,45);
            var start=obj.GetComponent<Button>();
            start.onClick=new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(start.onClick,new UnityAction(manager.StartBattle));
            var label=obj.GetComponentInChildren<TMP_Text>(true);
            if(label!=null)
            {
                label.text="Start";
                var labelRect=label.rectTransform;
                labelRect.anchorMin=Vector2.zero; labelRect.anchorMax=Vector2.one;
                labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;
            }

            var so=new SerializedObject(manager);
            so.FindProperty("playerSelectScreen").objectReferenceValue=playerSelect.gameObject;
            so.FindProperty("startButton").objectReferenceValue=start;
            so.FindProperty("startButtonText").objectReferenceValue=label;
            so.ApplyModifiedProperties();

            // Battle now opens player select; Campaign keeps loading Level 1.
            Undo.RecordObject(battle,"Set Up Start Screen");
            battle.onClick=new Button.ButtonClickedEvent();
            UnityEventTools.AddBoolPersistentListener(battle.onClick,new UnityAction<bool>(mode.gameObject.SetActive),false);
            UnityEventTools.AddBoolPersistentListener(battle.onClick,new UnityAction<bool>(playerSelect.gameObject.SetActive),true);

            Selection.activeGameObject=obj;
            EditorSceneManager.MarkSceneDirty(obj.scene);
        }
    }
}
