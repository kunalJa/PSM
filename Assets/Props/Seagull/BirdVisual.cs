using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Sits as a child of a BirdSlot (under the BirdOrbitRig). Handles fly-in from an
/// off-orbit offset to its slot's local origin, and fly-out (delivery) back to that
/// offset before hiding.
///
/// IsBusy is true while a fly-in or fly-out coroutine is running; the manager uses
/// this to avoid hiding/showing a bird that's mid-animation.
/// </summary>
public class BirdVisual : MonoBehaviour
{
    [Tooltip("Animator on the Seagull model (child). Auto-found if left empty.")]
    [SerializeField] private Animator birdAnimator;
    [Tooltip("Local offset (relative to the BirdSlot) that the bird flies in from / out to.")]
    [SerializeField] private Vector3 offscreenLocalOffset = new Vector3(0f, 8f, 0f);

    [Tooltip("Random variation added to the fly-in START position, per axis. Each component is a +/- range; a random value in that range is picked each flight.")]
    [SerializeField] private Vector3 flyInStartRandomness = Vector3.zero;

    [SerializeField] private float flyInDuration = 1.2f;
    [Tooltip("Easing for the fly-in: x is normalized time 0-1, y is progress 0-1. Steep start + shallow end = fast entry, gentle deceleration.")]
    [SerializeField] private AnimationCurve flyInEase = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 2.2f),   // steep out-tangent: fast start
        new Keyframe(1f, 1f, 0.35f, 0f)); // shallow in-tangent: slows but doesn't stop dead

    [Header("Break-away (phase 1)")]
    [Tooltip("Max turn rate (degrees/sec) while banking from the orbit heading toward the side of the screen. Lower = wider, lazier arc.")]
    [SerializeField] private float breakAwayTurnSpeed = 90f;
    [Tooltip("Seconds over which turning and acceleration ramp up from zero. Higher = the bird holds its orbit heading and speed for longer before banking away.")]
    [SerializeField] private float breakAwayRampTime = 0.8f;
    [Tooltip("How quickly the bird speeds up as it leaves (units/sec^2).")]
    [SerializeField] private float breakAwayAcceleration = 8f;

    [Header("Delivery pass (phase 2)")]
    [Tooltip("Camera used to place the delivery flight. Uses Camera.main if left empty.")]
    [SerializeField] private Camera flightCamera;
    [Tooltip("Seconds for the close pass across the screen. Lower = faster.")]
    [SerializeField] private float deliveryPassDuration = 1.4f;
    [Tooltip("Distance from the camera of the close pass. Smaller = bigger bird on screen.")]
    [SerializeField] private float passDistanceFromCamera = 4f;
    [Tooltip("Screen height of the close pass. 0 = bottom of screen, 1 = top.")]
    [Range(0f, 1f)]
    [SerializeField] private float passViewportHeight = 0.75f;
    [Tooltip("How far through the close pass (0-1) the envelope is dropped.")]
    [Range(0f, 1f)]
    [SerializeField] private float dropAtPassProgress = 0.5f;
    [Tooltip("How far past the screen edge the bird travels, as a fraction of screen width. Increase if the bird pops in/out while still visible.")]
    [SerializeField] private float offscreenViewportMargin = 0.25f;
    [Tooltip("Rotate the bird to face its direction of travel during the delivery flight.")]
    [SerializeField] private bool faceFlightDirection = true;
    [Tooltip("Rotation correction if the model's nose doesn't point along +Z. Only used when Face Flight Direction is on.")]
    [SerializeField] private Vector3 modelForwardEuler = Vector3.zero;

    
   

    public bool IsBusy { get; private set; }
    public bool IsVisible => gameObject.activeSelf;

    private Coroutine _activeRoutine;
    private Vector3 _homeLocalPosition; // the bird's natural slot position, captured from the scene
    private Vector3 _flyInStartPosition; // randomized start point, chosen once per fly-in
    private Quaternion _homeLocalRotation;
    private Vector3 _homeLocalScale;
    private Transform _slotParent;
    private bool _detached;
    private const float MaxBreakAwaySeconds = 6f;
    private float gapBeforeDeliveryPass = 0.4f; // seconds of off-screen time between phase 1 and phase 2

    void Awake()
    {
        // Capture wherever the designer placed the bird in the scene as its home position.
        // Fly-in and fly-out are computed as offsets from this, so each bird returns to
        // its own slot instead of all collapsing to (0,0,0).
        _homeLocalPosition = transform.localPosition;
        _homeLocalRotation = transform.localRotation;
        _homeLocalScale = transform.localScale;
        _slotParent = transform.parent;

        if (birdAnimator == null)
            birdAnimator = GetComponentInChildren<Animator>(true);

        // Start hidden; manager decides when to show.
        gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        if (_activeRoutine != null) { StopCoroutine(_activeRoutine); _activeRoutine = null; }
        IsBusy = false;
        gameObject.SetActive(false);
        ReturnToSlot();
    }

    private void ReturnToSlot()
    {
        if (_detached)
        {
            transform.SetParent(_slotParent, false);
            _detached = false;
        }
        transform.localPosition = _homeLocalPosition;
        transform.localRotation = _homeLocalRotation;
        transform.localScale = _homeLocalScale;
    }

    private void MoveTo(Vector3 worldPosition)
    {
        Vector3 delta = worldPosition - transform.position;
        transform.position = worldPosition;
        if (faceFlightDirection && delta.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.LookRotation(delta) * Quaternion.Euler(modelForwardEuler);
    }

    public void FlyIn()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        gameObject.SetActive(true);
        // Pick the randomized start point once so the initial position and the
        // coroutine's lerp start agree with each other.
        _flyInStartPosition = _homeLocalPosition + offscreenLocalOffset + new Vector3(
            UnityEngine.Random.Range(-flyInStartRandomness.x, flyInStartRandomness.x),
            UnityEngine.Random.Range(-flyInStartRandomness.y, flyInStartRandomness.y),
            UnityEngine.Random.Range(-flyInStartRandomness.z, flyInStartRandomness.z));
        transform.localPosition = _flyInStartPosition;
        if (birdAnimator != null)
            birdAnimator.Play(0, 0, UnityEngine.Random.value); // Random start position in the animation for variation
        _activeRoutine = StartCoroutine(FlyInRoutine());
    }

    /// <summary>
    /// Plays the delivery flight: the bird leaves the screen sideways, then makes a
    /// close pass across the screen. Invokes <paramref name="onDrop"/> part-way through
    /// the close pass (the envelope drop) and <paramref name="onComplete"/> once the
    /// bird is off-screen and hidden.
    /// </summary>
    public void FlyOut(Action onDrop = null, Action onComplete = null)
    {
        if (!gameObject.activeSelf) { onDrop?.Invoke(); onComplete?.Invoke(); return; }
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(FlyOutRoutine(onDrop, onComplete));
    }

    private IEnumerator FlyInRoutine()
    {
        IsBusy = true;
        float t = 0f;
        Vector3 start = _flyInStartPosition;
        Vector3 end = _homeLocalPosition;
        while (t < flyInDuration)
        {
            t += Time.deltaTime;
            float p = flyInEase.Evaluate(Mathf.Clamp01(t / flyInDuration));
            transform.localPosition = Vector3.LerpUnclamped(start, end, p);
            yield return null;
        }
        transform.localPosition = end;
        IsBusy = false;
        _activeRoutine = null;
    }

    private IEnumerator FlyOutRoutine(Action onDrop, Action onComplete)
    {
        IsBusy = true;
        bool dropped = false;
        Camera cam = flightCamera != null ? flightCamera : Camera.main;

        if (cam == null)
        {
            Debug.LogWarning("BirdVisual: no camera found for the delivery flight, skipping animation");
        }
        else
        {
            // Sample the bird's orbit velocity over one frame while it is still on the rig.
            Vector3 previousWorld = transform.position;
            yield return null;
            Vector3 velocity = Time.deltaTime > 0f ? (transform.position - previousWorld) / Time.deltaTime : Vector3.zero;

            // Leave the orbit rig so its rotation no longer bends the flight path.
            transform.SetParent(null, true);
            _detached = true;

            // Phase 1: carry on along the orbit heading at orbit speed, then gradually bank
            // toward the side of the screen the bird is already travelling to and fly off.
            Vector3 position = transform.position;
            Vector3 right = cam.transform.right;
            float speed = velocity.magnitude;
            float sideways = speed > 0.0001f ? Vector3.Dot(velocity / speed, right) : 0f;
            bool exitRight = Mathf.Abs(sideways) > 0.05f ? sideways > 0f : cam.WorldToViewportPoint(position).x >= 0.5f;
            Vector3 exitDirection = exitRight ? right : -right;
            Vector3 heading = speed > 0.0001f ? velocity / speed : exitDirection;
            float exitX = exitRight ? 1f + offscreenViewportMargin : -offscreenViewportMargin;

            float t = 0f;
            while (t < MaxBreakAwaySeconds)
            {
                t += Time.deltaTime;
                float ramp = Mathf.SmoothStep(0f, 1f, t / Mathf.Max(breakAwayRampTime, 0.0001f));
                heading = Vector3.RotateTowards(heading, exitDirection, breakAwayTurnSpeed * ramp * Mathf.Deg2Rad * Time.deltaTime, 0f);
                speed += breakAwayAcceleration * ramp * Time.deltaTime;
                position += heading * (speed * Time.deltaTime);
                transform.position = position;
                if (faceFlightDirection)
                {
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(heading) * Quaternion.Euler(modelForwardEuler),
                        breakAwayTurnSpeed * Time.deltaTime);
                }

                Vector3 viewport = cam.WorldToViewportPoint(position);
                if (viewport.z <= 0f || (exitRight ? viewport.x > exitX : viewport.x < exitX)) break;
                yield return null;
            }

            // Off-screen beat before the bird comes back for the delivery pass.
            if (gapBeforeDeliveryPass > 0f)
                yield return new WaitForSeconds(gapBeforeDeliveryPass);

            // Phase 2: come back across the screen close to the camera, dropping the envelope.
            float passToX = exitRight ? -offscreenViewportMargin : 1f + offscreenViewportMargin;
            t = 0f;
            while (t < deliveryPassDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / deliveryPassDuration);
                MoveTo(cam.ViewportToWorldPoint(new Vector3(Mathf.Lerp(exitX, passToX, p), passViewportHeight, passDistanceFromCamera)));
                if (!dropped && p >= dropAtPassProgress)
                {
                    dropped = true;
                    onDrop?.Invoke();
                }
                yield return null;
            }
        }

        if (!dropped) onDrop?.Invoke();

        IsBusy = false;
        _activeRoutine = null;
        gameObject.SetActive(false);
        ReturnToSlot();
        onComplete?.Invoke();
    }
}
