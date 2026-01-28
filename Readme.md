# EasySave - Solution de Sauvegarde Professionnelle

**EasySave** est un logiciel de sauvegarde de données robuste conçu pour les entreprises. Il permet de gérer des travaux de sauvegarde (complets ou différentiels) via une interface en ligne de commande (CLI) ou une interface graphique (GUI), tout en assurant un suivi en temps réel et une sécurité via cryptage.

---

## Fonctionnalités Clés

### Gestion des Sauvegardes

- **Types :** Sauvegardes complètes et différentielles.
- **Sources/Cibles :** Support des disques locaux, externes et lecteurs réseau (chemins UNC).
- **Flexibilité :** Exécution unitaire ou séquentielle (V1/V2) et parallèle (V3).

### Suivi & Logs

- **EasyLog.dll :** Une bibliothèque partagée pour l'écriture des journaux d'activité.
- **Formats :** Logs en JSON (V1.0) et XML/JSON (V1.1+).
- **État en Temps Réel :** Fichier `state.json` permettant de suivre la progression (taille, nombre de fichiers, % d'avancement).

### Sécurité & Métier

- **CryptoSoft :** Intégration d'un module de cryptage externe pour les fichiers sensibles.
- **Logiciel Métier :** Détection automatique des logiciels de travail pour mettre en pause ou interdire les sauvegardes (évite la corruption de fichiers ou la perte de performance).

---

## Architecture Technique (Multi-Projets)

La solution est découpée en plusieurs projets pour respecter la séparation des responsabilités et permettre l'évolution vers la version 2.0 (GUI) et 3.0.

### 1. `EasySave.Core` (Bibliothèque de classes)

- **Rôle :** Le moteur du logiciel.
- **Contenu :** Logique de sauvegarde (copie de fichiers, calcul différentiel), modèles de données (`Job`, `Config`), et gestion du multi-langue.
- **Dépendance :** Référence `EasyLog.dll`.

### 2. `EasyLog` (Bibliothèque de classes / DLL)

- **Rôle :** Composant transverse pour la journalisation.
- **Contenu :** Logique d'écriture des logs journaliers (JSON/XML) et gestion de l'état en temps réel (`state.json`).
- **Particularité :** Génère la DLL demandée par le cahier des charges.

### 3. `EasySave.Cli` (Application Console)

- **Rôle :** Interface utilisateur pour la V1.0 et V1.1.
- **Contenu :** Analyse des arguments de ligne de commande (ex: `1-3`), affichage des menus et interactions console.
- **Dépendance :** Référence `EasySave.Core`.

### 4. `EasySave.Gui` (Application WPF/Avalonia)

- **Rôle :** Interface utilisateur pour la V2.0 et V3.0.
- **Contenu :** Fenêtres XAML, ViewModels (MVVM).
- **Dépendance :** Référence `EasySave.Core`.

---

## Schéma des dépendances

Voici comment les projets "communiquent" entre eux :

- **`Cli`** ➡️ regarde ➡️ **`Core`** ➡️ regarde ➡️ **`EasyLog`**
- **`Gui`** ➡️ regarde ➡️ **`Core`** ➡️ regarde ➡️ **`EasyLog`**

> **Note importante :** Le projet `EasyLog` est volontairement isolé car le cahier des charges précise qu'il doit pouvoir être réutilisé par d'autres projets à l'avenir.

## 🛠️ Installation et Utilisation

### Prérequis

- SDK .NET 6.0 ou supérieur.
- Environnement Windows (pour WPF) ou multi-plateforme (pour Avalonia).

### Compilation

```bash
# Restaurer les dépendances et compiler la solution
dotnet build EasySave.sln

```

### Utilisation (CLI)

L'exécutable peut être lancé avec des arguments pour automatiser les travaux :

- **Exécuter les travaux 1 à 3 :**

```bash
EasySave.exe 1-3

```

- **Exécuter les travaux 1 et 3 :**

```bash
EasySave.exe 1;3

```

---

## Tableau Comparatif des Versions

| Fonctionnalité       | V1.0       | V1.1       | V2.0       | V3.0                |
| -------------------- | ---------- | ---------- | ---------- | ------------------- |
| **Interface**        | Console    | Console    | Graphique  | Graphique           |
| **Langues**          | FR / EN    | FR / EN    | FR / EN    | FR / EN             |
| **Limite Travaux**   | 5          | 5          | Illimité   | Illimité            |
| **Format Logs**      | JSON       | JSON / XML | JSON / XML | JSON / XML          |
| **Mode d'exécution** | Séquentiel | Séquentiel | Séquentiel | Parallèle           |
| **Cryptage**         | Non        | Non        | Oui        | Oui (Mono-instance) |
| **Logiciel Métier**  | Ignoré     | Ignoré     | Arrêt      | Pause Automatique   |
| **Docker**           | Non        | Non        | Non        | Centralisation Logs |

---

## Configuration

Les fichiers de configuration et de logs sont stockés dans des emplacements compatibles avec les environnements serveurs (hors `C:\temp`).

- **Logs journaliers :** `logs/YYYY-MM-DD.json` (ou `.xml`)
- **Fichier d'état :** `state.json`

---

## Auteurs

Projet développé dans le cadre du cursus CESI école d'ingénieurs par :

- NOIRET Robin
- RUET Sébastien
- DROUHIN Louis
