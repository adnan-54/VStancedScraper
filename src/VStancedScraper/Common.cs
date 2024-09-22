internal static class Common
{
    public const string PHPSESSID = "";
    public const string ctf9d32bcc1039e927 = "";

    public const string BASE_PATH = @"C:\VStanced";

    public const int MAX_CONCURRENT_REQUESTS = 5;

    public static SemaphoreSlim RequestsSemaphore = new(MAX_CONCURRENT_REQUESTS);
}
