using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using AspNetCoreTodo.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AspNetCoreTodo.Services;
using AspNetCoreTodo.Models;
using Microsoft.EntityFrameworkCore.Internal;
using System;

namespace AspNetCoreTodo
{
    public class Startup
    {
        private IWebHostEnvironment _env;
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _env = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private bool isSqlite(string dataSource)
        {
            return dataSource.EndsWith(".sqlite.db");
        }
        private bool isLocalDB(string dataSource)
        {
            return dataSource.EndsWith(".mdf");
        }

        private string getDataSource(string connStr)
        {
            var pieces = from p in connStr.Split(";")
                         select p.Trim();
            var dataSources = from p in pieces
                              where p.StartsWith("Data Source")
                              select p.Split("=")[1];
            return dataSources.FirstOrDefault();

        }
        private void configureDatabase(IServiceCollection services)
        {
            var connStr = Configuration.GetConnectionString("DefaultConnection");
            string dataSource = getDataSource(connStr);
            if (isLocalDB(dataSource)) {
                configureLocalDB(services,dataSource);
            } else if (isSqlite(dataSource)) {
                configureSqlLite(services, dataSource);
            }
            else {
                configureMsSQL(services, connStr);
            }
        }
        private void configureMsSQL(IServiceCollection services, string connStr)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connStr));
        }

        private void configureLocalDB(IServiceCollection services, string fileName)
        {
            var filePath = _env.ContentRootPath + @"\" + fileName;
            var connStr = $"Data Source=(LocalDb)\\MSSQLLocalDB;Integrated Security=SSPI;AttachDBFilename={filePath}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connStr));
        }

        private void configureSqlLite(IServiceCollection services, string fileName)
        {
            var filePath = _env.ContentRootPath + Path.DirectorySeparatorChar + fileName;
            var connStr = $"Filename={filePath}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connStr));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IRedirectService, RedirectService>();

            configureDatabase(services);
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews();
           services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
