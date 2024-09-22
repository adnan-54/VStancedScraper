using System.Text.RegularExpressions;

namespace VstancedScraper;

partial class ForumDownloader
{
    private const string URL_TEMPLATE = "http://vstanced.com/forums.php?m=posts&q={0}&d={1}";
    private static readonly string OUTPUT_FOLDER =  Path.Combine(Common.BASE_PATH, "forums");

    [GeneratedRegex(@"<a href=""forums\.php\?m=posts&amp;q=\d+&amp;d=(\d+)"">&gt;&gt;<\/a>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PageCountRegex();

    private readonly SemaphoreSlim semaphore = Common.RequestsSemaphore;

    private readonly Logger logger;
    private readonly HttpClient httpClient;

    public ForumDownloader(Logger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    public async Task DownloadForums(int startIndex, int endIndex)
    {
        await logger.Write("Downloading forums...");

        if (Directory.Exists(OUTPUT_FOLDER))
            Directory.Delete(OUTPUT_FOLDER, true);
        Directory.CreateDirectory(OUTPUT_FOLDER);

        var tasks = Enumerable.Range(startIndex, endIndex - startIndex + 1).Select(DownloadPage);
        await Task.WhenAll(tasks);

        await logger.Write("All forums downloaded");
    }

    private async Task DownloadPage(int forumIndex)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Downloading forum {forumIndex}...");

            var url = string.Format(URL_TEMPLATE, forumIndex, 0);
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await logger.Write($"Failed to download forum {forumIndex}: {response.ReasonPhrase}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            
            var fileName = Path.Combine(OUTPUT_FOLDER, $"{forumIndex} 1.html");
            await File.WriteAllTextAsync(fileName, content);
            
            await logger.Write($"Downloaded initial page for forum {forumIndex}");
            await logger.Write("Checking for extra pages");

            var pagesCount = GetPagesCount(content);

            if (pagesCount != 0)
            {
                await logger.Write($"Forum {forumIndex} has {pagesCount} pages");

                for( int i = 1; i < pagesCount; i++)
                {
                    await logger.Write($"Downloading page {i+1} for forum {forumIndex}");

                    url = string.Format(URL_TEMPLATE, forumIndex, i * 40);
                    response = await httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        await logger.Write($"Failed to download page {i+1} for forum {forumIndex}: {response.ReasonPhrase}");
                        continue;
                    }

                    content = await response.Content.ReadAsStringAsync();
                    fileName = Path.Combine(OUTPUT_FOLDER, $"{forumIndex} {i+1}.html");
                    await File.WriteAllTextAsync(fileName, content);
                }
            }
            else
                await logger.Write($"No extra pages found for forum {forumIndex}");
            
            await logger.Write($"Forum {forumIndex} downloaded successfully");
        }
        catch (Exception ex)
        {
            await logger.Write($"Error downloading forum {forumIndex}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static int GetPagesCount(string content)
    {
        try
        {
            var extractedPostCount = PageCountRegex().Match(content).Groups[1].Value;
            return (int.Parse(extractedPostCount) / 40) + 1;
        }
        catch
        {
            return 0;
        }
    }

}
