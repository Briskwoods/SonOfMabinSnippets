// @title: Turn Based Combat Manager
// @description: Stripped down TBC Manager for SoM Project
// @category: system, pattern
// @tags: state-machine, AI, FSM

public class TBCManager : MonoBehaviour
{
    [Header("Main Variables - Characters")]
    public List<CombatCharacterController> characters = new List<CombatCharacterController>();
    public List<CombatCharacterController> targets = new List<CombatCharacterController>();
    public List<MenuModel> combatMenus = new List<MenuModel>();

    public CombatCharacterController activeCharacter, nextCharacter;
    public CombatMenuController menuController;
    public int index = 0;

    [Header("Round Variables")]
    public List<Rounds> rounds = new List<Rounds>();
    public Rounds activeRound;

    public static event Action OnRoundStart;
    public static event Action OnRoundEnd;

    [SerializeField] private bool playerTurn = false;

    [Header("Control Variables")]
    public List<CombatCharacterController> players = new List<CombatCharacterController>();
    public List<CombatCharacterController> enemies = new List<CombatCharacterController>();
    [SerializeField] private List<CombatCharacterController> allCharacters = new List<CombatCharacterController>();


    // Should be a list
    public List<Ability> storedAbilities = new List<Ability>();
    public CombatCharacterController storedTarget;

    public float initDelay = 0.5f, delayBetweenAbilities = .5f;

    public static event Action OnCombatInitiated;
    
    // Variable thats controllable externally via signals to promote modularity,
    // Singleton is faster but more tightly coupled
   
    public int expectedCharacters = 0; /*{ get; set; }*/

    [Header("Combat Variables")]
    [SerializeField] private Ability selectedAbility;

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////// SUBSCRIPTION EVENTS /////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private void OnEnable()
    {
        EventBus.Subscribe<StartCombatEventData>(InitCombat);

        EventBus.Subscribe<SelectAbilityEventData>(StoreCurrentAbility);
        EventBus.Subscribe(EventType.UseStoredAbility, UseStoredAbility);
        EventBus.Subscribe(EventType.EndCurrentTurn, EndCurrentTurn);
        EventBus.Subscribe<LoadCharacterEventData>(LoadingCharacterToManager);
        EventBus.Subscribe(EventType.OnMoveForwardEnd, OnMoveForwardEnd);

        EventBus.Subscribe<OnCharacterDieEventData>(OncharacterDeath);

        menuController.OnFleeRequested += OnFlee;
        menuController.OnAbilitiesMenuRequested += HandleAbilitiesMenuRequest;
        menuController.OnAbilitySelected += HandleAbilitySelection;


        InputReciever.OnChangeStance += ChangeStance;
    }


    private void OnDisable()
    {
        EventBus.Unsubscribe<StartCombatEventData>(InitCombat);

        EventBus.Unsubscribe<SelectAbilityEventData>(StoreCurrentAbility);
        EventBus.Unsubscribe(EventType.UseStoredAbility, UseStoredAbility);
        EventBus.Unsubscribe(EventType.EndCurrentTurn, EndCurrentTurn);
        EventBus.Unsubscribe<LoadCharacterEventData>(LoadingCharacterToManager);
        EventBus.Unsubscribe(EventType.OnMoveForwardEnd, OnMoveForwardEnd);

        EventBus.Unsubscribe<OnCharacterDieEventData>(OncharacterDeath);


        menuController.OnFleeRequested -= OnFlee;
        menuController.OnAbilitiesMenuRequested -= HandleAbilitiesMenuRequest;
        menuController.OnAbilitySelected -= HandleAbilitySelection;


        InputReciever.OnChangeStance += ChangeStance;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////// INITIALISATION ////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // To Start this properly a trigger condition thats needed is for the Trigger to send the party size
    // or Characters size to the TBC manager before starting to add characters to the pool
    // This is specific to combat trigger operation and there comes as a later task but good to note now

    public bool GetPlayerTurn => playerTurn;

    // This is for external fns to subscribe to the Combat manager externally 
    // Review: Redundant Code
    public void OnCombatInit()
    {
        // Invoke actions subscribed to this fn.
        OnCombatInitiated?.Invoke();
        Debug.Log("Combat Manager: Invoked Fns Subbed to Init fn.");
    }

    // This is the Start of the Combat Sequence
    private void InitCombat(StartCombatEventData eventData)
    {
        // First we set the scene and enemytypes/enemies to face
        // then we get the expected enemy Number, lets first create a script that triggers combat with the 
        // 1. Player group size + enemy size
        expectedCharacters = eventData.groupSize;
        // 2. Load Characters into manager
        EventBus.Raise(EventType.LoadCharacters);
        // Call any fn directly subscibed to Init
        OnCombatInit();
    }

    // To Be Added: We need to set the characters exrternally, dont forget
    public void LoadingCharacterToManager(LoadCharacterEventData eventData)
    {
        Debug.Log("Combat Manager: Attempting to Load Character to Manager");
        if (!characters.Contains(eventData.character))
        {
            characters.Add(eventData.character);
            Debug.Log($"Combat Manager: Loaded {eventData.character.profile.characterName} to Manager");
        }

        if (characters.Count >= expectedCharacters)
        {
            Debug.Log($"Combat Manager: All characters ready!");
            // Do next function when charcaters are ready
            StartCoroutine(nameof(InitialiseVariables));
        }
    }

    IEnumerator InitialiseVariables()
    {
        // First we initialise everyones health after adding them to the system
        for (int a = 0; a < characters.Count; a++)
        {
            characters[a].Init();
            Debug.Log($"Combat: {characters[a].profile.characterName} health initialised");
        }
        yield return new WaitForSeconds(0.1f);

        // Sort Characters into their proper lists
        SortCharacters();

        // Initialise the rounds
        InitRounds();
        Debug.Log("Combat: Init Ended");
    }

    public void SortCharacters()
    {
        players = new List<CombatCharacterController>(GetPlayers());
        enemies = new List<CombatCharacterController>(GetEnemies());
        Debug.Log($"Combat: Characters Sorted.");
    }
   
    public List<CombatCharacterController> GetEnemies()
    {
        List<CombatCharacterController> enemies = new List<CombatCharacterController>();
        foreach(CombatCharacterController character in characters)
        {
            if (character.isAi && character.profile.IsAlive()) { 
                enemies.Add(character); 
                Debug.Log($"Combat: Enemies: {character.profile.characterName} Added");
            }
        }
        return enemies;
    }

    // Used to retain a list of player side characters
    public List<CombatCharacterController> GetPlayers()
    {
        List<CombatCharacterController> players = new List<CombatCharacterController>();
        foreach (CombatCharacterController character in characters)
        {
            if (!character.isAi && character.profile.IsAlive())
            {
                players.Add(character);
                Debug.Log($"Combat: Player: {character.profile.characterName} Added");
            }
        }
        return players;
    }

    public List<CombatCharacterController> GetAllCharacters()
    {
        allCharacters.Clear();

        allCharacters.AddRange(GetPlayers());
        allCharacters.AddRange(GetEnemies());

        Debug.Log($"Combat: Player: All Characters loaded");
        return allCharacters;
    }


    public void InitRounds()
    {
        Debug.Log($"Combat Manager: Initialising Rounds");
        CreateRounds(2);

        // Set the chatacters in the rounds
        SetCharactersInRounds();

        // Calculate Turn Orders in Rounds
        CalculateTurnOrders();

        // Set active round to be current round
        activeRound = rounds[0];

        Debug.Log($"Combat Manager: Active Character Set");
        activeCharacter = activeRound.GetFirstCharacter();
        nextCharacter = activeRound.GetNextCharacter();

        // Enable Rounds UI
        menuController.roundsUIController.Open();

        // Set Active Character Stance
        CheckIfCharacterHasMoreThanOneStance();
        menuController.SetStance(activeCharacter.profile.GetActiveStance());
        
        // Load Characters into Rounds
        Debug.Log($"Combat Manager: Round 1 and 2 ready");
        menuController.roundsUIController.SetBars(activeRound, rounds[1]);

        // We can now start the Rounds
        StartRound();
    }


    public void CreateRounds(int roundNumber)
    {
        for(int i = 0; i < roundNumber; i++)
        {
            Rounds round = new Rounds();
            rounds.Add(round);
        }
        Debug.Log($"Round Manager: Created new round/rounds.");
    }

    public void AddAnotherRoundMidGame()
    {
        Rounds newRound = new Rounds();
        newRound.SetCharacters(characters);
        newRound.CalculateTurn();
        rounds.Add(newRound);
        Debug.Log($"Round Manager: Created new round/rounds midgame.");
    }

    public void SetCharactersInRounds()
    {
        for(int i = 0; i < rounds.Count; i++)
        {
            rounds[i].SetCharacters(characters);
        }
        Debug.Log($"Round Manager: Set Characters in round/rounds.");
    }


    public void CalculateTurnOrders()
    {
        for(int i = 0; i < rounds.Count; i++)
        {
            rounds[i].CalculateTurn();
        }
        Debug.Log($"Round Manager: Sorted Turn orders in rounds.");
    }
    
    public void CalculateTurnOrdersForNewRound(Rounds newRound)
    {
        newRound.CalculateTurn();

        Debug.Log($"Round Manager: Sorted Turn orders for {newRound}.");
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////// COMBAT MENU FUNCTIONS ////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void HandleAbilitiesMenuRequest(MenuModel submenu)
    {
        // First, ensure data is loaded to UI
        menuController.DisplayAbilities(activeCharacter.characterAbilities);

        // Then open the submenu
        menuController.OpenSubMenu(submenu);
        menuController.RemoveUnavailableAbilitiesFromSelection();
        Debug.Log("Combat: Ability Menu Loaded");
    }

    bool multicasting = false;

    public void HandleAbilitySelection(int btnPos)
    {
        Debug.Log($"Combat: {activeCharacter.profile.characterName}'s {activeCharacter.characterAbilities[btnPos]} has been selected");
        // First we check the selected abilities
        selectedAbility = activeCharacter.characterAbilities[btnPos];

        EnterTargetingMode();
    }
    

    void EnterTargetingMode()
    {
        // Is this a multiselection of targets or not? 
        switch (selectedAbility.selectionType)
        {
            case Ability.SelectType.singleSelection:
                // Open Targeting UI
                break;
            case Ability.SelectType.multiSelection:
                ToggleMulticastingState(true);
                
                break;
        }

        // if so thats ok, step only matters at the end when the selection is done
        // what do we do next?
        // we look at the ability target & the player targer
        switch (selectedAbility.targetGroup)
        {
            case Ability.TargetGroup.everyone:
                targets = GetAllCharacters();

                // Swotch to Everyone target UI
                // Close Combat UI
                menuController.ToggleCombatHUD(false);
                // Open Select Everone UI
                menuController.ToggleSelectHUD(true);

               

                //menuController.HighlightTargets(targets);

                // Load EveryOne Select UI Data
                //menuController.OpenSubMenu(menuController.selectUI);
                menuController.LoadTargets(targets);
                //menuController.RemoveUnavailableTargetsFromSelection();

                break;
            case Ability.TargetGroup.opponentTeam:
                if (playerTurn)
                {
                    targets = new List<CombatCharacterController>(GetEnemies());
                }
                else
                {
                    targets = new List<CombatCharacterController>(GetPlayers());
                }

                CheckAndOpenRightTargetSelectionUI();
                break;
            case Ability.TargetGroup.myTeam :
                if (playerTurn)
                {
                    targets = new List<CombatCharacterController>(GetPlayers());
                }
                else
                {
                    targets = new List<CombatCharacterController>(GetEnemies());
                }

                CheckAndOpenRightTargetSelectionUI();
                break;
        }
    }

    public void ToggleMulticastingState(bool toggle)
    {
        multicasting = true;
        menuController.DisplaySinglecastAbilities(activeCharacter.characterAbilities);
    }



    void CheckAndOpenRightTargetSelectionUI()
    {
        // Check target mode
        
    }

    public void OnTargetSelected()
    {
        if (multicasting) { }

        // if multicasting is on we go back to the ability selection UI and continue till the max ability selection limit is reached 
        switch (multicasting)
        {
            case true:
                // Store Ability and Target into a queue

                // Open Abilities Menu again, DO NOT CRASH THE MENU SYSTEM!
                break;
            case false:
                // Execute stored abilites
                break;
        }
    }



    public void CheckIfCharacterHasMoreThanOneStance()
    {
        if(activeCharacter.profile.GetAvailableStancesMenuSize() > 1)
        {
            // Enable the buttons to show that the player can change stances
        }
    }

    public void ChangeStance(float value)
    {
        if (activeCharacter.profile.GetAvailableStancesMenuSize() > 0)
        {
            Stance activeStance = activeCharacter.profile.GetActiveStance();
            List<Stance> avaliableStances = new List<Stance>(activeCharacter.profile.GetAvailableStances());
            int index = avaliableStances.IndexOf(activeStance);
            int nextIndex = index + (int)value;

            if (nextIndex < 0) { nextIndex = avaliableStances.Count - 1; }
            if (nextIndex > (avaliableStances.Count - 1)) { nextIndex = 0; }

            activeStance = avaliableStances[nextIndex];

            activeCharacter.profile.SetActiveStance(activeStance);
            menuController.SetStance(activeStance);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////// COMBAT FUNCTIONS /////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    [ContextMenu("Start Combat")]
    // Once a character is determined to start we ping them begin combat
    public void StartRound()
    {
        // Check who's turn it is
        if (activeCharacter.isAi) { playerTurn = false; } else { playerTurn = true; }

        Debug.Log($"Combat: Current Character Turn: {activeCharacter.profile.characterName}");
        // Invoke Round Start Events/Actions attached to start of Round
        EventBus.Raise(EventType.OnRoundStart);
        // Sends command to active player to start combat.
        activeCharacter.BeginTurn();
        Debug.Log($"Combat: Combat Started");
    }

    public void TriggerNextCharacterTurn()
    {
        // We check if the battle is over before going to the next turn
        switch (CheckIfBattleIsOver())
        {
            case true:
                // End Combat  
                Debug.Log($"Combat: Combat Instance Over");
                EndCombat();
                break;
            case false:
                Debug.Log($"Combat: Triggered Next Turn");
                // Remove Current Character from Turn Order
                activeRound.RemoveCharacterFromCharacterTurns(activeCharacter);
                menuController.roundsUIController.UpdateRoundUI(activeCharacter);
                // We check if the round is over
                switch (activeRound.CheckIfRoundIsOver())
                {
                    case true:
                        UpdateRound();
                        break;
                    case false:
                        GoToNextCharacter();
                        break;
                }
                break;
        }
    }

    private void GoToNextCharacter()
    {
        // Set active character as next character
        activeCharacter = nextCharacter;
        // Update Character Abilities Menu
        LoadAbilitiesOntoMenuEventData loadAbilitiesOntoMenuEventData = new LoadAbilitiesOntoMenuEventData(activeCharacter);
        EventBus.Raise(loadAbilitiesOntoMenuEventData);
        Debug.Log("Combat: Loaded abilities menu");
        // Update the next character
        // but first we check if next character is available
        switch (activeRound.characterTurnOrder.Count > 1)
        {
            case true:
                nextCharacter = activeRound.GetNextCharacter();
                break;
            case false:
                nextCharacter = null;
                break;
        }

        // Check if next xter if player or not
        if (activeCharacter.isAi) { playerTurn = false; } else { playerTurn = true; }
        Debug.Log($"Combat: Next turn triggered");

        // I think we should reset the menu each turn but also, should  be sent in with a bool to ensure proper xter turn actions happen
        CombatMenuResetEventData combatMenuResetEventData = new CombatMenuResetEventData(playerTurn);
        EventBus.Raise(combatMenuResetEventData);

        StartRound();
    }

    public void OnMoveForwardEnd()
    {
        // Reset Combat Menu (Control)
        menuController.ResetCombatMenu();

        // Enable Combat menu
        menuController.ToggleCombatHUD(true);
        Debug.Log($"Combat: Current Character Turn: {activeCharacter}");

        // Allow Player input
        CodeManager.Instance._turnBasedCombatManager.menuController.ToggleInput(true);
    }

    private void UpdateRound()
    {
        // Invoke Round Start Events/Actions attached to end of Round
        EventBus.Raise(EventType.OnRoundEnd);

        Debug.Log("Start Next Round");
        // Clear Active Round
        rounds.Remove(activeRound);
        // what I need to do is create another round 
        AddAnotherRoundMidGame();
        // Set new round to be the first index after prior round is removed from the list
        activeRound = rounds[0];
        // Set the active character and next character
        activeCharacter = activeRound.GetFirstCharacter();
        nextCharacter = activeRound.GetNextCharacter();

        // Small delay before starting next round is a good idea
        Debug.Log("Delay Round Start here");

        // Update Round UI
        menuController.roundsUIController.SetBars(activeRound, rounds[1]); 

        // Begin next round
        StartRound();
    }

    public void OncharacterDeath(OnCharacterDieEventData eventData)
    {
        // Add Fn to call when a character dies in the TBC Manager
        // 1. Remove self from TBC Manager
        characters.Remove(eventData.character);

        // 2. Remove xter from all rounds
        activeRound.characterTurnOrder.Remove(eventData.character);
        foreach (Rounds round in rounds)
        {
            round.characterTurnOrder.Remove(eventData.character);
        }

        // 3. Remove xter from active control var
        if (players.Contains(eventData.character)) { players.Remove(eventData.character); }
        if (enemies.Contains(eventData.character)) { enemies.Remove(eventData.character); }

        // 4. Update the next character in xter turn order
        if (activeRound.characterTurnOrder.Count > 1) { nextCharacter = activeRound.GetNextCharacter(); }
    }

    // Here we check if all players/enemies are dead
    // Needs to be expanded so that when game is over we can determine win/lose conditions
    public bool CheckIfBattleIsOver() => (players.Count < 1 || enemies.Count < 1) ?  true:  false;

    public void EndCombat()
    {
        Debug.Log("Combat: Combat Over");
    }
    public void OnFlee()
    {
        Debug.Log("Combat: Fled Combat");
    }

    public void ActiveCharacterPlayAction()
    {
        Debug.Log($"Combat: Use Stored Ability on target");
        activeCharacter.PlayAction();
    }

    public void ActiveCharacterUseAbility(int abilityId, CombatCharacterController target)
    {
        // Flagged for Review: Probable Redundant Code
        // Test use case for Ability use after ability select
        activeCharacter.characterAbilities[abilityId].Activate(activeCharacter, target);
        Debug.Log($"Combat: {activeCharacter.profile.name} has selected {activeCharacter.characterAbilities[abilityId]} to use on {target.profile.name}");
    }
    
    public void StoreCurrentAbility(SelectAbilityEventData eventData)
    {
        // Flagged for Review: Code Cleanup 
        storedAbilities.Add(activeCharacter.characterAbilities[eventData.selectedId]);
        Debug.Log($"Combat: Stored Ability called {activeCharacter.characterAbilities[eventData.selectedId]}");
    }

    public void UseStoredAbility()
    {
        // Flagged for Review: Code Cleanup 
        StartCoroutine(nameof(UseStoredAbilities));
        ////TBCManager.TriggerStoredActionOnTarget();
        //Debug.Log($"Combat: Ability used on {storedTarget.profile.characterName}: {storedAbility}");
        ////ActiveCharacterPlayAction();
        //storedAbility.Activate(activeCharacter, storedTarget);
    }

    IEnumerator UseStoredAbilities()
    {
        for (int i = 0; i < storedAbilities.Count; i++)
        {
            Debug.Log($"Combat: Ability used on {storedTarget.profile.characterName}: {storedAbilities[i]}");
            storedAbilities[i].Activate(activeCharacter, storedTarget);
            yield return new WaitForSeconds(delayBetweenAbilities);
        }
    }

    public void EndCurrentTurn()
    {
        // Flagged for Review: Code Cleanup
        activeCharacter.TriggerMoveBack();
        Debug.Log("Combat: Character Step Back Triggered");
    }
}


[System.Serializable]
public class Rounds
{
    public List<CombatCharacterController> players = new List<CombatCharacterController>();
    public List<CombatCharacterController> enemies = new List<CombatCharacterController>();
    public List<CombatCharacterController> characterTurnOrder = new List<CombatCharacterController>();

    // Called internally, can be used but not yet defined what for
    public List<CombatCharacterController> GetCharacters()
    {
        // Sends out a list of all characters
        List<CombatCharacterController> allCharacters = new List<CombatCharacterController>();

        allCharacters.AddRange(players);
        allCharacters.AddRange(enemies);

        return allCharacters;
    }


    // Called from outside to set characters before a fight
    public void SetCharacters(List<CombatCharacterController> characters)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i].profile.characterType == CharacterProfileSheets.CharacterType.Player)
            {
                if (!players.Contains(characters[i]))
                {
                    players.Add(characters[i]);
                    Debug.Log($"Round Manager: {characters[i].profile.characterName} has been loaded as a Player.");
                }
            }
            else
            {
                if (!players.Contains(characters[i]))
                {
                    enemies.Add(characters[i]);
                    Debug.Log($"Round Manager: {characters[i].profile.characterName} has been loaded as an Enemy.");
                }
            }
        }
            Debug.Log($"Round Manager: players and enemies  loaded.");
    }

    [ContextMenu("Calculate Turns Test")]
    public void CalculateTurn()
    {
        // First we add all characters into a single list
        characterTurnOrder.AddRange(players);
        characterTurnOrder.AddRange(enemies);
        Debug.Log($"Round Manager: CharacterTurns loaded with all characters.");

        // We sort through the list based on character speeds
        characterTurnOrder.Sort(delegate (CombatCharacterController left, CombatCharacterController right)
        {
            // To sort from highest speed to lowest speed we do right v left comparison, to sort small to high we do left v right
            int cat = right.profile.GetSpeed().CompareTo(left.profile.GetSpeed());
            Debug.Log($"{left.profile.characterName} has a speed of {left.profile.GetSpeed()} whilst {right.profile.characterName} has a speed of {right.profile.GetSpeed()}");
            Debug.Log(cat);
            return cat;
        });

        Debug.Log($"Round Manager: CharacterTurns sorted based on Character Speed, sorted from Highest to Lowest.");
    }

    public CombatCharacterController GetFirstCharacter()
    {
        return characterTurnOrder[0];
    }

    public CombatCharacterController GetNextCharacter()
    {
        return characterTurnOrder[1];
    }

    public void RemoveCharacterFromCharacterTurns(CombatCharacterController activeCharacter)
    {
        // This function removes the active character from the turn order
        characterTurnOrder.Remove(activeCharacter);
    }

    public bool CheckIfRoundIsOver()
    {
        switch (characterTurnOrder.Count == 0)
        {
            case true:
                Debug.Log($"Round Manager: Round is over");
                return true;
            case false:
                Debug.Log($"Characters left in round: {characterTurnOrder.Count}");
                return false;
        }
    }
}