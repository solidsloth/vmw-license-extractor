using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace LicenseExtractor.Lib
{
    public static class ServiceCollectionExtensions
    {
        private static readonly LicenseExtractorOptions options = new LicenseExtractorOptions();

        public static IServiceCollection AddLicenseExractor(
            this IServiceCollection serviceCollection,
            Action<LicenseExtractorOptions> configureOptions = null)
        {
            configureOptions?.Invoke(options);

            serviceCollection.AddSingleton<PdfKeyExtractor>();

            return serviceCollection;
        }
    }
}
