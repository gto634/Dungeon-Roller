using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class MeteorStoneOffset : MonoBehaviour
{
    [FormerlySerializedAs("meteorite")]
    [SerializeField] MeteorBrain meteor;
    [SerializeField, Min(0.01f)] float interval = 1f;
    [SerializeField, Min(0f)] float distance = 0.075f;
    Vector3 restPosition, direction;
    double elapsed;
    bool initialized;

    void Reset() => meteor = GetComponentInParent<Rigidbody>()?.GetComponentInChildren<MeteorBrain>(true);

    void Start()
    {
        if (meteor == null)
        {
            Debug.LogError("MeteorStoneOffset: assign the meteor brain.", this);
            enabled = false;
            return;
        }
        restPosition = transform.localPosition;
        float angle = Random.Range(0f, Mathf.PI * 2f);
        direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        initialized = true;
    }

    void LateUpdate()
    {
        if (!initialized) return;
        if (meteor == null || !meteor.IsFalling)
        {
            elapsed = 0d;
            transform.localPosition = restPosition;
            return;
        }
        float seconds = Mathf.Max(0.01f, interval);
        elapsed = (elapsed + Time.deltaTime) % (seconds * 2d);
        Vector3 offset = elapsed >= seconds ? direction * Mathf.Max(0f, distance) : Vector3.zero;
        transform.localPosition = restPosition + (transform.parent != null ? transform.parent.InverseTransformVector(offset) : offset);
    }

    void OnDisable()
    {
        elapsed = 0d;
        if (initialized) transform.localPosition = restPosition;
    }
}
