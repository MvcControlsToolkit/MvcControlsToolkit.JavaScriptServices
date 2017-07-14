using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;


namespace MvcControlsToolkit.JavaScriptServices
{
    public class RazorViewRenderer
    {
        private IRazorViewEngine _viewEngine;
        private ITempDataProvider _tempDataProvider;
        private IServiceProvider _serviceProvider;
        private IModelMetadataProvider _modelMetadataProvider;
        private ActionContext actionContext;
        IView view;
        public RazorViewRenderer(
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
        public RazorViewRenderer PrepareView(string viewName)
        {
            actionContext = BuildActionContext();

            var viewEngineResult = _viewEngine.FindView(actionContext, viewName, false);

            if (!viewEngineResult.Success)
            {
                throw new InvalidOperationException(string.Format(Resources.NoView, viewName));
            }

            view = viewEngineResult.View;
            return this;
        }
        public async Task<string> RenderViewToStringAsync<TModel>(string name, TModel model)
        {
            using(var output = new StringWriter())
            {
                await RenderViewToStreamAsync<TModel>(output, model);
                return output.ToString();
            }
        }
        public async Task<RazorViewRenderer> RenderViewToFileAsync<TModel>(string filename, TModel model)
        {
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            using (var output = new StreamWriter(File.OpenWrite(filename)))
            {
                await RenderViewToStreamAsync<TModel>(output, model);
                return this;
            }
        }
        
        public async Task<RazorViewRenderer> RenderViewToStreamAsync<TModel>(TextWriter textWriter, TModel model)
        {
            if (textWriter == null) throw new ArgumentNullException(nameof(textWriter));

            var viewContext = new ViewContext(
                actionContext,
                view,
                typeof(TModel) == typeof(object) ?
                new ViewDataDictionary(
                    metadataProvider: _modelMetadataProvider,
                    modelState: new ModelStateDictionary())
                {
                    Model = model
                } :
                new ViewDataDictionary<TModel>(
                    metadataProvider: _modelMetadataProvider,
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
            return this;
        }
        private ActionContext BuildActionContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = _serviceProvider;
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}
