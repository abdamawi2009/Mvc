// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewViewComponentResult : IViewComponentResult
    {
        // {0} is the component name, {1} is the view name.
        private const string ViewPathFormat = "Components/{0}/{1}";

        private readonly IViewEngine _viewEngine;
        private readonly string _viewName;
        private readonly ViewDataDictionary _viewData;

        public ViewViewComponentResult([NotNull] IViewEngine viewEngine, [NotNull] string viewName,
            ViewDataDictionary viewData)
        {
            _viewEngine = viewEngine;
            _viewName = viewName;
            _viewData = viewData;
        }

        public void Execute([NotNull] ViewComponentContext context)
        {
            throw new NotImplementedException("There's no support for syncronous views right now.");
        }

        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            string qualifiedViewName;
            if (_viewName.Length > 0 && _viewName[0] == '/')
            {
                // View name that was passed in is already a rooted path, the view engine will handle this.
                qualifiedViewName = _viewName;
            }
            else
            {
                // This will produce a string like: 
                //  
                //  Components/Cart/Default
                //
                // The view engine will combine this with other path info to search paths like:
                //
                //  Views/Shared/Components/Cart/Default.cshtml
                //  Views/Home/Components/Cart/Default.cshtml
                //  Areas/Blog/Views/Shared/Components/Cart/Default.cshtml
                //
                // This supports a controller or area providing an override for component views.
                qualifiedViewName = string.Format(
                    CultureInfo.InvariantCulture,
                    ViewPathFormat,
                    ViewComponentConventions.GetComponentName(context.ComponentType),
                    _viewName);
            }

            var view = FindView(context.ViewContext.RouteValues, qualifiedViewName);

            var childViewContext = new ViewContext(
                context.ViewContext,
                view,
                _viewData ?? context.ViewContext.ViewData,
                context.Writer);

            using (view as IDisposable)
            {
                await view.RenderAsync(childViewContext);
            }
        }

        private IView FindView([NotNull] IDictionary<string, object> context, [NotNull] string viewName)
        {
            var result = _viewEngine.FindView(context, viewName);
            if (!result.Success)
            {
                var locations = string.Empty;
                if (result.SearchedLocations != null)
                {
                    locations = Environment.NewLine +
                        string.Join(Environment.NewLine, result.SearchedLocations);
                }

                throw new InvalidOperationException(Resources.FormatViewEngine_ViewNotFound(viewName, locations));
            }

            return result.View;
        }
    }
}
