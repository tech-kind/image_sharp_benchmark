// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using Threshold;

var summary = BenchmarkRunner.Run<ThresholdMeasurements>();