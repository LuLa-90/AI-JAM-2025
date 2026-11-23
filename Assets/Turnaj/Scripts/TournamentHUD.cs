using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnaj {
    /// <summary>
    /// Everything the audience sees on the Game view (captured by OBS). Built in code at Awake,
    /// so it needs no prefab editing. HP boards come from a StatisticsGUI prefab (see StatsBoardPrefab).
    /// Nothing director-only may be shown here.
    /// </summary>
    public class TournamentHUD : MonoBehaviour {
        [Tooltip("Screen-space copy of the training StatsGUI (StatisticsGUI with RobotBoardA/B).")]
        public StatisticsGUI StatsBoardPrefab;
        public Color AccentColor = new Color(1f, 0.78f, 0.1f);
        public Color DangerColor = new Color(1f, 0.25f, 0.2f);

        private TextMeshProUGUI matchupText, roundTitleText, centerText, flashText, timerText, scoreText, pausedText;
        private GameObject fightGroup, winGroup;
        private TextMeshProUGUI winTitle, winName, winScore, winPoints;
        private StatisticsGUI stats;
        private Coroutine centerRoutine, flashRoutine;
        private GameObject waitingBand, centerBand, flashBand;

        private void LateUpdate() {
            waitingBand.SetActive(!string.IsNullOrEmpty(matchupText.text));
            centerBand.SetActive(!string.IsNullOrEmpty(centerText.text));
            flashBand.SetActive(!string.IsNullOrEmpty(flashText.text));
        }

        private static GameObject Band(Transform parent, string name, float y, float height) {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = new Vector2(0f, y);
            var img = go.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            img.raycastTarget = false;
            return go;
        }

        private void Awake() {
            var canvasGo = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            fightGroup = Group(root, "Fight");
            timerText = Text(fightGroup.transform, "Timer", "90", 72, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(300, 100));
            scoreText = Text(fightGroup.transform, "Score", "", 44, new Vector2(0.5f, 1f), new Vector2(0, -135), new Vector2(1400, 70));

            // Dark bands keep text readable over the bright arena floor on the projector.
            waitingBand = Band(root, "WaitingBand", 60, 380);
            matchupText = Text(root, "Matchup", "", 84, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1800, 140));
            roundTitleText = Text(root, "RoundTitle", "", 110, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1600, 160));
            roundTitleText.color = AccentColor;
            centerBand = Band(root, "CenterBand", 0, 240);
            centerText = Text(root, "Center", "", 170, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1800, 260));
            flashBand = Band(root, "FlashBand", -200, 130);
            flashText = Text(root, "Flash", "", 90, new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(1600, 140));
            flashText.color = AccentColor;
            pausedText = Text(root, "Paused", "PAUSED", 96, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1000, 140));
            pausedText.gameObject.SetActive(false);

            winGroup = Group(root, "Win");
            Band(winGroup.transform, "WinBand", 90, 620);
            winTitle = Text(winGroup.transform, "WinTitle", "WINNER", 90, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1400, 130));
            winTitle.color = AccentColor;
            winName = Text(winGroup.transform, "WinName", "", 150, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(1800, 200));
            winScore = Text(winGroup.transform, "WinScore", "", 80, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1800, 120));
            winPoints = Text(winGroup.transform, "WinPoints", "", 56, new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(1800, 90));
            winGroup.SetActive(false);

            if (StatsBoardPrefab != null) {
                stats = Instantiate(StatsBoardPrefab, root);
                stats.name = "Stats Boards";
                stats.transform.SetParent(fightGroup.transform, false);
            }
            Clear();
        }

        public void SetRobots(RobotAgent a, RobotAgent b) {
            if (stats != null) stats.SetRobots(a, b);
        }

        public void Clear() {
            fightGroup.SetActive(false);
            winGroup.SetActive(false);
            matchupText.text = roundTitleText.text = centerText.text = flashText.text = "";
        }

        public void ShowWaiting(string nameA, string nameB, string roundTitle) {
            StopRoutine(ref centerRoutine);
            fightGroup.SetActive(false);
            winGroup.SetActive(false);
            matchupText.text = $"{nameA}  <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>vs</color>  {nameB}";
            roundTitleText.text = roundTitle;
            centerText.text = flashText.text = "";
        }

        public void ShowFightLayout() {
            matchupText.text = roundTitleText.text = "";
            fightGroup.SetActive(true);
            winGroup.SetActive(false);
        }

        public void SetScore(string nameA, int a, int b, string nameB) {
            scoreText.text = $"{nameA}   <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{a} : {b}</color>   {nameB}";
        }

        public void SetTimer(float secondsLeft, bool suddenDeath) {
            if (suddenDeath) {
                timerText.text = "SUDDEN DEATH";
                timerText.color = DangerColor;
                return;
            }
            timerText.text = Mathf.CeilToInt(Mathf.Max(0f, secondsLeft)).ToString();
            timerText.color = secondsLeft < 10f ? DangerColor : Color.white;
        }

        /// <summary>Big centre text; duration &lt;= 0 keeps it until replaced.</summary>
        public void ShowCenter(string text, Color color, float duration = 0f) {
            StopRoutine(ref centerRoutine);
            centerText.text = text;
            centerText.color = color;
            if (duration > 0f) centerRoutine = StartCoroutine(HideAfter(centerText, duration));
        }

        public void Flash(string text, float duration) {
            StopRoutine(ref flashRoutine);
            flashText.text = text;
            flashRoutine = StartCoroutine(HideAfter(flashText, duration));
        }

        public void SetPaused(bool paused) {
            pausedText.gameObject.SetActive(paused);
        }

        public void ShowWinner(string winner, string nameA, int a, int b, string nameB) {
            StopRoutine(ref centerRoutine);
            fightGroup.SetActive(false);
            matchupText.text = roundTitleText.text = centerText.text = flashText.text = "";
            winGroup.SetActive(true);
            winName.text = winner;
            winScore.text = $"{nameA}   <color=#{ColorUtility.ToHtmlStringRGB(AccentColor)}>{a} : {b}</color>   {nameB}";
            winPoints.text = $"{MatchRules.PointsLabel(MatchRules.LeaguePoints(a, b))}          {MatchRules.PointsLabel(MatchRules.LeaguePoints(b, a))}";
        }

        private IEnumerator HideAfter(TextMeshProUGUI t, float seconds) {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
            t.text = "";
        }

        private void StopRoutine(ref Coroutine c) {
            if (c != null) StopCoroutine(c);
            c = null;
        }

        private static GameObject Group(Transform parent, string name) {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Vector2 anchor, Vector2 pos, Vector2 box) {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = value;
            t.fontSize = size;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.4f;
            t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(0, 0, 0, 255);
            t.raycastTarget = false;
            return t;
        }
    }
}
