## v1.5.0 (2026-02-13)

### Feat

- **gui**: implement settings page

### Fix

- **gui**: implement XML log display with dynamic format detection
- **gui**: implement logs format change with settings
- **gui**: implement subscribe to language change event
- **core**: update language

## v1.4.0 (2026-02-13)

### Feat

- internationalization of code and app
- **EasySave.Gui**: add confirmation for deletion
- **EasySave.Gui**: merge jobs and state pagees
- **EasySave.Gui**: add state page
- **EasySave.Utils**: add utils project, containing some converters

### Fix

- **EasySave.Gui**: change way to start statefile watcher
- **EasySave.Gui**: replace icons by svg and fix button colors
- **EasySave.Gui**: fix size and file remaining count size
- **EasySave.Gui**: launching a job doesn't block UI anymore

## v1.3.0 (2026-02-12)

### Feat

- **logs-page**: add logs in the GUI

### Fix

- **gui-logs**: fix log display
- **gui-log**: add stage edit on the repo
- **UML**: fix typo
- **gui**: create errorDialog for error popup
- **gui**: implement business app check for gui

## v1.2.0 (2026-02-12)

### Feat

- **gui**: implement create, execute and delete jobs + change grid for job list
- **gui**: add joblist card
- **gui**: add style on the gui
- **gui**: add menu + buttons
- **EasySave.GUI**: add Avalonia based GUI
- **core**: add logic for app check. Curent use hardcoded app name

### Fix

- **EasySave.GUI**: fix buttons size and fix dialogs theme
- **ci**: fix ci build error
- **core**: change the hardcoded name of business app to a json key into the config file
- **cryptosoft.exe**: add new version of cryptosoft.exe
- **core**: fix deduplicate method
- **language**: fix strings
- **global**: fix conflicts
- **jobmanager**: change language of error descriptions and logs for english
- **cryptosoft**: fix error intergration cryptosoft
- **global**: fix conflicts

### Refactor

- **GUI**: refacto code
- **jobmanager**: add encryption time into logs
- **jobmanager**: refacto executeCryptosoft method to avoid code duplication

## v1.1.1 (2026-02-10)

### Fix

- fix todo and change default language to english

## v1.1.0 (2026-02-10)

### Feat

- **logs**: add change logs-mod features
- **cryptosoft**: add cryptosoft.exe in the project
- **XmlLogFormatter.cs**: Added the function to write logs in XML format

### Fix

- **xml-and-json-log**: fix swipe xml and json logs

## v1.0.0 (2026-02-06)

### Feat

- **StateTracker**: implement statetracker in jobmanager
- **StateTracker**: add StateTracker class

### Fix

- **core**: fix conflicts
- **StateTracker**: fix StateTracker
- **StateTracker**: make optionnal some params in StateEntry class constructor

## v0.4.1 (2026-02-06)

### Fix

- **easylog**: implement unc format for logger

## v0.4.0 (2026-02-06)

### Feat

- **jobmanager.cs**: add method ExecuteDifferentialBackup
- **Jobmanager.cs**: add launchjob method for full type
- **jobmanager.cs**: add GetJobs method
- **JobManager.cs**: add removejob method
- **configParser.cs**: add configParser class

### Fix

- fix conflicts
- **easylog**: fix reopen log file logicin order to keep clear structure
- **easylog**: close log files when exit
- **jobmanager.cs**: fix class

## v0.3.0 (2026-02-06)

### Fix

- **easylog**: fix json format, daily rotation and path logic

## v0.2.2 (2026-02-05)

## v0.2.1 (2026-02-05)

### Feat

- **models**: create and implement StateEntry model with ToString method
- **models**: create and implement LogEntry with normalized method
- **EasySave.Models**: add ToString method to Job class

### Fix

- **models**: fix typo
- **models**: add comment for isActive fields

## v0.1.1 (2026-02-04)

### Feat

- **cli**: add method write to write a msg in cli

## v0.1.0 (2026-02-04)

### Feat

- **CLI**: cli with multilanguage
- **language**: implement language resx file

## v0.2.0 (2026-02-04)

### Feat

- **easylog**: create and implement easylog main classe
- **easylog**: create and implement json log formatter
- **easylog**: create log formatter interface
- **EasySave.Models**: add ToString method to Job class
- **EasySave.Models**: add Job and JobType classes

### Fix

- **easylog**: add project to solution file
- **easylog**: clear old class file

## v0.1.1 (2026-02-04)

### Feat

- **cli**: add method write to write a msg in cli

## v0.1.0 (2026-02-04)

### Feat

- **CLI**: cli with multilanguage
- **language**: implement language resx file
