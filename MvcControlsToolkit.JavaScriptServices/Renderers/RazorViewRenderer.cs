using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;


namespace MvcControlsToolkit.JavaScriptServices
{
    public class RazorViewRenderer
    {
        private IRazorViewEngine _viewEngine;
        private ITempDataProvider _tempDataProvider;
        private IServiceProvider _serviceProvider;
        public RazorViewRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }
        public async Task<string> RenderViewToStringAsync<TModel>(string name, TModel model)
        {
            using(var output = new StringWriter())
            {
                await RenderViewToStreamAsync<TModel>(output, name, model);
                return output.ToString();
            }
        }
        public async Task<string> RenderViewToStringAsync<TModel>(string filename, string name, TModel model)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            using (var output = new StreamWriter(File.OpenWrite(filename)))
            {
                await RenderViewToStreamAsync<TModel>(output, name, model);
                return output.ToString();
            }
        }
        public async Task RenderViewToStreamAsync<TModel>(TextWriter textWriter, string viewName, TModel model)
        {
            if (textWriter == null) throw new ArgumentNullException(nameof(textWriter));
            if (viewName == null) throw new ArgumentNullException(nameof(viewName));
            var actionContext = BuildActionContext();

            var viewEngineResult = _viewEngine.FindView(actionContext, viewName, false);

            if (!viewEngineResult.Success)
            {
                throw new InvalidOperationException(string.Format(Resources.NoView, viewName));
            }

            var view = viewEngineResult.View;

            
            var viewContext = new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary<TModel>(
                    metadataProvider: new EmptyModelMetadataProvider(),
                    modelState: new ModelStateDictionary())
                {
                    Model = model
                },
                new TempDataDictionary(
                    actionContext.HttpContext,
                    _tempDataProvider),
                textWriter,
                new HtmlHelperOptions());

            await view.RenderAsync(viewContext);  
        }
        private ActionContext BuildActionContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = _serviceProvider;
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}
