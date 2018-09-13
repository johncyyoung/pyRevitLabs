# pyRevit Command Line Tool Help

`pyrevit.exe` is the command line tool, developed specifically to install and configure pyRevit in your production environment. Each section below show cases a specific set of functionality of the command line tool.ß

## Managing pyRevit clones

### Installing pyRevit

Options:

``` bash
pyrevit install [--core] [--branch=<branch_name>] [<dest_path>]
pyrevit install <repo_url> <dest_path> [--core] [--branch=<branch_name>]
```

Examples:

``` bash
# Install pyRevit at the default location (`%appdata%/pyRevit`)
pyrevit install

# Install pyRevit at specific location
pyrevit install "C:\pyRevit"

# Install a specific branch of pyRevit
pyrevit install "C:\pyRevit" --branch=develop
```

``` bash
pyrevit (-h | --help)
pyrevit (-V | --version)
pyrevit help
pyrevit blog
pyrevit docs
pyrevit source
pyrevit youtube
pyrevit support
pyrevit register <repo_path>
pyrevit unregister <repo_path>
pyrevit uninstall [(--all | <repo_path>)] [--clearconfigs]
pyrevit setprimary <repo_path>
pyrevit checkout <branch_name> [<repo_path>]
pyrevit setcommit <commit_hash> [<repo_path>]
pyrevit setversion <tag_name> [<repo_path>]
pyrevit update [--all] [<repo_path>]
pyrevit attach (--all | <revit_year>) [<repo_path>] [--allusers]
pyrevit detach (--all | <revit_year>)
pyrevit setengine latest (--all | --attached | <revit_year>) [<repo_path>]
pyrevit setengine dynamosafe (--all | --attached | <revit_year>) [<repo_path>]
pyrevit setengine <engine_version> (--all | --attached | <revit_year>) [<repo_path>]
pyrevit extensions list
pyrevit extensions search [<search_pattern>]
pyrevit extensions info <extension_name>
pyrevit extensions help <extension_name>
pyrevit extensions open <extension_name>
pyrevit extensions install <extension_name> <dest_path> [--branch=<branch_name>]
pyrevit extensions install (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>]
pyrevit extensions uninstall <extension_name>
pyrevit extensions paths
pyrevit extensions paths (add | remove) <extensions_path>
pyrevit extensions (enable | disable) <extension_name>
pyrevit open
pyrevit info
pyrevit revit list [--installed]
pyrevit revit killall
pyrevit revit fileinfo <file_path>
pyrevit clearcache (--all | <revit_year>)
pyrevit allowremotedll [(enable | disable)]
pyrevit checkupdates [(enable | disable)]
pyrevit autoupdate [(enable | disable)]
pyrevit rocketmode [(enable | disable)]
pyrevit logs [(none | verbose | debug)]
pyrevit filelogging [(enable | disable)]
pyrevit loadbeta [(enable | disable)]
pyrevit usagelogging
pyrevit usagelogging enable (file | server) <dest_path>
pyrevit usagelogging disable
pyrevit outputcss [<css_path>]
pyrevit config seed
pyrevit config <option_path> (enable | disable)
pyrevit config <option_path> [<option_value>]


Options:
-h --help                   Show this screen.
-V --version                Show version.
--debug                     Print docopt options and logger debug messages.
--quiet                     Do not print any logger messages.
--core                      Install original pyRevit core only (no defualt tools).
--all                       All applicable items.
--attached                  All Revits that are configured to load pyRevit.
--authgroup=<auth_groups>   User groups authorized to use the extension.
--branch=<branch_name>      Target git branch name.
```