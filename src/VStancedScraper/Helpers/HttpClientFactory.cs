using System.Net;

namespace VstancedScraper;

class HttpClientFactory
{
    private static HttpClient? httpClient;

    public static HttpClient CreateHttpClient()
    {
        EnsureHttpClient();

        return httpClient!;
    }

    private static void EnsureHttpClient()
    {
        if (httpClient is not null)
            return;

        var httpHandler = CreateHttpHandler();

        httpClient = new HttpClient(httpHandler);
    }

    private static HttpClientHandler CreateHttpHandler()
    {
        var cookies = CreateCookies();

        return new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            CookieContainer = cookies,
            UseCookies = true,
            AllowAutoRedirect = false
        };
    }

    private static CookieContainer CreateCookies()
    {
        var cookies = new CookieContainer();
        cookies.Add(new Cookie("PHPSESSID", Common.PHPSESSID, "/", "vstanced.com"));
        cookies.Add(new Cookie("ctf9d32bcc1039e927", Common.ctf9d32bcc1039e927, "/", ".vstanced.com"));

        return cookies;
    }
}