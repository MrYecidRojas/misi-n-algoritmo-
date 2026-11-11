using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Va en un objeto vacío llamado "GameManager".
/// Flujo: Objetivos (Enter) -> Semáforo -> Juego -> Pincho -> Pregunta (V/F)
///        -> correcta: sigue | incorrecta: retroalimentación + Enter = reinicia y -1 vida.
/// Meta: suena sonido_ganar y termina el nivel.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    enum Estado { Objetivos, Semaforo, Jugando, Pregunta, Retroalimentacion, Terminado }
    Estado estado;

    [Header("Jugador y obstáculos")]
    public PlayerController jugador;
    public GirarPuas cilindroPuas;

    [Header("UI (arrastra los objetos de la jerarquía)")]
    public GameObject objetivos;
    public GameObject[] preguntas = new GameObject[3];            // preguntaUno, Dos, Tres
    public GameObject[] retroalimentaciones = new GameObject[3];  // retroalimentacionUno, Dos, Tres
    public GameObject panelVictoria;                              // opcional
    public TMP_Text textoVidas;                                   // opcional

    [Header("Semáforo (arrastra los cilindros)")]
    public Renderer bombilloRojo;       // cilindroRojo
    public Renderer bombilloAmarillo;   // cilindroAmarrillo
    public Renderer bombilloVerde;      // cilidroVerde
    public float tiempoRojo = 2f;
    public float tiempoAmarillo = 1.5f;

    [Header("Audio")]
    public AudioClip musicaFondo;
    public AudioClip sonidoGanar;
    public AudioClip sonidoPerder;
    [Range(0f, 1f)] public float volumenMusica = 0.4f;

    [Header("Preguntas")]
    [Tooltip("true = Verdadero (V), false = Falso (F). Pregunta 1: V, 2: V, 3: F")]
    public bool[] respuestasCorrectas = { true, true, false };

    [Header("Vidas")]
    public int vidasIniciales = 3;
    [Tooltip("Segundos sin poder chocar después de responder bien")]
    public float invulnerabilidad = 2f;

    // static: se conserva al recargar la escena
    static int vidas = -1;

    int preguntaActual;
    AudioSource fuenteMusica, fuenteEfectos;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        if (vidas < 0) vidas = vidasIniciales;

        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.clip = musicaFondo;
        fuenteMusica.loop = true;
        fuenteMusica.playOnAwake = false;
        fuenteMusica.volume = volumenMusica;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
    }

    void Start()
    {
        foreach (var p in preguntas) if (p) p.SetActive(false);
        foreach (var r in retroalimentaciones) if (r) r.SetActive(false);
        if (panelVictoria) panelVictoria.SetActive(false);

        Luz(bombilloRojo, Color.red, false);
        Luz(bombilloAmarillo, Color.yellow, false);
        Luz(bombilloVerde, Color.green, false);

        jugador.puedeMoverse = false;
        cilindroPuas.activo = false;

        if (objetivos) objetivos.SetActive(true);
        ActualizarVidas();
        estado = Estado.Objetivos;
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Objetivos:
                if (PresionoEnter())
                {
                    if (objetivos) objetivos.SetActive(false);
                    estado = Estado.Semaforo;
                    StartCoroutine(SecuenciaSemaforo());
                }
                break;

            case Estado.Pregunta:
                if (Input.GetKeyDown(KeyCode.V)) Responder(true);
                else if (Input.GetKeyDown(KeyCode.F)) Responder(false);
                break;

            case Estado.Retroalimentacion:
                if (PresionoEnter()) Reiniciar();
                break;

            case Estado.Terminado:
                if (PresionoEnter())
                {
                    vidas = vidasIniciales;
                    Reiniciar();
                }
                break;
        }
    }

    bool PresionoEnter()
    {
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    // ---------- Semáforo ----------

    IEnumerator SecuenciaSemaforo()
    {
        Luz(bombilloRojo, Color.red, true);
        yield return new WaitForSeconds(tiempoRojo);

        Luz(bombilloRojo, Color.red, false);
        Luz(bombilloAmarillo, Color.yellow, true);
        yield return new WaitForSeconds(tiempoAmarillo);

        Luz(bombilloAmarillo, Color.yellow, false);
        Luz(bombilloVerde, Color.green, true);

        IniciarJuego();
    }

    void IniciarJuego()
    {
        estado = Estado.Jugando;
        jugador.puedeMoverse = true;
        cilindroPuas.activo = true;
        if (musicaFondo) fuenteMusica.Play();
    }

    void Luz(Renderer r, Color c, bool encendida)
    {
        if (r == null) return;
        Material m = r.material; // instancia propia
        Color apagado = new Color(c.r * 0.2f, c.g * 0.2f, c.b * 0.2f, 1f);
        m.color = encendida ? c : apagado;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", encendida ? c * 2f : Color.black);
    }

    // ---------- Eventos del jugador ----------

    public void JugadorChocoPincho()
    {
        if (estado != Estado.Jugando) return;
        estado = Estado.Pregunta;

        if (sonidoPerder) fuenteEfectos.PlayOneShot(sonidoPerder);

        jugador.puedeMoverse = false;
        jugador.Detener();

        preguntaActual = Random.Range(0, preguntas.Length);
        preguntas[preguntaActual].SetActive(true);

        Time.timeScale = 0f; // pausa pinchos y todo lo demás
    }

    public void JugadorLlegoMeta()
    {
        if (estado != Estado.Jugando) return;
        estado = Estado.Terminado;

        fuenteMusica.Stop();
        if (sonidoGanar) fuenteEfectos.PlayOneShot(sonidoGanar);

        cilindroPuas.activo = false;
        jugador.puedeMoverse = false;
        jugador.AnimarVictoria();

        if (panelVictoria) panelVictoria.SetActive(true);
    }

    // ---------- Preguntas ----------

    void Responder(bool respuestaJugador)
    {
        preguntas[preguntaActual].SetActive(false);

        if (respuestaJugador == respuestasCorrectas[preguntaActual])
        {
            // Correcta: continúa donde iba
            Time.timeScale = 1f;
            jugador.DarInvulnerabilidad(invulnerabilidad);
            jugador.puedeMoverse = true;
            estado = Estado.Jugando;
        }
        else
        {
            // Incorrecta: -1 vida, retroalimentación, Enter para reiniciar
            vidas--;
            ActualizarVidas();
            retroalimentaciones[preguntaActual].SetActive(true);
            jugador.AnimarDerrota();
            estado = Estado.Retroalimentacion;
        }
    }

    // ---------- Vidas / reinicio ----------

    void ActualizarVidas()
    {
        if (textoVidas == null) return;
        textoVidas.text = vidas > 0
            ? "Vidas: " + vidas
            : "Sin vidas - al reiniciar tendrás " + vidasIniciales;
    }

    void Reiniciar()
    {
        if (vidas <= 0) vidas = vidasIniciales;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
