using System; // Namespace pour les fonctionnalités de base de .NET
using System.Drawing; // Pour manipuler les graphiques et images
using System.Drawing.Imaging; // Pour définir les formats d'image
using System.IO; // Pour manipuler les flux de données
using System.Threading.Tasks; // Pour gérer les opérations asynchrones
using Azure.Storage.Blobs; // Pour interagir avec Azure Blob Storage
using Microsoft.Azure.Functions.Worker; // Pour définir une fonction Azure
using Microsoft.Azure.Functions.Worker.Extensions.ServiceBus; // Pour le déclencheur Service Bus
using Microsoft.Extensions.Logging; // Pour la journalisation

namespace Company.Functions
{
    public class ServiceBusQueueFunction
    {
        // Constantes pour les noms des conteneurs source et destination
        private const string SourceContainerName = "images"; // Conteneur source contenant les blobs d'origine
        private const string DestinationContainerName = "processed-images"; // Conteneur de destination pour les blobs traités

        [Function("ServiceBusQueueFunction")] 
        // Attribut indiquant qu'il s'agit d'une fonction Azure nommée "ServiceBusQueueFunction"
        public async Task Run(
            // Déclencheur Service Bus qui reçoit le nom d'un blob depuis une queue nommée "messagequeue"
            [ServiceBusTrigger("messagequeue", Connection = "ServiceBusConnectionString")] string blobName,
            FunctionContext context) // Contexte de la fonction pour accéder au logger et aux métadonnées
        {
            // Création d'un logger pour enregistrer des messages
            var logger = context.GetLogger("ServiceBusQueueFunction");
            logger.LogInformation($"Message reçu de la queue : {blobName}");

            // Récupération de la chaîne de connexion pour Azure Blob Storage
            var blobConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var blobServiceClient = new BlobServiceClient(blobConnectionString);

            // Accéder aux conteneurs source et destination
            var sourceContainer = blobServiceClient.GetBlobContainerClient(SourceContainerName);
            var destinationContainer = blobServiceClient.GetBlobContainerClient(DestinationContainerName);

            try
            {
                // Obtenir le client Blob pour le fichier source
                var sourceBlob = sourceContainer.GetBlobClient(blobName);
                if (!await sourceBlob.ExistsAsync()) 
                {
                    // Vérifier si le blob existe, sinon enregistrer une erreur
                    logger.LogError($"Le blob {blobName} n'existe pas.");
                    return;
                }

                // Télécharger le contenu du blob source dans un flux en mémoire
                await using var originalBlobStream = new MemoryStream();
                await sourceBlob.DownloadToAsync(originalBlobStream);

                // Traiter l'image et ajouter un watermark
                await using var processedBlobStream = new MemoryStream();
                ProcessImage(originalBlobStream, processedBlobStream, "Watermark Text");
                processedBlobStream.Position = 0; // Réinitialiser la position du flux avant de le réutiliser

                // Charger l'image traitée dans le conteneur de destination
                var destinationBlob = destinationContainer.GetBlobClient(blobName);
                await destinationBlob.UploadAsync(processedBlobStream, overwrite: true);

                logger.LogInformation($"Fichier {blobName} traité et sauvegardé dans {DestinationContainerName}.");

                // Supprimer le fichier source après traitement
                await sourceBlob.DeleteAsync();
                logger.LogInformation($"Fichier original {blobName} supprimé.");
            }
            catch (Exception ex)
            {
                // Gestion des erreurs avec enregistrement des logs
                logger.LogError($"Erreur lors du traitement du fichier {blobName} : {ex.Message}");
            }
        }

        // Méthode pour traiter une image et ajouter un watermark
        private static void ProcessImage(Stream inputStream, Stream outputStream, string watermarkText)
        {
            using var image = Image.FromStream(inputStream); // Charger l'image d'entrée depuis le flux
            using var bitmap = new Bitmap(image); // Créer un objet Bitmap modifiable
            using var graphics = Graphics.FromImage(bitmap); // Obtenir l'objet Graphics pour dessiner sur l'image

            // Définir les paramètres du texte pour le watermark
            var font = new Font("Arial", 24, FontStyle.Bold);
            var brush = new SolidBrush(Color.FromArgb(50, 255, 255, 255)); // Couleur blanche semi-transparente
            var textSize = graphics.MeasureString(watermarkText, font); // Calculer la taille du texte

            // Ajouter le texte du watermark en répétant sur toute l'image
            for (float y = 0; y < bitmap.Height; y += textSize.Height + 20)
            {
                for (float x = 0; x < bitmap.Width; x += textSize.Width + 20)
                {
                    graphics.DrawString(watermarkText, font, brush, new PointF(x, y));
                }
            }

            // Sauvegarder l'image traitée dans le flux de sortie au format JPEG
            bitmap.Save(outputStream, ImageFormat.Jpeg);
        }
    }
}
