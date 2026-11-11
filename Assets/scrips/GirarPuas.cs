using UnityEngine;

/// <summary>
/// Va en el objeto "cilidroPuas". Gira sobre su eje (Z por defecto)
/// solo cuando el GameManager lo activa (semáforo en verde).
/// </summary>
public class GirarPuas : MonoBehaviour
{
    [Tooltip("Grados por segundo")]
    public float velocidad = 60f;

    [Tooltip("Eje local de giro. (0,0,1) = eje Z")]
    public Vector3 eje = new Vector3(0f, 0f, 1f);

    [HideInInspector] public bool activo = false;

    void Update()
    {
        if (!activo) return;
        transform.Rotate(eje.normalized, velocidad * Time.deltaTime, Space.Self);
    }
}
