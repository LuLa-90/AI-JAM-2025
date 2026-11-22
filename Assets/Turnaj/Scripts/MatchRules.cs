using UnityEngine;

namespace Turnaj {
    public enum ArenaOwner { TeamA, TeamB, Neutral }
    public enum TimeoutOutcome { TeamAWins, TeamBWins, Draw, SuddenDeath }

    /// <summary>Pure tournament rules (no scene access), covered by EditMode tests.</summary>
    public static class MatchRules {
        public const int RoundsToWin = 2;
        /// <summary>RobotAgent dies at weighted HP &lt;= 0.2; the HP bar is drawn from 0.2 to 1.</summary>
        public const float DeathHealth = 0.2f;
        public const float FlawlessBarPercent = 0.51f;

        /// <summary>League points: 2:0 = 3, 2:1 = 2, 1:2 = 1, 0:2 = 0.</summary>
        public static int LeaguePoints(int won, int lost) {
            if (won >= RoundsToWin) return lost == 0 ? 3 : 2;
            return won > 0 ? 1 : 0;
        }

        /// <summary>Weighted HP as the audience sees it on the average bar (0..1).</summary>
        public static float BarPercent(float weightedHealth) {
            return Mathf.Clamp01((weightedHealth - DeathHealth) / (1f - DeathHealth));
        }

        public static bool IsFlawless(float winnerWeightedHealth) {
            return BarPercent(winnerWeightedHealth) > FlawlessBarPercent;
        }

        /// <summary>
        /// Time ran out: higher HP (whole % on the bar) wins. Equal HP -> sudden death in the final round,
        /// otherwise a draw (the round is replayed).
        /// </summary>
        public static TimeoutOutcome ResolveTimeout(int roundIndex, float hpA, float hpB) {
            int a = Mathf.RoundToInt(BarPercent(hpA) * 100f);
            int b = Mathf.RoundToInt(BarPercent(hpB) * 100f);
            if (a > b) return TimeoutOutcome.TeamAWins;
            if (b > a) return TimeoutOutcome.TeamBWins;
            return roundIndex >= 2 ? TimeoutOutcome.SuddenDeath : TimeoutOutcome.Draw;
        }

        public static bool IsMatchOver(int scoreA, int scoreB) {
            return scoreA >= RoundsToWin || scoreB >= RoundsToWin;
        }

        public static ArenaOwner ArenaForRound(int roundIndex) {
            switch (roundIndex) {
                case 0: return ArenaOwner.TeamA;
                case 1: return ArenaOwner.TeamB;
                default: return ArenaOwner.Neutral;
            }
        }

        /// <summary>Team A is home in its own arena and in the (symmetrical) neutral arena.</summary>
        public static bool TeamAIsHome(int roundIndex) {
            return ArenaForRound(roundIndex) != ArenaOwner.TeamB;
        }

        public static string RoundTitle(int roundIndex) {
            return roundIndex >= 2 ? "FINAL ROUND" : $"ROUND {roundIndex + 1}";
        }

        /// <summary>Point mirrored through the arena centre on the floor plane (height kept).</summary>
        public static Vector3 MirrorThroughCentre(Vector3 point, Vector3 centre) {
            return new Vector3(2f * centre.x - point.x, point.y, 2f * centre.z - point.z);
        }

        public static string PointsLabel(int points) {
            return points == 1 ? "+1 point" : $"+{points} points";
        }
    }

    /// <summary>Fires on the second tap within the window; a fired pair resets.</summary>
    public class DoubleTap {
        private readonly float window;
        private float lastTap = float.NegativeInfinity;

        public DoubleTap(float window = 0.4f) {
            this.window = window;
        }

        public bool Tap(float now) {
            if (now - lastTap <= window) {
                lastTap = float.NegativeInfinity;
                return true;
            }
            lastTap = now;
            return false;
        }
    }
}
