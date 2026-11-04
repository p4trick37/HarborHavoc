using UnityEditor;
using UnityEngine;

namespace HarborBattle.Editor
{
    public class BattleItemTester : EditorWindow
    {
        private int playerIndex;
        private PowerUp item=PowerUp.SmokeScreen;
        private float duration=15;
        [MenuItem("Battle Mode/Item Tester")]
        public static void Open() => GetWindow<BattleItemTester>("Battle Item Tester");
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Give any item directly to a player",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Press Play and join/start the arena. Choose a player and item. Test duration overrides only the granted copy; the regular pool is unchanged.",MessageType.Info);
            var arena=BattleArena.Instance;
            item=(PowerUp)EditorGUILayout.EnumPopup("Item",item);
            duration=Mathf.Max(.1f,EditorGUILayout.FloatField("Test duration (seconds)",duration));
            if(!Application.isPlaying || arena==null || arena.Boats.Count==0)
            {EditorGUILayout.HelpBox("No battle players found. Start Play Mode and join first.",MessageType.Warning);return;}
            var names=new string[arena.Boats.Count];for(int i=0;i<names.Length;i++)names[i]="P"+(i+1)+(arena.Boats[i].Alive?"":" (eliminated)");
            playerIndex=Mathf.Clamp(playerIndex,0,names.Length-1);playerIndex=EditorGUILayout.Popup("Player",playerIndex,names);
            using(new EditorGUI.DisabledScope(item==PowerUp.None || !arena.Boats[playerIndex].Alive))
            {
                if(GUILayout.Button("Give item (activate with X / LT)"))arena.Boats[playerIndex].GiveTestItem(item,duration);
                if(GUILayout.Button("Give and activate now"))arena.Boats[playerIndex].GiveTestItem(item,duration,true);
            }
            EditorGUILayout.Space();EditorGUILayout.LabelField("Drag-and-drop alternatives",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Assets/BattleMode/TestPickups",EditorStyles.wordWrappedLabel);
        }
        private void OnInspectorUpdate() {Repaint();}
    }

    [CustomEditor(typeof(BattleTestPickup))]
    public class BattleTestPickupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Test-only pickup. Drag into BattleArena and sail over it. Offense activates with X/Square (E/Enter); defense with LT/L2 (Shift).",MessageType.Info);
            DrawDefaultInspector();
        }
        private void OnSceneGUI()
        {
            var pickup=(BattleTestPickup)target;
            Handles.Label(pickup.transform.position+Vector3.up*1.1f,"TEST: "+BattleItemPopup.Name(pickup.item));
        }
    }
}
