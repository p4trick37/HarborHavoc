using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HarborBattle.Editor
{
    public static class BattleInputValidation
    {
        private static Gamepad testPad;
        private static BattleArena arena;
        private static int step;
        private static double next;
        private static Vector3 position;

        [MenuItem("Battle Mode/Validate Merged Input")]
        public static void ValidateAsset()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            Check(prefab!=null,"Missing Player prefab");
            var source=prefab.GetComponent<PlayerInput>()?.actions;
            Check(source!=null,"Player prefab must reference its mapping asset");
            foreach(var action in new[] {"Player/Move","Player/Look","Player/Shoot","Battle/Join","Battle/StartMatch","Battle/Reset","Battle/Boost","Battle/Activate"})
                source.FindAction(action,true);
            Debug.Log("PASS: original Player prefab mapping and all battle actions are present.");
        }

        [MenuItem("Battle Mode/Test Controller Lobby In Play Mode")]
        public static void RunPlayTest()
        {
            Check(EditorApplication.isPlaying,"Enter Play Mode in BattleArena first.");
            Check(testPad==null,"A controller test is already running.");
            ValidateAsset();
            arena=UnityEngine.Object.FindFirstObjectByType<BattleArena>();
            Check(arena!=null,"BattleArena must be open.");
            arena.SendMessage("Restart");
            testPad=InputSystem.AddDevice<Gamepad>();
            step=0;
            next=EditorApplication.timeSinceStartup+.5;
            EditorApplication.update+=Advance;
        }

        private static void Advance()
        {
            if(EditorApplication.timeSinceStartup<next) return;
            try
            {
                Check(EditorApplication.isPlaying && arena!=null,"Play Mode ended during validation.");
                switch(step++)
                {
                    case 0:
                        InputSystem.QueueStateEvent(testPad,new GamepadState().WithButton(GamepadButton.Start));
                        next=EditorApplication.timeSinceStartup+.4; break;
                    case 1:
                        Check(arena.Boats.Count==1,"First Start must join exactly one full Player prefab");
                        var input=arena.Boats[0].GetComponent<PlayerInput>();
                        bool paired=false;
                        if(input!=null) foreach(var device in input.devices) if(device==testPad) paired=true;
                        Check(paired,"PlayerInput must pair to the joined controller");
                        Check(arena.Boats[0].transform.Find("Boat")!=null,"Battle must retain the original prefab Boat child");
                        Check(!arena.Running,"Joining must not immediately start the match");
                        InputSystem.QueueStateEvent(testPad,new GamepadState());
                        next=EditorApplication.timeSinceStartup+.3; break;
                    case 2:
                        InputSystem.QueueStateEvent(testPad,new GamepadState().WithButton(GamepadButton.Start));
                        next=EditorApplication.timeSinceStartup+3.8; break;
                    case 3:
                        Check(arena.Running && arena.Boats.Count==1,"Second Start must force solo testing without immediate victory");
                        position=arena.Boats[0].transform.position;
                        InputSystem.QueueStateEvent(testPad,new GamepadState {leftStick=new Vector2(.8f,0),rightStick=new Vector2(0,.8f),rightTrigger=1});
                        next=EditorApplication.timeSinceStartup+.6; break;
                    case 4:
                        Check(arena.Running,"Solo mode must remain running");
                        Check(arena.Boats[0].transform.position.x>position.x+.3f,"Mapped controller movement must move the prefab");
                        Check(UnityEngine.Object.FindObjectsByType<BattleShot>(FindObjectsSortMode.None).Length>0,"Mapped trigger must shoot");
                        Report("PASS: full prefab spawn, controller pairing, Start join, second Start solo launch, movement, shooting, solo stays running.");
                        Cleanup(); break;
                }
            }
            catch(Exception exception) { Report("FAIL: "+exception); Debug.LogException(exception); Cleanup(); }
        }

        private static void Report(string text) { File.WriteAllText("Library/BattleInputValidation.txt",text); Debug.Log(text); }
        private static void Cleanup()
        {
            EditorApplication.update-=Advance;
            if(arena!=null) arena.SendMessage("Restart");
            if(testPad!=null) InputSystem.RemoveDevice(testPad);
            testPad=null; arena=null;
        }
        private static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }
    }
}
