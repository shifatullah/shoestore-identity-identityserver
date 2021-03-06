using System.Collections.Generic;
using System.Security.Claims;
using IdentityServer4.Test;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IdentityModel;
using IdentityServer4;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using IdentityServer4.EntityFramework.DbContexts;
using System.Linq;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ShoeStore.Identity.IdentityServer
{
    public class Startup
    {
        private static string _environment;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = Configuration.GetValue<string>("ss-identity-db-connection-string");

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            //services.AddRazorPages();
            services.AddDbContext<ApplicationDbContext>(builder =>
                builder.UseSqlServer(connectionString, sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddControllersWithViews();

            services.AddIdentityServer()
                //.AddInMemoryClients(Clients.Get())
                //.AddInMemoryIdentityResources(Resources.GetIdentityResources())
                //.AddInMemoryApiResources(Resources.GetApiResources())
                //.AddInMemoryApiScopes(Resources.GetApiScopes())
                //.AddTestUsers(Users.Get())
                .AddAspNetIdentity<IdentityUser>()
                .AddDeveloperSigningCredential()
                .AddOperationalStore(options => options.ConfigureDbContext =
                    builder => builder.UseSqlServer(
                        connectionString,
                        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)))
                .AddConfigurationStore(options => options.ConfigureDbContext =
                    builder => builder.UseSqlServer(
                        connectionString,
                        sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.RequireHeaderSymmetry = false;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //string XForwardedPathBase = "X-Forwarded-PathBase";
            //string XForwardedProto = "X-Forwarded-Proto";

            app.Use((context, next) =>
            {
                //if (context.Request.Headers.TryGetValue(XForwardedPathBase, out StringValues pathBase))
                //{
                //    context.Request.PathBase = new PathString(pathBase);
                //}

                //if (context.Request.Headers.TryGetValue(XForwardedProto, out StringValues proto))
                //{
                //    context.Request.Protocol = proto;
                //}
                context.Request.Scheme = "https";
                return next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseForwardedHeaders();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseForwardedHeaders();
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            string redirectUri = Configuration.GetValue<string>("ss-identity-url");
            InitializeDbTestData(app, redirectUri);

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseIdentityServer();
            app.UseEndpoints(endpoints =>
            {
                //endpoints.MapRazorPages();
                endpoints.MapDefaultControllerRoute();
            });

            // metadata returns as http
            //var forwardOptions = new ForwardedHeadersOptions
            //{
            //    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            //    RequireHeaderSymmetry = false
            //};

            //forwardOptions.KnownNetworks.Clear();
            //forwardOptions.KnownProxies.Clear();

            // ref: https://github.com/aspnet/Docs/issues/2384
            // app.UseForwardedHeaders();
        }

        internal class Clients
        {
            public static IEnumerable<Client> Get(string redirectUrl)
            {
                
                
                return new List<Client> {
                    new Client {
                        ClientId = "productsAPIClient",
                        ClientName = "Example Client Credentials Client Application",
                        AllowedGrantTypes = GrantTypes.ClientCredentials,
                        // todo: Sensitive information will be stored in Db or Vault
                        ClientSecrets = new List<Secret> {
                            new Secret("SuperSecretPassword".Sha256())},
                        AllowedScopes = new List<string> { "productsAPI.read" }
                    },
                    new Client {
                        ClientId = "adminClient",
                        ClientName = "Example Client Application",
                        ClientSecrets = new List<Secret> { new Secret("SuperSecretPassword".Sha256()) }, // change me!

                        AllowedGrantTypes = GrantTypes.Code,
                        RedirectUris = new List<string> { "https://localhost:5002/signin-oidc" },
                        AllowedScopes = new List<string>
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            IdentityServerConstants.StandardScopes.Email,
                            "role",
                            "productsAPI.read"
                        },

                        RequirePkce = true,
                        AllowPlainTextPkce = false
                    }
                };
            }
        }

        internal class Resources
        {
            public static IEnumerable<IdentityResource> GetIdentityResources()
            {
                return new List<IdentityResource> {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResources.Email(),
                    new IdentityResource {
                        Name = "role",
                        UserClaims = new List<string> {"role"}
                    }
                };
            }

            public static IEnumerable<ApiResource> GetApiResources()
            {
                return new List<ApiResource> {
                    new ApiResource {
                        Name = "productsAPI",
                        DisplayName = "Shoe Store Products API",
                        Description = "Shoe Store Products API Access",
                        UserClaims = new List<string> {"role"},
                        ApiSecrets = new List<Secret> {new Secret("scopeSecret".Sha256())},
                        Scopes = new List<string> {
                            "productsAPI.read",
                            "productsAPI.write"
                        }
                    }
                };
            }
            public static IEnumerable<ApiScope> GetApiScopes()
            {
                return new[]
                {
                    new ApiScope("productsAPI.read", "Read Access to products API"),
                    new ApiScope("productsAPI.write", "Write Access to products API")
                };
            }
        }

        internal class Users
        {
            public static List<TestUser> Get()
            {
                return new List<TestUser>()
                {
                    new TestUser()
                    {
                        //SubjectId = "e31db763-0b10-4185-9bf8-8f395b44f315",
                        SubjectId = "1",
                        Username = "test",
                        // todo: Sensitive information will be stored in Db or Vault
                        Password = "Password123|",
                        Claims = new List<Claim>
                        {
                            new Claim(JwtClaimTypes.Email, "dummy@dummy"),
                            new Claim(JwtClaimTypes.Role, "admin")
                        }
                    }
                };
            }
        }

        private static void InitializeDbTestData(IApplicationBuilder app, string redirectUri)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

                if (!context.Clients.Any())
                {                    
                    foreach (var client in Clients.Get(redirectUri))
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Resources.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var scope in Resources.GetApiScopes())
                    {
                        context.ApiScopes.Add(scope.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Resources.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                if (!userManager.Users.Any())
                {
                    foreach (var testUser in Users.Get())
                    {
                        var identityUser = new IdentityUser(testUser.Username)
                        {
                            Id = testUser.SubjectId
                        };

                        IdentityResult result = userManager.CreateAsync(identityUser, testUser.Password).Result;                        
                        userManager.AddClaimsAsync(identityUser, testUser.Claims.ToList()).Wait();
                    }
                }
            }
        }
    }
}
