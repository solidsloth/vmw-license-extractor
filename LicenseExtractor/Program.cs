using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using LicenseExtractor.Lib;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace LicenseExtractor
{
    [HelpOption]
    class Program
    {
        private readonly ILogger logger;
        private readonly IConsole console;
        private readonly PdfKeyExtractor parser;

        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();

            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
            });

            services.AddLicenseExractor();

            IServiceProvider provider = services.BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions     
                .UseDefaultConventions()
                .UseConstructorInjection(provider);
            app.Execute(args);
        }

        public Program(
            ILogger<Program> logger,
            IConsole console,
            PdfKeyExtractor parser)
        {
            this.logger = logger;
            this.console = console;
            this.parser = parser;
        }

        [Required]
        [FileExists]
        [Argument(0, Name = "Path", Description = "The path to the pdf to parse.")]
        public string FilePath { get; set; }

        [Required]
        [Argument(1, Name = "Search", Description = "A string to search for in the key name.")]
        public string Search { get; set; }

        public int OnExecuteAsync()
        {
            string licenseKey;
            try
            {
                licenseKey = this.parser.FindKey(FilePath, Search.Trim());
            }
            catch
            {
                this.console.Error.WriteLine("Error extracting key...");
                return 1;
            }

            if (licenseKey == null)
            {
                this.console.Error.WriteLine("Key not found...");
                return 1;
            }

            this.console.WriteLine(licenseKey);

            return 0;
        }
    }
}
