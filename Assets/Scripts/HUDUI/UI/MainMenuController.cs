using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private MainMenuButtonItem[] buttons;

    [Header("Animation")]
    [SerializeField] private float animationSpeed = 10f;
    [SerializeField] private float selectedScale = 1.18f;
    [SerializeField] private float normalScale = 0.95f;
    [SerializeField] private float selectedXOffset = 35f;

    [Header("Scene Loading")]
    [SerializeField] private string playSceneName = "Level_01";

    private int selectedIndex;

    private void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Setup(this);
        }

        selectedIndex = 0;
    }

    private void Update()
    {
        HandleInput();
        UpdateButtonVisuals();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            ChangeSelection(1);
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            ChangeSelection(-1);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ActivateSelected();
        }
    }

    private void ChangeSelection(int direction)
    {
        selectedIndex += direction;

        if (selectedIndex < 0)
            selectedIndex = buttons.Length - 1;

        if (selectedIndex >= buttons.Length)
            selectedIndex = 0;
    }

    public void SelectButton(MainMenuButtonItem button)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == button)
            {
                selectedIndex = i;
                return;
            }
        }
    }

    private void UpdateButtonVisuals()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = i == selectedIndex;

            buttons[i].SetVisual(
                selected,
                animationSpeed,
                selectedScale,
                normalScale,
                selectedXOffset
            );
        }
    }

    public void ActivateSelected()
    {
        string choice = buttons[selectedIndex].buttonName;

        switch (choice)
        {
            case "Play":
                SceneManager.LoadScene(playSceneName);
                break;

            case "Continue":
                Debug.Log("Continue selected.");
                break;

            case "Settings":
                Debug.Log("Settings selected.");
                break;

            case "Extras":
                Debug.Log("Extras selected.");
                break;

            case "Quit":
                Application.Quit();
                Debug.Log("Quit selected.");
                break;
        }
    }
}