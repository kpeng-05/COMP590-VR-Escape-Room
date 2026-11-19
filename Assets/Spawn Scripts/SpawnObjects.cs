using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [SerializeField] private GameObject knight;
    [SerializeField] private GameObject lightsaber;
    [SerializeField] private GameObject baton;

    void Start()
    {
        if (knight != null) knight.SetActive(false);
        if (lightsaber != null) lightsaber.SetActive(false);
        if (baton != null) baton.SetActive(false);
    }

    public void SpawnKnight()
    {
        if (knight == null || knight.activeSelf) return;
        knight.SetActive(true);
    }

    public void SpawnLightsaber()
    {
        // Only spawn once, even if the robot code is solved again.
        if (lightsaber == null || lightsaber.activeSelf) return;
        lightsaber.SetActive(true);
    }

    public void SpawnBaton()
    {
        if (baton == null || baton.activeSelf) return;
        baton.SetActive(true);
    }
}
