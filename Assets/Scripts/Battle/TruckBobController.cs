using UnityEngine;

public class TruckBobController : MonoBehaviour
{
    [Header("Truck Parts")]
    [SerializeField] private RectTransform rearPart;
    [SerializeField] private RectTransform midPart;
    [SerializeField] private RectTransform frontPart;

    [Header("Bump Settings")]
    [SerializeField] private float bumpInterval = 2.5f;   // 何秒に1回揺れるか
    [SerializeField] private float bumpDuration = 0.35f;  // 揺れている時間
    [SerializeField] private float amplitude = 6f;        // 上下の大きさ
    [SerializeField] private float shakeSpeed = 3f;       // 揺れ自体の速さ

    [Header("Phase Offset")]
    [SerializeField] private float rearPhase = 0f;
    [SerializeField] private float midPhase = 0.15f;
    [SerializeField] private float frontPhase = 0.3f;

    [Header("Part Strength")]
    [SerializeField] private float rearStrength = 1.0f;
    [SerializeField] private float midStrength = 0.6f;
    [SerializeField] private float frontStrength = 0.8f;

    private Vector2 rearBasePosition;
    private Vector2 midBasePosition;
    private Vector2 frontBasePosition;

    private float timer;
    private float bumpTimer;
    private bool isBumping;

    private void Awake()
    {
        if (rearPart != null)
        {
            rearBasePosition = rearPart.anchoredPosition;
        }

        if (midPart != null)
        {
            midBasePosition = midPart.anchoredPosition;
        }

        if (frontPart != null)
        {
            frontBasePosition = frontPart.anchoredPosition;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!isBumping && timer >= bumpInterval)
        {
            timer = 0f;
            bumpTimer = 0f;
            isBumping = true;
        }

        if (isBumping)
        {
            bumpTimer += Time.deltaTime;

            float normalizedTime = bumpTimer / bumpDuration;

            if (normalizedTime >= 1f)
            {
                isBumping = false;
                ResetParts();
                return;
            }

            float fade = 1f - normalizedTime;

            MovePart(rearPart, rearBasePosition, rearPhase, rearStrength, bumpTimer, fade);
            MovePart(midPart, midBasePosition, midPhase, midStrength, bumpTimer, fade);
            MovePart(frontPart, frontBasePosition, frontPhase, frontStrength, bumpTimer, fade);
        }
    }

    private void MovePart(
        RectTransform part,
        Vector2 basePosition,
        float phase,
        float strength,
        float time,
        float fade
    )
    {
        if (part == null)
        {
            return;
        }

        float y = Mathf.Sin((time * shakeSpeed + phase) * Mathf.PI * 2f)
                  * amplitude
                  * strength
                  * fade;

        part.anchoredPosition = basePosition + new Vector2(0f, y);
    }

    private void ResetParts()
    {
        if (rearPart != null)
        {
            rearPart.anchoredPosition = rearBasePosition;
        }

        if (midPart != null)
        {
            midPart.anchoredPosition = midBasePosition;
        }

        if (frontPart != null)
        {
            frontPart.anchoredPosition = frontBasePosition;
        }
    }
}



