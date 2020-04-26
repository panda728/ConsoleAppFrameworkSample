﻿using System;
using System.IO;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ConsoleAppFrameworkSampleApp {
    public class Program {
        static async Task Main(string[] args) {
            Log.Logger = CreateLogger();
            try {
                await CreateHostBuilder(args).RunConsoleAppFrameworkAsync(args);
            } catch (Exception ex) {
                Log.Fatal(ex, "Host terminated unexpectedly");
            } finally {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) => {
                    services.Configure<Settings>(
                        hostContext.Configuration.GetSection("Settings"));
                });

        public static ILogger CreateLogger() =>
            new LoggerConfiguration()
                .ReadFrom.Configuration(CreateBuilder().Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        
        public static IConfigurationBuilder CreateBuilder() =>
             new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables();
    }
}