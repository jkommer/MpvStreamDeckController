namespace MpvStreamDeckController.Tests;

public static class TaskTestExtensions
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public static Task WithTimeout(this Task task) => task.WaitAsync(DefaultTimeout, CancellationToken);

    public static Task<T> WithTimeout<T>(this Task<T> task) => task.WaitAsync(DefaultTimeout, CancellationToken);

    public static Task<T> WithTimeout<T>(this ValueTask<T> task) => task.AsTask().WaitAsync(DefaultTimeout, CancellationToken);
}
