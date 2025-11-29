using DG.Tweening;
using UnityEngine;

public class MarkerActivator : MonoBehaviour
{
    public float bounceDuration = 0.25f;
    public float bounceStrength = 0.25f;

    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void ActivateMarker(Color targetColor)
    {
        transform.DOPunchScale(Vector3.one * bounceStrength, bounceDuration, 1, 0.5f);

        rend.material.DOColor(targetColor, bounceDuration);
    }
}
