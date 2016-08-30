using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SuperStartupBuilder.Data;
using SuperStartupBuilder.Models;
using Microsoft.AspNetCore.Builder;

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
                .WithMiddleware(appBuilder =>
                    appBuilder.Run(httpContext =>
                    {
                        throw new InvalidOperationException();
                    }))
                .WithStaticFiles()
                .WithUserIdentity()
                    .WithUserType<ApplicationUser, ApplicationDbContext>()
                .WithMvc();

            superStartupBuilder.Run();
        }
    }
}
