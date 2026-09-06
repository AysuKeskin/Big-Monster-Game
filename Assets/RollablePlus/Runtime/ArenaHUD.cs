using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RollablePlus
{
    public sealed class ArenaHUD : MonoBehaviour
    {
        [Header("Game manager")]
        [SerializeField] ArenaGame game;
        [Header("Screens")]
        [SerializeField] RectTransform root, titleScreen, inGame, results, options;
        [Header("In-game text")]
        [SerializeField] TextMeshProUGUI collected, timer, lives, dashHint, notice;
        [Header("Results and settings text")]
        [SerializeField] TextMeshProUGUI resultTitle, resultDetails, primaryLabel, musicLabel, soundEffectsLabel, effectsLabel, volumeLabel, backLabel;
        [Header("Menu controls")]
        [SerializeField] Button playButton, primaryButton, menuButton, backButton, restartButton, mainMenuButton;
        [SerializeField] Slider volume;
        bool optionsOpen;

        public bool HasLayout => root != null;
        public void Initialize(ArenaGame owner)
        {
            game = owner;
            RefreshSettings(); ShowState();
        }
        public void OpenMenu()
        {
            if (game.IsPlaying) { game.TogglePause(); return; }
            optionsOpen = true; SetMenuVisibility();
        }
        public void CloseMenu()
        {
            game.SaveSettings();
            if (game.State == ArenaGame.GameState.Paused) { game.TogglePause(); return; }
            optionsOpen = false; SetMenuVisibility();
        }
        public void ToggleMenu() { if (optionsOpen) CloseMenu(); else OpenMenu(); }
        public void Continue()
        {
            if (game.State == ArenaGame.GameState.RoundComplete) game.NextRound();
            else game.Restart();
        }
        public void ShowState()
        {
            if (optionsOpen) game.SaveSettings();
            bool atTitle = game.State == ArenaGame.GameState.Menu;
            bool atResult = game.State == ArenaGame.GameState.Won || game.State == ArenaGame.GameState.Lost || game.State == ArenaGame.GameState.RoundComplete;
            titleScreen.gameObject.SetActive(atTitle);
            inGame.gameObject.SetActive(game.IsPlaying || game.State == ArenaGame.GameState.Paused);
            results.gameObject.SetActive(atResult);
            optionsOpen = game.State == ArenaGame.GameState.Paused;
            if (atResult)
            {
                if (game.State == ArenaGame.GameState.RoundComplete)
                {
                    resultTitle.text = "Round complete!";
                    resultDetails.text = "Time: " + TimeString(game.Elapsed) + "\nLives: " + game.Health + " / 3";
                    primaryLabel.text = "Next Round";
                }
                else
                {
                    resultTitle.text = game.State == ArenaGame.GameState.Won ? "You Win!" : "You Lose!";
                    resultDetails.text = "Time: " + TimeString(game.Elapsed) + "\nScore: " + game.Score + "     Best: " + game.Best;
                    primaryLabel.text = "Play Again";
                }
            }
            SetMenuVisibility(); Refresh();
        }
        void SetMenuVisibility()
        {
            options.gameObject.SetActive(optionsOpen);
            backLabel.text = game.State == ArenaGame.GameState.Paused ? "Resume" : "Back";
            bool hasRun = game.State != ArenaGame.GameState.Menu;
            restartButton.gameObject.SetActive(hasRun); mainMenuButton.gameObject.SetActive(hasRun);
            RefreshSettings();
            GameObject selected = optionsOpen ? backButton.gameObject : game.State == ArenaGame.GameState.Menu ? playButton.gameObject : results.gameObject.activeSelf ? primaryButton.gameObject : null;
            EventSystem.current?.SetSelectedGameObject(selected);
        }
        public void RefreshSettings()
        {
            if (musicLabel == null) return;
            musicLabel.text = game.MusicMuted ? "Music: Off" : "Music: On";
            soundEffectsLabel.text = game.SoundEffectsMuted ? "Sound Effects: Off" : "Sound Effects: On";
            effectsLabel.text = game.ReducedEffects ? "Visual Effects: Reduced" : "Visual Effects: Full";
            volume.SetValueWithoutNotify(game.Volume);
            volumeLabel.text = Mathf.RoundToInt(game.Volume * 100) + "%";
        }
        public void Refresh()
        {
            if (collected == null) return;
            collected.text = "Collected: " + game.Collected + " / " + game.pickups.Length;
            timer.text = "Time: " + TimeString(game.Elapsed);
            lives.text = "Lives: " + game.Health + "     Round: " + game.Round + " / 3";
            dashHint.text = game.player.DashReady >= 1 ? "Space: dash" : "Dash recharging...";
            notice.text = game.Toast.Length > 0 ? game.Toast : game.PortalReady ? "Exit open!" : "";
        }
        public static string TimeString(float seconds) => ((int)seconds / 60).ToString("00") + ":" + ((int)seconds % 60).ToString("00");
    }
}
