using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SayItAgain
{
    /// <summary>Hold right mouse to zoom. While zoomed, reports the landmark closest to the view centre.</summary>
    public sealed class Binoculars : MonoBehaviour
    {
        public Camera viewCamera;
        public float zoomFov = 16f;
        public float zoomSpeed = 10f;
        public float focusAngle = 4f;
        public float maxDistance = 900f;

        public bool IsZoomed { get; private set; }
        /// <summary>0 = normal view, 1 = fully zoomed. Drives the overlay.</summary>
        public float ZoomAmount { get; private set; }
        public Landmark FocusedLandmark { get; private set; }
        public event Action<Landmark> FocusChanged;

        InputAction aim;

        void Awake()
        {
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
            aim = new InputAction("Binoculars", InputActionType.Button, "<Mouse>/rightButton");
            viewCamera.fieldOfView = ComfortSettings.FieldOfView;
        }

        void OnEnable() => aim.Enable();
        void OnDisable() => aim.Disable();
        void OnDestroy() => aim.Dispose();

        /// <summary>Menus call this so zoom drops when the player opens a card or pauses.</summary>
        public bool Blocked { get; set; }

        void Update()
        {
            IsZoomed = !Blocked && aim.IsPressed();
            float baseFov = ComfortSettings.FieldOfView;
            float target = IsZoomed ? zoomFov : baseFov;
            viewCamera.fieldOfView = ComfortSettings.ReduceMotion
                ? target
                : Mathf.Lerp(viewCamera.fieldOfView, target, 1f - Mathf.Exp(-zoomSpeed * Time.deltaTime));
            ZoomAmount = Mathf.InverseLerp(baseFov, zoomFov, viewCamera.fieldOfView);

            var focused = ZoomAmount > 0.8f ? FindFocused() : null;
            if (focused != FocusedLandmark)
            {
                FocusedLandmark = focused;
                FocusChanged?.Invoke(focused);
            }
        }

        Landmark FindFocused()
        {
            var origin = viewCamera.transform.position;
            var forward = viewCamera.transform.forward;
            Landmark best = null;
            float bestAngle = focusAngle;
            foreach (var lm in Landmark.All)
            {
                var to = lm.FocusPosition - origin;
                float dist = to.magnitude;
                if (dist > maxDistance) continue;
                float angle = Vector3.Angle(forward, to);
                if (angle > bestAngle) continue;
                if (Physics.Linecast(origin, lm.FocusPosition, out var hit, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(lm.transform))
                    continue;
                best = lm;
                bestAngle = angle;
            }
            return best;
        }
    }
}
