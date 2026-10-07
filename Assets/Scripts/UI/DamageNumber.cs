using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float riseSpeed = 1.5f;

    private float timer;
    private Color initialColor;
    private Camera mainCamera;

    public void Initialize(int damage)
    {
        damageText.text = damage.ToString();
        initialColor = damageText.color;
        timer = 0f;

        mainCamera = Camera.main;
    }

    private void Awake()
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>();
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Mantém o texto voltado para a câmera.
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        // Desaparece gradualmente.
        float progress = Mathf.Clamp01(timer / lifetime);

        Color color = initialColor;
        color.a = 1f - progress;

        damageText.color = color;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}