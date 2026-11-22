using NUnit.Framework;
using UnityEngine;

namespace Turnaj.Tests {
    public class MatchRulesTests {
        [TestCase(2, 0, 3)]
        [TestCase(2, 1, 2)]
        [TestCase(1, 2, 1)]
        [TestCase(0, 2, 0)]
        public void MatchRules_LeaguePoints(int won, int lost, int expected) {
            Assert.AreEqual(expected, MatchRules.LeaguePoints(won, lost));
        }

        [Test]
        public void MatchRules_BarPercent_ScalesFromDeathThreshold() {
            Assert.AreEqual(0f, MatchRules.BarPercent(0.2f), 1e-4f);
            Assert.AreEqual(0f, MatchRules.BarPercent(0.1f), 1e-4f);
            Assert.AreEqual(1f, MatchRules.BarPercent(1f), 1e-4f);
            Assert.AreEqual(0.5f, MatchRules.BarPercent(0.6f), 1e-4f);
        }

        [Test]
        public void MatchRules_IsFlawless_Above51PercentOfBar() {
            Assert.IsTrue(MatchRules.IsFlawless(0.62f));   // bar 52.5 %
            Assert.IsFalse(MatchRules.IsFlawless(0.60f));  // bar 50 %
            Assert.IsFalse(MatchRules.IsFlawless(0.51f));  // raw 51 % is only ~39 % of the bar
        }

        [TestCase(0, 0, false)]
        [TestCase(1, 1, false)]
        [TestCase(2, 0, true)]
        [TestCase(1, 2, true)]
        public void MatchRules_IsMatchOver(int a, int b, bool expected) {
            Assert.AreEqual(expected, MatchRules.IsMatchOver(a, b));
        }

        [TestCase(0, ArenaOwner.TeamA)]
        [TestCase(1, ArenaOwner.TeamB)]
        [TestCase(2, ArenaOwner.Neutral)]
        public void MatchRules_ArenaForRound(int round, ArenaOwner expected) {
            Assert.AreEqual(expected, MatchRules.ArenaForRound(round));
        }

        [Test]
        public void MatchRules_HomeIsTeamA_ExceptRoundTwo() {
            Assert.IsTrue(MatchRules.TeamAIsHome(0));
            Assert.IsFalse(MatchRules.TeamAIsHome(1));
            Assert.IsTrue(MatchRules.TeamAIsHome(2));
        }

        [Test]
        public void MatchRules_MirrorThroughCentre_KeepsHeight() {
            var m = MatchRules.MirrorThroughCentre(new Vector3(1f, 0.05f, 2f), Vector3.zero);
            Assert.AreEqual(new Vector3(-1f, 0.05f, -2f), m);
        }

        [TestCase(0, 0.9f, 0.5f, TimeoutOutcome.TeamAWins)]
        [TestCase(1, 0.4f, 0.7f, TimeoutOutcome.TeamBWins)]
        [TestCase(2, 0.4f, 0.7f, TimeoutOutcome.TeamBWins)]
        [TestCase(0, 0.8f, 0.8f, TimeoutOutcome.Draw)]
        [TestCase(1, 1f, 1f, TimeoutOutcome.Draw)]
        [TestCase(2, 0.8f, 0.8f, TimeoutOutcome.SuddenDeath)]
        [TestCase(2, 0.8f, 0.801f, TimeoutOutcome.SuddenDeath)] // same whole % on the bar
        public void MatchRules_TimeoutOutcome(int round, float hpA, float hpB, TimeoutOutcome expected) {
            Assert.AreEqual(expected, MatchRules.ResolveTimeout(round, hpA, hpB));
        }

        [Test]
        public void DoubleTap_SecondTapWithinWindowFires() {
            var tap = new DoubleTap(0.4f);
            Assert.IsFalse(tap.Tap(10f));
            Assert.IsTrue(tap.Tap(10.3f));
        }

        [Test]
        public void DoubleTap_SlowTapsDoNotFire_AndFiringResets() {
            var tap = new DoubleTap(0.4f);
            Assert.IsFalse(tap.Tap(10f));
            Assert.IsFalse(tap.Tap(10.5f));
            Assert.IsTrue(tap.Tap(10.7f));
            Assert.IsFalse(tap.Tap(10.9f)); // a third quick tap starts a new pair
        }
    }
}
