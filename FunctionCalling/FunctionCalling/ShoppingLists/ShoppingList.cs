namespace FunctionCalling.ShoppingLists;

public class ShoppingList
{
    public List<Ingredient> Ingredients { get; set; } = new List<Ingredient>();

    public List<Ingredient> IngredientsToIgnore { get; set; } = new List<Ingredient>();
}
