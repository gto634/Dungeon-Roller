using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExperienceOrbBurst
{
    public static void Emit(ExperienceOrb prefab, Scene scene, Rigidbody player, LevelManager experience,
        float amount, Vector3 origin, Vector3[] velocities)
    {
        if (amount <= 0f || velocities.Length == 0) return;
        var colliders = new SphereCollider[velocities.Length];
        float remaining = amount;
        for (int i = 0; i < velocities.Length; i++)
        {
            ExperienceOrb orb = Object.Instantiate(prefab, origin, Quaternion.identity);
            if (orb.gameObject.scene != scene) SceneManager.MoveGameObjectToScene(orb.gameObject, scene);
            float reward = amount / velocities.Length;
            if (i == velocities.Length - 1) reward = remaining;
            remaining -= reward;
            orb.Initialize(player, experience, reward, velocities[i]);
            SphereCollider collider = orb.GetComponent<SphereCollider>();
            for (int j = 0; j < i; j++) Physics.IgnoreCollision(collider, colliders[j]);
            colliders[i] = collider;
        }
    }

    public static Vector3 ConeVelocity(Vector3 direction, float halfAngle, float speed, float variation, Vector2 upwardRange)
    {
        speed = Mathf.Max(0f, speed) * Random.Range(1f - Mathf.Clamp01(variation), 1f + Mathf.Clamp01(variation));
        float minimum = Mathf.Max(0f, upwardRange.x);
        float upward = Random.Range(minimum, Mathf.Max(minimum, upwardRange.y));
        float spread = Mathf.Clamp(halfAngle, 0f, 89f) * Mathf.Deg2Rad;
        upward = Mathf.Min(upward, speed * Mathf.Tan(spread));
        float elevation = Mathf.Atan2(upward, speed);
        float yaw = Mathf.Acos(Mathf.Clamp01(Mathf.Cos(spread) / Mathf.Max(.0001f, Mathf.Cos(elevation)))) * Mathf.Rad2Deg;
        Vector3 launch = Quaternion.AngleAxis(Random.Range(-yaw, yaw), Vector3.up) * direction;
        return launch * speed + Vector3.up * upward;
    }
}
