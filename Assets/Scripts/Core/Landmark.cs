using System.Collections.Generic;
using UnityEngine;

namespace SayItAgain
{
    /// <summary>A named place the binoculars can identify. Names come from the localization table.</summary>
    public sealed class Landmark : MonoBehaviour
    {
        public static readonly List<Landmark> All = new List<Landmark>();

        public string nameKey = "lm_hill";
        [Tooltip("Optional point to aim at; defaults to this transform.")]
        public Transform focusPoint;

        public Vector3 FocusPosition => focusPoint != null ? focusPoint.position : transform.position;
        public string DisplayName => Loc.Get(nameKey);

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }
}
