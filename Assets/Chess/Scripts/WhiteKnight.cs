using UnityEngine;
using UnityEngine.Events;

public class WhiteKnight : MonoBehaviour
{
    [SerializeField] private float targetX;
    [SerializeField] private float targetY;
    [SerializeField] private float targetZ;

    [SerializeField] private UnityEvent onCorrectPlacement;

    private Vector3 targetPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        targetPoint = new Vector3(targetX, targetY, targetZ);
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Vector3.Distance(targetPoint, transform.position);
        if (distance <= 0.1)
        {
            onCorrectPlacement?.Invoke();
        }
    }
}
