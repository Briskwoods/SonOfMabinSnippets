using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("Main Variables")]
    [SerializeField] protected MenuModel prevMenu;[SerializeField] protected MenuModel currentMenu;
    public List<Button> allButtons;
    public Button currentButton;
    int currentBtnPosition { get; set; } = 0;


    bool /*isSelecting = false,*/ isScrolling = false;
    bool submenuOpen { get; set; } = false;
    private int _currentIndex = 0;

    protected Stack<MenuModel> menuHistory = new Stack<MenuModel>();

    private void Start()
    {
        Debug.Log($"Menu: Menu Controller Initialised");
        currentButton = allButtons[0];
        currentBtnPosition = allButtons.IndexOf(currentButton);
        Debug.Log($"Menu: Current Btn: {currentButton} and Current Btn Position: {currentBtnPosition}");
    }

    private void OnEnable()
    {
        InputReciever.OnSubmitPressed += ClickBtn;
        InputReciever.OnBackPressed += GoBack;
        InputReciever.OnNavigate += NavigateMenu;
        InputReciever.OnMenuOpen += OpenMenu;
    }

    private void OnDisable()
    {
        InputReciever.OnSubmitPressed -= ClickBtn;
        InputReciever.OnBackPressed -= GoBack;
        InputReciever.OnNavigate -= NavigateMenu;
        InputReciever.OnMenuOpen -= OpenMenu;
    }

    public void OpenMenu()
    {
        Debug.Log("Menu: Menu Opened");
    }

    // Start is called before the first frame update
    public void OnMenuItemSelected()
    {
        Debug.Log($"Menu: {currentButton.name} is selected" );
    }

    public void NavigateMenu(int direction)
    {
        _currentIndex += direction;
        if (_currentIndex < 0) _currentIndex = allButtons.Count - 1;
        if (_currentIndex >= allButtons.Count) _currentIndex = 0;

        EventSystem.current.SetSelectedGameObject(allButtons[_currentIndex].gameObject);
        currentButton = allButtons[_currentIndex];
        Debug.Log($"Menu: Selected: {allButtons[_currentIndex].name}");
    }

    void ScrollControl()
    {
        Vector2 moveInput = CodeManager.Instance._inputReciever.moveVal;
        
        switch (moveInput.x < -.8 || moveInput.y > .8)
        {
            case true:
                NavigateMenuUp();
                break;
            case false:
                break;
        }

        switch (moveInput.y < -.8 || moveInput.x > .8)
        {
            case true:
                NavigateMenuDown();
                break;
            case false:
                break;
        }
    }


    void TriggerScroll()
    {
        if (!isScrolling) {
            isScrolling = true;
            StartCoroutine(nameof(Scroll));
        }
    }

    public void StopScroll()
    {
        isScrolling = false;
    }

    IEnumerator Scroll()
    {
        while (isScrolling)
        {
            ScrollControl();
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Menu: Stopped Scrolling");
    }

    public void NavigateMenuUp()
    {
        //Debug.Log("Scroll Up");
        int prevIndex = currentBtnPosition - 1;
        if (prevIndex < 0) { prevIndex = allButtons.Count - 1; }
        EventSystem.current.SetSelectedGameObject(allButtons[prevIndex].gameObject);
        Debug.Log($"Menu: {allButtons[prevIndex].gameObject.name} is selected");
        currentBtnPosition = prevIndex;
        currentButton = allButtons[currentBtnPosition];
    }

    public void NavigateMenuDown()
    {
        //Debug.Log("Scroll Down");
        int nextIndex = currentBtnPosition + 1;
        if (nextIndex > allButtons.Count - 1) { nextIndex = 0; }
        EventSystem.current.SetSelectedGameObject(allButtons[nextIndex].gameObject);
        Debug.Log($"Menu: {allButtons[nextIndex].gameObject.name} is selected");
        currentBtnPosition = nextIndex;
        currentButton = allButtons[currentBtnPosition];
    }

    //[ContextMenu("Click Btn")]
    public void ClickBtn()
    {
        Debug.Log($"Menu: {currentButton.name} is clicked");
        currentButton.onClick.Invoke();
    }

    public MenuModel GetCurrentMenu() => currentMenu;
    public void SetCurrentMenu(MenuModel menuToSet) => currentMenu = menuToSet; 
    public MenuModel GetPrevMenu() => prevMenu;
    public void SetPrevMenu(MenuModel menuToSet)
    {
        prevMenu = menuToSet;
        Debug.Log($"Menu: Prev menu is {menuToSet.name}");
    }

    public void SetCurrentButtonIndex(int pos) => currentBtnPosition = pos;
    public int GetCurrentButtonIndex() => currentBtnPosition;

    public void SetSubMenuOpen(bool set) => submenuOpen = set;
    public bool GetSubMenuOpen() => submenuOpen;


    public void LoadMenuData(MenuModel menu)
    {
        Debug.Log($"Menu: {menu} Menu loaded.");
        allButtons = new List<Button>(menu.menuButtons);
        currentBtnPosition = menu.menuIndex;
        currentButton = allButtons[currentBtnPosition];
    }

    public void AddToMenuHistory(MenuModel menu)
    {
        menuHistory.Push(menu);
        Debug.Log($"Menu: Loaded {menu} into the Stack, current stack size is {menuHistory.Count}");
    }
    public void RemoveFromMenuHistory()
    {
        menuHistory.Pop();
        Debug.Log($"Menu: Removed item from the Stack, current stack size is {menuHistory.Count}");
    }

    public MenuModel RemoveFromMenuHistoryAndRetain()
    {
        MenuModel toRemove = menuHistory.Pop();
        Debug.Log($"Menu: Removed {toRemove} from the Stack, current stack size is {menuHistory.Count}");
        return toRemove;
    }

    //[ContextMenu("Go Back")]
    public virtual void GoBack()
    {
        Debug.Log($"Menu: Back is clicked");
        if (menuHistory.Count > 0 && prevMenu != null)
        {
            currentMenu.Close();
            MenuModel previousMenu = RemoveFromMenuHistoryAndRetain(); // Get the last active menu
            Debug.Log(menuHistory.Count);
            
            currentMenu = previousMenu;
            allButtons = new List<Button>(previousMenu.menuButtons);
            currentBtnPosition = previousMenu.menuIndex;
            currentButton = allButtons[currentBtnPosition];
            
            if (menuHistory.Count > 0)
            {
                prevMenu = menuHistory.Peek();
                Debug.Log($"Menu: prev Menu is {menuHistory.Peek()}");
            }
            else
            {
                prevMenu = null;
                submenuOpen = false;
            }
        }
        else
        {
            CloseMainMenu();
        }
    }

    public void CloseMainMenu()
    {
        // Close Menu
        Debug.Log($"Menu: Main Menu Closed");
    }

    int called = 0;
    public void OpenSubMenu(MenuModel submenu)
    {
        called++;
        Debug.Log($"called {called} times");
        submenuOpen = true;
        // Here we also move our nav point to the next menu at default menu index
        SetPrevMenu(currentMenu);

        if (!menuHistory.Contains(currentMenu)) { AddToMenuHistory(currentMenu); }

        prevMenu.menuIndex = _currentIndex;
        
        LoadMenuData(submenu);
        SetCurrentMenu(submenu);
        
        currentMenu.Open();
    }

    [ContextMenu("Close Sub Menu")]
    public virtual void CloseSubMenu()
    {
        currentMenu.Close();
        MenuModel previousMenu = RemoveFromMenuHistoryAndRetain(); // Get the last active menu
        Debug.Log($"Menu: Menu History Size -> {menuHistory.Count}");

        currentMenu = previousMenu;
        allButtons = new List<Button>(previousMenu.menuButtons);
        currentBtnPosition = previousMenu.menuIndex;
        currentButton = allButtons[currentBtnPosition];

        if (menuHistory.Count > 0)
        {
            prevMenu = menuHistory.Pop();
            Debug.Log($"Menu: prev Menu is {prevMenu.name}");
        }
        else
        {
            prevMenu = null;
            submenuOpen = false;

        }
    }


    // Shouldn't exist here////// Should essentially be in the input manager but i know this is for the script to basically be uncallable with specific fns
    // tbf I think this is unneccessary as we can disable to GO when needed to unsubscirbe the events and prevent the from happening 
    public void ToggleInput(bool toggle)
    {
        // This function should be the one that controls the statemachine active state
        //CodeManager.Instance._inputReciever.gameObject.SetActive(toggle);
        Debug.Log($"Menu: Toggle Input State {toggle}");
        switch (toggle)
        {
            case true:
                EventSystem.current.SetSelectedGameObject(allButtons[_currentIndex].gameObject);
                break;
            case false:
                EventSystem.current.SetSelectedGameObject(null);
                break;
        }

        // control below
        CodeManager.Instance._inputReciever.gameObject.SetActive(toggle);

    }

    // We need a function that updates the pointer position on events called and not when it happens persistently
    // function should be called UpdatePointer or Highlight Action or sth, leaning more towards highlight action
    // Each time its called the pointer position is moved to that obj
    // Can be updated to show/match the item box size and position
}
