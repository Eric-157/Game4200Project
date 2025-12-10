using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the ingredient total on the main menu. Use a separate UI element from the in-game HUD.
/// </summary>
public class MainMenuIngredientUI : MonoBehaviour
{
    public Text ingredientsText;
    private GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (ingredientsText == null) return;
        if (gm == null) gm = GameManager.Instance;
        if (gm != null)
            ingredientsText.text = $"Ingredients: {gm.ingredients}";
        else
            ingredientsText.text = "Ingredients: 0";
    }
}
