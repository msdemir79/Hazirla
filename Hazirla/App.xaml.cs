using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Windows;
namespace Hazirla;
public partial class App : System.Windows.Application
{
    private Mutex? instance;
    private CancellationTokenSource? shutdown;
    private Task? listener;
    private bool ownsMutex;
    private static readonly string InstanceName="Hazirla-"+WindowsIdentity.GetCurrent().User!.Value+"-"+System.Diagnostics.Process.GetCurrentProcess().SessionId;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        instance=new Mutex(true,@"Local\"+InstanceName,out ownsMutex);
        if(!ownsMutex)
        {
            try
            {
                using var pipe=new NamedPipeClientStream(".",InstanceName,PipeDirection.Out,PipeOptions.CurrentUserOnly);
                pipe.Connect(5000);pipe.WriteByte(1);
            }
            catch(IOException){System.Windows.MessageBox.Show("Hazırla zaten açık. Sistem tepsisindeki H ikonundan açabilirsin.","Hazırla");}
            catch(TimeoutException){System.Windows.MessageBox.Show("Hazırla zaten açık. Sistem tepsisindeki H ikonundan açabilirsin.","Hazırla");}
            Shutdown();return;
        }
        shutdown=new CancellationTokenSource();
        var window=new MainWindow();MainWindow=window;window.Show();
        listener=ListenAsync(shutdown.Token);
    }
    private async Task ListenAsync(CancellationToken ct)
    {
        while(!ct.IsCancellationRequested)
        {
            try
            {
                using var server=new NamedPipeServerStream(InstanceName,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(ct);
                var buffer=new byte[1];
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(2000);
                if(await server.ReadAsync(buffer,timeout.Token)>0)
                    await Dispatcher.InvokeAsync(()=>{if(MainWindow is MainWindow window)window.RestoreFromLaunch();});
            }
            catch(OperationCanceledException){if(ct.IsCancellationRequested)break;}
            catch(IOException){ }
        }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        shutdown?.Cancel();
        if(ownsMutex)instance?.ReleaseMutex();instance?.Dispose();
        base.OnExit(e);
    }
}
