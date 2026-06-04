using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using WriteableBitmapEx.Benchmarks;

// Run in-process: WriteableBitmap works on the host's (MTA) thread, and this avoids generating a
// separate WPF-targeted runner project. Pass --filter to scope the run, e.g. "-- --filter *Clear*".
var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

BenchmarkSwitcher.FromAssembly(typeof(DrawingBenchmarks).Assembly).Run(args, config);
