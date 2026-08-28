namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class SingleInstanceServiceTests
{
    [TestMethod]
    public void TryAcquire_AllowsOnlyOneOwnerAtATime()
    {
        string mutexName = $@"Local\MyMediaImport.App.Tests.{Guid.NewGuid():N}";
        using SingleInstanceService firstInstance = new(mutexName);
        using SingleInstanceService secondInstance = new(mutexName);

        bool firstAcquired = firstInstance.TryAcquire();
        bool secondAcquired = secondInstance.TryAcquire();

        Assert.IsTrue(firstAcquired);
        Assert.IsFalse(secondAcquired);
    }

    [TestMethod]
    public void Dispose_ReleasesOwnershipForNextInstance()
    {
        string mutexName = $@"Local\MyMediaImport.App.Tests.{Guid.NewGuid():N}";
        SingleInstanceService firstInstance = new(mutexName);
        Assert.IsTrue(firstInstance.TryAcquire());
        firstInstance.Dispose();

        using SingleInstanceService nextInstance = new(mutexName);

        Assert.IsTrue(nextInstance.TryAcquire());
    }
}
