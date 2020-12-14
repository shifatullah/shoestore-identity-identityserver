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

namespace ShoeStore.Identity.IdentityServer
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
            //services.AddRazorPages();

            services.AddControllersWithViews();

            services.AddIdentityServer()
                .AddInMemoryClients(Clients.Get())
                .AddInMemoryIdentityResources(Resources.GetIdentityResources())
                .AddInMemoryApiResources(Resources.GetApiResources())
                .AddInMemoryApiScopes(Resources.GetApiScopes())
                .AddTestUsers(Users.Get())
                .AddDeveloperSigningCredential();
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
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

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
        }

        internal class Clients
        {
            public static IEnumerable<Client> Get()
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
                        SubjectId = "",
                        Username = "test",
                        // todo: Sensitive information will be stored in Db or Vault
                        Password = "password",
                        Claims = new List<Claim>
                        {
                            new Claim(JwtClaimTypes.Email, "dummy@dummy"),
                            new Claim(JwtClaimTypes.Role, "admin")
                        }
                    }
                };
            }
        }
    }
}
