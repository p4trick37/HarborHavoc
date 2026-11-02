using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace HarborBattle.Editor
{
    public static class BattleCargoValidation
    {
        private static BattleArena arena;
        private static Gamepad first,second;
        private static Gamepad[] savedLobby;
        private static BattleAiBoat cargo;
        private static BattlePickup expiring;
        private static int step,health;
        private static double next;
        private static Vector3 trafficPosition;

        [MenuItem("Battle Mode/Test Fog And Cargo In Play Mode")]
        public static void Run()
        {
            Check(EditorApplication.isPlaying,"Enter Play Mode in BattleArena first.");
            Check(first==null,"A cargo test is already running.");
            arena=UnityEngine.Object.FindFirstObjectByType<BattleArena>();
            Check(arena!=null && arena.cargoBoatPrefab!=null,"Missing battle arena or cargo prefab.");
            savedLobby=BattleLobby.Players.ToArray(); BattleLobby.Players.Clear();
            arena.SendMessage("Restart");
            first=InputSystem.AddDevice<Gamepad>(); second=InputSystem.AddDevice<Gamepad>();
            step=0; Wait(.5); EditorApplication.update+=Advance;
        }

        private static void Wait(double seconds) {next=EditorApplication.timeSinceStartup+seconds;}
        private static void Place(BattleBoat boat,Vector2 point)
        {boat.transform.position=point;boat.GetComponent<Rigidbody2D>().position=point;}
        private static void Advance()
        {
            if(EditorApplication.timeSinceStartup<next) return;
            try
            {
                Check(EditorApplication.isPlaying && arena!=null,"Play Mode ended during test.");
                switch(step++)
                {
                    case 0:
                        InputSystem.QueueStateEvent(first,new GamepadState().WithButton(GamepadButton.Start));
                        InputSystem.QueueStateEvent(second,new GamepadState().WithButton(GamepadButton.Start)); Wait(.4); break;
                    case 1:
                        Check(arena.Boats.Count==2,"Two controllers must join.");
                        InputSystem.QueueStateEvent(first,new GamepadState()); InputSystem.QueueStateEvent(second,new GamepadState()); Wait(.2); break;
                    case 2:
                        InputSystem.QueueStateEvent(first,new GamepadState().WithButton(GamepadButton.Start)); Wait(3.7); break;
                    case 3:
                        InputSystem.QueueStateEvent(first,new GamepadState());
                        Check(arena.Running,"Round must start.");
                        Check(GameObject.Find("Breakwater")==null,"Arena walls must be gone.");
                        Check(UnityEngine.Object.FindObjectsByType<BattlePickup>(FindObjectsSortMode.None).Length==0,"No ambient pickups may spawn.");
                        health=arena.Boats[0].Health;
                        Place(arena.Boats[0],new Vector2(BattleArena.HalfWidth+3,-10)); Wait(1.1); break;
                    case 4:
                        Check(health-arena.Boats[0].Health>=9 && health-arena.Boats[0].Health<=14,"Fog must damage a player beyond the passable edge.");
                        Place(arena.Boats[0],new Vector2(-10,-10)); health=arena.Boats[0].Health; Wait(.3); break;
                    case 5:
                        Check(arena.Boats[0].Health==health,"Fog damage must stop in safe water.");
                        cargo=arena.SpawnCargoBoat();
                        Check(cargo!=null && cargo.Health==30 && !cargo.canShoot,"Cargo defaults must be 30 health and shooting off.");
                        var screen=Camera.main.WorldToViewportPoint(cargo.transform.position);
                        Check(screen.x<0 || screen.x>1 || screen.y<0 || screen.y>1,"Cargo must enter from off-screen.");
                        cargo.speed=0;
                        cargo.SetRoute(new Vector2(-4,-10),new Vector2(-3,-10),new Vector2(-2,-10),new Vector2(-1,-10));
                        Physics2D.SyncTransforms();
                        for(int i=0;i<3;i++) BattleShot.Spawn(arena.Boats[0],Vector2.right,10);
                        Wait(.5); break;
                    case 6:
                        Check(cargo==null || !cargo.Alive,"Player projectiles must sink cargo boats.");
                        var drops=UnityEngine.Object.FindObjectsByType<BattlePickup>(FindObjectsSortMode.None);
                        Check(drops.Length==1,"A sunk cargo boat must drop exactly one item.");
                        Check(arena.Boats[0].ReadyItem==PowerUp.None,"The killer must not receive an item automatically.");
                        Place(arena.Boats[1],drops[0].transform.position); Wait(.4); break;
                    case 7:
                        Check(arena.Boats[1].ReadyItem!=PowerUp.None && arena.Boats[0].ReadyItem==PowerUp.None,"A different collector must receive the loot.");
                        Check(UnityEngine.Object.FindObjectsByType<BattlePickup>(FindObjectsSortMode.None).Length==0,"Loot must be consumed once.");
                        float lifetime=arena.lootLifetime; arena.lootLifetime=.3f;
                        expiring=arena.DropLoot(new Vector2(12,-12)); arena.lootLifetime=lifetime;
                        Wait(.6); break;
                    case 8:
                        Check(expiring==null,"Unclaimed loot must expire.");
                        Check(arena.Running,"Cargo destruction must not count toward victory.");
                        Wait(3); break;
                    case 9:
                        cargo=arena.CargoBoats.Find(boat=>boat!=null && boat.Alive);
                        Check(cargo!=null,"Traffic must keep spawning after cargo is sunk.");
                        trafficPosition=cargo.transform.position; Wait(.6); break;
                    case 10:
                        Check(cargo!=null && Vector3.Distance(trafficPosition,cargo.transform.position)>.2f,"Traffic must follow its curved route.");
                        Check(arena.CargoBoats.FindAll(boat=>boat!=null && boat.Alive).Count<=arena.maxCargoBoats,"Traffic must respect the alive cap.");
                        Report("PASS: no walls/ambient pickups, passable damaging fog, safe re-entry, off-screen cargo, projectile kill, one loot drop, non-killer collection, expiry, ongoing curved traffic, player-only victory.");
                        Cleanup(); break;
                }
            }
            catch(Exception exception) {Report("FAIL: "+exception);Debug.LogException(exception);Cleanup();}
        }
        private static void Report(string message) {File.WriteAllText("Library/BattleCargoValidation.txt",message);Debug.Log(message);}
        private static void Cleanup()
        {
            EditorApplication.update-=Advance;
            if(arena!=null) arena.SendMessage("Restart");
            if(first!=null) InputSystem.RemoveDevice(first);
            if(second!=null) InputSystem.RemoveDevice(second);
            if(savedLobby!=null) BattleLobby.Set(savedLobby);
            first=null;second=null;arena=null;
        }
        private static void Check(bool condition,string message) {if(!condition) throw new Exception(message);}
    }
}
