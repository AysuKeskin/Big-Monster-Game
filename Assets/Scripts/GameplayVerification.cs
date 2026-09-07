#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEditor;

namespace BigMonster
{
    // Added only by the explicit editor verification command; excluded from players.
    public sealed class GameplayVerification : MonoBehaviour
    {
        readonly List<string> checks = new List<string>();
        readonly List<string> errors = new List<string>();
        Keyboard keyboard;
        int oldBest, oldMuted, oldEffects;
        float oldVolume;
        int oldMusicMute, oldSfxMute;
        bool hadMusicMute, hadSfxMute;
        string output;
        void OnEnable() { Application.logMessageReceived += Log; }
        void Log(string text, string stack, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors.Add(text + "\n" + stack); }
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../verification")); Directory.CreateDirectory(output);
            hadMusicMute = PlayerPrefs.HasKey("BigMonster.MusicMuted"); hadSfxMute = PlayerPrefs.HasKey("BigMonster.SoundEffectsMuted");
            oldMusicMute = PlayerPrefs.GetInt("BigMonster.MusicMuted"); oldSfxMute = PlayerPrefs.GetInt("BigMonster.SoundEffectsMuted");
            oldVolume = PlayerPrefs.GetFloat("BigMonster.Volume", 1f);
            oldBest = PlayerPrefs.GetInt("BigMonster.Best", 0); oldMuted = PlayerPrefs.GetInt("BigMonster.Muted", 0); oldEffects = PlayerPrefs.GetInt("BigMonster.ReducedEffects", 0);
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return new WaitForSecondsRealtime(2);
            var game = FindAnyObjectByType<ArenaGame>();
            Check(game != null, "Game director loads"); if (game == null) { Finish(); yield break; }
            Check(game.State == ArenaGame.GameState.Menu, "Starts at main menu");
            Check(game.pickups.Length == 4, "Original four pickup cubes preserved");
            Check(game.music != null && game.music.clip != null && game.music.clip.name == "Music_Background06", "Original selected background music preserved");
            Capture("01-menu", game);
            game.Restart(); yield return new WaitForSeconds(.1f);
            Check(game.Health == 3 && game.Collected == 0 && game.Round == 1, "New game resets all counters");
            foreach (var cube in game.pickups)
            {
                bool found = NavMesh.SamplePosition(cube.home, out var end, 2, NavMesh.AllAreas);
                var path = new NavMeshPath();
                bool reachable = found && NavMesh.SamplePosition(game.player.transform.position, out var start, 3, NavMesh.AllAreas) && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
                Check(reachable, "Pickup reachable on original navigation: " + cube.name);
            }
            Vector3 before = game.player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(.6f);
            Check(Vector3.Distance(before, game.player.transform.position) > .25f, "Keyboard input moves physical ball");
            Check(new Vector2(game.player.Body.linearVelocity.x, game.player.Body.linearVelocity.z).magnitude <= game.player.maxSpeed + .1f, "Movement respects speed limit");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            yield return new WaitForSeconds(.06f);
            Check(game.player.DashReady < 1, "Space consumes dash charge");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            game.TogglePause(); float elapsed = game.Elapsed; Vector3 paused = game.player.transform.position;
            yield return new WaitForSecondsRealtime(.3f);
            Check(game.State == ArenaGame.GameState.Paused && Mathf.Approximately(elapsed, game.Elapsed) && Vector3.Distance(paused, game.player.transform.position) < .01f, "Pause freezes timer and physics");
            game.TogglePause(); Check(game.IsPlaying && Time.timeScale == 1, "Resume restores simulation");
            game.Restart(); yield return new WaitForSeconds(2.1f);
            game.Hit(game.player.transform.position + Vector3.left); Check(game.Health == 2, "Enemy damage removes one shield");
            game.Hit(game.player.transform.position + Vector3.left); Check(game.Health == 2, "Invulnerability prevents repeated contact damage");
            game.Restart();
            yield return new WaitForSeconds(.2f);
            Capture("02-gameplay", game);
            // Trigger pickups with real collider overlaps, then exercise the exit and all rounds.
            for (int round = 1; round <= 3; round++)
            {
                foreach (var cube in game.pickups)
                {
                    game.player.ResetAt(cube.transform.position); Physics.SyncTransforms();
                    yield return new WaitForSeconds(.12f);
                }
                Check(game.Collected == 4 && game.PortalReady, "Round " + round + ": physical pickup triggers unlock portal");
                int score = game.Score; game.Collect(game.pickups[0]); Check(game.Score == score, "Round " + round + ": duplicate pickup cannot score");
                game.player.ResetAt(game.portal.position + Vector3.up * .7f);
                yield return new WaitForSeconds(.15f);
                Check(game.State == (round == 3 ? ArenaGame.GameState.Won : ArenaGame.GameState.RoundComplete), "Round " + round + ": exit completes correct state");
                if (round < 3) { game.NextRound(); yield return new WaitForSeconds(.1f); Check(game.Collected == 0 && game.Round == round + 1, "Next round restores pickups"); }
            }
            Capture("03-complete", game);
            Check(game.Best >= game.Score, "High score persists");
            game.Restart();
            for (int i = 0; i < 3; i++) { yield return new WaitForSeconds(2.1f); game.Hit(game.player.transform.position + Vector3.left); }
            Check(game.State == ArenaGame.GameState.Lost, "Three hits lead to loss");
            game.Restart(); Check(game.IsPlaying && game.Health == 3 && game.Score == 0, "Restart after loss is clean");
            game.SetVolume(-1); Check(game.Volume == 0, "Volume clamps at zero");
            game.SetVolume(2); Check(game.Volume == 1, "Volume clamps at one");
            game.SetVolume(.37f); Check(Mathf.Approximately(game.music.volume, .23f * .37f), "Volume scales music");
            Check(Mathf.Approximately(PlayerPrefs.GetFloat("BigMonster.Volume"), .37f), "Volume preference is recorded");
            bool musicMuted = game.MusicMuted, sfxMuted = game.SoundEffectsMuted;
            game.ToggleMusic();
            Check(game.MusicMuted != musicMuted && game.music.mute == game.MusicMuted && game.SoundEffectsMuted == sfxMuted && game.soundEffects.mute == sfxMuted, "Music toggle does not change sound effects");
            game.ToggleMusic(); game.ToggleSoundEffects();
            Check(game.SoundEffectsMuted != sfxMuted && game.soundEffects.mute == game.SoundEffectsMuted && game.MusicMuted == musicMuted && game.music.mute == musicMuted, "Sound effects toggle does not change music");
            game.ToggleSoundEffects();
            bool reduced = game.ReducedEffects; game.ToggleEffects(); Check(game.ReducedEffects != reduced, "Reduced effects toggle works"); game.ToggleEffects();
            game.Menu(); Check(game.State == ArenaGame.GameState.Menu && Time.timeScale == 1, "Return to main menu");
            Finish();
        }
        void Check(bool value, string label) { checks.Add((value ? "PASS " : "FAIL ") + label); if (!value) errors.Add(label); }
        void Capture(string name, ArenaGame game)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera = game.arenaCamera.GetComponent<Camera>();
            var canvas = FindObjectsByType<Canvas>()[0];
            var priorMode = canvas.renderMode; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var target = new RenderTexture(1440, 900, 24); target.Create(); var previous = camera.targetTexture; camera.targetTexture = target; Canvas.ForceUpdateCanvases(); camera.Render();
            var active = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(1440, 900, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1440, 900), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = active; camera.targetTexture = previous; canvas.renderMode = priorMode; target.Release(); Destroy(image); Destroy(target);
        }
        void Finish()
        {
            Application.logMessageReceived -= Log;
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (hadMusicMute) PlayerPrefs.SetInt("BigMonster.MusicMuted", oldMusicMute); else PlayerPrefs.DeleteKey("BigMonster.MusicMuted");
            if (hadSfxMute) PlayerPrefs.SetInt("BigMonster.SoundEffectsMuted", oldSfxMute); else PlayerPrefs.DeleteKey("BigMonster.SoundEffectsMuted");
            PlayerPrefs.SetFloat("BigMonster.Volume", oldVolume); PlayerPrefs.SetInt("BigMonster.Best", oldBest); PlayerPrefs.SetInt("BigMonster.Muted", oldMuted); PlayerPrefs.SetInt("BigMonster.ReducedEffects", oldEffects); PlayerPrefs.Save();
            File.WriteAllText(Path.Combine(output, "verification.txt"), string.Join("\n", checks) + "\n\nRuntime errors:\n" + string.Join("\n", errors));
            Debug.Log("BIGMONSTER_VERIFICATION_" + (errors.Count == 0 ? "PASSED" : "FAILED") + " checks=" + checks.Count + " errors=" + errors.Count);
            SessionState.SetBool("BigMonster.Verifying", false);
            EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }
    }
}
#endif
