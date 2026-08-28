namespace MyMediaImport.App.Tests;

[TestClass]
public sealed class WindowPlacementValidatorTests
{
    [TestMethod]
    public void TryGetVisibleGeometry_WhenWindowIsOnAvailableMonitor_ReturnsGeometry()
    {
        WindowPlacementSettings settings = CreateSettings(120d, 80d, 1180d, 760d);
        WindowWorkArea[] workAreas = [new(0d, 0d, 1920d, 1040d)];

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            workAreas,
            out WindowGeometry geometry);

        Assert.IsTrue(isValid);
        Assert.AreEqual(120d, geometry.Left);
        Assert.AreEqual(80d, geometry.Top);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenFormerMonitorWasRemoved_ReturnsFalse()
    {
        WindowPlacementSettings settings = CreateSettings(2200d, 100d, 1000d, 700d);
        WindowWorkArea[] workAreas = [new(0d, 0d, 1920d, 1040d)];

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            workAreas,
            out WindowGeometry _);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenMonitorUsesNegativeCoordinates_ReturnsTrue()
    {
        WindowPlacementSettings settings = CreateSettings(-1500d, 100d, 1000d, 700d);
        WindowWorkArea[] workAreas =
        [
            new(-1920d, 0d, 1920d, 1040d),
            new(0d, 0d, 1920d, 1040d)
        ];

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            workAreas,
            out WindowGeometry _);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenUsableTitleBarPartRemainsVisible_ReturnsTrue()
    {
        WindowPlacementSettings settings = CreateSettings(1856d, 100d, 1000d, 700d);
        WindowWorkArea[] workAreas = [new(0d, 0d, 1920d, 1040d)];

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            workAreas,
            out WindowGeometry _);

        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenOnlySliverRemainsVisible_ReturnsFalse()
    {
        WindowPlacementSettings settings = CreateSettings(1910d, 100d, 1000d, 700d);
        WindowWorkArea[] workAreas = [new(0d, 0d, 1920d, 1040d)];

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            workAreas,
            out WindowGeometry _);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenTitleBarIsAboveWorkArea_ReturnsFalse()
    {
        WindowPlacementSettings settings = CreateSettings(100d, -24d, 1000d, 700d);
        WindowWorkArea[] workAreas = [new(0d, 0d, 1920d, 1040d)];

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            workAreas,
            out WindowGeometry _);

        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenGeometryIsInvalid_ReturnsFalse()
    {
        WindowWorkArea[] workAreas = [new(0d, 0d, 1920d, 1040d)];
        WindowPlacementSettings invalidSize = CreateSettings(100d, 100d, 0d, 700d);
        WindowPlacementSettings invalidPosition = CreateSettings(
            double.NaN,
            100d,
            1000d,
            700d);

        bool sizeIsValid = WindowPlacementValidator.TryGetVisibleGeometry(
            invalidSize,
            workAreas,
            out WindowGeometry _);
        bool positionIsValid = WindowPlacementValidator.TryGetVisibleGeometry(
            invalidPosition,
            workAreas,
            out WindowGeometry _);

        Assert.IsFalse(sizeIsValid);
        Assert.IsFalse(positionIsValid);
    }

    [TestMethod]
    public void TryGetVisibleGeometry_WhenNoWorkAreaExists_ReturnsFalse()
    {
        WindowPlacementSettings settings = CreateSettings(100d, 100d, 1000d, 700d);

        bool isValid = WindowPlacementValidator.TryGetVisibleGeometry(
            settings,
            Array.Empty<WindowWorkArea>(),
            out WindowGeometry _);

        Assert.IsFalse(isValid);
    }

    private static WindowPlacementSettings CreateSettings(
        double left,
        double top,
        double width,
        double height) =>
        new()
        {
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            WindowState = "Normal"
        };
}
