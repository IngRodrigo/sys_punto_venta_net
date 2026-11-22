using System;
using System.ServiceModel;

namespace SysPuntoVenta.Frontend.Services
{
    public static class ServiceClientFactory
    {
        public static string BaseUrl { get; set; } = "http://localhost:5050";

        private static BasicHttpBinding CreateBinding()
        {
            return new BasicHttpBinding
            {
                MaxReceivedMessageSize = 10 * 1024 * 1024,
                SendTimeout = TimeSpan.FromSeconds(30),
                ReceiveTimeout = TimeSpan.FromSeconds(30),
                OpenTimeout = TimeSpan.FromSeconds(15)
            };
        }

        public static IAuthServiceClient CreateAuthClient()
        {
            var binding = CreateBinding();
            var endpoint = new EndpointAddress($"{BaseUrl}/AuthService.svc");
            var factory = new ChannelFactory<IAuthServiceClient>(binding, endpoint);
            return factory.CreateChannel();
        }

        public static IProductoServiceClient CreateProductoClient()
        {
            var binding = CreateBinding();
            var endpoint = new EndpointAddress($"{BaseUrl}/ProductoService.svc");
            var factory = new ChannelFactory<IProductoServiceClient>(binding, endpoint);
            return factory.CreateChannel();
        }

        public static IFacturacionServiceClient CreateFacturacionClient()
        {
            var binding = CreateBinding();
            var endpoint = new EndpointAddress($"{BaseUrl}/FacturacionService.svc");
            var factory = new ChannelFactory<IFacturacionServiceClient>(binding, endpoint);
            return factory.CreateChannel();
        }

        public static ICatalogosServiceClient CreateCatalogosClient()
        {
            var binding = CreateBinding();
            var endpoint = new EndpointAddress($"{BaseUrl}/CatalogosService.svc");
            var factory = new ChannelFactory<ICatalogosServiceClient>(binding, endpoint);
            return factory.CreateChannel();
        }
    }
}
