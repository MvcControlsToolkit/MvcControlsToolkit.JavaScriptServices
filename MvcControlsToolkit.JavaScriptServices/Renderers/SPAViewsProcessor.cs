using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.IO;
using MvcControlsToolkit.JavaScriptServices.Internals;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Hosting;

namespace MvcControlsToolkit.JavaScriptServices
{
    internal class SPAViewsProcessor
    {
        private IRazorViewEngine _viewEngine;
        private ITempDataProvider _tempDataProvider;
        private IServiceProvider _serviceProvider;
        private IModelMetadataProvider _modelMetadataProvider;
        public SPAViewsProcessor(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            IModelMetadataProvider modelMetadataProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _modelMetadataProvider = modelMetadataProvider;
        }
        private async Task ExecuteWithCulture(Func<Task> toProcess, CultureInfo culture)
        {
            var prevCulture = CultureInfo.CurrentUICulture;
            try
            {
                await toProcess();
            }
            finally
            {
                CultureInfo.CurrentUICulture = prevCulture;
            }
            
        }
        private string addCultureToFileName(string fileName, CultureInfo culture)
        {
            string ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext)) fileName = fileName + ("." + culture.Name);
            else
            {
                fileName = Path.ChangeExtension(fileName, culture.Name+"."+ext);
            }
            return fileName;
        }
        public async Task RenderViewAsync<TModel>(string viewName, string outputName, TModel model)
        {
            var localizationOptions=_serviceProvider.GetService<IOptions<RequestLocalizationOptions>>()?.Value;
            var interceptor = new ServiceInterceptor(_serviceProvider);
            var renderer = new RazorViewRenderer
                (
                    _viewEngine,
                    _tempDataProvider,
                    interceptor,
                    _modelMetadataProvider
                );
            renderer.PrepareView(viewName);
            if(localizationOptions!=null 
                && localizationOptions.DefaultRequestCulture != null
                && localizationOptions.DefaultRequestCulture.UICulture != null)
            {
                await ExecuteWithCulture(
                    async () => { await renderer.RenderViewToFileAsync(outputName, model); } ,
                    localizationOptions.DefaultRequestCulture.UICulture);
            }
            else await renderer.RenderViewToFileAsync(outputName, model);
            if(interceptor.UseLocalization &&
               localizationOptions != null &&
               localizationOptions.SupportedUICultures != null &&
               localizationOptions.SupportedUICultures.Count >0)
            {
                foreach(var culture in localizationOptions.SupportedUICultures)
                {
                    if(culture != localizationOptions?.DefaultRequestCulture?.UICulture)
                    {
                        await ExecuteWithCulture(
                            async () => { await renderer.RenderViewToFileAsync(
                                addCultureToFileName(outputName, culture), 
                                model); },
                            culture);
                    }
                }
            }
        }
        protected async Task RenderFolderTreeRecAsync(string sourceFolder, 
            string destinationFolder,
            string extension)
        {
            foreach(var file in Directory.EnumerateFiles(sourceFolder, "*." + extension))
            {
                var fileName = Path.GetFileName(file);
                await RenderViewAsync<object>(
                    file,
                    Path.Combine(destinationFolder, Path.ChangeExtension(fileName, "html")),
                    null
                    );
            }
            foreach(var directory in Directory.EnumerateDirectories(sourceFolder))
            {
                var dirName = Path.GetFileName(directory);
                var destDir = Path.Combine(destinationFolder, dirName);
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                await RenderFolderTreeRecAsync(dirName,
                            destDir,
                            extension);
            }
            
        }
        public async Task RenderFolderTreeAsync(string sourceFolder,
            string destinationFolder = null,
            string extension = "cshtml")
        {
            if (string.IsNullOrEmpty(sourceFolder))
                throw new ArgumentNullException(nameof(sourceFolder));
            if (string.IsNullOrEmpty(extension))
                throw new ArgumentNullException(nameof(extension));
            if (string.IsNullOrEmpty(destinationFolder))
                destinationFolder = sourceFolder;


            if (!Directory.Exists(sourceFolder))
                throw new DirectoryNotFoundException(string.Format(Resources.DirectoryNotFound, sourceFolder));
            if (!Directory.Exists(destinationFolder))
            {
                var enclosingDir = Path.GetDirectoryName(destinationFolder);
                if (!Directory.Exists(enclosingDir))
                    throw new DirectoryNotFoundException(string.Format(Resources.DirectoryNotFound, enclosingDir));
                Directory.CreateDirectory(destinationFolder);

            }
            await RenderFolderTreeRecAsync(sourceFolder, destinationFolder, extension);
        }
    }
}
