using System;
using System.Net;
using System.Threading.Tasks;

namespace MediaRatingsPlatform.Http {
    public class HttpServer {
        private readonly HttpListener _listener;
        private readonly Router _router;

        public HttpServer(string[] prefixes, Router router) {
            _listener = new HttpListener();
            foreach (var prefix in prefixes) _listener.Prefixes.Add(prefix);
            _router = router;
        }

        public void Start() {
            _listener.Start();
            Console.WriteLine("Listening...");
            Task.Run(() => ListenAsync());
        }

        private async Task ListenAsync() {
            while (_listener.IsListening) {
                try {
                    var context = await _listener.GetContextAsync();
                    await _router.HandleRequest(context);
                } catch (Exception ex) {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}