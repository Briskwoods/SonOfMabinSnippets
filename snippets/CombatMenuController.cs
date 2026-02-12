// @title: Combat Menu Controller
// @description: A menu controller that uses a combination of MVP Architecture and Observer Patterns to control sending and recieving data from the UI
// @category: systems, patterns, utilities
// @tags: UI, MVP, Observer Pattern, MVVC/MVP

using System;
using System.Collections.Generic;
using System.Diagnostics;

public class CombatMenuController : MenuController
{
    [Header("HUD Objects")]
    public GameObject canvas;
    public GameObject actionsHUD, selectHUD, roundsHUD;

    [Header("Stance System")]
    public List<GameObject> stanceButtons = new List<GameObject>();
    public GameObject currentStance;

    [Header("Private Variables")]
    bool isSelecting = false;
    TBCManager combatManager;

    [Header("Ability Queue")]
    public TextMeshProUGUI queueStatusText;

    [Header("Combat UI's and Sub-Menu's")]
    public CombatHUDModel combatUI;
    public SelectHUDModel selectUI;
    public AbilityHUDModel abilitiesUI;

    [Header("Data Containers")]
    public List<TargetBtnDataContainer> targetDataContainers = new List<TargetBtnDataContainer>();
    public List<AbilityBtnDataContainer> abilityDataContainers = new List<AbilityBtnDataContainer>();

    [Header("Rounds UI System")]
    public RoundsUIController roundsUIController;

    // ACTIONS AND EVENTS
    public event Action<MenuModel> OnAbilitiesMenuRequested;
    public event Action<AbilityBtnDataContainer> OnAbilitySelected;
    public event Action<MenuModel> OnSelectTargetMenuRequested;
    public event Action<MenuModel> OnInventoryMenuRequested;
    public event Action OnAttackRequested;
    public event Action OnParryRequested;
    public event Action OnTauntRequested;
    public event Action OnFleeRequested;
    public event Action<TargetBtnDataContainer> OnTargetSelected;

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////// ON START/AWAKE EVENTS ///////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetCombatManager(TBCManager manager)
    {
        combatManager = manager;
    }

    void SetupTargetButtons()
    {
        selectUI.Init();
        targetDataContainers = new List<TargetBtnDataContainer>(selectUI.dataContainers);
    }

    void SetupAbilityButtons()
    {
        abilitiesUI.Init();
        abilityDataContainers = new List<AbilityBtnDataContainer>(abilitiesUI.dataContainers);
    }

    private void Start()
    {
        Debug.Log($"Combat Menu: Current Menu: {currentMenu}");
        SetupTargetButtons();
        SetupAbilityButtons();
    }

    private void OnEnable()
    {
        // INPUT RECIVER FUNCITONS
        InputReciever.OnSubmitPressed += ClickBtn;
        InputReciever.OnBackPressed += GoBack;
        InputReciever.OnNavigate += NavigateMenu;
        InputReciever.OnMenuOpen += OpenMenu;

        // TURN BASED COMBAT FUNCTIONS
        EventBus.Subscribe<CombatMenuResetEventData>(ResetCombatMenu);
    }

    private void OnDisable()
    {
        // INPUT RECIVER FUNCITONS
        InputReciever.OnSubmitPressed -= ClickBtn;
        InputReciever.OnBackPressed -= GoBack;
        InputReciever.OnNavigate -= NavigateMenu;
        InputReciever.OnMenuOpen -= OpenMenu;

        // TURN BASED COMBAT FUNCTIONS
        EventBus.Unsubscribe<CombatMenuResetEventData>(ResetCombatMenu);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////// STANCE MENU FUNCTIONALITY ///////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void DisableOtherStanceBtns()
    {
        foreach (GameObject button in stanceButtons)
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

        if (menuHistory.Count < 1)
        {
            SetCurrentMenu(combatUI);
            LoadMenuData(combatUI);
            RemoveInactiveButtonsFromCombatUI(stance);
            currentButton = allButtons[GetCurrentButtonIndex()];
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////// COMBAT MENU FUNCTIONALITY ///////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public bool CheckIfCombatHUDIsActive() => actionsHUD.activeInHierarchy ? true : false;

    public void ToggleCombatHUD(bool toggle) => actionsHUD.SetActive(toggle);

    public void ToggleSelectHUD(bool toggle)
    {
        ToggleCombatHUD(!toggle);
        selectHUD.SetActive(toggle);
        isSelecting = toggle;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////// ACTIONS /////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void OnAbilitiesMenuButtonClicked(MenuModel submenu)
    {
        OnAbilitiesMenuRequested?.Invoke(submenu);
    }

    public void OnAbilitySelectedClicked(AbilityBtnDataContainer abilityBtnData)
    {
        OnAbilitySelected?.Invoke(abilityBtnData);
    }

    public void OnSelectTargetClicked(MenuModel submenu)
    {
        OnSelectTargetMenuRequested?.Invoke(submenu);
    }

    public void OnTargetSelectedClcicked(TargetBtnDataContainer targetData)
    {
        OnTargetSelected?.Invoke(targetData);
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

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////// ABILITIES SUB-MENU FUNCTIONALITY //////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void DisplayAbilities(List<Ability> characterAbilities)
    {
        // First we disable all ability btns
        DisableAbilityButtons();
        allButtons.Clear();
        // After we update the Button text/data for each ability to match ability name
        for (int i = 0; i < characterAbilities.Count; i++)
        {
            abilityDataContainers[i].LoadAbilityDataToContainer(characterAbilities[i]);
            abilityDataContainers[i].Open();
        }
    }

    public void OpenAbilitiesSubMenu(MenuModel submenu)
    {
        OpenSubMenu(submenu);
        List<Button> temp = new List<Button>();
        for (int i = 0; i < abilityDataContainers.Count; i++)
        {
            if (abilityDataContainers[i].gameObject.activeSelf) { temp.Add(abilityDataContainers[i].GetButton()); }
        }
        Debug.Log($"Combat Menu: Disabled Unavailable Ability Buttons");
        allButtons = new List<Button>(temp);

        RefreshSelectorPosition();
    }

    public void DisableAbilityButtons()
    {
        for (int i = 0; i < abilityDataContainers.Count; i++)
        {
            abilityDataContainers[i].Close();
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////// TARGET SUB-MENU FUNCTIONALITY ///////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void LoadDataOntoTargetSubmenu(Ability ability, List<CombatCharacterController> targets)
    {
        switch (ability.selectionType)
        {
            case Ability.SelectType.singleSelection:
                LoadTargets(targets, selectUI, false);
                break;
            case Ability.SelectType.multiSelection:
                LoadTargets(targets, selectUI, true);
                break;
        }
    }

    public void LoadTargets(List<CombatCharacterController> targetsToLoad, SelectHUDModel menuToOpen, bool isMutipleTargets)
    {
        switch (isMutipleTargets)
        {
            case true:
                DisableTargetButtons();
                targetDataContainers[0].LoadTargetDataToContainer(targetsToLoad);
                targetDataContainers[0].Open();
                OpenTargetsSubMenu(menuToOpen);
                break;
            case false:
                DisableTargetButtons();
                for (int i = 0; i < targetsToLoad.Count; i++)
                {
                    List<CombatCharacterController> dataContainerList = new List<CombatCharacterController>();
                    dataContainerList.Add(targetsToLoad[i]);
                    targetDataContainers[i].LoadTargetDataToContainer(dataContainerList);
                    targetDataContainers[i].Open();
                    targetDataContainers[i].RenameTargetButton(targetsToLoad[i].name);
                }
                OpenTargetsSubMenu(menuToOpen);
                break;
        }
    }

    public void OpenTargetsSubMenu(SelectHUDModel submenu)
    {
        OpenSubMenu(submenu);
        // We would also toggle the Select HUD active at some point
        ToggleSelectHUD(true);
        //We then remove inactive buttons from the current list
        List<Button> temp = new List<Button>();
        for (int i = 0; i < targetDataContainers.Count; i++)
        {
            if (targetDataContainers[i].gameObject.activeSelf) { temp.Add(targetDataContainers[i].targetButton); }
        }
        allButtons = new List<Button>(temp);
        RefreshSelectorPosition();
    }

    public void DisableTargetButtons()
    {
        for (int i = 0; i < targetDataContainers.Count; i++)
        {
            targetDataContainers[i].Close();
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////// MENU AND CONTROL FUNCTIONS ////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void GoBack()
    {
        switch (isSelecting)
        {
            case true:
                ToggleSelectHUD(false);
                ToggleCombatHUD(true);
                base.GoBack();
                // similar fn for Inventory should be added
                if (currentMenu == abilitiesUI) { RemoveUnavailableAbilitiesFromSelection(); }
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
        for (int i = 0; i < targetDataContainers.Count; i++)
        {
            if (targetDataContainers[i].gameObject.activeSelf) { temp.Add(targetDataContainers[i].GetButton()); }
        }
        allButtons = new List<Button>(temp);
        Debug.Log($"Combat Menu: Disabled Unavailable Target Buttons");
    }


    public void RemoveUnavailableAbilitiesFromSelection()
    {
        List<Button> temp = new List<Button>();
        for (int i = 0; i < abilityDataContainers.Count; i++)
        {
            if (abilityDataContainers[i].gameObject.activeSelf) { temp.Add(abilityDataContainers[i].GetButton()); }
        }
        Debug.Log($"Combat Menu: Disabled Unavailable Ability Buttons");
        allButtons = new List<Button>(temp);
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
            // Added as a catch to prevent UI coming up during player turn, but the TBC Manager should handle this not here
            if (combatManager.GetPlayerTurn)
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
}