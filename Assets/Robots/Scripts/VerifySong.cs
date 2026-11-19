using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Robot "code" controller. Stores the last three robots hit, in hit order
/// (index = order of interaction, value = robot's Unity order number; repeats allowed).
/// The list is judged only when it holds three entries: correct = SpawnObject (the lightsaber), wrong = cleared.
/// </summary>
public class VerifySong : MonoBehaviour
{
    [SerializeField] private UnityEvent SpawnObject;
    [SerializeField] private int[] solution = { 3, 2, 1 };

    private int[] entered;
    private int count;
    private bool solved;

    public bool Solved => solved;

    private void Awake()
    {
        entered = new int[solution.Length];
    }

    // Wired to every RobotSing.robotPlayed event.
    public void ReceiveRobotOrder(int order)
    {
        if (solved) return;

        entered[count++] = order;
        if (count < entered.Length) return;

        bool correct = true;
        for (int i = 0; i < entered.Length; i++)
        {
            if (entered[i] != solution[i]) { correct = false; break; }
        }

        if (correct)
        {
            solved = true;
            SpawnObject?.Invoke();
        }
        Clear();
    }

    public void Clear()
    {
        count = 0;
        Array.Clear(entered, 0, entered.Length);
    }
}
