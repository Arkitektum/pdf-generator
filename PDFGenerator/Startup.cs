using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PDFGenerator.Security;
using PDFGenerator.Services;
using System.Threading.Tasks;

namespace PDFGenerator
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables()
                .Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddMemoryCache();

            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<IPDFService, PDFService>();
            services.AddTransient<IAuthorizationHandler, ApiKeyRequirementHandler>();

            services.Configure<PDFServiceConfig>(Configuration.GetSection(PDFServiceConfig.SectionName));

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Events.OnRedirectToLogin = ctx =>
                    {
                        ctx.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                });

            var authenticationConfig = Configuration.GetSection(AuthenticationConfig.SectionName);
            var apiKey = authenticationConfig.GetSection("ApiKey").Value;

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiKeyPolicy",
                    policyBuilder => policyBuilder.AddRequirements(new ApiKeyRequirement(new[] { apiKey })));
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();

            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "PDF Generator API v1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();            

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
