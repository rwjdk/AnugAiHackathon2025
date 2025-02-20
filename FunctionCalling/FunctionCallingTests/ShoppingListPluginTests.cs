using FunctionCalling.ShoppingLists;

namespace FunctionCallingTests;

public class ShoppingListPluginTests
{
    [Fact]
    public async Task DrDk()
    {
        var httpPlugin = new HttpPlugin();

        string html = await httpPlugin.GetWebPage("https://www.dr.dk/mad/opskrift/spisemedprice/pillola-isicia-omentata-romerske-frikadeller");

        Assert.NotNull(html);
        Assert.Contains("1/2 tsk stødt spidskommen", html);
    }

    [Fact]
    public async Task AllRecipes()
    {
        var httpPlugin = new HttpPlugin();

        string html = await httpPlugin.GetWebPage("https://www.allrecipes.com/pistachio-hummus-recipe-11680823");

        Assert.NotNull(html);
        Assert.Contains("garbanzo", html);
    }
}
