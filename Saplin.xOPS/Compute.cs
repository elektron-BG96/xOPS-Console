﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Saplin.xOPS
{
    /// <summary>
    /// Contains a number of methods accepting the number of iterations and returning ammount of seconds it took to complete the operations
    /// </summary>
    public class Compute
    {
        public const int flopsPerIteration = 34; // to be determined based on IL disassembly
        public const int inopsPerIteration = 38;  // to be determined based on IL disassembly

        protected Single prevSingleY;
        protected Double prevDoubleY;
        protected Int32 prevInt32Y;
        protected Int64 prevInt64Y;

        const int microIterationSize = 1 * 1000 * 1000;

        private Stopwatch sw = new Stopwatch();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public double RunXops(int iterations, bool inops, bool precision64bit)
        {
            breakCalled = false;
            var blocks = iterations / microIterationSize;
            var currIterations = microIterationSize;
            double time = 0;
            int i;

            sw.Reset();

            double accum = 0;
            long prevElapsed = 0;

            for (i = 0; i <= blocks; i++)
            {
                if ((breakCalled && !runningInMtMode) || (mtBreakCalled && runningInMtMode)) return -1d;

                if (i == blocks)
                {
                    currIterations = iterations % microIterationSize;
                    if (currIterations == 0) break;
                }

                if (inops && precision64bit)
                {
                    RunInops64Bit(currIterations);
                }
                else if (inops && !precision64bit)
                {
                    RunInops32Bit(currIterations);
                }
                else if (!inops && precision64bit)
                {
                    RunFlops64Bit(currIterations);
                }
                else
                {
                    RunFlops32Bit(currIterations);
                }

                time = ((Double)(sw.ElapsedTicks - prevElapsed)) / Stopwatch.Frequency;
                accum += TimeToGigaOPS(time, microIterationSize, 1, inops: inops);
                prevElapsed = sw.ElapsedTicks
;            }

            time = ((Double)sw.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, 1, inops: inops);

            LastResultSTGigaOPSAveraged = accum/i;

            return time;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void RunFlops32Bit(int iterations)
        {
            // Single precision has 23 bit mantise which in normilized form gives 24 significant bits, i.e. ~16,7m values.
            // The main loop uses Single as a counter and it will stop gorwing after 16.7m iterations as mantisa won't have the precision and counter will stall, endless loop will happen
            if (iterations > 16 * 1000 * 1000)
                throw new ArgumentOutOfRangeException("For single precision float calculations the number of iterations can't be more than 16 millions");

            Single counter = 0, increment = 1, max = iterations;
            Single startValue = -(Single)Math.PI, endValue = (Single)Math.PI, x = startValue, x2, y = 0, pi2 = (Single)(Math.PI * Math.PI), four = 4;
            Single funcInc = (endValue - startValue) / iterations;

            sw.Start();
            // Changes to the body of the loop must be refelected in flopsPerIteration const
            while (counter < max)
            {
                counter = counter + increment;

                x2 = x * x;

                //y = (pi2 - 4 * x2);
                y = four * x2;
                y = pi2 - y;

                //y /= (pi2 + x2);
                x2 = pi2 + x2;
                y = y / x2;

                x = x + funcInc;
            }

            sw.Stop();

            prevDoubleY = y;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void RunFlops64Bit(int iterations)
        {
            Double counter = 0, increment = 1, max = iterations;
            Double startValue = -(Double)Math.PI, endValue = (Double)Math.PI, x = startValue, x2, y = 0, pi2 = (Double)(Math.PI * Math.PI), four = 4;
            Double funcInc = (endValue - startValue) / iterations;

            sw.Start();

            // Changes to the body of the loop must be refelected in flopsPerIteration const
            while (counter < max)
            {
                counter = counter + increment;

                x2 = x * x;

                //y = (pi2 - 4 * x2);
                y = four * x2;
                y = pi2 - y;

                //y /= (pi2 + x2);
                x2 = pi2 + x2;
                y = y / x2;

                x = x + funcInc;
            }

            sw.Stop();

            prevDoubleY = y;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void RunInops32Bit(int iterations)
        {
            Int32 counter = 0, increment = 1, max = iterations;
            Int32 x = Int32.MinValue, x2, y = 0, coef = 3, four = 4, two = 2;
            Int32 funcInc = (Int32)((UInt32.MaxValue) / (Int32)iterations);

            sw.Start();

            // Changes to the body of the loop must be refelected in inopsPerIteration const
            while (counter < max)
            {
                counter = counter + increment;

                x2 = x/two;

                //y = (coef - 4 * x2);
                y = four * x2;
                y = coef - y;

                //y /= (coef + x2);
                x2 = coef + x2;
                y = y / x2;

                //y -= coef;
                y = y - coef;

                x = x + funcInc;
            }

            sw.Stop();

            prevInt32Y = y;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void RunInops64Bit(int iterations)
        {
            Int64 counter = 0, increment = 1, max = iterations;
            Int64 x = Int64.MinValue, x2, y = 0, coef = 3, four = 4, two = 2;
            Int64 funcInc = (Int64)(UInt64.MaxValue / (UInt64)iterations);
            
            sw.Start();

            // Changes to the body of the loop must be refelected in inopsPerIteration const
            while (counter < max)
            {
                counter = counter + increment;

                x2 = x / two;

                //y = (coef - 4 * x2);
                y = four * x2;
                y = coef - y;

                //y /= (coef + x2);
                x2 = coef + x2;
                y = y / x2;

                //y -= coef;
                y = y - coef;

                x = x + funcInc;
            }

            sw.Stop();

            prevInt64Y = y;
        }

        private Stopwatch threadsStopwatch = new Stopwatch();
        ManualResetEventSlim startThreads =  new ManualResetEventSlim();
        CountdownEvent threadsDoneCountdown = new CountdownEvent(1);
        CountdownEvent threadsReadyCountdown = new CountdownEvent(1);

        private void SingleThreadBody(int iterations, bool inops = false, bool precision64Bit = false)
        {

            var sw = new Stopwatch(); sw.Start();

            if (threadsReadyCountdown.IsSet || mtBreakCalled) return;
            threadsReadyCountdown.Signal();
            startThreads.Wait();

            Debug.WriteLine("Started (ms): " + sw.ElapsedMilliseconds);

            RunXops(iterations, inops, precision64Bit);

            //if (!inops)
            //{
            //    if (precision64Bit) RunFlops64Bit(iterations); else RunFlops32Bit(iterations);
            //}
            //else
            //{
            //    if (precision64Bit) RunInops64Bit(iterations); else RunInops32Bit(iterations);
            //}

            Debug.WriteLine("Done (ms): " + sw.ElapsedMilliseconds);

            if (threadsDoneCountdown.IsSet || mtBreakCalled) return;
            threadsDoneCountdown.Signal();
        }

        //List<Thread> thrds;

        Thread[] thrds;
        bool runningInMtMode = false;

        /// <summary>
        /// Runs flops and inops calcuations in dedicated threads
        /// </summary>
        /// <remarks>
        /// Using tasks may lead to pauses and stalling (tens of seconds), seems like snatdard schedulaer doesn't kick off all tasks right away and they keep waiting for a long time. No such problem with threads
        /// </remarks>
        ///<returns>Seconds it took to complete the calculations</returns>
        public Double RunXopsMultiThreaded(int iterations, int threads, bool inops = false, bool precision64Bit = false, bool useTasks = false)
        {
            runningInMtMode = true;
            
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);

            mtBreakCalled = false;

            //if (thrds == null) thrds = new List<Thread>();

            //if (thrds.Count < threads)
            //    for (var i = 0; i < threads - threads.Count; i++)
            //        thrds.Add(null);

            thrds = new Thread[threads];
            var tasks = new Task[threads];

            Debug.WriteLine("Multi-" + (useTasks ? "Tasks" : "Threads"));

            threadsDoneCountdown.Reset(threads);
            threadsReadyCountdown.Reset(threads);
            startThreads.Reset();

            for (int i = 0; i < threads; i++)
            {
                if (!useTasks)
                {
                    // TODO, check values are properly passed to delefate for already created threads
                    thrds[i] = new Thread(() => { SingleThreadBody(iterations, inops, precision64Bit); });
                    thrds[i].IsBackground = true;
                    thrds[i].Start();
                }
                else
                {
                    tasks[i] = new Task(() => { SingleThreadBody(iterations, inops, precision64Bit); });
                    tasks[i].Start();
                }
            }

            threadsReadyCountdown.Wait();
            // Starting stopwatch after Set() leads to spkes in results on Windows, might be issue due to thread scheduling (starting timer might happer after thread is started)
            threadsStopwatch.Restart();
            startThreads.Set();

            threadsDoneCountdown.Wait();
            threadsStopwatch.Stop();

            var time = ((Double)threadsStopwatch.ElapsedTicks) / Stopwatch.Frequency;

            LastResultGigaOPS = TimeToGigaOPS(time, iterations, threads, inops);

            runningInMtMode = false;

            return time;
        }

        public double LastResultGigaOPS
        {
            get; private set;
        }

        /// <summary>
        /// In Single Threaded calculations measure average of each individual run's GFLOPS, rather than divide time it took to complete all runs
        /// by the number of operations. Less influence of freezes that might happen during a single run on the overal result 
        /// </summary>
        public double LastResultSTGigaOPSAveraged
        {
            get; private set;
        }

        public static double TimeToGigaOPS(double time, int iterations, int threads, bool inops)
        {
            return (double)(inops ? inopsPerIteration : flopsPerIteration) * iterations * threads / time / 1000000000;
        }

        public static int CpuCores
        {
            get { return Environment.ProcessorCount; }
        }

        private volatile bool breakCalled = false;
        private volatile bool mtBreakCalled = false;

        public void BreakExecution()
        {
            if (thrds != null)
            {
                breakCalled = mtBreakCalled = true;
                
                threadsReadyCountdown.Reset(0);
                threadsDoneCountdown.Reset(0);
            }
        }
    }
}
