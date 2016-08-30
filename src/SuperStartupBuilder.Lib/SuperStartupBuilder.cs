using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SuperStartupBuilder
{
    public interface ISuperStartupBuilder
    {
        IList<Action<IConfigurationBuilder, IHostingEnvironment>> ConfigurationBuilderActions { get; }

        IList<Action<IServiceCollection, IConfigurationRoot>> ConfigureServicesActions { get; }

        IList<Action<IApplicationBuilder, ILoggerFactory, IHostingEnvironment, IConfigurationRoot>> ConfigureActions { get; }
    }

    public class SuperStartupBuilder : ISuperStartupBuilder
    {
        public SuperStartupBuilder()
        {
            ConfigurationBuilderActions = new List<Action<IConfigurationBuilder, IHostingEnvironment>>();
            ConfigureServicesActions = new List<Action<IServiceCollection, IConfigurationRoot>>();
            ConfigureActions = new List<Action<IApplicationBuilder, ILoggerFactory, IHostingEnvironment, IConfigurationRoot>>();
        }

        public IList<Action<IConfigurationBuilder, IHostingEnvironment>> ConfigurationBuilderActions { get; }
        public IList<Action<IServiceCollection, IConfigurationRoot>> ConfigureServicesActions { get; }
        public IList<Action<IApplicationBuilder, ILoggerFactory, IHostingEnvironment, IConfigurationRoot>> ConfigureActions { get; }

        public void Run()
        {
            var loggerFactory = new LoggerFactory();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseLoggerFactory(loggerFactory);

            var configBuilder = new ConfigurationBuilder();

            // TODO: how to get this???
            var hostingEnv = new HostingEnvironment()
            {
                ContentRootPath = @"C:\Users\elipton\Documents\Visual Studio 2015\Projects\SuperStartupBuilder\src\SuperStartupBuilder",
                EnvironmentName = "Development",
            };

            foreach (var configBuilderAction in ConfigurationBuilderActions)
            {
                configBuilderAction(configBuilder, hostingEnv);
            }

            var config = configBuilder.Build();

            var host = hostBuilder
                .ConfigureServices(services =>
                {
                    foreach (var configureServicesAction in ConfigureServicesActions)
                    {
                        configureServicesAction(services, config);
                    }
                })
                .Configure(appBuilder =>
                {
                    foreach (var configureAction in ConfigureActions)
                    {
                        configureAction(appBuilder, loggerFactory, hostingEnv, config);
                    }
                })
                .Build();

            host.Run();
        }
    }

    public static class SuperStartupBuilderExtensions
    {
        //public static TSuperStartupBuilder WithFoo<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder, string foo)
        //    where TSuperStartupBuilder : ISuperStartupBuilder
        //{
        //    return superStartupBuilder;
        //}

        public static TSuperStartupBuilder WithCondition<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder, Func<bool> condition, Action<TSuperStartupBuilder> subSuperStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            if (condition != null && condition() && superStartupBuilder != null)
            {
                subSuperStartupBuilder(superStartupBuilder);
            }

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithMiddleware<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder, Action<IApplicationBuilder> appBuilderAction)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((appBuilder, _, __, ___) => appBuilderAction(appBuilder));

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithMvc<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureServicesActions.Add((services, _) => services.AddMvc());
            superStartupBuilder.ConfigureActions.Add((appBuilder, _, __, ___) => appBuilder.UseMvcWithDefaultRoute());

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithConsoleLogger<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((app, loggerFactory, hostingEnv, config) =>
            {
                loggerFactory.AddConsole(config.GetSection("Logging"));
            });

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithDebugLogger<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((app, loggerFactory, hostingEnv, config) =>
            {
                // TODO: Revert this
                loggerFactory.AddDebug(LogLevel.Trace);
            });

            return superStartupBuilder;
        }

        public static IdentitySubBuilder<TSuperStartupBuilder> WithUserIdentity<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((app, loggerFactory, hostingEnv, config) =>
            {
                app.UseIdentity();
            });

            return new IdentitySubBuilder<TSuperStartupBuilder>(superStartupBuilder);
        }

        public class IdentitySubBuilder<TSuperStartupBuilder> : ISuperStartupBuilder
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            public IdentitySubBuilder(TSuperStartupBuilder superStartupBuilder)
            {
                SubBuilder = superStartupBuilder;
            }

            public TSuperStartupBuilder SubBuilder { get; }

            public TSuperStartupBuilder WithUserType<TUser, TDataContext>()
                where TUser : class
                where TDataContext : DbContext
            {
                SubBuilder.ConfigureServicesActions.Add((services, _) => services.AddIdentity<TUser, IdentityRole>()
                    .AddEntityFrameworkStores<TDataContext>()
                    .AddDefaultTokenProviders());

                return SubBuilder;
            }

            public IList<Action<IConfigurationBuilder, IHostingEnvironment>> ConfigurationBuilderActions
            {
                get
                {
                    return SubBuilder.ConfigurationBuilderActions;
                }
            }

            public IList<Action<IApplicationBuilder, ILoggerFactory, IHostingEnvironment, IConfigurationRoot>> ConfigureActions
            {
                get
                {
                    return SubBuilder.ConfigureActions;
                }
            }

            public IList<Action<IServiceCollection, IConfigurationRoot>> ConfigureServicesActions
            {
                get
                {
                    return SubBuilder.ConfigureServicesActions;
                }
            }
        }

        public static TSuperStartupBuilder WithStaticFiles<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((app, loggerFactory, hostingEnv, config) =>
            {
                app.UseStaticFiles();
            });

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithErrorPages<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((app, loggerFactory, hostingEnv, config) =>
            {
                if (hostingEnv.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseDatabaseErrorPage();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                }

            });

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithBrowserLink<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigureActions.Add((app, loggerFactory, hostingEnv, config) =>
            {
                if (hostingEnv.IsDevelopment())
                {
                    app.UseBrowserLink();
                }

            });

            return superStartupBuilder;
        }

        public static SqlDataSubBuilder<TSuperStartupBuilder> WithSqlServer<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {

            return new SqlDataSubBuilder<TSuperStartupBuilder>(superStartupBuilder);
        }

        public class SqlDataSubBuilder<TSuperStartupBuilder> : ISuperStartupBuilder
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            public SqlDataSubBuilder(TSuperStartupBuilder superStartupBuilder)
            {
                SubBuilder = superStartupBuilder;
            }

            public TSuperStartupBuilder SubBuilder { get; }

            public TSuperStartupBuilder WithDataContextType<TDataContext>()
                where TDataContext : DbContext
            {
                SubBuilder
                    .ConfigureServicesActions.Add(
                        (services, config) =>
                            services
                                .AddDbContext<TDataContext>(options =>
                                    options.UseSqlServer(config.GetConnectionString("DefaultConnection"))));

                return SubBuilder;
            }

            public IList<Action<IConfigurationBuilder, IHostingEnvironment>> ConfigurationBuilderActions
            {
                get
                {
                    return SubBuilder.ConfigurationBuilderActions;
                }
            }

            public IList<Action<IApplicationBuilder, ILoggerFactory, IHostingEnvironment, IConfigurationRoot>> ConfigureActions
            {
                get
                {
                    return SubBuilder.ConfigureActions;
                }
            }

            public IList<Action<IServiceCollection, IConfigurationRoot>> ConfigureServicesActions
            {
                get
                {
                    return SubBuilder.ConfigureServicesActions;
                }
            }
        }

        public static TSuperStartupBuilder WithConfig<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder, Action<IConfigurationBuilder, IHostingEnvironment> configBuilderAction)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigurationBuilderActions.Add(configBuilderAction);

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithAppSettings<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigurationBuilderActions.Add((config, env) =>
            {
                config
                    .SetBasePath(env.ContentRootPath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            });

            return superStartupBuilder;
        }

        public static TSuperStartupBuilder WithUserSecrets<TSuperStartupBuilder>(this TSuperStartupBuilder superStartupBuilder)
            where TSuperStartupBuilder : ISuperStartupBuilder
        {
            superStartupBuilder.ConfigurationBuilderActions.Add((config, env) =>
            {
                if (env.IsDevelopment())
                {
                    config
                        .SetBasePath(env.ContentRootPath)
                        .AddUserSecrets();
                }
            });

            return superStartupBuilder;
        }
    }
}
