using System.Text;
using UnityEditor;
using UnityEngine;

public static class HitSparkParticleSetupTools
{
    private const string MenuRoot = "Tools/Rehab Rhythm";
    private const string NormalSparkPath = "Assets/Prefeb/HitSpark_Normal.prefab";
    private const string StrongSparkPath = "Assets/Prefeb/HitSpark_Strong.prefab";
    private const string HoldSparkPath = "Assets/Prefeb/HitSpark_Hold Variant.prefab";

    [MenuItem(MenuRoot + "/Apply Hit Spark Particle Settings")]
    public static void ApplyHitSparkParticleSettings()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[HitSparkParticleSetupTools] Apply Hit Spark Particle Settings");

        ApplyParticleSettings(NormalSparkPath, ConfigureNormalSpark, log);
        ApplyParticleSettings(StrongSparkPath, ConfigureStrongSpark, log);
        ApplyParticleSettings(HoldSparkPath, ConfigureHoldSpark, log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(log.ToString());
    }

    [MenuItem(MenuRoot + "/Validate Hit Spark Particle Settings")]
    public static void ValidateHitSparkParticleSettings()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("[HitSparkParticleSetupTools] Validate Hit Spark Particle Settings");

        ValidateParticleSettings(NormalSparkPath, log);
        ValidateParticleSettings(StrongSparkPath, log);
        ValidateParticleSettings(HoldSparkPath, log);

        Debug.Log(log.ToString());
    }

    private static void ApplyParticleSettings(string prefabPath, System.Action<ParticleSystem> configure, StringBuilder log)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            log.AppendLine($"WARNING: Missing prefab {prefabPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            if (particleSystems.Length == 0)
            {
                log.AppendLine($"WARNING: {prefabPath} has no ParticleSystem.");
                return;
            }

            if (particleSystems.Length > 1)
            {
                log.AppendLine($"WARNING: {prefabPath} has {particleSystems.Length} ParticleSystems. Only the first one was modified.");
            }

            configure(particleSystems[0]);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            log.AppendLine($"OK: Applied settings to {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureNormalSpark(ParticleSystem particleSystem)
    {
        ConfigureCommonSpark(
            particleSystem,
            duration: 0.35f,
            loop: false,
            lifetimeMin: 0.25f,
            lifetimeMax: 0.35f,
            speedMin: 0.7f,
            speedMax: 1.2f,
            sizeMin: 0.08f,
            sizeMax: 0.14f,
            stopAction: ParticleSystemStopAction.Destroy,
            burstCount: 16,
            rateOverTime: 0f,
            radius: 0.5f,
            randomDirectionAmount: 0.2f,
            useBurst: true);
    }

    private static void ConfigureStrongSpark(ParticleSystem particleSystem)
    {
        ConfigureCommonSpark(
            particleSystem,
            duration: 0.4f,
            loop: false,
            lifetimeMin: 0.3f,
            lifetimeMax: 0.45f,
            speedMin: 1.0f,
            speedMax: 1.7f,
            sizeMin: 0.11f,
            sizeMax: 0.2f,
            stopAction: ParticleSystemStopAction.Destroy,
            burstCount: 30,
            rateOverTime: 0f,
            radius: 0.55f,
            randomDirectionAmount: 0.25f,
            useBurst: true);
    }

    private static void ConfigureHoldSpark(ParticleSystem particleSystem)
    {
        ConfigureCommonSpark(
            particleSystem,
            duration: 0.5f,
            loop: true,
            lifetimeMin: 0.25f,
            lifetimeMax: 0.4f,
            speedMin: 0.2f,
            speedMax: 0.6f,
            sizeMin: 0.06f,
            sizeMax: 0.12f,
            stopAction: ParticleSystemStopAction.None,
            burstCount: 0,
            rateOverTime: 6f,
            radius: 0.5f,
            randomDirectionAmount: 0.2f,
            useBurst: false);
    }

    private static void ConfigureCommonSpark(
        ParticleSystem particleSystem,
        float duration,
        bool loop,
        float lifetimeMin,
        float lifetimeMax,
        float speedMin,
        float speedMax,
        float sizeMin,
        float sizeMax,
        ParticleSystemStopAction stopAction,
        short burstCount,
        float rateOverTime,
        float radius,
        float randomDirectionAmount,
        bool useBurst)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.duration = duration;
        main.loop = loop;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;
        main.stopAction = stopAction;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = rateOverTime;
        if (useBurst)
        {
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0f, burstCount);
            burst.cycleCount = 1;
            burst.repeatInterval = 0.01f;
            burst.probability = 1f;
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
        }
        else
        {
            emission.SetBursts(new ParticleSystem.Burst[0]);
        }

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.arc = 360f;
        shape.randomDirectionAmount = randomDirectionAmount;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateAlphaFadeGradient(Color.white);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 50;
        }
    }

    private static Gradient CreateAlphaFadeGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static void ValidateParticleSettings(string prefabPath, StringBuilder log)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            log.AppendLine($"WARNING: Missing prefab {prefabPath}");
            return;
        }

        ParticleSystem[] particleSystems = prefab.GetComponentsInChildren<ParticleSystem>(true);
        if (particleSystems.Length == 0)
        {
            log.AppendLine($"ERROR: {prefabPath} has no ParticleSystem.");
            return;
        }

        ParticleSystem particleSystem = particleSystems[0];
        ParticleSystem.MainModule main = particleSystem.main;
        ParticleSystem.EmissionModule emission = particleSystem.emission;
        ParticleSystem.ShapeModule shape = particleSystem.shape;

        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
        emission.GetBursts(bursts);

        log.AppendLine($"Prefab: {prefabPath}");
        log.AppendLine($"  ParticleSystems={particleSystems.Length}");
        log.AppendLine($"  duration={main.duration:F2}");
        log.AppendLine($"  loop={main.loop}");
        log.AppendLine($"  startLifetime={DescribeCurve(main.startLifetime)}");
        log.AppendLine($"  startSpeed={DescribeCurve(main.startSpeed)}");
        log.AppendLine($"  startSize={DescribeCurve(main.startSize)}");
        log.AppendLine($"  shapeType={shape.shapeType}");
        log.AppendLine($"  radius={shape.radius:F2}");
        log.AppendLine($"  rateOverTime={DescribeCurve(emission.rateOverTime)}");
        log.AppendLine($"  burstCount={bursts.Length}");
        for (int i = 0; i < bursts.Length; i++)
        {
            log.AppendLine($"    burst[{i}]: time={bursts[i].time:F2}, count={DescribeCurve(bursts[i].count)}, cycles={bursts[i].cycleCount}, interval={bursts[i].repeatInterval:F2}, probability={bursts[i].probability:F2}");
        }
    }

    private static string DescribeCurve(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant.ToString("F2");
            case ParticleSystemCurveMode.TwoConstants:
                return $"{curve.constantMin:F2}-{curve.constantMax:F2}";
            case ParticleSystemCurveMode.Curve:
                return "Curve";
            case ParticleSystemCurveMode.TwoCurves:
                return "TwoCurves";
            default:
                return curve.mode.ToString();
        }
    }
}
