using Grpc.Core;
using Microsoft.Extensions.Logging;
using ON.Authentication.SimpleAuth.Service.Helpers;
using ON.Fragments.Generic;
using System.Threading.Tasks;

namespace ON.Authentication.SimpleAuth.Service.Services
{
    public class ServiceOpsService : ServiceOpsInterface.ServiceOpsInterfaceBase
    {
        private readonly OfflineHelper offlineHelper;
        private readonly ILogger<ServiceOpsService> logger;

        public ServiceOpsService(OfflineHelper offlineHelper, ILogger<ServiceOpsService> logger)
        {
            this.offlineHelper = offlineHelper;
            this.logger = logger;
        }

        public override Task<ServiceStatusResponse> ServiceStatus(ServiceStatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ServiceStatusResponse()
            {
                Status = offlineHelper.CurrentStatus
            });
        }

        public override Task<ServiceStatusResponse> ServiceOperation(ServiceOperationRequest request, ServerCallContext context)
        {
            switch (request.Operation)
            {
                case ServiceOperationRequest.Types.ServiceOperation.Offline:
                    offlineHelper.TakeOffline();
                    break;
                case ServiceOperationRequest.Types.ServiceOperation.Online:
                    offlineHelper.BringOnline();
                    break;
                case ServiceOperationRequest.Types.ServiceOperation.Restart:
                    break;
            }

            return Task.FromResult(new ServiceStatusResponse());
        }
    }
}
