
namespace VstancedScraper;

class PageDownloader
{
    private const string URL_TEMPLATE = "http://vstanced.com/page.php?id={0}";
    private static readonly string OUTPUT_FOLDER = Path.Combine(Common.BASE_PATH, "pages");

    private readonly SemaphoreSlim semaphore = Common.RequestsSemaphore;

    private readonly Logger logger;
    private readonly HttpClient httpClient;

    public PageDownloader(Logger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }
    
    public async Task DownloadPages(int startIndex, int endIndex)
    {
        await logger.Write("Downloading pages...");

        if (Directory.Exists(OUTPUT_FOLDER))
            Directory.Delete(OUTPUT_FOLDER, true);
        Directory.CreateDirectory(OUTPUT_FOLDER);

        var tasks = Enumerable.Range(startIndex, endIndex - startIndex + 1).Select(DownloadPage);
        await Task.WhenAll(tasks);

        await logger.Write("All pages downloaded");
    }

    private async Task DownloadPage(int pageIndex)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Downloading page {pageIndex}...");
    
            var url = string.Format(URL_TEMPLATE, pageIndex);
            var response = await httpClient.GetAsync(url);
            
            if(!response.IsSuccessStatusCode)
            {
                await logger.Write($"Failed to download page {pageIndex}: {response.ReasonPhrase}");
                return;
            }

            var filePath = Path.Combine(OUTPUT_FOLDER, $"{pageIndex}.html");
            var content = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(filePath, content);

            await logger.Write($"Page {pageIndex} downloaded successfully");
        }
        catch (Exception ex)
        {
            await logger.Write($"Error downloading page {pageIndex}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }
}
