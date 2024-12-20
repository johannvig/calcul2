using Microsoft.Extensions.Hosting; // Importation du namespace pour configurer et démarrer un hôte générique
using Microsoft.Azure.Functions.Worker.Configuration; // Importation du namespace pour la configuration des Azure Functions Worker

// Création d'un hôte générique pour exécuter les Azure Functions
var host = new HostBuilder()
	// Configure les paramètres par défaut pour une application Azure Functions Worker
	.ConfigureFunctionsWorkerDefaults() 
	// Note : Utilisez `.ConfigureFunctionsWebApplication()` si vous travaillez avec un modèle basé sur une application Web.
	.Build(); // Construit l'hôte avec les paramètres définis

// Démarre l'hôte et exécute l'application Azure Functions
host.Run();
