using System.Collections.Generic;

namespace AntiBayer.Core.Series;

public sealed record SeriesJob(
    IReadOnlyList<string> Files, string OutputFolder, string Prefix, string Suffix,
    ParameterSet Set, BayerPattern Pattern, int Seed, bool SwapBytes);

public sealed record PreflightIssue(string? File, string Message, bool Excludes);

public sealed record PreflightResult(IReadOnlyList<string> Runnable, IReadOnlyList<PreflightIssue> Issues);

public sealed record SeriesProgress(int FileIndex, int FileCount, int Frame, int FrameCount);

public sealed record SeriesReport(
    IReadOnlyList<string> Completed, IReadOnlyList<(string File, string Reason)> Failed,
    IReadOnlyList<string> Skipped, bool Cancelled, bool StoppedDiskFull);
