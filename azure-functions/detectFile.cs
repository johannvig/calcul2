using System; // Namespace pour les fonctionnalités de base de .NET
using System.IO; // Pour manipuler les flux de données (Streams)
using System.Threading.Tasks; // Pour les méthodes asynchrones
using Microsoft.Azure.Functions.Worker; // Pour définir une fonction Azure et son contexte
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs; // Pour le déclencheur BlobTrigger
using Microsoft.Extensions.Logging; // Pour la journalisation
using Azure.Messaging.ServiceBus; // Pour interagir avec Azure Service Bus

namespace Company.Functions
{
    // Classe contenant la fonction Azure déclenchée par un Blob
    public class BlobTriggeredFunction
    {
        // Nom de la queue Service Bus où les messages seront envoyés
        private const string QueueName = "messagequeue";

        // Définition de la fonction Azure
        [Function("BlobTriggeredFunction")]
        public async Task Run(
            // Déclencheur lié au conteneur "images" dans Azure Blob Storage
            [BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] Stream blob, 
            // Récupération du nom du blob déclencheur
            string name,
            // Contexte de la fonction, utilisé pour des métadonnées et la journalisation
            FunctionContext context)
        {
            // Récupération d'un logger pour écrire des logs dans Azure Application Insights ou la console
            var logger = context.GetLogger("BlobTriggeredFunction");
            logger.LogInformation($"Blob déclenché : {name}, Taille : {blob.Length} octets");

            // Lecture de la chaîne de connexion à Azure Service Bus depuis les variables d'environnement
            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");

            // Création d'un client Service Bus
            await using var client = new ServiceBusClient(serviceBusConnectionString);
            // Création d'un sender pour envoyer des messages à la queue spécifiée
            var sender = client.CreateSender(QueueName);

            try
            {
                // Création d'un message contenant le nom du blob
                var message = new ServiceBusMessage(name);

                // Envoi du message à la queue Service Bus
                await sender.SendMessageAsync(message);
                logger.LogInformation($"Message envoyé à la queue pour le fichier {name}");
            }
            catch (Exception ex)
            {
                // Gestion des erreurs et journalisation en cas d'échec
                logger.LogError($"Erreur lors de l'envoi du message : {ex.Message}");
            }
            finally
            {
                // Libération des ressources : fermeture du sender et du client Service Bus
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}
