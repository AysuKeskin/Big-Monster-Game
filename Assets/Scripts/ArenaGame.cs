using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigMonster
{
    [RequireComponent(typeof(ArenaHUD))]
    public sealed class ArenaGame : MonoBehaviour
    {
        public enum GameState { Menu, Playing, Paused, RoundComplete, Won, Lost }
        public GameState State { get; private set; }
        public bool IsPlaying => State == GameState.Playing;
        public int Round { get; private set; } = 1;
        public int Collected { get; private set; }
        public int Health { get; private set; } = 3;
        public int Score { get; private set; }
        public float Elapsed { get; private set; }
        public int Best => PlayerPrefs.GetInt("BigMonster.Best", 0);
        [Header("Characters and pickups")]
        public PlayerMotor player;
        public ArenaChaser enemy;
        public ArenaPickup[] pickups;

        [Header("Exit")]
        public Transform portal;
        public Renderer portalRing;
        public Material portalLocked, portalOpen;

        [Header("Audio sources in the scene")]
        [Tooltip("The Music object under Game Manager. Plays the looping background track.")]
        public AudioSource music;
        [Tooltip("The Sound Effects object under Game Manager. Plays event clips with PlayOneShot.")]
        public AudioSource soundEffects;

        [Header("Sound clips - drag audio files here")]
        [Tooltip("Played when the player collects a cube.")]
        public AudioClip collectSound;
        [Tooltip("Played when the monster damages the player.")]
        public AudioClip hitSound;
        [Tooltip("Played once when a round is completed.")]
        public AudioClip winSound;
        [Tooltip("Played when the player dashes.")]
        public AudioClip dashSound;

        [Header("Visual effects")]
        public ParticleSystem collectFX, hitFX;
        [Tooltip("Optional extra particle effect for completing a round.")]
        public ParticleSystem winFX;
        public ArenaCamera arenaCamera;
        public TrailRenderer playerTrail;

        ArenaHUD hud;
        Vector3 spawn;
        Rigidbody[] dynamicBodies;
        Vector3[] dynamicPositions;
        Quaternion[] dynamicRotations;
        float immuneUntil, lastCollect = -99, toastUntil;
        int combo;
        string toast = "";
        public string Toast => Time.unscaledTime < toastUntil ? toast : "";
        public bool MusicMuted { get; private set; }
        public bool SoundEffectsMuted { get; private set; }
        public float Volume { get; private set; } = 1f;
        public bool ReducedEffects { get; private set; }
        public bool Invulnerable => Time.time < immuneUntil;
        public bool PortalReady => pickups != null && Collected >= pickups.Length;
        void Awake()
        {
            Time.timeScale = 1;
            Application.targetFrameRate = 120;
            State = GameState.Menu;
            spawn = player.transform.position;
            // Both audio sources are saved in the scene and assigned in the Inspector.
            if (soundEffects == null)
            {
                Debug.LogError("Assign Game Manager > Sound Effects to the Sound Effects field.", this);
                enabled = false;
                return;
            }
            soundEffects.playOnAwake = false;
            int previousMute = PlayerPrefs.GetInt("BigMonster.Muted", 0);
            MusicMuted = PlayerPrefs.GetInt("BigMonster.MusicMuted", previousMute) == 1;
            SoundEffectsMuted = PlayerPrefs.GetInt("BigMonster.SoundEffectsMuted", previousMute) == 1;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat("BigMonster.Volume", 1f));
            ReducedEffects = PlayerPrefs.GetInt("BigMonster.ReducedEffects", 0) == 1;
            var bodies = new System.Collections.Generic.List<Rigidbody>();
            foreach (var body in FindObjectsByType<Rigidbody>()) if (body != player.GetComponent<Rigidbody>()) bodies.Add(body);
            dynamicBodies = bodies.ToArray(); dynamicPositions = new Vector3[bodies.Count]; dynamicRotations = new Quaternion[bodies.Count];
            for (int i = 0; i < bodies.Count; i++) { dynamicPositions[i] = bodies[i].position; dynamicRotations[i] = bodies[i].rotation; }
            foreach (var pickup in pickups) pickup.home = pickup.transform.position;
        }
        void Start()
        {
            PrepareInterface();
            ApplySound(); ApplyEffects(); Freeze(true); UpdatePortal();
            if (music != null && !music.isPlaying) music.Play();
        }
        public void PrepareInterface()
        {
            hud = GetComponent<ArenaHUD>();
            hud.Initialize(this);
        }
        void Update()
        {
            var kb = Keyboard.current; var pad = Gamepad.current;
            bool pausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);
            if (pausePressed && hud != null) hud.ToggleMenu();
            if (kb != null && kb.rKey.wasPressedThisFrame && State == GameState.Paused) Restart();
            if (kb != null && kb.enterKey.wasPressedThisFrame && (UnityEngine.EventSystems.EventSystem.current == null || UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null))
            {
                if (State == GameState.Menu || State == GameState.Won || State == GameState.Lost) Restart();
                else if (State == GameState.RoundComplete) NextRound();
                else if (State == GameState.Paused) TogglePause();
            }
            if (IsPlaying)
            {
                Elapsed += Time.deltaTime;
                if (PortalReady && Vector3.Distance(new Vector3(player.transform.position.x, 0, player.transform.position.z), new Vector3(portal.position.x, 0, portal.position.z)) < 1.35f) FinishRound();
                if (playerTrail != null) playerTrail.emitting = player.Body.linearVelocity.sqrMagnitude > .6f;
            }
            if (portalRing != null) portalRing.transform.Rotate(0, 0, (PortalReady ? 28 : 8) * Time.deltaTime, Space.Self);
            if (hud != null) hud.Refresh();
        }
        public void Restart()
        {
            Round = 1; Health = 3; Score = 0; Elapsed = 0;
            ResetRound(); Notify("Collect the cubes, then reach the exit.", 4);
        }
        public void NextRound()
        {
            if (State != GameState.RoundComplete) return;
            Round++; Health = Mathf.Min(3, Health + 1); ResetRound();
            Notify("Round " + Round, 2);
        }
        void ResetRound()
        {
            Time.timeScale = 1; Collected = 0; combo = 0; lastCollect = -99;
            Freeze(false); player.ResetAt(spawn);
            for (int i = 0; i < dynamicBodies.Length; i++)
            {
                var body = dynamicBodies[i]; if (body == null) continue;
                body.position = dynamicPositions[i]; body.rotation = dynamicRotations[i];
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            }
            foreach (var pickup in pickups) pickup.ResetPickup();
            enemy.ResetEnemy(); immuneUntil = Time.time + 2;
            if (playerTrail != null) playerTrail.Clear();
            State = GameState.Playing; UpdatePortal(); hud.ShowState();
        }
        public void Collect(ArenaPickup pickup)
        {
            if (!IsPlaying || pickup.collected) return;
            pickup.collected = true; pickup.gameObject.SetActive(false);
            combo = Time.time - lastCollect < 6 ? Mathf.Min(combo + 1, 4) : 1;
            lastCollect = Time.time; Collected++; Score += 100 * combo;
            PlayFX(collectFX, pickup.home); Play(collectSound, 1 + .09f * Collected);
            if (PortalReady) { UpdatePortal(); Notify("Exit open!", 4); }
        }
        public void Hit(Vector3 source)
        {
            if (!IsPlaying || Invulnerable || player.Dashing) return;
            Health--; immuneUntil = Time.time + 2;
            PlayFX(hitFX, player.transform.position); Play(hitSound, 1);
            if (!ReducedEffects) arenaCamera.shake = .15f;
            Vector3 away = player.transform.position - source; away.y = 0;
            player.Body.AddForce(away.normalized * 5, ForceMode.VelocityChange);
            Notify("Hit!", 1.5f);
            if (Health <= 0) End(GameState.Lost);
        }
        public void Fell()
        {
            if (!IsPlaying) return;
            immuneUntil = 0; Hit(player.transform.position + Vector3.forward);
            if (IsPlaying) player.ResetAt(spawn);
        }
        void FinishRound()
        {
            Score += 250 + Health * 100;
            PlayFX(winFX, portal.position); Play(winSound, 1);
            if (Round >= 3) { Score += Mathf.Max(0, 900 - Mathf.FloorToInt(Elapsed * 3)); End(GameState.Won); }
            else End(GameState.RoundComplete);
        }
        void End(GameState state)
        {
            State = state; Freeze(true);
            if (state == GameState.Won || state == GameState.Lost)
            {
                if (Score > Best) PlayerPrefs.SetInt("BigMonster.Best", Score);
                PlayerPrefs.Save();
            }
            hud.ShowState();
        }
        void Freeze(bool frozen)
        {
            if (player.Body == null) return;
            player.Body.isKinematic = frozen;
            if (playerTrail != null) playerTrail.emitting = false;
        }
        public void TogglePause()
        {
            if (State == GameState.Playing) { State = GameState.Paused; Time.timeScale = 0; }
            else if (State == GameState.Paused) { State = GameState.Playing; Time.timeScale = 1; }
            else return;
            hud.ShowState();
        }
        public void Menu()
        {
            Time.timeScale = 1; State = GameState.Menu; Freeze(true); hud.ShowState();
        }
        public void ToggleMusic()
        {
            MusicMuted = !MusicMuted;
            PlayerPrefs.SetInt("BigMonster.MusicMuted", MusicMuted ? 1 : 0);
            PlayerPrefs.Save(); ApplySound(); hud.RefreshSettings();
        }
        public void ToggleSoundEffects()
        {
            SoundEffectsMuted = !SoundEffectsMuted;
            PlayerPrefs.SetInt("BigMonster.SoundEffectsMuted", SoundEffectsMuted ? 1 : 0);
            PlayerPrefs.Save(); ApplySound(); hud.RefreshSettings();
        }
        public void SetVolume(float value)
        {
            Volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("BigMonster.Volume", Volume);
            ApplySound();
            if (hud != null) hud.RefreshSettings();
        }
        public void SaveSettings() { PlayerPrefs.Save(); }
        void ApplySound()
        {
            if (music != null) { music.mute = MusicMuted; music.volume = .23f * Volume; }
            soundEffects.mute = SoundEffectsMuted; soundEffects.volume = .65f * Volume;
        }
        public void Quit()
        {
            SaveSettings(); Time.timeScale = 1;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        public void ToggleEffects()
        {
            ReducedEffects = !ReducedEffects; PlayerPrefs.SetInt("BigMonster.ReducedEffects", ReducedEffects ? 1 : 0); PlayerPrefs.Save(); ApplyEffects(); hud.RefreshSettings();
        }
        void ApplyEffects()
        {
            // Reduced mode keeps gameplay feedback, with a shorter movement trail.
            if (playerTrail != null)
            {
                playerTrail.enabled = true;
                playerTrail.time = ReducedEffects ? .22f : .42f;
                playerTrail.emitting = IsPlaying && player.Body != null && player.Body.linearVelocity.sqrMagnitude > .6f;
            }
            var data = arenaCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (data != null) data.renderPostProcessing = !ReducedEffects;
            if (ReducedEffects)
            {
                arenaCamera.shake = 0;

            }
        }
        void UpdatePortal() { if (portalRing != null) portalRing.sharedMaterial = PortalReady ? portalOpen : portalLocked; }
        public void Dash() { Play(dashSound, 1); }
        void Play(AudioClip clip, float pitch)
        {
            if (clip == null) return;
            soundEffects.pitch = pitch;
            soundEffects.PlayOneShot(clip);
        }
        void PlayFX(ParticleSystem fx, Vector3 point)
        {
            if (fx == null) return;
            fx.gameObject.SetActive(true);
            // Restart the burst for every new event, including consecutive hits or pickups.
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fx.transform.position = point;
            fx.Play(true);
        }
        void Notify(string message, float seconds) { toast = message; toastUntil = Time.unscaledTime + seconds; }
        void OnApplicationFocus(bool focus) { if (!focus && IsPlaying) TogglePause(); }
        void OnDestroy() { Time.timeScale = 1; PlayerPrefs.Save(); }
    }
}
