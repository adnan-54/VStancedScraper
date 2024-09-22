using HtmlAgilityPack;
using VstancedScraper;

namespace VStancedScraper;

class FileDownloader
{
    private static readonly string OUTPUT_FOLDER = Path.Combine(Common.BASE_PATH, "downloads", "local_files");
    private static readonly string LOCAL_DOWNLOADS_FILE = Path.Combine(Common.BASE_PATH, "downloads", "_local_files.txt");

    private readonly SemaphoreSlim semaphore = new(1);

    private readonly Logger logger;
    private readonly HttpClient httpClient;

    public FileDownloader(Logger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }


    public async Task DownloadFiles()
    {
        await logger.Write("Getting download links...");

        if (!File.Exists(LOCAL_DOWNLOADS_FILE))
        {
            await logger.Write("Local downloads file not found");
            return;
        }

        if (Directory.Exists(OUTPUT_FOLDER))
            Directory.Delete(OUTPUT_FOLDER, true);
        Directory.CreateDirectory(OUTPUT_FOLDER);

        var downloadLinks = await File.ReadAllLinesAsync(LOCAL_DOWNLOADS_FILE);
        var downloadTasks = downloadLinks.Select(DownloadFile);

        await Task.WhenAll(downloadTasks);

        await logger.Write("All download links downloaded");
    }

    private async Task DownloadFile(string fileInfo)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Downloading file {fileInfo}...");

            var pageNumber = fileInfo[..fileInfo.IndexOf(':')];
            var downloadLink = fileInfo[(fileInfo.IndexOf(':') + 1)..].Trim();
            var fileName = GetFilenameFromUrl(downloadLink);

            var response = await httpClient.GetStreamAsync(downloadLink);
            using var fileStream = File.Create(Path.Combine(OUTPUT_FOLDER, fileName));
            await response.CopyToAsync(fileStream);

            await logger.Write($"File {fileInfo} downloaded");
        }
        catch (Exception ex)
        {
            await logger.Write($"Error getting download file from {fileInfo}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string GetFilenameFromUrl(string url)
    {
        return url[(url.LastIndexOf('/') + 1)..];
    }
}
