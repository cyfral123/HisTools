using UnityEngine;
using TMPro;

public class LookAtPlayer : MonoBehaviour
{
    public Transform player;
    public float minDistanceSqr = 0.1f;
    public Color textColor = Color.clear;

    private TextMeshPro tmp;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 lookDir = transform.position - player.position;

        if (lookDir.sqrMagnitude > minDistanceSqr)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
            if (textColor != Color.clear)
                tmp?.color = textColor;
        }
    }
}
