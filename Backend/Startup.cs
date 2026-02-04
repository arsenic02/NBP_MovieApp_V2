using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Neo4jClient;
using System.Diagnostics;
using StackExchange.Redis;
namespace HR
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {            
            var muxer = ConnectionMultiplexer.Connect(
               new ConfigurationOptions
               {
                   EndPoints = { { "redis-11433.c300.eu-central-1-1.ec2.cloud.redislabs.com", 11433 } },
                   User = "default",
                   Password = "LzNUKmRZrOp12xTikAXCLdYGiQTwvAMp"
               });

           
            if (!muxer.IsConnected)
            {
                throw new InvalidOperationException("Redis connection failed");
            }


            var db = muxer.GetDatabase();

            //dodato da resi error 500
            services.AddSingleton<IConnectionMultiplexer>(muxer); 
            services.AddSingleton<IDatabase>(db); 
            //

            //Do ovde

            const string uri = "neo4j+s://b6e9a333.databases.neo4j.io";
            const string user = "neo4j";
            const string password = "C4GJvavRADsgwkpfS_HYnmP_PGjBevmjoL8NMHI1jcI";
            services.AddCors(options =>
            {
                options.AddPolicy("CORS", policy =>
                {
                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins("http://localhost:5500",
                                    "https://localhost:5500",
                                    "http://127.0.0.1:5500",
                                    "https://127.0.0.1:5500");
                    //.AllowAnyOrigin();
                });
            });
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HR", Version = "v1" });
            });

            var client = new BoltGraphClient(new Uri(uri), user, password);
            client.ConnectAsync();
            services.AddSingleton<IGraphClient>(client);
            services.AddSingleton<IDatabase>(db);//Dodato
        }
       
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HR v1"));
            }

            //dodato
            app.UseStaticFiles();  // Ovo omogućava serviranje statičkih fajlova iz wwwroot foldera

            app.UseCors("CORS");
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
