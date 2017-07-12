using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace MvcControlsToolkit.JavaScriptServices.Internals
{
    internal class ServiceInterceptor : IServiceProvider
    {
        private IServiceProvider _serviceProvider;
        private bool _useLocalization=false;
        public ServiceInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public bool UseLocalization { get { return _useLocalization; } }
        public object GetService(Type serviceType)
        {
            if (typeof(IStringLocalizer).IsAssignableFrom(serviceType) ||
                typeof(IHtmlLocalizer) == serviceType ||
                typeof(IViewLocalizer) == serviceType
                ) _useLocalization = true;
            return _serviceProvider.GetService(serviceType);
        }
    }
}
