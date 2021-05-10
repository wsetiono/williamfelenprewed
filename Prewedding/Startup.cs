using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

// https://referbruv.com/blog/posts/configuring-response-caching-headers-in-an-aspnet-core-api
// https://github.com/dotnet/AspNetCore.Docs/issues/10987
namespace Prewedding
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //add response caching for quicker responses
              services.AddResponseCaching((options) =>
              {
                  // restrict on what max size of response 
                  // can be cached in bytes               
                  options.MaximumBodySize = 1024;
                  options.UseCaseSensitivePaths = true;
              });
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();

            app.UseResponseCaching();

            // response caching should be done only for GET requests
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Method.Equals(HttpMethod.Get))
                {
                    ctx.Response.GetTypedHeaders().CacheControl =
                    new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(10)
                    };
                    ctx.Response.Headers[HeaderNames.Vary] =
                        new string[] { "Accept-Encoding" };
                }

                await next();
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    // Cache static file for 1 year
                    if (!string.IsNullOrEmpty(context.Context.Request.Query["v"]))
                    {
                        context.Context.Response.Headers.Add("cache-control", new[] { "public,max-age=31536000" });
                        context.Context.Response.Headers.Add("Expires", new[] { DateTime.UtcNow.AddYears(1).ToString("R") }); // Format RFC1123
                    }
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
