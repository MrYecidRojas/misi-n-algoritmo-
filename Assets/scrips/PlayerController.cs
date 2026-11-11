using UnityEngine;

/// <summary>
/// Va en el personaje (tripo_convert...). Requiere CharacterController.
/// W/S = avanzar/retroceder, A/D = girar (o desplazarse lateral), Espacio = saltar.
/// Parámetros del Animator: Speed (float), Jump (trigger), Gana (trigger), Pierde (trigger).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 5f;
    public float velocidadGiro = 120f;
    [Tooltip("true: A/D giran al personaje (la cámara hija gira con él). false: A/D se desplazan de lado.")]
    public bool girarConAD = true;
    public float alturaSalto = 1.5f;
    public float gravedad = -20f;

    [Header("Animación")]
    public Animator animator;

    [HideInInspector] public bool puedeMoverse = false;

    CharacterController cc;
    Vector3 velVertical;
    float invulnerableHasta;

    static readonly int HashSpeed  = Animator.StringToHash("Speed");
    static readonly int HashJump   = Animator.StringToHash("Jump");
    static readonly int HashGana   = Animator.StringToHash("Gana");
    static readonly int HashPierde = Animator.StringToHash("Pierde");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Importante: sin root motion (el movimiento lo hace el código)
        // y con tiempo sin escala para que la animación de derrota
        // se vea aunque el juego esté en pausa (Time.timeScale = 0).
        animator.applyRootMotion = false;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
    }

    void Update()
    {
        bool enSuelo = cc.isGrounded;
        if (enSuelo && velVertical.y < 0f) velVertical.y = -2f;

        float h = 0f, v = 0f;
        if (puedeMoverse)
        {
            h = Input.GetAxisRaw("Horizontal"); // A / D
            v = Input.GetAxisRaw("Vertical");   // W / S
        }

        Vector3 mov;
        if (girarConAD)
        {
            transform.Rotate(0f, h * velocidadGiro * Time.deltaTime, 0f);
            mov = transform.forward * v;
        }
        else
        {
            mov = transform.forward * v + transform.right * h;
        }
        mov = Vector3.ClampMagnitude(mov, 1f) * velocidad;

        // Salto
        if (puedeMoverse && enSuelo && Input.GetKeyDown(KeyCode.Space))
        {
            velVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            animator.SetTrigger(HashJump);
        }

        velVertical.y += gravedad * Time.deltaTime;
        cc.Move((mov + velVertical) * Time.deltaTime);

        float girando = (girarConAD && Mathf.Abs(h) > 0.1f) ? 1f : 0f;
        float animSpeed = Mathf.Max(mov.magnitude / velocidad, girando * 0.5f);
        animator.SetFloat(HashSpeed, puedeMoverse ? animSpeed : 0f);
    }

    // ---- Detección de pinchos y meta ----

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null) return;

        if (other.CompareTag("Pincho"))
        {
            if (Time.unscaledTime < invulnerableHasta) return;
            GameManager.Instance.JugadorChocoPincho();
        }
        else if (other.CompareTag("Meta"))
        {
            GameManager.Instance.JugadorLlegoMeta();
        }
    }

    // Por si las metas tienen collider NO trigger
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (GameManager.Instance != null && hit.collider.CompareTag("Meta"))
            GameManager.Instance.JugadorLlegoMeta();
    }

    // ---- Llamados por el GameManager ----

    public void DarInvulnerabilidad(float segundos)
    {
        invulnerableHasta = Time.unscaledTime + segundos;
    }

    public void Detener()         { animator.SetFloat(HashSpeed, 0f); }
    public void AnimarVictoria()  { animator.SetTrigger(HashGana); }
    public void AnimarDerrota()   { animator.SetTrigger(HashPierde); }
}
