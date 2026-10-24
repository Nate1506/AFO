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
        VillageDirector director;
        float trailDistance;
        int trailIndex;
        Vector3 offset;
        Animator animator;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            Vary();
            animator = GetComponentInChildren<Animator>();
            offset = Random.insideUnitSphere * 2f;
            offset.y = 0f;
        }

        /// <summary>
        /// The prefab carries every character model, all hidden; show one at random.
        /// Some walk slower (elders) and some are small and quick (children).
        /// </summary>
        void Vary()
        {
            var models = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in transform)
                if (child.GetComponent<Animator>() != null) models.Add(child);
            if (models.Count > 0) models[Random.Range(0, models.Count)].gameObject.SetActive(true);
            float r = Random.value;
            if (r < 0.2f) { transform.localScale *= 0.7f; agent.speed *= 1.1f; }
            else if (r < 0.4f) agent.speed *= 0.75f;
        }

        public void Follow(VillageDirector director, int slot)
        {
            if (Current != State.Waiting) return;
            this.director = director;
            trailDistance = followDistance + slot * 1.3f;
            trailIndex = director.TrailEnd - 1;
            Current = State.Following;
        }

        void Update()
        {
            if (Current == State.Following && director != null && agent.isOnNavMesh)
            {
                // Walk the leader's own footsteps one by one, so a villager who falls behind still crosses
                // where the player crossed instead of cutting a corner through the water.
                int last = director.TrailEnd - 1 - Mathf.CeilToInt(trailDistance);
                var flat = director.TrailAt(trailIndex) - transform.position;
                flat.y = 0f;
                while (trailIndex < last && flat.sqrMagnitude < 9f)
                {
                    trailIndex++;
                    flat = director.TrailAt(trailIndex) - transform.position;
                    flat.y = 0f;
                }
                var target = (trailIndex < last ? director.TrailAt(Mathf.Min(trailIndex + 2, last)) : director.TrailPoint(trailDistance)) + offset * 0.4f;
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
