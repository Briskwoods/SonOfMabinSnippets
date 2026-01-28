// @title: Combat Menu Controller
// @description: The Combat Menu controller inherits the Menu Controller class but is utilised for sending and recieving signals from the Combat UI's
// @category: systems, patterns, utilities
// @tags: UI, MVVC/MVC, Observer Pattern

public class CombatMenuController : MenuController
{
    [Header("Combat Variables")]
    // This script will be used for mainly combat menu functionality and works as an extension of the menu controller
    public GameObject canvas; public GameObject actionsHUD, selectHUD, roundsHUD;

    public List<GameObject> stanceButtons = new List<GameObject>();
    public GameObject currentStance;

    bool isSelecting = false;

    public CombatHUDModel combatUI;
    public MenuModel selectUI, abilitiesUI;
    public List<Button> targetButtons = new List<Button>();
    public List<Button> abilityButtons = new List<Button>();

    public RoundsUIController roundsUIController;

    public event Action<MenuModel> OnAbilitiesMenuRequested;
    public event Action<int> OnAbilitySelected;
    public event Action<MenuModel> OnSelectTargetMenuRequested;
    public event Action<MenuModel> OnInventoryMenuRequested;
    public event Action OnAttackRequested;
    public event Action OnParryRequested;
    public event Action OnTauntRequested;
    public event Action OnFleeRequested;
    public event Action OnTargetSelected;

    private void Start()
    {
        Debug.Log($"Combat Menu: Current Menu: {currentMenu}");
        targetButtons = new List<Button>(selectUI.menuButtons);
        abilityButtons = new List<Button>(abilitiesUI.menuButtons);
    }

    private void OnEnable()
    {
        InputReciever.OnSubmitPressed += ClickBtn;
        InputReciever.OnBackPressed += GoBack;
        InputReciever.OnNavigate += NavigateMenu;
        InputReciever.OnMenuOpen += OpenMenu;

        // Turn Based Combat Events
        EventBus.Subscribe<CombatMenuResetEventData>(ResetCombatMenu);
    }

    private void OnDisable()
    {
        InputReciever.OnSubmitPressed -= ClickBtn;
        InputReciever.OnBackPressed -= GoBack;
        InputReciever.OnNavigate -= NavigateMenu;
        InputReciever.OnMenuOpen -= OpenMenu;

        // Turn Based Combat Events
        EventBus.Unsubscribe<CombatMenuResetEventData>(ResetCombatMenu);
    }

    void DisableOtherStanceBtns()
    {
        foreach(GameObject button in stanceButtons)
        {
            Debug.Log($"Combat Menu: Disabled {button.name}");
            button.SetActive(false);
        }
    }

    public void SetStance(Stance stance)
    {
        // Needs to take into account the Button order in the UI needs to match the Stances order
        int stanceBtn = (int)stance.stanceType;
        DisableOtherStanceBtns();
        currentStance = stanceButtons[stanceBtn];
        currentStance.SetActive(true);
        Debug.Log($"Combat Menu: Enabled {currentStance.name}");

        // From there we need to
        // 1. Remove the inactive buttons from the all buttons list that copies from the Menu Model
        
        if(menuHistory.Count < 1)
        {
            SetCurrentMenu(combatUI);
            LoadMenuData(combatUI);
            RemoveInactiveButtonsFromCombatUI(stance);
            currentButton = allButtons[GetCurrentButtonIndex()];
        }
    }

    public bool CheckIfCombatHUDIsActive() => actionsHUD.activeInHierarchy ? true : false;

    public void ToggleCombatHUD(bool toggle)=> actionsHUD.SetActive(toggle);

    public void ToggleSelectHUD(bool toggle)
    {
        selectHUD.SetActive(toggle);
        isSelecting = toggle;
    }

    

    public void LoadTargets(List<CombatCharacterController> targetsToLoad)
    { 
        Debug.Log("Combat Menu: Targets Loaded");
        DisableTargetButtons();

        // This fn will change to basically highlight the active enemies whilst only enabling the btn in the bg6 
        for(int i = 0; i < targetsToLoad.Count; i++)
        {
            targetButtons[i].gameObject.name = targetsToLoad[i].name;
            targetButtons[i].GetComponentInChildren<TextMeshProUGUI>().SetText(targetsToLoad[i].name);
            targetButtons[i].gameObject.SetActive(true);
        }
    }

    public void OnAbilitiesMenuButtonClicked(MenuModel submenu)
    {
        OnAbilitiesMenuRequested?.Invoke(submenu);
    }

    public void OnAbilitySelectedClicked()
    {
        OnAbilitySelected?.Invoke(GetCurrentButtonIndex());
    }

    public void OnSelectTargetClicked(MenuModel submenu)
    {
        OnSelectTargetMenuRequested?.Invoke(submenu);
    }

    public void OnTargetSelectedClcicked()
    {
        OnTargetSelected?.Invoke();
    }

    public void OnInventoryButtonClicked(MenuModel submenu)
    {
        OnInventoryMenuRequested?.Invoke(submenu);
    }

    public void OnFleeButtonClicked()
    {
        OnFleeRequested?.Invoke();
    }

    public void OnAttackButtonClicked()
    {
        OnAttackRequested?.Invoke();
    }

    public void OnParryButtonClicked()
    {
        OnParryRequested?.Invoke();
    }

    public void OnTauntButtonClicked()
    {
        OnTauntRequested?.Invoke();
    }

    public RectTransform highlighter;
    [SerializeField] private float offsetFromButton = 10f;
    public void UpdateHighlighterPosition()
    {
        // disabled by default
        if (!highlighter.gameObject.activeSelf) highlighter.gameObject.SetActive(true);

        // Exit if highlighter is emprt
        RectTransform buttonRectTransform = currentButton.GetComponent<RectTransform>();
        if (highlighter == null || buttonRectTransform == null) return;

        // Update highlighter position
        highlighter.SetParent(currentButton.transform.parent, false);
        if (highlighter.parent != buttonRectTransform.parent)
        {
            Debug.LogWarning("Arrow and button should share the same parent for accurate positioning");
        }

        // Account for button's pivot point
        Vector2 buttonPos = buttonRectTransform.anchoredPosition;
        Rect buttonRect = buttonRectTransform.rect;
        Vector2 buttonPivot = buttonRectTransform.pivot;

        // Calculate the actual left edge position
        float leftEdgeX = buttonPos.x - (buttonRect.width * buttonPivot.x);
        float centerY = buttonPos.y + (buttonRect.height * (0.5f - buttonPivot.y));

        highlighter.anchoredPosition = new Vector2(
            leftEdgeX - offsetFromButton,
            centerY
        );
    }


    public void DisplayAbilities(List<Ability> characterAbilities)
    {
        // First we disable all ability btns
        DisableAbilityButtons();
        allButtons.Clear();

        // After we update the Button text/data for each ability to match ability name
        for (int i = 0; i < characterAbilities.Count; i++)
        {
            abilityButtons[i].gameObject.name = characterAbilities[i].name;
            abilityButtons[i].GetComponentInChildren<TextMeshProUGUI>().SetText(characterAbilities[i].name);
            abilityButtons[i].gameObject.SetActive(true);
            allButtons.Add(abilityButtons[i]);
        }
    }

    public void DisplaySinglecastAbilities(List<Ability> characterAbilities)
    {
        // First we disable all ability btns
        DisableAbilityButtons();
        allButtons.Clear();

        // After we update the Button text/data for each ability to match ability name
        for (int i = 0; i < characterAbilities.Count; i++)
        {
            if (characterAbilities[i].selectionType != Ability.SelectType.multiSelection)
            {
                abilityButtons[i].gameObject.name = characterAbilities[i].name;
                abilityButtons[i].GetComponentInChildren<TextMeshProUGUI>().SetText(characterAbilities[i].name);
                abilityButtons[i].gameObject.SetActive(true);
                allButtons.Add(abilityButtons[i]);
            }
        }
    }

    public override void GoBack()
    {
        switch (isSelecting) // Switch to use/check state of the system
        {
            case true:
                ToggleSelectHUD(false);
                ToggleCombatHUD(true);
                base.GoBack();

                if (currentMenu == abilitiesUI) { RemoveUnavailableAbilitiesFromSelection(); }
                // similar fn for Inventory should be added


                Debug.Log($"Combat Menu: Back from Select Targets UI");
                break;
            case false:
                base.GoBack(); 
                RefreshCombatUI();
                Debug.Log($"Combat Menu: Normal Back");
                break;
        }
    }

    public void RemoveInactiveButtonsFromCombatUI(Stance stance)
    {
        int stanceBtn = (int)stance.stanceType;
        Button activeStanceBtn = combatUI.stanceButtons[stanceBtn];

        List<Button> activeMenuButtons = new List<Button>();

        if (!activeMenuButtons.Contains(activeStanceBtn)) activeMenuButtons.Add(activeStanceBtn);
        activeMenuButtons.AddRange(combatUI.menuButtons);
        
        allButtons = new List<Button>(activeMenuButtons);
    }

    public void RemoveUnavailableTargetsFromSelection()
    {
        List<Button> temp = new List<Button>();

        for (int i = 0; i < targetButtons.Count; i++)
        {
            if (targetButtons[i].gameObject.activeSelf) { temp.Add(targetButtons[i]); }
        }

        allButtons = new List<Button>(temp);
        Debug.Log($"Combat Menu: Disabled Unavailable Target Buttons");
    }


    public void RemoveUnavailableAbilitiesFromSelection()
    {
        List<Button> temp = new List<Button>();

        for (int i = 0; i < abilityButtons.Count; i++)
        {
            if (abilityButtons[i].gameObject.activeSelf) { temp.Add(abilityButtons[i]); }
        }
        Debug.Log($"Combat Menu: Disabled Unavailable Ability Buttons");
        allButtons = new List<Button>(temp);

        // Above causes an issue in scrolling through the menu when poorly initialised
    }

    public void RefreshCombatUI()
    {
        combatUI.Open();
        // From there we need to
        // 1. Remove the inactive buttons from the all buttons list that copies from the Menu Model
        SetCurrentMenu(combatUI);
        LoadMenuData(combatUI);

        int stanceBtn = stanceButtons.IndexOf(currentStance);
        Button activeStanceBtn = combatUI.stanceButtons[stanceBtn];

        List<Button> activeMenuButtons = new List<Button>();

        if (!activeMenuButtons.Contains(activeStanceBtn)) activeMenuButtons.Add(activeStanceBtn);
        activeMenuButtons.AddRange(combatUI.menuButtons);

        allButtons = new List<Button>(activeMenuButtons);
        currentButton = allButtons[GetCurrentButtonIndex()];
    }

    public void ResetCombatMenu()
    {
        if (isSelecting)
        {
            // Close Sub menu if open 
            if (menuHistory.Count > 0) { CloseSubMenu();}
            // First we clear menu history stack to reset menu nav
            menuHistory.Clear();
            // need to prevent unnecessary calls here
            RefreshCombatUI();
            Debug.Log($"Combat Menu: Reset From Selecting Menu.");
        }
        else
        {
            ToggleSelectHUD(false);

            // Added as a catch to prevent UI coming up during player turn, but the TBC Manager should handle this not here
            if (CodeManager.Instance._turnBasedCombatManager.GetPlayerTurn)
            {
                ToggleCombatHUD(true);
            }

            base.GoBack();
            Debug.Log($"Combat Menu: Reset from Normal Menu.");
        }
    }

    public void ResetCombatMenu(CombatMenuResetEventData combatMenuResetEventData)
    {
        if (isSelecting)
        {
            // Close Sub menu if open 
            if (menuHistory.Count > 0) { CloseSubMenu(); }
            // First we clear menu history stack to reset menu nav
            menuHistory.Clear();
            // need to prevent unnecessary calls here
            RefreshCombatUI();
            Debug.Log($"Combat Menu: Reset From Selecting Menu.");
        }
        else
        {
            ToggleSelectHUD(false);
            base.GoBack();
            Debug.Log($"Combat Menu: Reset from Normal Menu.");
        }
    }

    public void DisableAbilityButtons()
    {
        for (int i = 0; i < abilityButtons.Count; i++)
        {
            abilityButtons[i].gameObject.SetActive(false); // Hide extra buttons
        }
    }

    public void DisableTargetButtons()
    {
        for (int i = 0; i < targetButtons.Count; i++)
        {
            targetButtons[i].gameObject.SetActive(false); // Hide extra buttons
        }
    }
}