# 🎯 Backlog Manager - Présentation Projet

## 📋 Vue d'ensemble

**Backlog Manager** est une application desktop de gestion de backlog et de suivi de projet développée spécifiquement pour BNP Paribas. Elle centralise la gestion des tâches, le suivi du temps, la planification et les indicateurs de performance dans une interface moderne et intuitive.

---

## 🎨 Captures d'écran principales

### Dashboard
- Vue d'ensemble personnalisée avec salutation de l'utilisateur
- **Activités récentes** cliquables pour navigation rapide
- Tâches urgentes avec échéances
- Notifications importantes
- Accès rapide aux fonctionnalités (Kanban, Timeline, nouvelle tâche)
- Guide utilisateur intégré

### Backlog
- 3 vues : **Tâches**, **Projets**, **Archives**
- Filtres avancés (statut, priorité, type, développeur, projet)
- Recherche instantanée par titre
- Boutons d'action adaptés aux permissions utilisateur

### Kanban Board
- Vue en colonnes : À Faire → En Attente → À Prioriser → En Cours → Test → Terminé
- **Drag & Drop** pour déplacer les tâches
- Alertes visuelles selon les délais (🔴 URGENT, 🟠 ATTENTION, 🟢 OK)
- Filtres par développeur et projet

### CRA (Compte-Rendu d'Activité)
- **Vue Calendrier** : saisie mensuelle du temps par jour
- **Vue Historique** : consultation et filtrage des CRA passés
- Détection automatique des jours fériés français
- Saisie en jours (1j = 8h)

---

## 👥 Utilisateurs et Rôles

### 👨‍💼 Administrateur (J00001)
✅ Accès complet à toutes les fonctionnalités  
✅ Gestion des utilisateurs, projets et référentiels  
✅ Accès aux **Paramètres système** (sauvegarde, export/import)  
✅ Consultation des logs d'audit  
✅ Création de toutes les tâches  

### 📊 Chef de Projet (J20001)
✅ Création et gestion de projets  
✅ Priorisation et assignation des tâches  
✅ Consultation des KPI et statistiques  
✅ Suivi du planning et des sprints  
✅ Création de tâches normales et spéciales  

### 🧑‍💻 Business Analyst (J10001, J10002)
✅ Création de demandes et user stories  
✅ Création de tâches normales  
✅ Consultation du backlog et des KPI  
✅ Suivi des tâches  
✅ Création de congés/support  

### 💻 Développeur (J04831, J30001-J30004)
✅ Consultation du backlog  
✅ Mise à jour du statut des tâches assignées  
✅ Saisie du CRA (temps passé)  
✅ Vue Kanban pour le suivi quotidien  
✅ Création de **congés et support uniquement**  
❌ Pas de création de tâches normales  

---

## ✨ Fonctionnalités clés

### 🏠 Dashboard intelligent
- **Personnalisé** selon l'utilisateur connecté
- **Activités récentes dynamiques** :
  - Création/modification de tâches
  - Temps saisi sur les tâches
  - Congés et absences
  - Support apporté aux collègues
- **Navigation rapide** : cliquer sur une activité ouvre directement la tâche

### 📋 Gestion complète du Backlog
- **Types de tâches** :
  - Normales : User Story, Bug, Amélioration, Technique, Run
  - Spéciales : Congés, Non Travaillé, Support
- **Filtrage avancé** : statut, priorité, type, développeur, projet
- **3 vues** : Tâches actives, Projets, Archives
- **Permissions adaptées** : les développeurs voient uniquement "Congés/Support"

### 📊 Kanban visuel et interactif
- **6 colonnes** de workflow
- **Drag & Drop** fluide entre les statuts
- **Alertes colorées** selon les échéances
- **Cartes compactes** : titre, priorité, dev, temps restant, progression

### 📝 CRA (Compte-Rendu d'Activité)
- **Calendrier mensuel** pour saisie rapide
- **Historique** avec filtres par date et type
- **Jours fériés automatiques** (calendrier français)
- **Types d'activité** : Run, Dev, Autre, Congés, Non Travaillé, Support
- **Conversion automatique** : 1 jour = 8 heures

### ⏱️ Timeline / Planning
- Vue Gantt des tâches
- Visualisation des sprints
- Suivi des échéances
- Planning des disponibilités

### 📈 Statistiques & KPI
- Vélocité de l'équipe
- Taux de complétion
- Répartition par priorité et type
- Analyse des délais
- Temps passé vs estimé

### 🔔 Centre de notifications
- Alertes sur tâches urgentes
- Rappels de deadlines
- Changements de statut
- Notifications centralisées

### 🧑‍💼 Administration (Admin uniquement)
- Gestion des utilisateurs et des rôles
- Attribution des permissions
- Gestion de l'équipe
- Activation/désactivation des comptes

### 🔍 Audit & Traçabilité (Admin uniquement)
- Logs complets de toutes les actions
- Filtres par date, utilisateur, type d'action
- Export des logs
- Historique complet pour conformité

### ⚙️ Paramètres système (Admin uniquement)
**Sauvegarde automatique** :
- Activation par checkbox
- Intervalle configurable (5-120+ minutes)
- Nettoyage automatique (garde les 10 dernières)
- Format : `backup_auto_YYYYMMDD_HHMMSS.db`

**Sauvegarde manuelle** :
- Création à la demande
- Format : `backup_manual_YYYYMMDD_HHMMSS.db`

**Export de données** :
- **Export SQLite** : copie complète de la base (.db)
- **Export JSON** : données structurées lisibles
- **Export Complet** : ZIP contenant SQLite + JSON + README
- **Export CSV** : backlog pour Excel/compatibilité

**Import de données** :
- Import SQLite avec backup automatique de sécurité
- Interface prête pour import JSON

---

## 🔄 Workflow typique

### Pour un Développeur
1. **Connexion** avec code BNP (ex: J04831)
2. **Dashboard** : consultation des activités récentes et tâches urgentes
3. **Kanban** : déplacement des tâches (À Faire → En Cours → Test → Terminé)
4. **CRA** : saisie quotidienne/hebdomadaire du temps passé
5. **Congés** : création d'une tâche "Congés" via le backlog

### Pour un Chef de Projet
1. **Connexion** avec code BNP (ex: J20001)
2. **Backlog** : création de nouvelles tâches
3. **Assignation** : attribution des développeurs aux tâches
4. **Priorisation** : définition des priorités (Urgent, Haute, Moyenne, Basse)
5. **Timeline** : vue d'ensemble du planning
6. **KPI** : consultation des statistiques d'équipe

### Pour un Administrateur
1. **Connexion** avec code admin (J00001)
2. **Administration** : gestion des utilisateurs et rôles
3. **Paramètres** : configuration de la sauvegarde automatique
4. **Export** : sauvegarde complète des données (SQLite + JSON)
5. **Audit** : consultation des logs pour traçabilité

---

## 🎯 Niveaux de priorité

- **🔴 Urgente** : Traitement immédiat requis
- **🟠 Haute** : Important, à traiter rapidement  
- **🟡 Moyenne** : Priorité standard
- **🟢 Basse** : Peut attendre

---

## 🔄 Statuts des tâches

1. **À Faire** : Tâche créée, prête à démarrer
2. **En Attente** : Bloquée, dépendances à résoudre
3. **À Prioriser** : Nécessite décision de priorité
4. **En Cours** : Développement actif
5. **Test** : En phase de validation
6. **Terminé** : Complétée et validée

---

## 💾 Stockage et Sécurité

### Base de données
- **Type** : SQLite (locale, rapide, fiable)
- **Localisation** : `bin/Release/data/backlog.db`
- **Création** : Automatique au premier lancement

### Sauvegardes
- **Automatiques** : Configurables toutes les X minutes
- **Manuelles** : À la demande via Paramètres
- **Localisation** : Dossier `Backups/`
- **Rétention** : 10 dernières sauvegardes automatiques conservées

### Sécurité
- **Permissions granulaires** par rôle
- **Audit log complet** de toutes les actions
- **Données locales** : pas de cloud, contrôle total
- **Backup automatique** avant chaque import

---

## 🛠️ Technologies utilisées

- **Framework** : WPF (.NET Framework 4.8)
- **Base de données** : SQLite (System.Data.SQLite)
- **Architecture** : MVVM (Model-View-ViewModel)
- **Langage** : C# 8.0
- **Sérialisation** : System.Text.Json
- **Compression** : System.IO.Compression

### Avantages techniques
✅ **Application desktop** : pas de dépendance internet  
✅ **Données locales** : sécurité et confidentialité  
✅ **Performance** : interface fluide et réactive  
✅ **Personnalisable** : code source accessible pour évolutions  
✅ **Maintenable** : architecture propre et documentée  

---

## 🎨 Design et Expérience utilisateur

### Branding BNP
- **Couleur principale** : BNP Green (#00915A)
- Interface claire avec accents verts
- Logo BNP Paribas en header

### Interface moderne
- Design épuré et professionnel
- Navigation intuitive
- Feedback visuel immédiat
- Raccourcis et actions rapides

### Accessibilité
- Icônes claires et explicites
- Codes couleurs cohérents
- Tooltips informatifs
- Messages d'erreur compréhensibles

---

## 📊 Bénéfices pour l'équipe

### Gain de temps
⏱️ Centralisation des outils (plus besoin de Jira + Excel + emails)  
⏱️ Navigation rapide via Dashboard  
⏱️ Saisie CRA simplifiée  
⏱️ Filtres et recherches performants  

### Meilleure visibilité
👁️ Vue d'ensemble en temps réel  
👁️ Kanban pour suivi visuel  
👁️ KPI et statistiques automatiques  
👁️ Historique d'activité complet  

### Collaboration facilitée
🤝 Assignation claire des tâches  
🤝 Notifications des changements  
🤝 Support entre développeurs tracé  
🤝 Commentaires et historique des modifications  

### Conformité
✅ Audit log complet pour traçabilité  
✅ Sauvegardes automatiques  
✅ Export des données pour archivage  
✅ Permissions strictes par rôle  

---

## 🚀 Démarrage rapide

### Lancement de l'application
1. Double-cliquer sur `BacklogManager.exe`
2. Entrer votre code utilisateur BNP (format JXXXXX)
3. Cliquer sur "Se connecter"

### Premier contact
- **Consultez le Guide** via le bouton "📖 Voir le guide" du Dashboard
- **Explorez le Backlog** pour voir les tâches existantes
- **Testez le Kanban** en déplaçant une tâche
- **Saisissez du temps** dans le CRA

### Support
- Guide utilisateur intégré dans l'application
- Documentation README.md complète
- Fichier UTILISATEURS_TEST.txt avec les comptes de test

---

## 📞 Contacts et comptes de test

### Comptes disponibles

**Administrateur** :
- Username : `J00001` - Admin Système

**Business Analysts** :
- Username : `J10001` - Sophie Martin
- Username : `J10002` - Marc Dubois

**Chef de Projet** :
- Username : `J20001` - Catherine Leroy

**Développeurs** :
- Username : `J04831` - Pierre-Romain HanGP (Scrum Master)
- Username : `J30001` - Thomas Bernard
- Username : `J30002` - Julie Petit
- Username : `J30003` - Alexandre Robert
- Username : `J30004` - Émilie Moreau

---

## 🎯 Points forts de l'application

### Pour les managers
✅ **Visibilité totale** sur l'activité de l'équipe  
✅ **KPI automatiques** sans saisie manuelle  
✅ **Traçabilité complète** via audit logs  
✅ **Export facile** pour reportings  

### Pour les chefs de projet
✅ **Priorisation claire** des tâches  
✅ **Planification visuelle** avec Timeline  
✅ **Assignation simplifiée** des ressources  
✅ **Suivi en temps réel** de l'avancement  

### Pour les développeurs
✅ **Interface simple** et rapide  
✅ **Kanban visuel** pour organiser son travail  
✅ **CRA intégré** (plus besoin d'Excel)  
✅ **Congés faciles** à déclarer  

### Pour l'entreprise
✅ **Solution on-premise** : données sous contrôle  
✅ **Pas d'abonnement** cloud coûteux  
✅ **Personnalisable** selon besoins futurs  
✅ **Évolutif** : nouvelles fonctionnalités possibles  

---

## 🔮 Évolutions possibles

### Court terme
- Graphiques KPI enrichis
- Export Excel natif
- Notifications par email
- Thèmes personnalisables

### Moyen terme
- API REST pour intégrations
- Application mobile (consultation)
- Planning Poker (chiffrage collaboratif)
- Tableaux de bord personnalisables

### Long terme
- Version web (intranet BNP)
- Intelligence artificielle (prédiction de charges)
- Intégration avec d'autres outils BNP
- Multi-projets / multi-équipes

---

## ✅ Conclusion

**Backlog Manager** est une solution complète, moderne et sécurisée pour la gestion de projet et le suivi d'activité. Conçue spécifiquement pour BNP Paribas, elle répond aux besoins de tous les profils utilisateurs tout en respectant les exigences de sécurité et de traçabilité de l'entreprise.

### Prêt pour production
✅ Application stable et testée  
✅ Base de données SQLite fiable  
✅ Sauvegardes automatiques configurées  
✅ Permissions correctement implémentées  
✅ Documentation complète fournie  

### Prochaines étapes
1. Formation des utilisateurs (sessions de 30min par rôle)
2. Phase pilote avec une équipe test (2-4 semaines)
3. Collecte de feedback et ajustements
4. Déploiement généralisé

---

**Questions / Démonstration en direct disponible** 🎬
