using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. 
        // Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // get our token key from appsettings
            var key = Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings:Token").Value);

            // inject datacontext and use lamda expression 
            // to define what database where gonna use
            services.AddDbContext<DataContext>(x => 
                x.UseSqlite(Configuration.GetConnectionString("DefaultConnection"))
                // uncomment to make ef ignore warnings
                .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.IncludeIgnoredWarning))
                );
            // automatically seed data at start up
            services.AddTransient<Seed>();
            services.AddCors();
            
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
            services.AddAutoMapper();
            // inject our auth repository
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IDatingRepository, DatingRepository>();

            // inject our authentication scheme
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddMvc().AddJsonOptions(opt =>
            {
                opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                // uncommment to user global action filter
                // that affect all action in our application
                // services.AddScoped<LogUserActivity>();
            });

            // scope action filters
            services.AddScoped<LogUserActivity>();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // use when running in development mode
        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            // get our token key from appsettings
            var key = Encoding.ASCII.GetBytes(Configuration.GetSection("AppSettings:Token").Value);

            // inject datacontext and use lamda expression 
            // to define what database where gonna use
            services.AddDbContext<DataContext>(x => 
                x.UseSqlite(Configuration.GetConnectionString("DefaultConnection"))
                // uncomment to make ef ignore warnings
                .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.IncludeIgnoredWarning))
                );
            // automatically seed data at start up
            services.AddTransient<Seed>();
            services.AddCors();
            
            services.Configure<CloudinarySettings>(Configuration.GetSection("CloudinarySettings"));
            services.AddAutoMapper();
            // inject our auth repository
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IDatingRepository, DatingRepository>();

            // inject our authentication scheme
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };
                });

            services.AddMvc().AddJsonOptions(opt =>
            {
                opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                // uncommment to user global action filter
                // that affect all action in our application
                // services.AddScoped<LogUserActivity>();
            });

            // scope action filters
            services.AddScoped<LogUserActivity>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Seed seeder)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // use global exception handler to handle exception
                // in production mode with some configuration
                app.UseExceptionHandler(builder => 
                {
                    // executes this particular middle ware
                    // and pass async context
                    builder.Run(async context => {

                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }    
                    });
                });                
            }

            // populate users data
            seeder.SeedUser();

            app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());
            app.UseAuthentication();
            
            // telss kestral to run set of default files
            app.UseDefaultFiles();

            // tell dotnet to run static files
            app.UseStaticFiles();

            app.UseMvc(routes => {
                // route for angular application
                // tells mvc that any routes it doesn't recognize
                // then use MapSpaFallbackRoute
                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Fallback", action = "Index"}
                );
            });
        }
    }
}
