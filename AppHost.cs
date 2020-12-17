using System;
using Funq;
using ServiceStack;

namespace aws_lambda_test
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("LambdaServiceStack", typeof(MyServices).Assembly) { }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false)
            });
            
            PreRequestFilters.Add((req,res) => res.UseBufferedStream = true);
            //Handle Exceptions occurring in Services:

            this.ServiceExceptionHandlers.Add((httpReq, request, exception) => null);

            //Handle Unhandled Exceptions occurring outside of Services
            //E.g. Exceptions during Request binding or in filters:
            this.UncaughtExceptionHandlers.Add((req, res, operationName, ex) => {
                Console.WriteLine($"Error: {ex.GetType().Name}: {ex.Message} - {ex.StackTrace}");
                res.EndRequest(skipHeaders: true);
            });
        }
    }
    
    public class MyServices : Service
    {
        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }
    }
    
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
    }
}