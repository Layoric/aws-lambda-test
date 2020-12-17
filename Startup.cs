using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Configuration;

namespace aws_lambda_test
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
                options.EnableEndpointRouting = false);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Use(next => context =>
            {
                context.Request.EnableBuffering();
                return next(context);
            });
            
            app.UseMiddleware<CustomMiddleware>();

            app.UseStaticFiles(new StaticFileOptions());

            app.UseServiceStack(new AppHost
            {
                AppSettings = new AppSettings(),
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
    
    public class CustomMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var originalBody = context.Response.Body;
            using (var ms = new MemoryStream()) 
            {
                context.Response.Body = ms;
                long length = 0;
                context.Response.OnStarting((state) =>
                {
                    context.Response.Headers.ContentLength = length;
                    return Task.FromResult(0);
                }, context);
                await _next(context);
                length = context.Response.Body.Length;
                context.Response.Body.Position = 0;
                await ms.CopyToAsync(originalBody);
            }
        }
    }

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
