using System.Net;

internal partial class Program{

static bool KeepGoing = true;
static List<Task> OngoingTasks = new List<Task>();

    static async Task Main(string[] args) {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Listener has been started, listening... ");

        CancellationTokenSource token = new CancellationTokenSource();
        var proc = ProcessAsync(listener, token.Token).ContinueWith(async task => {
            await Task.WhenAll(OngoingTasks.ToArray());
        });

        string? cmd = Console.ReadLine();
        KeepGoing = false;
        token.Cancel();
        if (proc is not null)
            await proc;
        listener.Stop();
        Console.WriteLine("Listener has been stoped.");
    }


    static async Task ProcessAsync(HttpListener listener, CancellationToken ct) {
        using (StreamWriter logs = new StreamWriter("logs.txt", true))
        while (KeepGoing)
        {
            Console.WriteLine(OngoingTasks.Count);
            if (OngoingTasks.Count < 10){
                Task? task = listener.GetContextAsync().WaitAsync(ct).ContinueWith(t => {
                    RespondTo(t.Result, t.Id, logs);
                }, ct);
                OngoingTasks.Add(task);
            }
            else{
                await Task.WhenAny(OngoingTasks);
            }
            OngoingTasks.RemoveAll(t => t.IsCompleted);
            if(ct.IsCancellationRequested) break;
        }
    }

    static void RespondTo(HttpListenerContext context, int id, StreamWriter logs){
        Console.WriteLine(id + " was called.");
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        bool flag = File.Exists("resources" + request.RawUrl);
        if(!flag){
            response.StatusCode = 404;
        }

        using (Stream input = File.OpenRead("resources" + 
            (flag ? request.RawUrl : "/404.html")))
        using (Stream output = response.OutputStream)
        {
            input.CopyTo(output);
        }

        logs.WriteLine("user ip: " + request.RemoteEndPoint.Address.ToString() + ", status code: " + response.StatusCode + ", requested file: " + request.RawUrl + ", date: " + DateTime.Now);
        logs.Flush();
        Console.WriteLine(id + " has finished processing.");
    }

}