using UnityEngine;
using UnityEngine.Events;

public class VerifySong : MonoBehaviour
{
    [SerializeField] private UnityEvent SpawnObject;

    private int lastToSing;
    private bool solved;
    // "Yi Long Ma": stays true only while no wrong robot has been hit (including a wrong first robot).
    private bool solvedFirstTry = true;

    public bool Solved => solved;
    public bool SolvedFirstTry => solvedFirstTry;
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
                solved = true;
                SpawnObject.Invoke();
                enabled = false;
            }
        }
        else
        {
            if (!solved) solvedFirstTry = false; // hits after solving do not count
            lastToSing = 0;
        }
    }
}