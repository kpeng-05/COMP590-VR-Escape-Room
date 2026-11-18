using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject knight;
    [SerializeField] private GameObject lightsaber;
    [SerializeField] private GameObject baton;

    void Start()
    {
        knight.SetActive(false);
        lightsaber.SetActive(false);
        baton.SetActive(false);
    }

    public void SpawnKnight()
    {
        if (knight == null) return;
        knight.SetActive(true);
    }

    public void SpawnLightsaber()
    {
        if (lightsaber == null) return;
        lightsaber.SetActive(true);
    }

    public void SpawnBaton()
    {
        if (baton == null) return;
        baton.SetActive(true);
    }
}