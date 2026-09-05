using UnityEngine;
using UnityEngine.Rendering;

public static class ProjectileVisuals
{
    private static Material trailMaterial;

    public static void AddTrail(GameObject projectile, Color startColor, Color endColor, float width, float duration)
    {
        if (projectile == null)
        {
            return;
        }

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = duration;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.04f;
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        trail.alignment = LineAlignment.View;
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sharedMaterial = GetTrailMaterial();

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
            new[] { new GradientAlphaKey(startColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
    }

    public static void SpawnMuzzleFlash(Vector3 position, Color color, float size)
    {
        CombatFeedback.SpawnImpactPulse(position, color, size);
    }

    private static Material GetTrailMaterial()
    {
        if (trailMaterial != null)
        {
            return trailMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        trailMaterial = new Material(shader);
        trailMaterial.name = "RuntimeProjectileTrail";
        trailMaterial.color = Color.white;
        trailMaterial.hideFlags = HideFlags.HideAndDontSave;
        return trailMaterial;
    }
}
