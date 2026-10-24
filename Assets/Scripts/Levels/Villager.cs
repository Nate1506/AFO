using UnityEngine;
using UnityEngine.AI;

namespace SayItAgain
{
    /// <summary>A villager the village player leads to the hill. Only simulated on the village player's machine.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Villager : MonoBehaviour
    {
        public enum State { Waiting, Following, Swept, Safe }

        public State Current { get; private set; } = State.Waiting;
        public float followDistance = 2.5f;

        NavMeshAgent agent;
        Transform leader;
        Vector3 offset;
        Animator animator;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            offset = Random.insideUnitSphere * 2f;
            offset.y = 0f;
            Vary();
        }

        static readonly Color[] Clothes =
        {
            new Color(0.75f, 0.35f, 0.25f), new Color(0.3f, 0.45f, 0.7f), new Color(0.45f, 0.6f, 0.35f),
            new Color(0.8f, 0.7f, 0.4f), new Color(0.55f, 0.4f, 0.6f), new Color(0.6f, 0.6f, 0.6f),
        };

        /// <summary>Adults, elders (slower, grey) and children (small, quick) from one model.</summary>
        void Vary()
        {
            float r = Random.value;
            var color = Clothes[Random.Range(0, Clothes.Length)];
            if (r < 0.25f) { transform.localScale *= 0.65f; agent.speed *= 1.1f; }
            else if (r < 0.45f) { agent.speed *= 0.75f; color = Color.Lerp(color, Color.grey, 0.5f); }
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            foreach (var rend in GetComponentsInChildren<Renderer>()) rend.SetPropertyBlock(block);
        }

        public void Follow(Transform target)
        {
            if (Current != State.Waiting) return;
            leader = target;
            Current = State.Following;
        }

        void Update()
        {
            if (Current == State.Following && leader != null && agent.isOnNavMesh)
            {
                var target = leader.position + offset - leader.forward * followDistance;
                if ((agent.destination - target).sqrMagnitude > 1f) agent.SetDestination(target);
            }

            if (Current == State.Following || Current == State.Waiting)
            {
                if (HazardZone.IsDeadlyAt(transform.position)) Sweep();
                else if (Current == State.Following && SafeZone.Instance != null && SafeZone.Instance.Contains(transform.position)) Arrive();
            }

            if (animator != null && agent.enabled) animator.SetFloat("Speed", agent.velocity.magnitude);
        }

        void Sweep()
        {
            Current = State.Swept;
            agent.enabled = false;
            if (animator != null) animator.SetTrigger("Swept");
            // Carried off by the current: drift and sink out of sight.
            StartCoroutine(Drift());
        }

        System.Collections.IEnumerator Drift()
        {
            var dir = new Vector3(1f, 0f, 0.3f).normalized;
            for (float t = 0; t < 3f; t += Time.deltaTime)
            {
                transform.position += (dir * 3f + Vector3.down * 0.6f) * Time.deltaTime;
                yield return null;
            }
            gameObject.SetActive(false);
        }

        void Arrive()
        {
            Current = State.Safe;
            agent.SetDestination(transform.position);
            if (animator != null) animator.SetTrigger("Celebrate");
        }
    }
}
