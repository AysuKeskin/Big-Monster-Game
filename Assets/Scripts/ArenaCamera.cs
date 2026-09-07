using UnityEngine;
namespace BigMonster
{
    public sealed class ArenaCamera : MonoBehaviour
    {
        public ArenaGame game;
        public float shake;
        Camera view;
        void Awake() { view = GetComponent<Camera>(); }
        void LateUpdate()
        {
            float aspect = Mathf.Max(.5f, view.aspect);
            bool menu = game != null && game.State == ArenaGame.GameState.Menu;
            view.orthographicSize = Mathf.Max(menu ? 15 : 13.8f, 13.3f / aspect);
            Vector3 focus = Vector3.zero;
            if (!menu && game != null && game.player != null) focus += game.player.transform.position * .055f;
            Vector3 desired = focus + new Vector3(0, 27, -22);
            transform.position = Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-5 * Time.unscaledDeltaTime));
            transform.rotation = Quaternion.Euler(51, 0, 0);
            if (shake > 0) { transform.position += Random.insideUnitSphere * shake; shake = Mathf.Max(0, shake - Time.unscaledDeltaTime); }
        }
    }
}
