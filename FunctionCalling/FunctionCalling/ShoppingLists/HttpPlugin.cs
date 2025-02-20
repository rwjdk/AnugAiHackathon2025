using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;

namespace FunctionCalling.ShoppingLists
{
    public class HttpPlugin
    {
        [KernelFunction("get_web_page")]
        public async Task<string> GetWebPage(string url)
        {
            using var httpClient = new HttpClient();  // Should use HttpClientFactory - I am just lazy here

            string html = await httpClient.GetStringAsync(url);

            string noScriptHtml = Regex.Replace(html, "<script.*?>.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            string textOnly = Regex.Replace(noScriptHtml, "<.*?>", string.Empty);

            return textOnly;
        }
    }
}
