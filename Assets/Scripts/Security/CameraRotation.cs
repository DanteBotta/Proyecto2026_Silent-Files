using System;
using UnityEngine;

/// <summary>Oscillates around the initial local orientation without accumulating rotation error.</summary>
[DisallowMultipleComponent]
public class CameraRotation : MonoBehaviour
{
    [Header("Yaw offsets from the initial orientation (degrees)")]
    [SerializeField] private float minimumAngle = -45f;
    [SerializeField] private float maximumAngle = 45f;
    [SerializeField, Min(0f)] private float rotationSpeed = 30f;
    [SerializeField] private bool startTowardsMaximum = true;

    private Quaternion baseLocalRotation;
    private double phase;
    private float previousMinimum;
    private float previousMaximum;
    private bool movingTowardsMaximum;
    private bool initialized;

    public float CurrentYawOffset { get; private set; }

    private void Awake()
    {
        InitializeRotation();
    }

    private void InitializeRotation()
    {
        if (initialized)
            return;

        initialized = true;
        baseLocalRotation = transform.localRotation;
        movingTowardsMaximum = startTowardsMaximum;
        previousMinimum = Mathf.Min(minimumAngle, maximumAngle);
        previousMaximum = Mathf.Max(minimumAngle, maximumAngle);
        CurrentYawOffset = Mathf.Clamp(0f, previousMinimum, previousMaximum);
        SetPhaseFromCurrentAngle(previousMinimum, previousMaximum);
        ApplyRotation();
    }

    private void Update()
    {
        AdvanceRotation(Time.deltaTime);
    }

    private void AdvanceRotation(float deltaTime)
    {
        InitializeRotation();

        float minimum = Mathf.Min(minimumAngle, maximumAngle);
        float maximum = Mathf.Max(minimumAngle, maximumAngle);
        double span = (double)maximum - minimum;

        // Runtime Inspector edits keep the current position/direction when possible.
        if (minimum != previousMinimum || maximum != previousMaximum)
        {
            CurrentYawOffset = Mathf.Clamp(CurrentYawOffset, minimum, maximum);
            SetPhaseFromCurrentAngle(minimum, maximum);
            previousMinimum = minimum;
            previousMaximum = maximum;
        }

        if (span <= 0d)
        {
            phase = 0d;
            CurrentYawOffset = minimum;
        }
        else
        {
            // Modulo preserves overshoot, including frames crossing several endpoints.
            double period = span * 2d;
            phase = (phase + Math.Max(0d, rotationSpeed) * Math.Max(0d, deltaTime)) % period;
            movingTowardsMaximum = phase < span;
            CurrentYawOffset = (float)(minimum + (movingTowardsMaximum ? phase : period - phase));
        }

        ApplyRotation();
    }

    private void SetPhaseFromCurrentAngle(float minimum, float maximum)
    {
        double span = (double)maximum - minimum;
        double offset = (double)CurrentYawOffset - minimum;
        phase = movingTowardsMaximum ? offset : span * 2d - offset;
    }

    private void ApplyRotation()
    {
        // Parent-space yaw preserves the pitch/roll in the authored local rotation.
        transform.localRotation = Quaternion.AngleAxis(CurrentYawOffset, Vector3.up) * baseLocalRotation;
    }
}
