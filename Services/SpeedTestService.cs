using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace GvzkHubOptimizer.Services
{
    public class SpeedTestService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        static SpeedTestService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<double> TestPingAsync(string host = "1.1.1.1")
        {
            try
            {
                using var pinger = new Ping();
                long totalTime = 0;
                int successfulPings = 0;

                for (int i = 0; i < 4; i++)
                {
                    var reply = await pinger.SendPingAsync(host, 1500);
                    if (reply.Status == IPStatus.Success)
                    {
                        totalTime += reply.RoundtripTime;
                        successfulPings++;
                    }
                    await Task.Delay(40);
                }

                return successfulPings > 0 ? Math.Round((double)totalTime / successfulPings, 1) : 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<double> TestDownloadSpeedAsync(Action<double>? progress = null, CancellationToken ct = default)
        {
            try
            {
                // Teste Multi-Stream (4 conexões simultâneas para saturar banda, padrão dos grandes speedtests)
                int streamsCount = 4;
                long totalBytesReceived = 0;
                var sw = Stopwatch.StartNew();

                var tasks = new Task[streamsCount];
                var lastProgressUpdate = Stopwatch.StartNew();

                for (int i = 0; i < streamsCount; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            string url = $"https://speed.cloudflare.com/__down?bytes=25000000";
                            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                            if (!response.IsSuccessStatusCode) return;

                            using var stream = await response.Content.ReadAsStreamAsync(ct);
                            byte[] buffer = new byte[65536];
                            int read;

                            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                            {
                                Interlocked.Add(ref totalBytesReceived, read);

                                if (lastProgressUpdate.ElapsedMilliseconds > 150)
                                {
                                    double sec = sw.Elapsed.TotalSeconds;
                                    if (sec > 0)
                                    {
                                        double currentMbps = (Interlocked.Read(ref totalBytesReceived) * 8.0) / (sec * 1000000.0);
                                        progress?.Invoke(Math.Round(currentMbps, 2));
                                    }
                                    lastProgressUpdate.Restart();
                                }
                            }
                        }
                        catch { }
                    }, ct);
                }

                await Task.WhenAll(tasks);
                sw.Stop();

                double finalSec = sw.Elapsed.TotalSeconds;
                if (finalSec > 0 && totalBytesReceived > 0)
                {
                    double finalMbps = (totalBytesReceived * 8.0) / (finalSec * 1000000.0);
                    return Math.Round(finalMbps, 2);
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<double> TestUploadSpeedAsync(Action<double>? progress = null, CancellationToken ct = default)
        {
            try
            {
                // Teste de Upload em conexões paralelas
                int streamsCount = 3;
                long totalBytesUploaded = 0;
                var sw = Stopwatch.StartNew();

                var tasks = new Task[streamsCount];

                for (int i = 0; i < streamsCount; i++)
                {
                    tasks[i] = Task.Run(async () =>
                    {
                        try
                        {
                            int chunkSize = 6000000; // 6 MB cada stream
                            byte[] payload = new byte[chunkSize];
                            new Random().NextBytes(payload);

                            using var content = new ByteArrayContent(payload);
                            content.Headers.Add("Content-Type", "application/octet-stream");

                            using var response = await _httpClient.PostAsync("https://speed.cloudflare.com/__up", content, ct);
                            if (response.IsSuccessStatusCode)
                            {
                                Interlocked.Add(ref totalBytesUploaded, chunkSize);
                            }
                        }
                        catch { }
                    }, ct);
                }

                await Task.WhenAll(tasks);
                sw.Stop();

                double finalSec = sw.Elapsed.TotalSeconds;
                if (finalSec > 0 && totalBytesUploaded > 0)
                {
                    double finalMbps = (totalBytesUploaded * 8.0) / (finalSec * 1000000.0);
                    return Math.Round(finalMbps, 2);
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
