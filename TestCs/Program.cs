using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace TestCs
{
    internal class Program
    {
        private const int ThreadsCount = 5;
        private static readonly string[] Keys = new[] { "recognizer1", "recognizer2" };
        private static readonly long[] GetKeyCountConcurrentDictionary = new long[ThreadsCount];
        private static readonly long[] GetKeyCountDictionaryWithReaderWriterLock = new long[ThreadsCount];
        private static readonly ManualResetEventSlim syncEvent = new ManualResetEventSlim(false);

        private static ConcurrentDictionary<string, string> concurrentDictionary;

        private static readonly ReaderWriterLockSlim readerWriterLockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static Dictionary<string, string> dictionaryWithReaderWriterLock;

        private static void Main(string[] args)
        {
            concurrentDictionary = new ConcurrentDictionary<string, string>(Keys.ToDictionary(x => x, x => x));
            dictionaryWithReaderWriterLock = new Dictionary<string, string>(Keys.ToDictionary(x => x, x => x));

            TestConcurrentDictionary();
            Console.WriteLine("Read counts with concurrent dictionary:");
            Console.WriteLine($"{string.Join(", ", GetKeyCountConcurrentDictionary.OrderBy(x => x))}\n");

            TestDictionaryWithReaderWriterLock();
            Console.WriteLine("Read counts with dictionary + reader writer lock:");
            Console.WriteLine($"{string.Join(", ", GetKeyCountDictionaryWithReaderWriterLock.OrderBy(x => x))}");

            Console.ReadKey();
        }

        private static void TestConcurrentDictionary()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            syncEvent.Reset();
            var threads = new List<Thread>();
            for (int i = 0; i < ThreadsCount; ++i)
            {
                int threadIndex = i;
                var thread = new Thread(() => ConcurrentDictionaryThreadFunc(threadIndex, cancellationTokenSource.Token))
                {
                    IsBackground = true
                };
                thread.Start();
                threads.Add(thread);
            }

            syncEvent.Set();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            threads.ForEach(t => t.Join());
        }


        private static void ConcurrentDictionaryThreadFunc(int threadIndex, CancellationToken cancellationToken)
        {
            long readCounts = 0;
            syncEvent.Wait(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var key in Keys)
                {
                    concurrentDictionary.TryGetValue(key, out _);
                    ++readCounts;
                }
            }

            GetKeyCountConcurrentDictionary[threadIndex] = readCounts;
        }
        private static void TestDictionaryWithReaderWriterLock()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            syncEvent.Reset();
            var threads = new List<Thread>();
            for (int i = 0; i < ThreadsCount; ++i)
            {
                int threadIndex = i;
                var thread = new Thread(() => DictionaryWithReaderWriterLockThreadFunc(threadIndex, cancellationTokenSource.Token))
                {
                    IsBackground = true
                };
                thread.Start();
                threads.Add(thread);
            }

            syncEvent.Set();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            threads.ForEach(t => t.Join());
        }


        private static void DictionaryWithReaderWriterLockThreadFunc(int threadIndex, CancellationToken cancellationToken)
        {
            long readCounts = 0;
            syncEvent.Wait(cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var key in Keys)
                {
                    readerWriterLockSlim.EnterReadLock();
                    dictionaryWithReaderWriterLock.TryGetValue(key, out _);
                    readerWriterLockSlim.ExitReadLock();
                    ++readCounts;
                }
            }

            GetKeyCountDictionaryWithReaderWriterLock[threadIndex] = readCounts;
        }
    }
}
