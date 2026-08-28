namespace MyMediaImport.App;

internal readonly record struct WindowGeometry(
    double Left,
    double Top,
    double Width,
    double Height);

internal readonly record struct WindowWorkArea(
    double Left,
    double Top,
    double Width,
    double Height);
