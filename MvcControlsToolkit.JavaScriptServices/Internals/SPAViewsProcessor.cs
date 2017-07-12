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

namespace MvcControlsToolkit.JavaScriptServices.Internals
{
    internal class SPAViewsProcessor
    {
        private IRazorViewEngine _viewEngine;
        private ITempDataProvider _tempDataProvider;
        private IServiceProvider _serviceProvider;
        public SPAViewsProcessor(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
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
                fileName = fileName.Substring(0, fileName.Length - ext.Length) +
                    (culture.Name + "." + ext);
                
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
                    interceptor
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
    }
}
