using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace review
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await using var cache = new Cache<int>(TimeSpan.FromSeconds(5));
            await cache.PushAsync("hoge", 1, CancellationToken.None);
            var(res, error) = await cache.PopAsync("hoge", CancellationToken.None);
            if (error) Console.WriteLine(res);

            await Task.Delay(TimeSpan.FromSeconds(10));
            (res, error) = await cache.PopAsync("hoge", CancellationToken.None);
            if (!error) Console.WriteLine("cleaned up cache.");
        }
    }

    internal class Cache<T> : IAsyncDisposable
    {
        private readonly CancellationTokenSource cts;
        private readonly Task mainTask;
        private readonly ChannelWriter<IMessage> msgWriterChan;
        private Dictionary<string, T> data = new Dictionary<string, T>();

        public Cache(TimeSpan defaultTtl)
        {
            cts = new CancellationTokenSource();

            var msgChan = Channel.CreateBounded<IMessage>(5);
            msgWriterChan = msgChan.Writer;

            async Task MainLoopFunc()
            {
                // clean upの設定
                using var timer = new Timer(defaultTtl.TotalMilliseconds);
                timer.Elapsed += async (s, e) => await msgChan.Writer.WriteAsync(new CleanUp());
                timer.Start();

                // main loop
                await foreach (var msg in msgChan.Reader.ReadAllAsync(cts.Token))
                {
                    switch (msg)
                    {
                        case PushMs pushMs:
                            data.Add(pushMs.key, pushMs.value);
                            continue;

                        case PopMs popMs:
                            if (!data.TryGetValue(popMs.kye, out var value))
                            {
                                await popMs.replyChannel.WriteAsync((default, false));
                                popMs.replyChannel.Complete();
                                continue;
                            }

                            await popMs.replyChannel.WriteAsync((value, true));
                            popMs.replyChannel.Complete();
                            continue;

                        case CleanUp:
                            data = new Dictionary<string, T>();
                            continue;

                        case Stop:
                            // Channelの書き込みを終了して、キューが捌けるのを待つ。
                            msgChan.Writer.Complete();
                            continue;
                    }
                }

                Console.WriteLine("Terminated Cache.");
            }

            mainTask = Task.Factory.StartNew(async () => await MainLoopFunc(), TaskCreationOptions.LongRunning).Unwrap();
        }
        
        public async Task PushAsync(string key, T value, CancellationToken calCancellationToken)
        {
            await msgWriterChan.WriteAsync(new PushMs(key, value), calCancellationToken);
        }

        public async Task<(T, bool)> PopAsync(string key, CancellationToken cancellationToken)
        {
            var replyChan = Channel.CreateUnbounded<(T, bool)>();
            await msgWriterChan.WriteAsync(new PopMs(key, replyChan), cancellationToken);
            return await replyChan.Reader.ReadAsync(cancellationToken);
        }

        private interface IMessage {}
        private record PushMs(string key, T value) : IMessage;
        private record PopMs(string kye, ChannelWriter<(T, bool)> replyChannel) : IMessage;
        private record CleanUp : IMessage;
        private record Stop : IMessage;

        public async ValueTask DisposeAsync()
        {
            // ここをみて、もうちょっとしっかり実装する必要あり。
            // https://docs.microsoft.com/ja-jp/dotnet/standard/garbage-collection/implementing-disposeasync
            await msgWriterChan.WriteAsync(new Stop());
            await mainTask;
        }
    }
}