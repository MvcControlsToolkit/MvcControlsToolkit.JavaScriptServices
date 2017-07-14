using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using MvcControlsToolkit.JavaScriptServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;

namespace MvcControlsToolkit.Core.Extensions
{
    public static class ClientCompilationServicesExtensions
    {
        private const string defaultClientSourceDirectory = "ClientApp";
        private const string defaultRazorExtension = "cshtml";
        public static async Task<IApplicationBuilder> UseRazorClientTemplates(
            this IApplicationBuilder builder,
            IHostingEnvironment environment,
            string sourceDirectory= defaultClientSourceDirectory,
            string destinationDirectory=null, 
            string extension= defaultRazorExtension)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (!environment.IsDevelopment()) return builder;
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (sourceDirectory == null) sourceDirectory = defaultClientSourceDirectory;
            if (destinationDirectory == null) destinationDirectory = sourceDirectory;
            sourceDirectory = Path.Combine(environment.ContentRootPath, sourceDirectory);
            destinationDirectory = Path.Combine(environment.ContentRootPath, destinationDirectory);
            var services = builder.ApplicationServices;
            await new SPAViewsProcessor(
                services.GetService<IRazorViewEngine>(),
                services.GetService<ITempDataProvider>(),
                services,
                services.GetService<IModelMetadataProvider>()
                ).RenderFolderTreeAsync(
                    sourceDirectory,
                    destinationDirectory,
                    defaultRazorExtension);
            return builder;
        }
    }
}
