# EasyLog.Lib - Documentation

## Table des matières
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Architecture](#architecture)
4. [Utilisation](#utilisation)
5. [Exemples](#exemples)
6. [Référence API](#référence-api)
7. [Créer un formatter personnalisé](#créer-un-formatter-personnalisé)

---

## Introduction

**EasyLog.Lib** est une librairie C# destiné à la journalisation (logging) d'événements. Elle permet d'écrire des logs dans un fichier avec un formatage personnalisable via un système de formateurs extensible.

### Caractéristiques principales

- Formatage flexible : Implémentez votre propre format de log (JSON, CSV, texte brut, etc.)
- Gestion des répertoires : Création automatique des répertoires si nécessaire
- Chemin dynamique : Changez le fichier de destination des logs à tout moment

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

// Initialiser EasyLog avec le formatter et le chemin du fichier
EasyLog logger = new EasyLog(formatter, "logs/application.log");

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
EasyLog logger = new EasyLog(formatter, "logs/user_events.log");

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

**Sortie dans le fichier** :
```json
{"timestamp":"2025-02-05 14:30:45","name":"auth","content":{"event_type":"login","user_id":42,"ip_address":"192.168.1.100","success":true}}
```

---

## Référence

### Classe EasyLog

#### Constructeur
```csharp
public EasyLog(ILogFormatter formatter, string logPath)
```

**Paramètres:**
- `formatter` (ILogFormatter) : Implémentation du formatter pour mettre en forme les logs
- `logPath` (string) : Chemin du fichier de log (créé s'il n'existe pas)

#### Méthode Write
```csharp
public void Write(DateTime timestamp, string name, Dictionary<string, object> content)
```

**Description:** Écrit un log formaté dans le fichier.

**Paramètres:**
- `timestamp` (DateTime) : Horodatage de l'événement
- `name` (string) : Nom ou catégorie de l'événement
- `content` (Dictionary<string, object>) : Données de l'événement

#### Méthode SetLogPath
```csharp
public void SetLogPath(string newLogPath)
```

**Description:** Change le fichier de destination des logs.

**Paramètres:**
- `newLogPath` (string) : Nouveau chemin du fichier de log

#### Méthode GetCurrentLogPath
```csharp
public string GetCurrentLogPath()
```

**Description:** Retourne le chemin actuellement utilisé pour les logs.

**Retour:** Le chemin du fichier de log (string)

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

## Version
- v1.0.0 - Version initiale avec format JSON
- Framework cible : .NET 10.0
