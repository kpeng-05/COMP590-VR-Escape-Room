using UnityEngine;
using UnityEngine.Events;

public class VerifySong : MonoBehaviour
{
    [SerializeField] private UnityEvent SpawnObject;

    private int lastToSing;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastToSing = 0;
    }

    public void ReceiveRobotOrder(int order)
    {
        if (order == lastToSing + 1)
        {
            lastToSing = order;
            if (lastToSing == 3)
            {
                SpawnObject.Invoke();
                enabled = false;
            }
        }
        else
        {
            lastToSing = 0;
        }
    }
}