﻿using System;
using System.Net;
using System.Web;
using System.Web.Helpers;

using Sitecore.Ship.Core;
using Sitecore.Ship.Core.Contracts;
using Sitecore.Ship.Core.Domain;
using Sitecore.Ship.Core.Services;
using Sitecore.Ship.Infrastructure.Configuration;
using Sitecore.Ship.Infrastructure.DataAccess;
using Sitecore.Ship.Infrastructure.Web;

namespace Sitecore.Ship.AspNet.Package
{

   
    public class LatestVersionCommand : CommandHandler
    {
        private readonly IInstallationRecorder _installationRecorder;

        public LatestVersionCommand(IInstallationRecorder installationRecorder)
        {
            _installationRecorder = installationRecorder;
        }

        public LatestVersionCommand() : this(new InstallationRecorder(new PackageHistoryRepository(), new PackageInstallationConfigurationProvider().Settings))
        {           
        }

       

        public override void HandleRequest(HttpContextBase context)
        {
            if (CanHandle(context))
            {
                var packageName = context.Request["name"];
                if (!string.IsNullOrWhiteSpace(packageName))
                {
                    var versionText = UpdatePackageVersionRetriver.GetUpdatePackageVersion(packageName);
                    TextResponse(versionText, HttpStatusCode.OK, context);
                    return;
                }

                try
                {
                    var installedPackage = _installationRecorder.GetLatestPackage();

                    if (installedPackage is InstalledPackageNotFound)
                    {
                        context.Response.StatusCode = (int) HttpStatusCode.NoContent;
                    }
                    else
                    {
                        var json = Json.Encode(new { installedPackage });

                        JsonResponse(json, HttpStatusCode.OK, context);
                    }
                }
                catch (NotFoundException)
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                }
            }
            else if (Successor != null)
            {
                Successor.HandleRequest(context);
            }
        }

        private static bool CanHandle(HttpContextBase context)
        {
            return context.Request.Url != null &&
                   context.Request.Url.AbsolutePath.EndsWith(ShipServiceUrl.PackageLatestVersion, StringComparison.InvariantCultureIgnoreCase) && context.Response.StatusCode != (int)HttpStatusCode.Unauthorized; ;
        }
    }
}