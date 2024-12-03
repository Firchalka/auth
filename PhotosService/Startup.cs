using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using PhotosService.Data;
using PhotosService.Models;
using PhotosService.Services;
using Serilog;

namespace PhotosService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder.WithOrigins("https://localhost:8001")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });

            services.AddControllers(options =>
                {
                    options.ReturnHttpNotAcceptable = true;
                    options.ModelBinderProviders.Insert(0, new JwtSecurityTokenModelBinderProvider());
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            var connectionString = Configuration.GetConnectionString("PhotosDbContextConnection")
                                   ?? "Data Source=PhotosService.db";
            services.AddDbContext<PhotosDbContext>(o => o.UseSqlite(connectionString));

            services.AddScoped<IPhotosRepository, LocalPhotosRepository>();

            services.AddAutoMapper(cfg => { cfg.CreateMap<PhotoEntity, PhotoDto>().ReverseMap(); },
                Array.Empty<Assembly>());

            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    const string authority = "https://localhost:7001";
                    const string apiResourceId = "photos_service";
                    const string apiResourceSecret = "photos_service_secret";

                    options.Authority = authority;
                    options.Audience = apiResourceId;

                    options.SecurityTokenValidators.Clear();
                    options.SecurityTokenValidators.Add(new IntrospectionSecurityTokenValidator(
                        authority, apiResourceId, apiResourceSecret));

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = context =>
                        {
                            JwtSecurityTokenModelBinder.SaveToken(context.HttpContext, context.SecurityToken);
                            return Task.CompletedTask;
                        }
                    };

                    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
                });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseSerilogRequestLogging();

            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}