using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class PageManager : MonoBehaviour
{
    [Header("Page Management")]
    public GameObject homePage;
    public GameObject backButton; 

    private Stack<GameObject> pageHistory = new Stack<GameObject>();

    void Start()
    {
        // Start with Home, but treat it as a Tab switch to initialize cleanly
        SwitchToTab(homePage);
    }

    // --- USE THIS FOR: Items inside a page (e.g. clicking a specific room) ---
    public void OpenPage(GameObject newPage)
    {
        // Prevent opening the same page we are already on
        if (pageHistory.Count > 0 && pageHistory.Peek() == newPage)
            return;

        // 1. Hide current page
        if (pageHistory.Count > 0)
        {
            pageHistory.Peek().SetActive(false);
        }

        // 2. Show new page
        newPage.SetActive(true);
        pageHistory.Push(newPage);

        // 3. Update Back Button
        UpdateUI();
    }

    // --- USE THIS FOR: Tab Bar Buttons (Home, Rooms, Settings) ---
    public void SwitchToTab(GameObject tabPage)
    {
        // 1. If we are already at the root of this tab, do nothing
        if (pageHistory.Count == 1 && pageHistory.Peek() == tabPage)
            return;

        // 2. Hide the current page (whatever it is)
        if (pageHistory.Count > 0)
        {
            pageHistory.Peek().SetActive(false);
        }

        // 3. CLEAR the history. Tabs wipe the slate clean.
        pageHistory.Clear();

        // 4. Show the new tab root
        tabPage.SetActive(true);
        pageHistory.Push(tabPage);

        // 5. Update UI (Back button will disappear since count is 1)
        UpdateUI();
    }

    public void GoBack()
    {
        if (pageHistory.Count > 1)
        {
            GameObject currentPage = pageHistory.Pop();
            currentPage.SetActive(false);

            pageHistory.Peek().SetActive(true);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // Only show back button if we are deeper than the root level
        if (backButton != null)
        {
            bool shouldShowBackButton = pageHistory.Count > 1;
            backButton.SetActive(shouldShowBackButton);
        }
    }
}