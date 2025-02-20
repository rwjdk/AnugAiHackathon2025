using Microsoft.SemanticKernel;
using System.Text.Json;

namespace FunctionCalling.ShoppingLists;

public class ShoppingListPlugin
{
    [KernelFunction("get_shopping_list")]
    public ShoppingList GetShoppingList()
    {
        if (!File.Exists(GetFilePath()))
        {
            return new ShoppingList();
        }

        string json = File.ReadAllText(GetFilePath());

        return JsonSerializer.Deserialize<ShoppingList>(json);
    }

    private void SaveShoppingList(ShoppingList shoppingList)
    {
        EnsureDirectoryExists(GetDirectoryPath());

        string json = JsonSerializer.Serialize(shoppingList, new JsonSerializerOptions() { WriteIndented = true });
        File.WriteAllText(GetFilePath(), json);
    }

    [KernelFunction("add_ingredient")]
    public void AddIngredient(string ingredientName, double amount, string unitName)
    {
        ShoppingList shoppingList = GetShoppingList();
        shoppingList.Ingredients.Add(new Ingredient { IngredientName = ingredientName, Amount = amount, UnitName = unitName });

        SaveShoppingList(shoppingList);
    }

    [KernelFunction("remove_ingredient")]
    public void RemoveIngredient(string ingredientName)
    {
        ShoppingList shoppingList = GetShoppingList();

        int affectedCount = shoppingList.Ingredients.RemoveAll(ingredient => ingredient.IngredientName == ingredientName);

        SaveShoppingList(shoppingList);
    }

    //[KernelFunction("add_ingredients")]
    //public void AddIngredients(List<Ingredient> ingredients)
    //{
    //    ShoppingList shoppingList = GetShoppingList();

    //    shoppingList.Ingredients.AddRange(ingredients);

    //    SaveShoppingList(shoppingList);
    //}

    [KernelFunction("add_ingredient_to_ignore")]
    public void AddIngredientToIgnore(string ingredientName)
    {
        ShoppingList shoppingList = GetShoppingList();
        shoppingList.IngredientsToIgnore.Add(new Ingredient { IngredientName = ingredientName });

        SaveShoppingList(shoppingList);
    }

    [KernelFunction("remove_ingredient_to_ignore")]
    public void RemoveIngredientToIgnore(string ingredientName)
    {
        ShoppingList shoppingList = GetShoppingList();

        int affectedCount = shoppingList.IngredientsToIgnore.RemoveAll(ingredient => ingredient.IngredientName == ingredientName);

        SaveShoppingList(shoppingList);
    }

    [KernelFunction("add_ingredients_to_ignore")]
    public void AddIngredientsToIgnore(List<string> ingredientNames)
    {
        ShoppingList shoppingList = GetShoppingList();

        foreach (string ingredientName in ingredientNames)
        {
            shoppingList.IngredientsToIgnore.Add(new Ingredient { IngredientName = ingredientName });
        }

        SaveShoppingList(shoppingList);
    }

    // ##### Private utility methods #####

    private string GetDirectoryPath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnugAiHackathon2025");
    }

    private string GetFilePath()
    {
        return Path.Combine(GetDirectoryPath(), "ShoppingList.json");
    }

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath) && directoryPath != "")
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}