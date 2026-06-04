using Xunit;

// WriteableBitmap is thread-affine and the BitmapContext keeps process-wide static state,
// so we serialize tests to keep the STA bitmap operations deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
