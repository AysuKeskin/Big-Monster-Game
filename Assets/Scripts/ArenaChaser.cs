using UnityEngine;
using UnityEngine.AI;
namespace BigMonster
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ArenaChaser : MonoBehaviour
    {
        public ArenaGame game;
        public Vector3 home;
        NavMeshAgent agent;
        float nextPath;
        void Awake() { agent = GetComponent<NavMeshAgent>(); home = transform.position; }
        void Update()
        {
            if (game == null || !agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = !game.IsPlaying;
            if (game.IsPlaying && Time.time > nextPath)
            {
                nextPath = Time.time + .15f;
                agent.speed = 2.1f + .55f * game.Round;
                agent.SetDestination(game.player.transform.position);
            }
        }
        public void ResetEnemy()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (NavMesh.SamplePosition(home, out var hit, 3, NavMesh.AllAreas)) agent.Warp(hit.position);
            if (agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; }
        }
    }
}
