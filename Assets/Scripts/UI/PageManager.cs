using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Needed for Button

public class PageManager : MonoBehaviour
{
    [Header("Page Management")]
    public GameObject homePage;
    public GameObject backButton; // Drag your persistent Back Button here

    private Stack<GameObject> pageHistory = new Stack<GameObject>();

    void Start()
    {
        // Start with Home
        OpenPage(homePage);
    }

    public void OpenPage(GameObject newPage)
    {
        // 1. Hide current page
        if (pageHistory.Count > 0)
        {
            pageHistory.Peek().SetActive(false);
        }

        // 2. Show new page
        newPage.SetActive(true);
        pageHistory.Push(newPage);

        // 3. Update the Back Button visibility
        UpdateUI();
    }

    public void GoBack()
    {
        // Only go back if there is history
        if (pageHistory.Count > 1)
        {
            // 1. Pop current page
            GameObject currentPage = pageHistory.Pop();
            currentPage.SetActive(false);

            // 2. Show previous page
            pageHistory.Peek().SetActive(true);

            // 3. Update UI
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // If there is more than 1 page in history, show the button. 
        // If we are at the root (1 page), hide it.
        bool shouldShowBackButton = pageHistory.Count > 1;
        backButton.SetActive(shouldShowBackButton);
    }
}