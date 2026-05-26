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
    [Tooltip("Local offset (relative to the BirdSlot) that the bird flies in from / out to.")]
    [SerializeField] private Vector3 offscreenLocalOffset = new Vector3(0f, 8f, 0f);

    [SerializeField] private float flyInDuration = 1.2f;
    [SerializeField] private float flyOutDuration = 0.9f;

    public bool IsBusy { get; private set; }
    public bool IsVisible => gameObject.activeSelf;

    private Coroutine _activeRoutine;
    private Vector3 _homeLocalPosition; // the bird's natural slot position, captured from the scene

    void Awake()
    {
        // Capture wherever the designer placed the bird in the scene as its home position.
        // Fly-in and fly-out are computed as offsets from this, so each bird returns to
        // its own slot instead of all collapsing to (0,0,0).
        _homeLocalPosition = transform.localPosition;

        // Start hidden; manager decides when to show.
        gameObject.SetActive(false);
    }

    public void HideImmediate()
    {
        if (_activeRoutine != null) { StopCoroutine(_activeRoutine); _activeRoutine = null; }
        IsBusy = false;
        gameObject.SetActive(false);
        transform.localPosition = _homeLocalPosition;
    }

    public void FlyIn()
    {
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        gameObject.SetActive(true);
        transform.localPosition = _homeLocalPosition + offscreenLocalOffset;
        _activeRoutine = StartCoroutine(FlyInRoutine());
    }

    /// <summary>
    /// Plays the delivery flight. Invokes <paramref name="onComplete"/> when the bird
    /// has finished animating off-screen, then hides itself.
    /// </summary>
    public void FlyOut(Action onComplete = null)
    {
        if (!gameObject.activeSelf) { onComplete?.Invoke(); return; }
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(FlyOutRoutine(onComplete));
    }

    private IEnumerator FlyInRoutine()
    {
        IsBusy = true;
        float t = 0f;
        Vector3 start = _homeLocalPosition + offscreenLocalOffset;
        Vector3 end = _homeLocalPosition;
        while (t < flyInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / flyInDuration));
            transform.localPosition = Vector3.LerpUnclamped(start, end, p);
            yield return null;
        }
        transform.localPosition = end;
        IsBusy = false;
        _activeRoutine = null;
    }

    private IEnumerator FlyOutRoutine(Action onComplete)
    {
        IsBusy = true;
        float t = 0f;
        Vector3 start = transform.localPosition;
        // Fly out the opposite way from how birds enter (downward by default), so
        // delivery looks like the bird dropped toward the player.
        Vector3 end = _homeLocalPosition - offscreenLocalOffset;
        while (t < flyOutDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / flyOutDuration));
            transform.localPosition = Vector3.LerpUnclamped(start, end, p);
            yield return null;
        }
        IsBusy = false;
        _activeRoutine = null;
        gameObject.SetActive(false);
        transform.localPosition = _homeLocalPosition;
        onComplete?.Invoke();
    }
}
