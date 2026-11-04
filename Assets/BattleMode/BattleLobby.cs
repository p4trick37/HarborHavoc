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
        // One color per player slot: boats, health bars and the start-screen cards all use these.
        public static readonly Color[] PlayerColors={ Color.cyan, new Color(1,.45f,.25f), Color.yellow, Color.magenta };
        public static Color PlayerColor(int index) => PlayerColors[index%PlayerColors.Length];
        public static readonly List<Gamepad> Players=new List<Gamepad>();
        public static bool HasPlayers => Players.Count>0;

        // Lives (stocks) each player gets, picked on the start screen's battle settings.
        public const int MinLives=1, MaxLives=9, DefaultLives=3;
        private static int lives=DefaultLives;
        public static int Lives
        {
            get => lives;
            set => lives=Mathf.Clamp(value,MinLives,MaxLives);
        }

        // Match format, picked on the battle settings: a single game, first to N wins,
        // or best of N games (most wins, decided early once someone has a majority).
        public enum SeriesType { Single, FirstTo, BestOf }
        public struct MatchFormat
        {
            public SeriesType Type; public int Games; public string Label;
            public MatchFormat(SeriesType type,int games,string label) { Type=type; Games=games; Label=label; }
        }
        public static readonly MatchFormat[] Formats=
        {
            new MatchFormat(SeriesType.Single,1,"Single Game"),
            new MatchFormat(SeriesType.FirstTo,2,"First to 2"),
            new MatchFormat(SeriesType.FirstTo,3,"First to 3"),
            new MatchFormat(SeriesType.FirstTo,5,"First to 5"),
            new MatchFormat(SeriesType.BestOf,3,"Best of 3"),
            new MatchFormat(SeriesType.BestOf,5,"Best of 5"),
            new MatchFormat(SeriesType.BestOf,7,"Best of 7"),
        };
        private static int formatIndex;
        public static int FormatIndex
        {
            get => formatIndex;
            set => formatIndex=(value%Formats.Length+Formats.Length)%Formats.Length;
        }
        public static MatchFormat Format => Formats[formatIndex];
        public static bool IsSeries => Format.Type!=SeriesType.Single;

        // Series score: wins per player slot and games finished so far.
        public static readonly int[] Wins=new int[MaxPlayers];
        public static int GamesPlayed { get; private set; }

        public static void ResetSeries()
        {
            System.Array.Clear(Wins,0,Wins.Length);
            GamesPlayed=0;
        }

        // winner is the player index, or -1 for a draw.
        public static void RecordGame(int winner)
        {
            GamesPlayed++;
            if(winner>=0 && winner<Wins.Length) Wins[winner]++;
        }

        public static bool SeriesOver => Format.Type==SeriesType.Single ? GamesPlayed>=1 : SeriesWinner>=0;

        // The player who has won the series, or -1 if it isn't decided (or a single game was drawn).
        public static int SeriesWinner
        {
            get
            {
                int best=-1, bestWins=0; bool tied=false;
                for(int i=0;i<Wins.Length;i++)
                {
                    if(Wins[i]>bestWins) { best=i; bestWins=Wins[i]; tied=false; }
                    else if(Wins[i]==bestWins && bestWins>0) tied=true;
                }
                switch(Format.Type)
                {
                    case SeriesType.FirstTo: return bestWins>=Format.Games ? best : -1;
                    case SeriesType.BestOf:
                        if(bestWins>=Format.Games/2+1) return best;
                        // All games played without a majority: most wins takes it; a tie plays on.
                        return GamesPlayed>=Format.Games && !tied ? best : -1;
                    default: return GamesPlayed>=1 && !tied ? best : -1;
                }
            }
        }

        public static void Set(IEnumerable<Gamepad> players)
        {
            Players.Clear();
            foreach(var pad in players)
                if(Players.Count<MaxPlayers) Players.Add(pad);
        }

        // Static state survives Play Mode restarts when domain reload is disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() { Players.Clear(); lives=DefaultLives; formatIndex=0; ResetSeries(); }
    }
}
