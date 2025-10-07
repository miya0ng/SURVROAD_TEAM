using UnityEngine;

public class DriftTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    [SerializeField] private Material trailMaterial;
    [SerializeField] private float trailVisibleTime = 1f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float startWidth = 0.3f;
    [SerializeField] private float endWidth = 0.3f;

    [Header("Drift Detection")]
    [SerializeField] private float minDriftAngle = 20f;
    [SerializeField] private float minSpeed = 12f;
    [SerializeField] private Rigidbody carRigidbody;

    [Header("Color")]
    [SerializeField] private Gradient trailColor;

    private TrailRenderer trailRenderer;
    private float phaseTimer = 0f;
    private bool wasEmitting = false;
    private bool isFading = false;

    private Material runtimeMat;
    private Color baseColor = Color.white;
    private int colorId = Shader.PropertyToID("_Color");
    private int baseColorId = Shader.PropertyToID("_BaseColor");

    private Gradient originalGradient;

    void Start()
    {
        trailRenderer = gameObject.AddComponent<TrailRenderer>();

        trailRenderer.time = 9999f;
        trailRenderer.startWidth = startWidth;
        trailRenderer.endWidth = endWidth;
        trailRenderer.textureMode = LineTextureMode.Tile;

        runtimeMat = new Material(trailMaterial);
        trailRenderer.material = runtimeMat;

        if (trailColor != null && trailColor.colorKeys.Length > 0)
        {
            originalGradient = new Gradient();
            originalGradient.SetKeys(trailColor.colorKeys, trailColor.alphaKeys);
            trailRenderer.colorGradient = originalGradient;
        }
        else
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.black, 0f),
                    new GradientColorKey(Color.black, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.8f, 1f)
                }
            );
            originalGradient = gradient;
            trailRenderer.colorGradient = gradient;
        }

        if (runtimeMat.HasProperty(baseColorId))
            baseColor = runtimeMat.GetColor(baseColorId);
        else if (runtimeMat.HasProperty(colorId))
            baseColor = runtimeMat.GetColor(colorId);
        else
            baseColor = Color.white;

        if (!carRigidbody) carRigidbody = GetComponentInParent<Rigidbody>();

        trailRenderer.emitting = false;
        SetMaterialAlpha(1f);
    }

    void Update()
    {
        if (!carRigidbody)
        {
            Debug.LogWarning("Rigidbody가 할당되지 않았습니다!");
            return;
        }

        bool isDrifting = CheckDrift();

        if (isDrifting)
        {
            isFading = false;
            phaseTimer = 0f;

            trailRenderer.emitting = true;

            trailRenderer.colorGradient = originalGradient;
            SetMaterialAlpha(1f);
        }
        else
        {
            if (wasEmitting && !isFading)
            {
                phaseTimer = 0f;
                isFading = false;
            }

            if (!isFading && phaseTimer < trailVisibleTime)
            {
                trailRenderer.emitting = false;
                phaseTimer += Time.deltaTime;

                if (phaseTimer >= trailVisibleTime)
                {
                    isFading = true;
                    phaseTimer = 0f;
                }
            }

            if (isFading)
            {
                trailRenderer.emitting = false;

                phaseTimer += Time.deltaTime;
                float t = Mathf.Clamp01(phaseTimer / fadeOutDuration);
                SetMaterialAlpha(1f - t);

                if (t >= 1f)
                {
                    trailRenderer.Clear();
                    SetMaterialAlpha(1f);
                    isFading = false;
                    phaseTimer = 0f;
                }
            }
        }

        wasEmitting = isDrifting;
    }

    private bool CheckDrift()
    {
        float speed = carRigidbody.linearVelocity.magnitude;
        if (speed < minSpeed) return false;

        Vector3 forwardDir = (transform.parent ? transform.parent.forward : transform.forward);
        Vector3 velDir = carRigidbody.linearVelocity.normalized;

        forwardDir.y = 0f;
        velDir.y = 0f;

        float angle = Vector3.Angle(forwardDir, velDir);
        return angle > minDriftAngle;
    }

    public void SetTrailActive(bool active)
    {
        if (trailRenderer) trailRenderer.emitting = active;
    }

    public void ClearTrail()
    {
        if (trailRenderer)
        {
            trailRenderer.Clear();
            isFading = false;
            phaseTimer = 0f;
            SetMaterialAlpha(1f);
        }
    }

    private void SetMaterialAlpha(float a)
    {
        if (!runtimeMat) return;

        if (runtimeMat.HasProperty(baseColorId))
        {
            Color c = baseColor; c.a = a;
            runtimeMat.SetColor(baseColorId, c);
        }
        else if (runtimeMat.HasProperty(colorId))
        {
            Color c = baseColor; c.a = a;
            runtimeMat.SetColor(colorId, c);
        }
        else
        {
            Color c = runtimeMat.color; c.a = a;
            runtimeMat.color = c;
        }
    }
}
