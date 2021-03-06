﻿using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public class EngineTargetStage : EngineStage
    {
        internal const int MinIterationCount = 15;
        internal const int MaxIterationCount = 100;
        internal const int MaxIdleIterationCount = 20;
        internal const double MaxIdleStdErrRelative = 0.05;

        private readonly int? targetCount;
        private readonly double maxStdErrRelative;
        private readonly bool removeOutliers;
        private readonly List<Measurement> measurements;

        public EngineTargetStage(IEngine engine) : base(engine)
        {
            targetCount = engine.TargetJob.ResolveValueAsNullable(RunMode.TargetCountCharacteristic);
            maxStdErrRelative = engine.TargetJob.ResolveValue(AccuracyMode.MaxStdErrRelativeCharacteristic, engine.Resolver);
            removeOutliers = engine.TargetJob.ResolveValue(AccuracyMode.RemoveOutliersCharacteristic, engine.Resolver);
            var maxSize =targetCount?? MaxIterationCount;
            measurements = new List<Measurement>(maxSize);
        }

        public List<Measurement> RunIdle(long invokeCount, int unrollFactor) 
            => RunAuto(invokeCount, IterationMode.IdleTarget, unrollFactor);

        public List<Measurement> RunMain(long invokeCount, int unrollFactor) 
            => Run(invokeCount, IterationMode.MainTarget, false, unrollFactor);

        internal List<Measurement> Run(long invokeCount, IterationMode iterationMode, bool runAuto, int unrollFactor)
            => runAuto || targetCount == null
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, targetCount.Value, unrollFactor);

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            int iterationCounter = 0;
            bool isIdle = iterationMode.IsIdle();
            double maxErrorRelative = isIdle ? MaxIdleStdErrRelative : maxStdErrRelative;
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(iterationMode, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);

                var statistics = MeasurementsStatistics.Calculate(measurements, removeOutliers);
                double actualError = statistics.StandardError;
                double maxError = maxErrorRelative * statistics.Mean;

                if (iterationCounter >= MinIterationCount && actualError < maxError)
                    break;

                if (iterationCounter >= MaxIterationCount || (isIdle && iterationCounter >= MaxIdleIterationCount))
                    break;
            }
            if (!IsDiagnoserAttached) WriteLine();
            return measurements;
        }

        private List<Measurement> RunSpecific(long invokeCount, IterationMode iterationMode, int iterationCount, int unrollFactor)
        {
            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode, i + 1, invokeCount, unrollFactor));

            if (!IsDiagnoserAttached) WriteLine();

            return measurements;
        }
    }
}