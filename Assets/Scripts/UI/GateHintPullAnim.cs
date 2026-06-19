using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>pull_anim — arrow1→2→3 순차 점등 (corner 방향으로 당기라는 가이드).</summary>
[DisallowMultipleComponent]
public sealed class GateHintPullAnim : MonoBehaviour
{
    [SerializeField] private float stepDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.35f;

    private GameObject[] _arrows;
    private Coroutine _loop;

    private void Awake()
    {
        _arrows = new[]
        {
            transform.Find("arrow1")?.gameObject,
            transform.Find("arrow2")?.gameObject,
            transform.Find("arrow3")?.gameObject,
        };

        DisableRaycasts();
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
            return;

        Stop();
        SetAll(false);
        _loop = StartCoroutine(Loop());
    }

    public void Stop()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }

        SetAll(false);
    }

    private IEnumerator Loop()
    {
        var waitStep = new WaitForSecondsRealtime(stepDuration);
        var waitHold = new WaitForSecondsRealtime(holdDuration);

        while (true)
        {
            SetAll(false);
            yield return waitStep;

            for (var i = 0; i < _arrows.Length; i++)
            {
                if (_arrows[i] != null)
                    _arrows[i].SetActive(true);

                yield return waitStep;
            }

            yield return waitHold;
        }
    }

    private void SetAll(bool active)
    {
        foreach (var arrow in _arrows)
        {
            if (arrow != null)
                arrow.SetActive(active);
        }
    }

    private void DisableRaycasts()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }
}
