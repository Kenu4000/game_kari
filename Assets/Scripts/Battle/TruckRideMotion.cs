using UnityEngine;

public class TruckRideMotion : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform truckRoot;

    [Header("Small Constant Motion")]
    [SerializeField] private float verticalAmplitude = 1.5f;
    [SerializeField] private float verticalSpeed = 9f;

    [Header("Tiny Rotation")]
    [SerializeField] private float rotateAmount = 0.2f;
    [SerializeField] private float rotateSpeed = 7f;

    private Vector2 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        if (truckRoot == null)
        {
            truckRoot = GetComponent<RectTransform>();
        }

        if (truckRoot != null)
        {
            basePosition = truckRoot.anchoredPosition;
            baseRotation = truckRoot.localRotation;
        }
    }

    private void Update()
    {
        if (truckRoot == null)
        {
            return;
        }

        float t = Time.time;

        float y = Mathf.Sin(t * verticalSpeed) * verticalAmplitude;
        float rot = Mathf.Sin(t * rotateSpeed) * rotateAmount;

        truckRoot.anchoredPosition = basePosition + new Vector2(0f, y);
        truckRoot.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rot);
    }
}