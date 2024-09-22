using HtmlAgilityPack;

namespace VstancedScraper;

class DownloadLinkDownloader
{
    private const string URL_TEMPLATE = "http://vstanced.com/page.php?id={0}";
    private static readonly string OUTPUT_FOLDER = Path.Combine(Common.BASE_PATH, "downloads");
    private static readonly string LOCAL_DOWNLOADS_FILE = Path.Combine(OUTPUT_FOLDER, "_local_files.txt");

    private readonly SemaphoreSlim semaphore = new(1);
    private readonly HtmlDocument htmlDocument = new();

    private readonly Logger logger;
    private readonly HttpClient httpClient;

    public DownloadLinkDownloader(Logger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task DownloadDownloadLinks(int startIndex, int endIndex)
    {
        await logger.Write("Getting download links...");

        if (Directory.Exists(OUTPUT_FOLDER))
            Directory.Delete(OUTPUT_FOLDER, true);
        Directory.CreateDirectory(OUTPUT_FOLDER);

        if (File.Exists(LOCAL_DOWNLOADS_FILE))
            File.Delete(LOCAL_DOWNLOADS_FILE);
        await File.WriteAllTextAsync(LOCAL_DOWNLOADS_FILE, string.Empty);

        var tasks = Enumerable.Range(startIndex, endIndex - startIndex + 1).Select(GetDownloadLink);
        await Task.WhenAll(tasks);

        await logger.Write("All download links downloaded");
    }

    private async Task GetDownloadLink(int pageIndex)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Getting download link for page {pageIndex}...");
                        
            if (!await IsDownloadPage(pageIndex))
            {
                await logger.Write($"Page {pageIndex} does not seems to be a download page");
                return;
            }

            var outputFilePath = Path.Combine(OUTPUT_FOLDER, $"{pageIndex}.txt");
            await File.WriteAllTextAsync(outputFilePath, string.Empty);

            var downloadLink = await TryGetDownloadLink(pageIndex);

            if (string.IsNullOrEmpty(downloadLink))
            {
                await logger.Write($"Download link for page {pageIndex} not found");
                return;
            }

            await logger.Write($"Download link for page {pageIndex}: {downloadLink}");

            if (downloadLink.Contains("vstanced.com/", StringComparison.OrdinalIgnoreCase))
            {
                await logger.Write($"Download for page {pageIndex} seems to be hosted locally");
                await File.AppendAllTextAsync(LOCAL_DOWNLOADS_FILE, $"{pageIndex}: {downloadLink}{Environment.NewLine}");
            }

            await File.WriteAllTextAsync(outputFilePath, downloadLink);
        }
        catch (Exception ex)
        {
            await logger.Write($"Error getting download link for page {pageIndex}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<bool> IsDownloadPage(int pageIndex)
    {
        var pageUrl = string.Format(URL_TEMPLATE, pageIndex);
        var pageResponse = await httpClient.GetAsync(pageUrl);
        var pageContent = await pageResponse.Content.ReadAsStreamAsync();

        htmlDocument.Load(pageContent);
        var sidebarText = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"sidebar\"]/div[2]/div/div[1]/div/h3")?.InnerText;
        var breadcrumbText = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/div/div/div[1]/div/h3/a[1]")?.InnerText;

        if(string.IsNullOrEmpty(sidebarText) || string.IsNullOrEmpty(breadcrumbText))
            return false;

        return sidebarText.Equals("Download", StringComparison.OrdinalIgnoreCase) && breadcrumbText.Equals("Downloads", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> TryGetDownloadLink(int pageIndex)
    {
        var pageUrl = string.Format(URL_TEMPLATE, pageIndex);
        var downloadUrl = $"{pageUrl}&a=dl";

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await logger.Write($"Attempt {attempt + 1} for page {pageIndex}...");

            if(attempt > 0) 
                await Task.Delay(100 * attempt);

            var downloadResponse = await httpClient.GetAsync(downloadUrl);
            
            if (downloadResponse.StatusCode != System.Net.HttpStatusCode.Redirect)
                continue;

            var downloadLink = downloadResponse.Headers.Location?.ToString();

            if(string.IsNullOrEmpty(downloadLink))
                continue;

            if (pageUrl.Equals(downloadLink, StringComparison.InvariantCultureIgnoreCase))
                continue;

            return downloadLink;    
        }

        return string.Empty;
    }
}