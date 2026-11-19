using System.Collections;
using UnityEngine;

/// <summary>
/// One 0.5 m piece of a cuttable wall. Only a lit lightsaber blade (BladeCollapse.IsOn) can cut it.
/// When cut it drops as physics debris, then disappears.
/// Uncuttable tiles (behind lamps and digits) ignore the blade.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WallTile : MonoBehaviour
{
    public const int DebrisLayer = 6; // "transparent": not a locomotion or teleport-blocker layer

    [SerializeField] private bool cuttable = true;
    [SerializeField] private WallBreachTracker tracker;
    [SerializeField] private int wallIndex;
    [SerializeField] private float debrisLifetime = 4f;

    private bool _cut;

    public bool Cuttable => cuttable;
    public bool IsCut => _cut;
    public int WallIndex => wallIndex;

    public void Configure(WallBreachTracker owner, int wall, bool canBeCut)
    {
        tracker = owner;
        wallIndex = wall;
        cuttable = canBeCut;
    }

    private void OnTriggerEnter(Collider other) => TryCut(other);
    private void OnTriggerStay(Collider other) => TryCut(other);

    private void TryCut(Collider other)
    {
        if (_cut || !cuttable) return;

        BladeCollapse saber = other.GetComponentInParent<BladeCollapse>();
        if (saber == null && other.attachedRigidbody != null)
        {
            saber = other.attachedRigidbody.GetComponentInChildren<BladeCollapse>();
        }
        if (saber == null || !saber.IsOn) return;

        Cut(other.bounds.center);
    }

    public void Cut(Vector3 from)
    {
        if (_cut || !cuttable) return;
        _cut = true;

        gameObject.layer = DebrisLayer;
        bool simulate = tracker == null || tracker.TryBeginDebris();
        if (simulate)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            Vector3 push = transform.position - from;
            push.y = 0f;
            rb.AddForce(push.normalized * 1.5f, ForceMode.VelocityChange);
            StartCoroutine(Despawn(true));
        }
        else
        {
            StartCoroutine(Despawn(false));
        }

        if (tracker != null) tracker.ReportCut(this);
    }

    private IEnumerator Despawn(bool wasSimulated)
    {
        if (wasSimulated) yield return new WaitForSeconds(debrisLifetime);
        if (wasSimulated && tracker != null) tracker.EndDebris();
        gameObject.SetActive(false);
    }
}
