﻿using Grpc.Core;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ON.Authorization.FakePayments.Web.Helper
{
    public class ServiceNameHelper
    {
        public readonly Channel PaymentsServiceChannel;

        public ServiceNameHelper(IConfiguration configuration)
        {
            var uri = configuration.GetServiceUri("fakepayservice");
            PaymentsServiceChannel = new Channel(uri.Host, uri.Port, ChannelCredentials.Insecure);
        }
    }
}
