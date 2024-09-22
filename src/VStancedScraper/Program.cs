using System.Diagnostics;
using VstancedScraper;
using VStancedScraper;

var stopWatch = Stopwatch.StartNew();

var httpClient = HttpClientFactory.CreateHttpClient();
var logger = new Logger();
var pageDownloader = new PageDownloader(logger, httpClient);
var downloadLinkGrabber = new DownloadLinkDownloader(logger, httpClient);
var forumDownloader = new ForumDownloader(logger, httpClient);
var userDownloader = new UserDownloader(logger, httpClient);
var pmDownloader = new PmDownloader(logger, httpClient);
var fileDownload = new FileDownloader(logger, httpClient);

await logger.Write("Starting downloads...");

await downloadLinkGrabber.DownloadDownloadLinks(1, 3100);
await pageDownloader.DownloadPages(1, 3100);
await forumDownloader.DownloadForums(1, 4500);
await userDownloader.DownloadUsers(1, 64550);
await pmDownloader.DownloadPms();
await fileDownload.DownloadFiles();


stopWatch.Stop();

await logger.Write("Done");
await logger.Write($"{stopWatch.Elapsed.Hours}h {stopWatch.Elapsed.Minutes}min {stopWatch.Elapsed.Seconds}sec elapsed");

Console.WriteLine("Press any key to exit...");
Console.ReadKey();