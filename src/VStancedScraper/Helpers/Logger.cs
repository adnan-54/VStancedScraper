namespace VstancedScraper;

internal class Logger
{
    private const string LOG_FILE = @"F:\VStanced\logs.txt";
    private readonly SemaphoreSlim semaphore = new(1);

    public Logger()
    {
        if(File.Exists(LOG_FILE))
            File.Delete(LOG_FILE);
    }

    public async Task Write(string message)
    {
        try
        {
            await semaphore.WaitAsync();

            message = $"{DateTime.Now}: {message}{Environment.NewLine}";
            Console.Write(message);
            await File.AppendAllTextAsync(LOG_FILE, message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            semaphore.Release();
        }
    }
}
