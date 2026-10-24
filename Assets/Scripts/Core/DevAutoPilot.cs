using System.Collections;
using System.Collections.Generic;
using SayItAgain.Net;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>
    /// Debug helper: walks the village rig through a plan (gather point, crossing, causeway, hill)
    /// and calls the villagers on the way, so a round can be tested without playing it by hand.
    /// </summary>
    public sealed class DevAutoPilot : MonoBehaviour
    {
        public float speed = 6f;
        public bool Done { get; private set; }

        public static DevAutoPilot Run(RelayCard plan)
        {
            var rig = GameplayBootstrap.LocalRig;
            var pilot = rig.AddComponent<DevAutoPilot>();
            pilot.StartCoroutine(pilot.Walk(plan));
            return pilot;
        }

        IEnumerator Walk(RelayCard plan)
        {
            var points = new List<Vector3>();
            GatherPoint gather = null;
            foreach (var g in FindObjectsByType<GatherPoint>(FindObjectsSortMode.None)) if (g.optionIndex == plan.gather) gather = g;
            HazardZone crossing = null, causeway = null;
            foreach (var z in HazardZone.All)
            {
                // Take the first crossing the plan does not forbid.
                if (z.rule == HazardZone.Rule.IfAvoidedCrossing && z.optionIndex != plan.avoid && crossing == null) crossing = z;
                if (z.rule == HazardZone.Rule.UnlessSafeRoute && z.optionIndex == plan.route) causeway = z;
            }

            points.Add(gather.transform.position);
            var c = crossing.transform.position;
            points.Add(new Vector3(c.x, 0, -2));
            points.Add(new Vector3(c.x, 0, 18));
            var w = causeway.transform.position;
            points.Add(new Vector3(w.x, 0, 18));
            points.Add(new Vector3(w.x, 0, 106));
            var top = SafeZone.Instance.transform.position;
            points.Add(new Vector3(Mathf.Min(w.x, top.x - 45f), 0, 106));
            points.Add(new Vector3(top.x, 0, top.z));

            var cc = GetComponent<CharacterController>();
            for (int i = 0; i < points.Count; i++)
            {
                while (true)
                {
                    var to = points[i] - transform.position;
                    to.y = 0;
                    if (to.magnitude < 1.2f) break;
                    transform.rotation = Quaternion.LookRotation(to);
                    cc.Move(to.normalized * speed * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
                    yield return null;
                }
                if (i == 0)
                {
                    VillageDirector.Instance.CallVillagersNear(transform);
                    yield return new WaitForSeconds(1f);
                }
            }
            // Wait for stragglers on the hill.
            yield return new WaitForSeconds(8f);
            Done = true;
        }
    }
}
