
namespace VstancedScraper;

class UserDownloader
{
    private const string URL_TEMPLATE = "http://archive.vstanced.com/users.php?m=details&id={0}";
    private static readonly string OUTPUT_FOLDER = Path.Combine(Common.BASE_PATH, "users");

    private readonly SemaphoreSlim semaphore = Common.RequestsSemaphore;

    private readonly Logger logger;
    private readonly HttpClient httpClient;

    public UserDownloader(Logger logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }
    
    public async Task DownloadUsers(int startIndex, int endIndex)
    {
        await logger.Write("Downloading users...");

        if (Directory.Exists(OUTPUT_FOLDER))
            Directory.Delete(OUTPUT_FOLDER, true);
        Directory.CreateDirectory(OUTPUT_FOLDER);

        var tasks = Enumerable.Range(startIndex, endIndex - startIndex + 1).Select(DownloadUser);
        await Task.WhenAll(tasks);

        await logger.Write("All users downloaded");
    }

    private async Task DownloadUser(int userIndex)
    {
        try
        {
            await semaphore.WaitAsync();
            await logger.Write($"Downloading user {userIndex}...");
    
            var url = string.Format(URL_TEMPLATE, userIndex);
            var response = await httpClient.GetAsync(url);
            
            if(!response.IsSuccessStatusCode)
            {
                await logger.Write($"Failed to download user {userIndex}: {response.ReasonPhrase}");
                return;
            }

            var filePath = Path.Combine(OUTPUT_FOLDER, $"{userIndex}.html");
            var content = await response.Content.ReadAsStringAsync();
            await File.WriteAllTextAsync(filePath, content);

            await logger.Write($"User {userIndex} downloaded successfully");
        }
        catch (Exception ex)
        {
            await logger.Write($"Error downloading user {userIndex}: {ex}");
        }
        finally
        {
            semaphore.Release();
        }
    }
}
