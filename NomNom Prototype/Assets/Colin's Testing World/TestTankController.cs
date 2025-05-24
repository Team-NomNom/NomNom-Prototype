using UnityEngine;
using UnityEngine.InputSystem;

public class TestTankController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;

    // Position of player camera
    [SerializeField]
    public Transform cameraTransform;

    // Apparently this is the replacement for Unity's input system
    [SerializeField]
    private InputSystem_Actions input;


    // These don't need to be separate materials, we should fix this
    [SerializeField]
    public Material redMaterial;

    [SerializeField]
    public Material blueMaterial;

    // Spawn locations for both sides
    [SerializeField]
    public Transform redStartingLocation;

    [SerializeField]
    public Transform blueStartingLocation;
    
    private Vector2 moveInput;

    

    [SerializeField]
    public Constants.Colors teamColor = Constants.Colors.Red;

    private Renderer renderer;

    void Start()
    {
        // Changes color and spawn location depending on team color
        renderer = GetComponent<Renderer>();
        renderer.sharedMaterial = teamColor == Constants.Colors.Red ? redMaterial : blueMaterial;
        transform.position = teamColor == Constants.Colors.Red ? redStartingLocation.position : blueStartingLocation.position;
        transform.rotation = teamColor == Constants.Colors.Red ? redStartingLocation.rotation : blueStartingLocation.rotation;
    }
    void Awake()
    {
        input = new InputSystem_Actions();
    }

    // Updates input vector whenever input action is read
    void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    void OnDisable()
    {
        input.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        float rotateDir = moveInput.x * rotateSpeed * Time.deltaTime;
        transform.Rotate(0, rotateDir, 0);

        Vector3 moveDir = transform.forward * moveInput.y * moveSpeed * Time.deltaTime;
        transform.position += moveDir;
        

    }
}
