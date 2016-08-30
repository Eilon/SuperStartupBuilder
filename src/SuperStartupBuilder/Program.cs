using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SuperStartupBuilder.Data;
using SuperStartupBuilder.Models;

namespace SuperStartupBuilder
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var superStartupBuilder = new SuperStartupBuilder()
                .WithConsoleLogger()
                .WithDebugLogger()
                .WithAppSettings()
                .WithUserSecrets()
                .WithConfig((configBuilder, env) =>
                {
                    configBuilder.AddEnvironmentVariables();
                })
                .WithSqlServer()
                    .WithDataContextType<ApplicationDbContext>()
                .WithErrorPages()
                .WithCondition(
                    () => DateTime.IsLeapYear(DateTime.Now.Year),
                    subBuilder => subBuilder.WithBrowserLink()
                )
                .WithStaticFiles()
                .WithUserIdentity()
                    .WithUserType<ApplicationUser, ApplicationDbContext>()
                .WithMvc()
                .WithMiddleware(appBuilder =>
                    appBuilder.Run(async httpContext =>
                    {
                        await httpContext.Response.WriteAsync("Hey!!!");
                    }));

            superStartupBuilder.Run();
        }
    }
}
