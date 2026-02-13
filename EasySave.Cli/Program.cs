using EasySave.Cli;

/// <summary>
/// EasySave CLI - Point d'entrée principal de l'application en ligne de commande.
///
/// Impact business: Démarre le gestionnaire de sauvegarde automatisé qui permet aux utilisateurs de:
/// - Créer et gérer des tâches de sauvegarde (Full/Differential)
/// - Exécuter des sauvegardes programmées ou à la demande
/// - Monitorer l'état des opérations via les logs
/// - Adapter les stratégies de sauvegarde (format, langue)
///
/// Objectif: Assurer la continuité de service par une protection de données robuste et flexible,
/// permettant la récupération rapide en cas de sinistre.
/// </summary>
var cli = new CLI();
cli.start();
