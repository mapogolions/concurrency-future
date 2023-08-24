using Futures.Internal;

namespace Futures.Tests;


public class FirstCompletedAwaiterPolicyTest
{
    [Fact]
    public void ShouldWaitFutureUntilItCompletedWithException()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future1.SetException(new InvalidOperationException());
        });

        // Act
        t.Start();
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Arrange
        Assert.Single(done);

    }

    [Fact]
    public void ShouldWaitFutureUntilItCompletedWithResult()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        var t = new Thread(() =>
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(50));
            future1.SetResult("foo");
        });

        // Act
        t.Start();
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Arrange
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItHasBeenCancelledBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.Cancel();

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItCompletedWithExceptionBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetException(new InvalidOperationException());

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Single(done);
    }

    [Fact]
    public void ShouldAddFutureToCompletedCollection_WhenItIsAlreadyCompletedWithResultBeforeWait()
    {
        // Arrange
        ICompletableFuture future1 = new Future();
        ICompletableFuture future2 = new Future();
        future1.SetResult("foo");

        // Act
        var done = Future.Wait(FutureWaitPolicy.FirtCompleted, (Future)future1, (Future)future2);

        // Assert
        Assert.Single(done);
    }
}
