using System.Text.RegularExpressions;

namespace VstancedScraper;

partial class PmDownloader
{
    private const string URL_TEMPLATE = "http://vstanced.com/pm.php?m=message&id={0}";

    private const string SENT_URL = "http://vstanced.com/pm.php?f=sentbox";
    private static readonly string SENT_OUTPUT_FOLDER = Path.Combine(Common.BASE_PATH, @"pms\sent");

    private const string RECEIVED_URL = "http://vstanced.com/pm.php";
    private static readonly string RECEIVED_OUTPUT_FOLDER = Path.Combine(Common.BASE_PATH, @"pms\received");

    [GeneratedRegex(@"pm\.php\?m=message&amp;id=([0-9]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PmIdRegex();

    private readonly SemaphoreSlim semaphore = Common.RequestsSemaphore;

    private readonly Logger logger;
    private readonly HttpClient httpClient;

    public PmDownloader(Logger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public Task DownloadPms()
    {
        var sentTask = DownloadSentPMs();
        var receivedTask = DownloadReceivedPMs();

        return Task.WhenAll(sentTask, receivedTask);
    }

    private async Task DownloadSentPMs()
    {
        await logger.Write("Downloading sent PMs...");

        if (Directory.Exists(SENT_OUTPUT_FOLDER))
            Directory.Delete(SENT_OUTPUT_FOLDER, true);
        Directory.CreateDirectory(SENT_OUTPUT_FOLDER);

        await DownloadPMsFrom(SENT_URL, SENT_OUTPUT_FOLDER);

        await logger.Write("All sent PMs downloaded");
    }

    private async Task DownloadReceivedPMs()
    {
        await logger.Write("Downloading received PMs...");

        if (Directory.Exists(RECEIVED_OUTPUT_FOLDER))
            Directory.Delete(RECEIVED_OUTPUT_FOLDER, true);
        Directory.CreateDirectory(RECEIVED_OUTPUT_FOLDER);

        await DownloadPMsFrom(RECEIVED_URL, RECEIVED_OUTPUT_FOLDER);

        await logger.Write("All received PMs downloaded");
    }

    private async Task DownloadPMsFrom(string listUrl, string outputFolder)
    {
        var pmsList = await GetPMsFrom(listUrl);
        var pmsTasks = pmsList.Select(pm => DownloadPMs(pm, outputFolder));

        await Task.WhenAll(pmsTasks);
    }

    private async Task<IEnumerable<int>> GetPMsFrom(string url)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Retrieving PMs from {url}...");
            
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await logger.Write($"Failed to retrieve PMs from url {url}: {response.ReasonPhrase}");
                return [];
            }

            var content = await response.Content.ReadAsStringAsync();

            var pms = PmIdRegex().Matches(content)
                .Select(r => r.Groups[1].Value)
                .Distinct()
                .Select(int.Parse)
                .ToList();

            await logger.Write($"{pms.Count} PMs retrieved from {url}");

            return pms;
        }
        catch (Exception ex)
        {
            await logger.Write($"Error while retrieving PMs from {url}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }

        return [];
    }

    private async Task DownloadPMs(int pmIndex, string directory)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Downloading PM {pmIndex}...");

            var url = string.Format(URL_TEMPLATE, pmIndex);
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) {
                await logger.Write($"Failed to download PM {pmIndex}: {response.ReasonPhrase}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var filePath = Path.Combine(directory, $"{pmIndex}.html");
            await File.WriteAllTextAsync(filePath, content);

            await logger.Write($"PM {pmIndex} downloaded successfully");
        }
        catch (Exception ex)
        {
            await logger.Write($"Error downloading PM {pmIndex}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }
}