// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication2
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next, IAuthenticationSchemeProvider schemes)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (schemes == null)
            {
                throw new ArgumentNullException(nameof(schemes));
            }

            _next = next;
            Schemes = schemes;
        }

        public IAuthenticationSchemeProvider Schemes { get; set; }

        public async Task Invoke(HttpContext context)
        {
            // Only is responsible for automatic authentication now
            var defaultAuthenticate = await Schemes.GetDefaultAuthenticateSchemeAsync();
            if (defaultAuthenticate != null)
            {
                var manager = context.RequestServices.GetRequiredService<IAuthenticationManager2>();
                var ticket = await manager.AuthenticateAsync(defaultAuthenticate.Name);
                context.User = ticket.Principal;
            }

            // Give each scheme a chance to handle the request
            var handlers = context.RequestServices.GetRequiredService<SchemeHandlerCache>();
            foreach (var scheme in await Schemes.GetPriorityOrderedSchemes())
            {
                var handler = await handlers.GetHandlerAsync(scheme.Name);
                var result = await handler.HandleRequestAsync();
                if (result.Handled)
                {
                    return;
                }
                if (result.Bypassed)
                {
                    break;
                }
                // result.Skipped is a no-op
            }
            await _next(context);
        }
    }
}