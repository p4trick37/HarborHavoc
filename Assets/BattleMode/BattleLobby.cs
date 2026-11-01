using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborBattle
{
    // Carries the start screen's joined players into the battle scene.
    // A null entry is the keyboard/mouse player.
    public static class BattleLobby
    {
        public const int MaxPlayers=4;
        public static readonly List<Gamepad> Players=new List<Gamepad>();
        public static bool HasPlayers => Players.Count>0;

        public static void Set(IEnumerable<Gamepad> players)
        {
            Players.Clear();
            foreach(var pad in players)
                if(Players.Count<MaxPlayers) Players.Add(pad);
        }

        // Static state survives Play Mode restarts when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Players.Clear();
    }
}
