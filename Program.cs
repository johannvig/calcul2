using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker.Configuration;

var host = new HostBuilder()
	.ConfigureFunctionsWorkerDefaults() // ou ConfigureFunctionsWebApplication() si vous utilisez ce mod�le
	.Build();

host.Run();
