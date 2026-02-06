# EasyLog.Lib - Documentation

## Table des matières
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Architecture](#architecture)
4. [Utilisation](#utilisation)
5. [Exemples](#exemples)
6. [Référence](#référence)
7. [Créer un formatter personnalisé](#créer-un-formatter-personnalisé)

---

## Introduction

**EasyLog.Lib** est une librairie C# destiné à la journalisation (logging) d'événements. Elle permet d'écrire des logs dans un fichier avec un formatage personnalisable via un système de formateurs extensible.

### Caractéristiques principales

- Formatage flexible : Implémentez votre propre format de log (JSON, CSV, texte brut, etc.)
- Gestion des répertoires : Création automatique des répertoires si nécessaire
- Rotation journalière : Rotation automatique des fichiers de log (un fichier par jour)
- Répertoire dynamique : Changez le répertoire de destination des logs à tout moment
- Normalisation UNC : Conversion automatique des chemins locaux en chemins UNC pour la compatibilité réseau

---

### Prérequis
- .NET 10.0 ou supérieur

---

## Architecture

### Composants principaux

#### 1. **EasyLog**
Classe responsable de l'écriture des logs dans un fichier. Elle utilise un formatter pour mettre en forme les données avant écriture.

#### 2. **ILogFormatter** (interface)
Interface pour les implémentations de formatage. Tout formatter doit implémenter cette interface.

#### 3. **JsonLogFormatter**
Formatter par défaut qui sérialise les logs au format JSON.

---

## Utilisation

### Initialisation basique

```csharp
// Créer un formatter (JSON dans cet exemple)
ILogFormatter formatter = new JsonLogFormatter();

// Initialiser EasyLog avec le formatter et le répertoire des logs
// Les logs seront automatiquement rotatés quotidiennement avec les fichiers nommés : yyyy-MM-dd_logs.json
EasyLog logger = new EasyLog(formatter, "logs");

// Écrire un log
var content = new Dictionary<string, object>
{
    { "action", "login" },
    { "userId", 123 },
    { "duration_ms", 456 }
};

logger.Write(DateTime.Now, "user_event", content);
```


---

## Exemples

### Exemple 1 : Logging d'événements utilisateur

```csharp
ILogFormatter formatter = new JsonLogFormatter();
EasyLog logger = new EasyLog(formatter, "logs/user_events");

// Événement de connexion
var loginEvent = new Dictionary<string, object>
{
    { "event_type", "login" },
    { "user_id", 42 },
    { "ip_address", "192.168.1.100" },
    { "success", true }
};

logger.Write(DateTime.Now, "auth", loginEvent);
```

**Sortie dans le fichier** (par exemple `logs/user_events/2025-02-05_logs.json`) :
```json
{"logs":[{"timestamp":"2025-02-05 14:30:45","name":"auth","content":{"event_type":"login","user_id":42,"ip_address":"192.168.1.100","success":true}}]}
```

**Rotation journalière** : Chaque jour, un nouveau fichier est créé automatiquement (par exemple `2025-02-06_logs.json` le jour suivant).

---

### Exemple 2 : Normalisation UNC des chemins

```csharp
ILogFormatter formatter = new JsonLogFormatter();
EasyLog logger = new EasyLog(formatter, "logs");

// Événement de sauvegarde avec des chemins locaux
var backupEvent = new Dictionary<string, object>
{
    { "sourcePath", "C:\\Documents\\Data" },
    { "destinationPath", "D:\\Backups\\Data" },
    { "status", "completed" }
};

logger.Write(DateTime.Now, "backup", backupEvent);
```

**Sortie dans le fichier** :
```json
{"logs":[{"timestamp":"2025-02-05 14:30:45","name":"backup","content":{"sourcePath":"\\\\localhost\\C$\\Documents\\Data","destinationPath":"\\\\localhost\\D$\\Backups\\Data","status":"completed"}}]}
```

**Note:** Les chemins locaux sont automatiquement convertis au format UNC pour assurer la compatibilité réseau et centralisée.

---

## Référence

### Classe EasyLog

#### Constructeur
```csharp
public EasyLog(ILogFormatter formatter, string logDirectory)
```

**Paramètres:**
- `formatter` (ILogFormatter) : Implémentation du formatter pour mettre en forme les logs
- `logDirectory` (string) : Chemin du répertoire des logs. Les logs sont stockés en fichiers quotidiens au format `yyyy-MM-dd_logs.json`

#### Méthode Write
```csharp
public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
```

**Description:** Écrit un log formaté dans le fichier. Les chemins contenus dans les données (clés contenant "Path") sont automatiquement normalisés au format UNC.

**Paramètres:**
- `timestamp` (DateTime) : Horodatage de l'événement
- `name` (string) : Nom ou catégorie de l'événement
- `content` (Dictionary<string, object>) : Données de l'événement

**Normalisation UNC:** Les chemins locaux (ex: `C:\Users\file.txt`) sont automatiquement convertis en chemins UNC (ex: `\\localhost\C$\Users\file.txt`)

#### Méthode SetLogPath
```csharp
public void SetLogPath(string newLogDirectory)
```

**Description:** Change le répertoire de destination des logs. Le fichier de log actuel est fermé avant le changement de répertoire.

**Paramètres:**
- `newLogDirectory` (string) : Nouveau chemin du répertoire des logs

#### Méthode GetCurrentLogPath
```csharp
public string GetCurrentLogPath()
```

**Description:** Retourne le chemin complet du fichier de log actuellement utilisé (incluant le nom du fichier avec la date).

**Retour:** Le chemin complet du fichier de log actuel (string)

#### Méthode GetLogDirectory
```csharp
public string GetLogDirectory()
```

**Description:** Retourne le chemin du répertoire des logs actuellement utilisé.

**Retour:** Le chemin du répertoire des logs (string)

#### Méthode Close
```csharp
public void Close()
```

**Description:** Ferme le fichier de log actuel en utilisant le formatter. Ceci est utile pour finaliser le fichier de log avant de terminer l'application.

---

### Interface ILogFormatter

```csharp
public interface ILogFormatter
{
    string Format(DateTime timestamp, string name, Dictionary<string, object> content);
}
```

**Description:** Définit le contrat pour les implémentations de formatage.

**Paramètres:**
- `timestamp` (DateTime) : Horodatage de l'événement
- `name` (string) : Nom de l'événement
- `content` (Dictionary<string, object>) : Contenu de l'événement

**Retour:** Une chaîne formatée prête à être écrite dans le fichier

---

### Classe JsonLogFormatter

```csharp
public class JsonLogFormatter : ILogFormatter
```

**Description:** Formatter qui sérialise les logs au format JSON compact (sans indentation).

**Format de sortie:**
```json
{"timestamp":"yyyy-MM-dd HH:mm:ss","name":"event_name","content":{...}}
```

---
