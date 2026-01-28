// @title: Basic Character Controller
// @description: Basic Character Controller used in the Overworld Navigation
// @category: system, optimisation, utilities
// @tags: CharacterController, Modular

public class BasicCharacterController : MonoBehaviour
{
    public Animator animator;
    public CharacterController controller;

    public Vector3 playerVelocity;

    private bool groundedPlayer;

    public float playerSpeed = 5f, speedModifier = 3f;

    private float gravityValue = -9.81f;

    private Vector3 move, currentSpeed;

    [SerializeField] private bool canMove;

    Vector2 lastDirection = Vector2.zero;

    // Start is called at the start of the game
    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        InputReciever.OnIteractPressed += Interact;
    }

    private void OnDisable()
    {
        InputReciever.OnIteractPressed -= Interact;
    }

    // Update is called once per frame
    void Update()
    {

        switch (canMove)
        {
            case true:
                Move();
                AnimatePlayer();
                break;
            case false: break;
        }
    }

    public void Move()
    {
        #region Mobile Movement

#if UNITY_ANDROID && !UNITY_EDITOR

        groundedPlayer = controller.isGrounded;
        move = new Vector3(CodeManager.Instance._inputReciever.X, 0, CodeManager.Instance._inputReciever.Y);
        move.Normalize();

        switch (groundedPlayer && playerVelocity.y < 0)
        {
            case true:
                playerVelocity.y = 0f;
                break;
            case false:
                break;
        }

        controller.Move(move * Time.deltaTime * playerSpeed);

        if (move != Vector3.zero)
        {
            controller.gameObject.transform.forward = move;
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

#endif
        #endregion

        #region
#if UNITY_STANDALONE

        // So We need to check  if controllers are connected, how many controllers are connected, and how to switch between controllers and keyboard as an option to maximise on this system and prevent dual inputs.

        groundedPlayer = controller.isGrounded;
        move = new Vector3(CodeManager.Instance._inputReciever.moveVal.x, 0, CodeManager.Instance._inputReciever.moveVal.y);
        move.Normalize();

        switch (groundedPlayer && playerVelocity.y < 0)
        {
            case true:
                playerVelocity.y = 0f;
                break;
            case false:
                break;
        }

        switch (CodeManager.Instance._inputReciever.sprint)
        {
            case true:
                controller.Move(move.normalized * Time.deltaTime * (playerSpeed + speedModifier));
                controller.transform.eulerAngles = new Vector3(0, 0, 0);
                break;
            case false:
                controller.Move(move.normalized * Time.deltaTime * playerSpeed);
                controller.transform.eulerAngles = new Vector3(0, 0, 0);
                break;
        }

        currentSpeed = controller.velocity;

        if (move != Vector3.zero)
        {
            controller.gameObject.transform.forward = move;
            lastDirection.Set(move.x, move.z);
        }

        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
#endif
        #endregion
    }

    void AnimatePlayer()
    {
        animator.SetFloat("Y", CodeManager.Instance._inputReciever.moveVal.y);
        animator.SetFloat("X", CodeManager.Instance._inputReciever.moveVal.x);
        animator.SetBool("Sprint", CodeManager.Instance._inputReciever.sprint);

        if (CodeManager.Instance._inputReciever.sprint)
        {
            animator.speed = 2;
        }

        animator.SetFloat("LastX", lastDirection.x);
        animator.SetFloat("LastY", lastDirection.y);
    }

    void Interact()
    {
        Debug.Log("Interact Clicked"); // listeners should debug this
    }

}
