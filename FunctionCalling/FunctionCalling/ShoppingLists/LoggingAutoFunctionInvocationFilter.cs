using Microsoft.SemanticKernel;

namespace FunctionCalling.ShoppingLists;

public class FunctionCallingFilter : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(AutoFunctionInvocationContext context, Func<AutoFunctionInvocationContext, Task> next)
    {
        ConsoleUtil.WriteLineInformation($"Function '{context.Function.Name}' called");
        if (context.Arguments != null)
        {
            for (int i = 0; i < context.Arguments.Count; i++)
            {
                ConsoleUtil.WriteLineInformation($"- Arg: {context.Arguments.ToList()[i]}");
            }
        }

        try
        {
            if (context.Arguments.Count == 3 && context.Arguments.Any(item => item.Key == "ingredientName") && context.Arguments.Single(item => item.Key == "ingredientName").Value.ToString().Contains("chocolate", StringComparison.CurrentCultureIgnoreCase))
            {
                ConsoleUtil.WriteLineError($"Error: Chocolate is not good for you!");
            }
            else
            {
                await next(context);
            }
        }
        catch (Exception e)
        {
            ConsoleUtil.WriteLineError("Error:" + e.Message);
            throw;
        }
    }
}