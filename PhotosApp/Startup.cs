using System;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using PhotosApp.Clients;
using PhotosApp.Clients.Models;
using PhotosApp.Data;
using PhotosApp.Models;
using PhotosApp.Services.Authorization;
using Serilog;

namespace PhotosApp
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Env = env;
            Configuration = configuration;
        }

        private IWebHostEnvironment Env { get; }
        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<PhotosServiceOptions>(Configuration.GetSection("PhotosService"));

            var mvc = services.AddControllersWithViews();
            services.AddRazorPages();
            if (Env.IsDevelopment())
                mvc.AddRazorRuntimeCompilation();

            services.AddHttpContextAccessor();

            var connectionString = Configuration.GetConnectionString("PhotosDbContextConnection")
                                   ?? "Data Source=PhotosApp.db";
            services.AddDbContext<PhotosDbContext>(o => o.UseSqlite(connectionString));
            services.AddScoped<IPhotosRepository, RemotePhotosRepository>();

            services.AddAutoMapper(cfg =>
            {
                cfg.CreateMap<PhotoEntity, PhotoDto>().ReverseMap();
                cfg.CreateMap<PhotoEntity, Photo>().ReverseMap();

                cfg.CreateMap<EditPhotoModel, PhotoEntity>()
                    .ForMember(m => m.FileName, options => options.Ignore())
                    .ForMember(m => m.Id, options => options.Ignore())
                    .ForMember(m => m.OwnerId, options => options.Ignore());
            }, Array.Empty<Assembly>());

            services.AddTransient<ICookieManager, ChunkingCookieManager>();

            const string oidcAuthority = "https://localhost:7001";
            var oidcConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{oidcAuthority}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever());
            services.AddSingleton<IConfigurationManager<OpenIdConnectConfiguration>>(oidcConfigurationManager);

            services.AddAuthentication()
                .AddOpenIdConnect("Passport", "Паспорт", options =>
                {
                    options.ConfigurationManager = oidcConfigurationManager;
                    options.Authority = oidcAuthority;

                    options.ClientId = "Photos App by OIDC";
                    options.ClientSecret = "secret";
                    options.ResponseType = "code";

                    options.Scope.Add("email");
                    options.Scope.Add("photos_app");
                    options.Scope.Add("photos");
                    options.Scope.Add("offline_access");

                    options.CallbackPath = "/signin-passport";
                    options.SignedOutCallbackPath = "/signout-callback-passport";

                    // NOTE: все эти проверки токена выполняются по умолчанию, указаны для ознакомления
                    options.TokenValidationParameters.ValidateIssuer = true; // проверка издателя
                    options.TokenValidationParameters.ValidateAudience = true; // проверка получателя
                    options.TokenValidationParameters.ValidateLifetime = true; // проверка не протух ли
                    options.TokenValidationParameters.RequireSignedTokens = true; // есть ли валидная подпись издателя

                    options.SaveTokens = true;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenResponseReceived = context =>
                        {
                            var tokenResponse = context.TokenEndpointResponse;
                            var tokenHandler = new JwtSecurityTokenHandler();

                            if (tokenResponse.AccessToken != null)
                                tokenHandler.ReadToken(tokenResponse.AccessToken);

                            if (tokenResponse.IdToken != null) tokenHandler.ReadToken(tokenResponse.IdToken);

                            if (tokenResponse.RefreshToken != null)
                                // NOTE: Это не JWT-токен
                            {
                            }

                            return Task.CompletedTask;
                        },
                        OnRemoteFailure = context =>
                        {
                            context.Response.Redirect("/");
                            context.HandleResponse();
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddScoped<IAuthorizationHandler, MustOwnPhotoHandler>();
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.AddPolicy(
                    "Beta",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        policyBuilder.RequireClaim("testing", "beta");
                    });
                options.AddPolicy(
                    "CanAddPhoto",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        policyBuilder.RequireClaim("subscription", "paid");
                    });
                options.AddPolicy(
                    "MustOwnPhoto",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        policyBuilder.AddRequirements(new MustOwnPhotoRequirement());
                    });
                options.AddPolicy(
                    "Dev",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        // policyBuilder.RequireRole("Dev");
                        // policyBuilder.AddAuthenticationSchemes(
                        //     JwtBearerDefaults.AuthenticationScheme,
                        //     IdentityConstants.ApplicationScheme);
                    });
            });

            services.AddAuthentication(options =>
                {
                    // NOTE: Схема, которую внешние провайдеры будут использовать для сохранения данных о пользователе
                    // NOTE: Так как значение совпадает с DefaultScheme, то эту настройку можно не задавать
                    options.DefaultSignInScheme = "Cookie";
                    // NOTE: Схема, которая будет вызываться, если у пользователя нет доступа
                    options.DefaultChallengeScheme = "Passport";
                    // NOTE: Схема на все остальные случаи жизни
                    options.DefaultScheme = "Cookie";
                })
                .AddCookie("Cookie", options =>
                {
                    // NOTE: Пусть у куки будет имя, которое расшифровывается на странице «Decode»
                    options.Cookie.Name = "PhotosApp.Auth";
                    // NOTE: Если не задать здесь путь до обработчика logout, то в этом обработчике
                    // будет игнорироваться редирект по настройке AuthenticationProperties.RedirectUri
                    options.LogoutPath = "/Passport/Logout";
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            if (Env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Exception");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Photos}/{action=Index}/{id?}");
            });
        }
    }
}