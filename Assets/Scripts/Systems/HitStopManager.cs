using System.Collections;
using UnityEngine;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager Instance;

    public float hitStopDuration = 0.07f;

    private Coroutine hitStopCoroutine;

    void Awake()
    {
        Instance = this;
    }

    public void HitStop()
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }

        hitStopCoroutine = StartCoroutine(HitStopCoroutine());
    }

    IEnumerator HitStopCoroutine()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = 1f;

        hitStopCoroutine = null;
    }
}