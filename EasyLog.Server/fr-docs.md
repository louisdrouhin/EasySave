# EasyLog.Server - Documentation

## Table des matières
1. [Introduction](#introduction)
2. [Architecture](#architecture)
3. [Installation](#installation)
4. [Configuration](#configuration)
5. [Utilisation](#utilisation)
6. [Protocole de communication](#protocole-de-communication)
7. [Déploiement et Docker](#déploiement-et-docker)
8. [Gestion du Serveur EasyLog Docker](#gestion-du-serveur-easylog-docker)
7. [Exemple de client](#exemple-de-client)
8. [Dépannage](#dépannage)

---

## Introduction

**EasyLog.Server** est un serveur de centralisation des logs qui reçoit les événements de journalisation en temps réel depuis des clients distants via TCP et les enregistre dans des fichiers locaux.

### Caractéristiques principales

- **Serveur TCP** : Écoute les connexions entrantes sur un port configurable
- **Réception asynchrone** : Traite plusieurs clients en parallèle sans bloquer
- **Format JSON** : Reçoit et enregistre les logs au format JSON
- **Rotation journalière** : Crée automatiquement de nouveaux fichiers de logs chaque jour
- **Intégration EasyLog.Lib** : Utilise la librairie EasyLog.Lib pour la gestion des logs
- **Gestion élégante de l'arrêt** : Support du shutdown graceful sur `Ctrl+C`
- **Logging d'activité** : Enregistre les événements du serveur (connexions, déconnexions, erreurs)

---

## Architecture

### Composants principaux

#### 1. **SocketServer**
Classe responsable du serveur TCP. Gère les connexions entrantes, la réception des données et leur stockage.

**Responsabilités:**
- Écoute les connexions TCP
- Accepte les clients distants
- Traite les messages JSON reçus
- Écrit les logs via EasyLog.Lib
- Gère le shutdown graceful

#### 2. **Program.cs**
Point d'entrée de l'application. Configure et démarre le serveur.

**Responsabilités:**
- Lecture des variables d'environnement (port, répertoire de logs)
- Création et démarrage du serveur
- Gestion du signal `Ctrl+C` pour l'arrêt propre

---

## Installation

### Prérequis

- SDK .NET 10.0 ou supérieur
- Port disponible (défaut: 5000)
- Accès en écriture au répertoire des logs

### Compilation

```bash
# Restaurer les dépendances et compiler
dotnet build EasyLog.Server.csproj

# Compiler en mode Release (recommandé pour la production)
dotnet publish EasyLog.Server.csproj --configuration Release
```

### Déploiement avec Docker

Un Dockerfile est fourni pour containeriser le serveur :

```bash
# Construire l'image Docker
docker build -t easylog-server .

# Lancer le conteneur
docker run -d -p 5000:5000 \
  -e LOG_PORT=5000 \
  -e LOG_DIR=/var/log/easylog \
  -v easylog-logs:/var/log/easylog \
  easylog-server
```

---

## Configuration

### Variables d'environnement

Le serveur se configure entièrement via les variables d'environnement :

| Variable | Description | Défaut | 
|----------|-------------|--------|
| `LOG_PORT` | Port TCP d'écoute | `5000` | 
| `LOG_DIR` | Répertoire de stockage des logs | `./logs` |

### Exemples de configuration

**Configuration locale (développement)**
```bash
set LOG_PORT=5000
set LOG_DIR=./logs
EasyLog.Server.exe
```

### Validation des paramètres

- **Port** : Doit être entre 1 et 65535
- **Répertoire** : Sera créé automatiquement s'il n'existe pas

---

## Utilisation

### Démarrage du serveur

**Sur Windows**
```bash
EasyLog.Server.exe
```

### Exemple de sortie console

```
Starting EasyLog Server...
Port: 5000
Log Directory: ./logs

[2025-02-18 14:30:45] EasyLog Server listening on port 5000
[2025-02-18 14:30:45] Log directory: ./logs
[2025-02-18 14:30:50] New client connected from 192.168.1.100:12345
[2025-02-18 14:30:52] Client disconnected (5 messages received)
[2025-02-18 14:35:10] New client connected from 192.168.1.101:54321
^C
[2025-02-18 14:35:15] Shutdown signal received
[2025-02-18 14:35:15] Server stopped
```

### Arrêt du serveur

Appuyez sur `Ctrl+C` pour arrêter le serveur proprement. Le serveur fermera les fichiers de logs et libérera les ressources avant de quitter.

---

## Protocole de communication

### Format des messages

Les clients envoient des logs au format JSON, une ligne par message :

```json
{
  "timestamp": "2025-02-18T14:30:45",
  "name": "backup",
  "content": {
    "sourcePath": "C:\\Documents",
    "destinationPath": "D:\\Backups",
    "filesCount": 42,
    "status": "completed"
  }
}
```

### Structure attendue

**Champs obligatoires:**
- `timestamp` (DateTime) : Horodatage du log au format ISO 8601
- `name` (string) : Catégorie ou nom de l'événement
- `content` (object) : Données supplémentaires

### Exemple de messages

**Message d'authentification**
```json
{"timestamp":"2025-02-18T14:30:45","name":"auth","content":{"user":"admin","ip":"192.168.1.100","success":true}}
```

**Message de sauvegarde**
```json
{"timestamp":"2025-02-18T14:31:00","name":"backup","content":{"jobId":"job-1","status":"started","fileCount":150,"totalSize":"5.2 GB"}}
```

**Message d'erreur**
```json
{"timestamp":"2025-02-18T14:31:15","name":"error","content":{"jobId":"job-1","errorCode":500,"message":"Destination directory not found"}}
```

### Gestion des erreurs

Si un message JSON est invalide :
- Le serveur enregistre l'erreur dans la console
- Continue à traiter les messages suivants
- Ne ferme pas la connexion client

Exemple d'erreur :
```
[2025-02-18 14:30:52] Invalid JSON from client: The JSON value could not be converted
```

---

## Déploiement et Docker

### ETAPE 1 : Publier l'exécutable Linux

C'est l'étape la plus importante : compiler le serveur pour Linux.

```bash
cd "C:/Users/Noiret/Documents/2 - Ressources/3.2 - Etudes/B2-genie_logiciel/0-PROJET/EasySave"

dotnet publish EasyLog.Server/EasyLog.Server.csproj -c Release -o ./EasyLog.Server/bin/Release/net10.0/publish -r linux-x64 --self-contained
```

Qu'est-ce que ça fait :
- `-c Release` : Compile en mode production (optimisé)
- `-o ./EasyLog.Server/bin/Release/net10.0/publish` : Dossier de sortie
- `-r linux-x64` : Crée un exécutable Linux (important!)
- `--self-contained` : Inclut le runtime .NET 10 dans l'exécutable

Résultat attendu :
```
Chemin: C:\Users...\EasyLog.Server\bin\Release\net10.0\publish\
```

---

### ETAPE 2 : Vérifier que l'exécutable Linux existe

```bash
ls "EasyLog.Server/bin/Release/net10.0/publish/EasyLog.Server"
```

Tu dois voir un fichier EasyLog.Server (sans .exe) d'environ 77-80 KB.

Vérifier c'est un vrai binaire Linux :
```bash
file "EasyLog.Server/bin/Release/net10.0/publish/EasyLog.Server"
```

Résultat attendu :
```
ELF 64-bit LSB pie executable, x86-64, version 1 (SYSV), dynamically linked
```

---

### ETAPE 3 : Construire l'image Docker

```bash
docker build -t easysave/easylog-server:latest -f EasyLog.Server/Dockerfile .
```

Qu'est-ce que ça fait :
- `-t easysave/easylog-server:latest` : Nom + tag de l'image
- `-f EasyLog.Server/Dockerfile` : Utilise ce Dockerfile
- `.` : Contexte = dossier courant

Résultat attendu :
```
[10] exporting to image
[10] naming to docker.io/easysave/easylog-server:latest done
[10] unpacking to docker.io/easysave/easylog-server:latest done
DONE 7.1s
```

---

## Gestion du Serveur EasyLog Docker

### 1. DÉMARRER le serveur

Première fois (créer + lancer le conteneur) :
```bash
docker run -d -p 5000:5000 -v easylog-volume:/logs --name easylog-server easysave/easylog-server:latest
```

Vérifier qu'il tourne :
```bash
docker ps
```

Voir les logs au démarrage :
```bash
docker logs easylog-server
```

---

### 2. ARRÊTER le serveur

```bash
docker stop easylog-server
```

Vérifier qu'il est arrêté :
```bash
docker ps -a
```

---

### 3. REDÉMARRER le serveur

Si déjà créé (arrêté) :
```bash
docker start easylog-server
```

Vérifier :
```bash
docker logs easylog-server
```

---

### 4. VOIR LES LOGS

Logs complets :
```bash
docker logs easylog-server
```

Logs en temps réel (suivi continu) :
```bash
docker logs -f easylog-server
```

Dernières 50 lignes :
```bash
docker logs --tail 50 easylog-server
```

Logs avec timestamps :
```bash
docker logs -t easylog-server
```

---

### 5. ACCÉDER AUX FICHIERS LOG

Voir les fichiers dans le volume :
```bash
docker run --rm -v easylog-volume:/logs alpine:latest ls -la /logs
```

Voir le contenu d'un fichier log :
```bash
docker run --rm -v easylog-volume:/logs alpine:latest cat /logs/nomDuFichier.json
```

Copier les logs sur ta machine :
```bash
docker run --rm -v easylog-volume:/logs -v C:\logs-export:/export alpine:latest cp -r /logs/* /export/
```

---

### 6. NETTOYER / SUPPRIMER

Arrêter + supprimer le conteneur (garde les logs) :
```bash
docker rm -f easylog-server
```

Supprimer aussi les logs (volume) :
```bash
docker volume rm easylog-volume
```

Supprimer tout (conteneur + volume + image) :
```bash
docker rm -f easylog-server
docker volume rm easylog-volume
docker rmi easysave/easylog-server:latest
```

---

### 7. TESTER LA CONNEXION

Éditer config.json :
```json
"easyLogServer": {
  "enabled": true,
  "mode": "server_only",
  "host": "localhost",
  "port": 5000
}
```

Lancer un job de test :
```bash
dotnet run --project EasySave.Cli/EasySave.Cli.csproj -- --config-path config.json --job test
```

Vérifier que le serveur a reçu les logs :
```bash
docker logs easylog-server
```

---

### 8. VÉRIFIER L'ÉTAT

État du conteneur :
```bash
docker ps -a | grep easylog-server
```

Infos détaillées :
```bash
docker inspect easylog-server
```

Utilisation ressources :
```bash
docker stats easylog-server
```

---
