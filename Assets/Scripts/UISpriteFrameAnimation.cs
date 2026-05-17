using UnityEngine;
using UnityEngine.UI;

public class UISpriteFrameAnimation : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Frames")]
    [SerializeField] private Sprite[] frames;

    [Header("Playback")]
    [SerializeField] private float frameRate = 6f;
    [SerializeField] private bool pingPong = true;
    [SerializeField] private bool playOnAwake = true;

    private int currentIndex;
    private int direction = 1;
    private float timer;
    private bool isPlaying;

    private void Reset()
    {
        targetImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        isPlaying = playOnAwake;

        if (frames != null && frames.Length > 0 && targetImage != null)
        {
            targetImage.sprite = frames[0];
        }
    }

    private void Update()
    {
        if (!isPlaying)
        {
            return;
        }

        if (targetImage == null || frames == null || frames.Length == 0)
        {
            return;
        }

        if (frameRate <= 0f)
        {
            return;
        }

        timer += Time.deltaTime;
        float interval = 1f / frameRate;

        while (timer >= interval)
        {
            timer -= interval;
            AdvanceFrame();
        }
    }

    private void AdvanceFrame()
    {
        if (frames.Length == 1)
        {
            targetImage.sprite = frames[0];
            return;
        }

        currentIndex += direction;

        if (pingPong)
        {
            if (currentIndex >= frames.Length)
            {
                currentIndex = frames.Length - 2;
                direction = -1;
            }
            else if (currentIndex < 0)
            {
                currentIndex = 1;
                direction = 1;
            }
        }
        else
        {
            if (currentIndex >= frames.Length)
            {
                currentIndex = 0;
            }
        }

        targetImage.sprite = frames[currentIndex];
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void ResetToFirstFrame()
    {
        currentIndex = 0;
        direction = 1;
        timer = 0f;

        if (targetImage != null && frames != null && frames.Length > 0)
        {
            targetImage.sprite = frames[0];
        }
    }
}