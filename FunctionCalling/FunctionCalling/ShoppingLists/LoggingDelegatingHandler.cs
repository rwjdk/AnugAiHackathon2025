namespace FunctionCalling.ShoppingLists;

public class LoggingDelegatingHandler : DelegatingHandler
{
    public LoggingDelegatingHandler(HttpMessageHandler? innerHandler = null)
    {
        InnerHandler = innerHandler ?? new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string requestContent = await request.Content?.ReadAsStringAsync(cancellationToken)!;
        ConsoleUtil.WriteLineInformation($"Raw Request to LLM: {request} \nRaw Request Content: {requestContent}");

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        ConsoleUtil.WriteLineInformation($"Raw Request from LLM: {response} \nRaw Request Content: {responseContent}");

        // Reset the stream
        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var newContent = new StreamContent(memoryStream);
        foreach (var header in response.Content.Headers)
        {
            newContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        response.Content = newContent;

        return response;
    }
}