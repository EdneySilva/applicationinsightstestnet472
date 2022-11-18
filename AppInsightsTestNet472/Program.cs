using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppInsightsTestNet472
{
    internal class Program
    {
        public static string InstrumentationKey => "";

        static async Task Main(string[] args)
        {
            if (!args.Contains("--testcase"))
                throw new ArgumentException("Please provide test case to use");

            var testCase = args.Last();
            Guid jobId = Guid.NewGuid();
            switch (testCase)
            {
                case "stcwbc":
                    await RunTestUsingServerTelemetryChannelWithBucketsControl(jobId);
                    break;
                case "stc":
                    await RunTestUsingServerTelemetryChannel(jobId);
                    break;
                case "custom":
                    await RunTestUsingCustomChannel(jobId);
                    break;
                default:
                    return;
            }

            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();
        }

        private static async Task RunTestUsingServerTelemetryChannel(Guid jobId)
        {
            Console.WriteLine("JobId: " + jobId);
            TestUsingServerTelemetryChannel testUsingServerTelemetryChannel = new TestUsingServerTelemetryChannel(InstrumentationKey, jobId);
            testUsingServerTelemetryChannel.MaxTelemetryBufferCapacity = 1000;
            await testUsingServerTelemetryChannel.RunAsync(40000);
            Console.WriteLine("Items requested to send: " + testUsingServerTelemetryChannel.RequestsToSend);
            Console.WriteLine("Buckets sent to AI: " + testUsingServerTelemetryChannel.Sended);
        }

        private static async Task RunTestUsingServerTelemetryChannelWithBucketsControl(Guid jobId)
        {
            Console.WriteLine("JobId: " + jobId);
            TestUsingServerTelemetryChannelWithBucketsControl testUsingServerTelemetryChannel = new TestUsingServerTelemetryChannelWithBucketsControl(InstrumentationKey, jobId);
            testUsingServerTelemetryChannel.MaxTelemetryBufferCapacity = 1000;
            await testUsingServerTelemetryChannel.RunAsync(40000);
            Console.WriteLine("Items requested to send: " + testUsingServerTelemetryChannel.RequestsToSend);
            Console.WriteLine("Buckets sent to AI: " + testUsingServerTelemetryChannel.Sended);
        }

        private static async Task RunTestUsingCustomChannel(Guid jobId)
        {
            throw new NotImplementedException();
        }
    }

    public class TestUsingServerTelemetryChannel
    {
        public int Sended => sended;
        public int RequestsToSend => requestsToSend;

        private static int sended;
        private static int requestsToSend;

        public int MaxTelemetryBufferCapacity
        {
            get { return _channel?.MaxTelemetryBufferCapacity ?? 0; }
            set
            {
                if (_channel != null)
                    _channel.MaxTelemetryBufferCapacity = value;
            }
        }

        public Guid JobId { get; }

        private readonly TelemetryClient _telemetryClient;
        private readonly ServerTelemetryChannel _channel;

        public TestUsingServerTelemetryChannel(string instrumentationKey, Guid jobId)
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.Active;
            _channel = new ServerTelemetryChannel();
            configuration.TelemetryChannel = _channel;
            _channel.TransmissionStatusEvent = new EventHandler<TransmissionStatusEventArgs>((obj, evt) =>
            {
                if (evt.Response.StatusCode == 200)
                {
                    Interlocked.Increment(ref sended);
                }
                else
                {
                    Console.Beep();
                    Console.WriteLine($"{evt.Response.StatusCode} - {evt.Response.StatusDescription ?? string.Empty}");
                }
            });
            configuration.ConnectionString = instrumentationKey;
            _telemetryClient = new TelemetryClient(configuration);
            JobId = jobId;
        }

        public async Task RunAsync(int ingestion, Func<Task> forceDelay = null)
        {
            var jobId = JobId.ToString();
            Parallel.For(0, ingestion, (e) =>
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("JobId", jobId);
                dictionary.Add("Index", e.ToString());
                dictionary.Add("Channel", "ServerTelemetryChannel");
                _telemetryClient.TrackTrace(DateTime.Now.ToString(), dictionary);
                Interlocked.Increment(ref requestsToSend);
                Console.WriteLine("sending " + e);
                if (forceDelay != null)
                    forceDelay().Wait();
            });
            _telemetryClient.FlushAsync(CancellationToken.None).Wait();
            _channel.Dispose();
        }
    }

    public class TestUsingServerTelemetryChannelWithBucketsControl
    {
        public int Sended => sended;
        public int RequestsToSend => requestsToSend;

        private static int sended;
        private static int requestsToSend;

        public int MaxTelemetryBufferCapacity
        {
            get { return _channel?.MaxTelemetryBufferCapacity ?? 0; }
            set
            {
                if (_channel != null)
                    _channel.MaxTelemetryBufferCapacity = value;
            }
        }

        public Guid JobId { get; }

        private readonly TelemetryClient _telemetryClient;
        private readonly ServerTelemetryChannel _channel;

        public TestUsingServerTelemetryChannelWithBucketsControl(string instrumentationKey, Guid jobId)
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.Active;
            _channel = new ServerTelemetryChannel();
            configuration.TelemetryChannel = _channel;
            _channel.TransmissionStatusEvent = new EventHandler<TransmissionStatusEventArgs>((obj, evt) =>
            {
                if (evt.Response.StatusCode == 200)
                {
                    Interlocked.Increment(ref sended);
                }
                else
                {
                    Console.Beep();
                    Console.WriteLine($"{evt.Response.StatusCode} - {evt.Response.StatusDescription ?? string.Empty}");
                }
            });
            configuration.ConnectionString = instrumentationKey;
            _telemetryClient = new TelemetryClient(configuration);
            JobId = jobId;
        }

        public async Task RunAsync(int ingestion, Func<Task> forceDelay = null)
        {
            var jobId = JobId.ToString();
            Parallel.For(0, ingestion, (e) =>
            {
                var dictionary = new Dictionary<string, string>();
                dictionary.Add("JobId", jobId);
                dictionary.Add("Index", e.ToString());
                dictionary.Add("Channel", "ServerTelemetryChannel");
                _telemetryClient.TrackTrace(DateTime.Now.ToString(), dictionary);
                Interlocked.Increment(ref requestsToSend);
                Console.WriteLine("sending " + e);
                if (forceDelay != null)
                    forceDelay().Wait();
            });
            _telemetryClient.FlushAsync(CancellationToken.None).Wait();
            await Task.Run(async () =>
            {
                var lastValue = 0;
                var attempts = 0;
                while (true)
                {
                    await _channel.FlushAsync(CancellationToken.None);
                    var result = (int)Math.Floor(sended / (decimal)MaxTelemetryBufferCapacity);
                    if (sended == requestsToSend)
                    {
                        attempts++;
                    }
                    lastValue = sended;

                    if (sended >= result || attempts == 5)
                    {
                        break;
                    }
                }
            });
            _channel.Dispose();
        }
    }
}
