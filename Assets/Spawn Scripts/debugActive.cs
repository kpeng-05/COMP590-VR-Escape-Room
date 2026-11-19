using UnityEngine;

public class ObjectActivator : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;

    private void Start()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }
    }
}
