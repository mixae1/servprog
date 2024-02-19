using System.Net;
internal partial class Program{

static bool KeepGoing = true;
static List<Task> OngoingTasks = new List<Task>();

    static void Main(string[] args) {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:8080/");
        listener.Start();
        Console.WriteLine("Listening...");
        ProcessAsync(listener).ContinueWith(async task => {
            await Task.WhenAll(OngoingTasks.ToArray());
        });

        var cmd = Console.ReadLine();
        if (cmd == "exit") {
            KeepGoing = false;
        }
    }


    static async Task ProcessAsync(HttpListener listener) {
        using (StreamWriter logs = new StreamWriter("logs.txt", true))
        while (KeepGoing && listener.IsListening)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            using (Stream input = File.OpenRead("resources" + 
                (File.Exists("resources" + request.RawUrl) ? request.RawUrl : "/404.html")))
            using (Stream output = response.OutputStream)
            {
                input.CopyTo(output);
            }

            await logs.WriteLineAsync("user ip: " + request.RemoteEndPoint.Address.ToString() + ", status code: " + response.StatusCode + ", requested file: " + request.RawUrl + ", date: " + DateTime.Now);
            await logs.FlushAsync();
        }
        listener.Stop();
    }

}