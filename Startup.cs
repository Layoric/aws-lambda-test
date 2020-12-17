using System;
using System.IO;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
}
