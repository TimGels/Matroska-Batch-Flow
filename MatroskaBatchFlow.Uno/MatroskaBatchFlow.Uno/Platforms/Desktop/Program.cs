using MatroskaBatchFlow.Uno;
using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseX11()
    .UseLinuxFrameBuffer()
    .UseMacOS()
    .UseWin32()
    .Build();

host.Run();
