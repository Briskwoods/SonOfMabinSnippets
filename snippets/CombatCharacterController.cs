// @title: Combat Character Controller
// @description: General Combat Controller used by all combat characters in the system
// @category: system, optimisation, utilities
// @tags: CharacterController, Modular

public class CombatCharacterController : MonoBehaviour
{
    [Header("Character Variables")]
    public CharacterProfileSheets profile;
    public bool isAi = false;

    [Header("Combat Movement Variables")]
    public float steps = 2, moveTime= 1;
    private Vector3 startPos;

    [Header("Combat Variables")]
    public CombatCharacterController target;
    private List<CombatCharacterController> enemies = new List<CombatCharacterController>();
    [SerializeField, Range(0,5)] private float abilityPoints = 0, APbaseValue =1;
    public float APmodifier = 0;
    [SerializeField] private bool canGetAbilityPoints = false;

    [Header("Abilities")]
    public List<Ability> characterAbilities = new List<Ability>();


    private void OnEnable()
    {
        EventBus.Subscribe(EventType.LoadCharacters, LoadCharacterToTheManager);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(EventType.LoadCharacters, LoadCharacterToTheManager);
    }

    public void LoadCharacterToTheManager()
    {
        // Send Character to TBCManager
        LoadCharacterEventData loadCharacterEventData = new LoadCharacterEventData(this);
        Debug.Log($"Character Profile: Loading {loadCharacterEventData.character.profile.characterName} to Manager");
        EventBus.Raise(loadCharacterEventData);
    }

    public void Init()
    {
        // Initialise Health Variables
        profile.Init();
        // Add more init needs here
        // Add self to TBCManager
    }

    public void BeginTurn()
    {
        startPos = transform.position;
        Debug.Log($"Character: {profile.characterName} Start Postion is {startPos}.");
        MoveForward();
    }

    public void MoveForward()
    {
        StartCoroutine(MoveForward(transform, (transform.position +(transform.forward*steps)), moveTime));
    }

    private IEnumerator MoveForward(Transform obj, Vector3 targetPos, float duration)
    {
        Debug.Log("Move Forward");
        startPos = obj.position;
        float elapsedTime = 0f;


        while (elapsedTime < duration) { 
            obj.position = Vector3.Lerp(startPos, targetPos, elapsedTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.position = targetPos;
        if (obj.position == targetPos) PromptAction();
    }

    public void PromptAction()
    {
        Debug.Log("Prompt Action");
        if (isAi)
        {
            // Enemy selects action as per player health, attributes etc and picks an action
            Debug.Log("Enemy Turn - picking action");
            Invoke(nameof(PlayAction), 0.2f);
        }
        else
        {
            // Prompt player to choose action on turn
            Debug.Log("Player Turn");
            // Trigger events to happen when move forward end
            EventBus.Raise(EventType.OnMoveForwardEnd);
        }
    }

    public void PlayAction()
    {
        if (isAi)
        {
            // Enemy does action then ends turn
            Debug.Log("Enemy Attacks!");
            Invoke(nameof(MoveBack), 0.5f);
        }
        else
        {
            // Attack needs to apply damage to a select character, player needs to pick who their target is
            Debug.Log("Player Attacks");
            Invoke(nameof(MoveBack), 1f);
        }
    }

    // So we need to prompt the player to select a target, and then upon selecting a target performing the attack on said target.
    // Simple, good challenge to get back into game dev  

    public void SelectTarget(CombatCharacterController selected)
    {
        enemies = CodeManager.Instance._turnBasedCombatManager.GetEnemies();
        // Allow player to select an enemy
        target = selected;
    }
 
    public void TriggerMoveBack()
    {
        MoveBack();
    }

    private void MoveBack()
    {
        StartCoroutine(MoveBack(transform, moveTime));
    }

    private IEnumerator MoveBack(Transform obj, float duration)
    {
        float elapsedTime = 0f;
           
        while (elapsedTime < duration)
        {
            obj.position = Vector3.Lerp(transform.position, startPos, elapsedTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        obj.position = startPos;
        if (obj.position == startPos) EndTurn();
    }

    public void EndTurn()
    {
        CodeManager.Instance._turnBasedCombatManager.TriggerNextCharacterTurn();

        // Control for AP system
        switch (canGetAbilityPoints)
        {
            case true:
                UpdateAbilityPoints(APbaseValue + APmodifier);
                break;
            case false:
                ResetAbilityPoints();
                break;
        }

    }


    #region Ability Points
    public void UpdateAbilityPoints(float value)
    {
        abilityPoints += (value);
    }

    public void ToggleAPAvailability(bool toggle)
    {
        canGetAbilityPoints = toggle;
    }

    public void ResetAbilityPoints()
    {
        abilityPoints = 0;
        ToggleAPAvailability(true);
    }

    #endregion


    #region Actions
    public void PhysicalAttack(CombatCharacterController target)
    {
        if (target == null || target.profile.IsAlive())
        {
            Debug.Log("Invalid Target or Target is already dead");
            return;
        }

        int damage = profile.GetPhysicalAttackPower();
        target.TakePhysicalDamage(damage);
    }
        
    public void MagicAttack(CombatCharacterController target)
    {
        if (target == null || target.profile.IsAlive())
        {
            Debug.Log("Invalid Target or Target is already dead");
            return;
        }

        int damage = profile.GetMagicAttackPower();
        target.TakeMagicalDamage(damage);
    }

    public void TakePhysicalDamage(int damage)
    {
        profile.TakePhysicalDamage(damage);

        if (!profile.IsAlive())
        {
            Die();
        }
    }

    public void TakeMagicalDamage(int damage)
    {
        profile.TakeMagicalDamage(damage);

        if (!profile.IsAlive())
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        profile.Heal(amount);
    }

    public void Die()
    {
        Debug.Log($"{profile.characterName} has been defeated");
        // Trigger Character Death Fns
        OnCharacterDieEventData onCharacterDieEventData = new OnCharacterDieEventData(this);
        EventBus.Raise(onCharacterDieEventData);
    }
    #endregion
}

