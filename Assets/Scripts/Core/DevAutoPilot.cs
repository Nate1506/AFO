using System.Collections;
using System.Collections.Generic;
using SayItAgain.Net;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>
    /// Debug helper: walks the village rig through a plan (gathering point, the planned road, the stairs
    /// the plan allows, the hilltop) and calls the villagers on the way, so a round can be tested without
    /// playing it by hand. Uses the RoutePath / HazardZone markers placed in the scene.
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
            foreach (var g in FindObjectsByType<GatherPoint>(FindObjectsSortMode.None))
                if (g.optionIndex == plan.gather) points.Add(g.transform.position);
            foreach (var r in FindObjectsByType<RoutePath>(FindObjectsSortMode.None))
                if (r.optionIndex == plan.route) points.AddRange(r.points);

            // The stairs the plan allows that are closest to where the road ends.
            var end = points[points.Count - 1];
            HazardZone stairs = null;
            foreach (var z in HazardZone.All)
                if (z.rule == HazardZone.Rule.IfAvoidedCrossing && z.optionIndex != plan.avoid
                    && (stairs == null || Vector3.Distance(z.transform.position, end) < Vector3.Distance(stairs.transform.position, end)))
                    stairs = z;
            if (stairs != null) points.Add(stairs.transform.position);
            points.Add(SafeZone.Instance.transform.position);

            var cc = GetComponent<CharacterController>();
            for (int i = 0; i < points.Count; i++)
            {
                float stuck = 0f;
                while (true)
                {
                    var to = points[i] - transform.position;
                    to.y = 0;
                    if (to.magnitude < 1.5f || stuck > 25f) break;
                    transform.rotation = Quaternion.LookRotation(to);
                    cc.Move(to.normalized * speed * Time.deltaTime + Vector3.down * 4f * Time.deltaTime);
                    stuck += Time.deltaTime;
                    yield return null;
                }
                if (i == 0)
                {
                    VillageDirector.Instance.CallVillagersNear(transform);
                    yield return new WaitForSeconds(1f);
                }
            }
            yield return new WaitForSeconds(10f);
            Done = true;
        }
    }
}
