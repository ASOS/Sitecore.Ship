using System;
using System.Net;
using System.Web;
using Newtonsoft.Json;
using Sitecore.Ship.Core;
using Sitecore.Ship.Core.Contracts;
using Sitecore.Ship.Core.Domain;
using Sitecore.Ship.Core.Services;
using Sitecore.Ship.Infrastructure;
using Sitecore.Ship.Infrastructure.Configuration;
using Sitecore.Ship.Infrastructure.DataAccess;
using Sitecore.Ship.Infrastructure.Diagnostics;
using Sitecore.Ship.Infrastructure.Install;
using Sitecore.Ship.Infrastructure.Update;
using Sitecore.Ship.Infrastructure.Web;

namespace Sitecore.Ship.AspNet.Package
{
    public class InstallPackageCommand : CommandHandler
    {
        private readonly IPackageRepository _repository;
        private readonly IInstallationRecorder _installationRecorder;
        private readonly IPublishService _publishService;
        private readonly ILog _logger;

        public InstallPackageCommand(IPackageRepository repository, IInstallationRecorder installationRecorder, IPublishService publishService, ILog logger)
        {
            _repository = repository;
            _installationRecorder = installationRecorder;
            _publishService = publishService;
            _logger = logger;
        }

        public InstallPackageCommand()
            : this(new PackageRepository(new UpdatePackageRunner(new PackageManifestReader())),
                   new InstallationRecorder(new PackageHistoryRepository(), new PackageInstallationConfigurationProvider().Settings),
                    new PublishService(), new Logger())
        {
        }

        public override void HandleRequest(HttpContextBase context)
        {
            if (CanHandle(context))
            {
                try
                {
                    var package = GetRequest(context.Request);
                    var manifest = _repository.AddPackage(package);
                    if (!package.AnalyzeOnly)
                    {
                        _installationRecorder.RecordInstall(package.Path, DateTime.Now);
                        foreach (var entry in manifest.Entries)
                        {
                            _logger.Write(string.Format("Ship: Adding {0} to publish queue", entry.ID));

                            if (entry.ID.HasValue)
                            {
                                if (entry.Version > 0)
                                {
                                    _publishService.AddToPublishQueue(entry.ID.Value, entry.Version, entry.Language);
                                }
                                else
                                {
                                    _publishService.AddToPublishQueue(entry.ID.Value);
                                }
                            }

                            _logger.Write(string.Format("Ship: {0} successfully added to publish queue", entry.ID));
                        }
                    }

                    var json = JsonConvert.SerializeObject(new { manifest.ManifestReport });
                    JsonResponse(json, manifest.ManifestReport.ErrorOccured, manifest.ManifestReport.WarningOccured, context);

                    context.Response.AddHeader("Location", ShipServiceUrl.PackageLatestVersion);
                }
                catch (NotFoundException)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
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
                   context.Request.Url.PathAndQuery.EndsWith("/services/package/install", StringComparison.InvariantCultureIgnoreCase) &&
                   context.Request.HttpMethod == "POST" && context.Response.StatusCode != (int)HttpStatusCode.Unauthorized;
        }

        private static InstallPackage GetRequest(HttpRequestBase request)
        {
            return new InstallPackage
            {
                Path = request.Form["path"],
                DisableIndexing = ParseBoolean(request.Form["DisableIndexing"]),
                EnableSecurityInstall = ParseBoolean(request.Form["EnableSecurityInstall"]),
                AnalyzeOnly = ParseBoolean(request.Form["AnalyzeOnly"]),
                SummeryOnly = ParseBoolean(request.Form["SummeryOnly"]),
                Version = request.Form["Version"]
            };
        }

        private static bool ParseBoolean(string request)
        {
            bool result;

            Boolean.TryParse(request, out result);

            return result;
        }
    }
}