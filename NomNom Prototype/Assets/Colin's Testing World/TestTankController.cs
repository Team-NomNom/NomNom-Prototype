using UnityEngine;
using UnityEngine.InputSystem;

public class TestTankController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 100f;
    [SerializeField]
    public Transform cameraTransform;
    [SerializeField]

    private InputSystem_Actions input;

    [SerializeField]
    public Material redMaterial;

    [SerializeField]
    public Material blueMaterial;

    [SerializeField]
    public Transform redStartingLocation;

    [SerializeField]
    public Transform blueStartingLocation;
    private Vector2 moveInput;


    void Start()
    {
    
    }
    void Awake()
    {
        input = new InputSystem_Actions();
    }

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
