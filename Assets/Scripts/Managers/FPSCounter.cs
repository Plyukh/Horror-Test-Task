using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

[DisallowMultipleComponent]
public class FPSCounter : MonoBehaviour
{
    [Header("Update")]
    public float updateInterval = 0.5f;

    [Header("Display")]
    public Text uiText;

    [Tooltip("Show milliseconds per frame as well")]
    public bool showMs = true;

    [Header("Controls")]
    [Tooltip("Key to toggle visibility")]
    public KeyCode toggleKey = KeyCode.F1;
    public bool startVisible = true;

    // runtime
    float accum = 0f; // accumulated FPS over interval
    int frames = 0;
    float timeLeft;   // time left for current interval
    float currentFps = 0f;
    float currentMs = 0f;
    bool visible;

    void Awake()
    {
        timeLeft = updateInterval;
        visible = startVisible;
    }

    void Update()
    {
        // toggle
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;

        // accumulate
        float delta = Time.unscaledDeltaTime; // unscaled so UI or timeScale changes don't affect reading
        frames++;
        accum += (delta > 0f) ? (1f / delta) : 0f;
        timeLeft -= delta;

        if (timeLeft <= 0f)
        {
            currentFps = (frames > 0) ? (accum / frames) : 0f;
            currentMs = (currentFps > 0f) ? (1000f / currentFps) : 0f;

            // reset
            timeLeft = updateInterval;
            accum = 0f;
            frames = 0;

            // update UI if needed
            if (visible)
            {
                if (uiText != null)
                {
                    uiText.text = FormatText(currentFps, currentMs);
                }
            }
        }
    }

    string FormatText(float fps, float ms)
    {
        if (showMs)
            return string.Format("{0:0.0} FPS\n{1:0.0} ms", fps, ms);
        else
            return string.Format("{0:0.0} FPS", fps);
    }

    void OnGUI()
    {
        if (!visible) return;

        if (uiText != null) return; // already shown by UI Text

        // Fallback: draw small label in top-left
        var style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 200, 50));
        GUILayout.Label(FormatText(currentFps, currentMs), style);
        GUILayout.EndArea();
    }

    // Public helpers
    public void Show() => visible = true;
    public void Hide() => visible = false;
    public void Toggle() => visible = !visible;
}
