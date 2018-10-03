# pyRevit Command Line Tool Help

`pyrevit` is the command line tool, developed specifically to install and configure pyRevit in your production/development environment. Each section below showcases a specific set of functionality of the command line tool.

- [Getting Help](#getting-help)
  * [pyrevit CLI version](#pyrevit-cli-version)
  * [pyRevit Online Resources](#pyrevit-online-resources)
- [Managing pyRevit clones](#managing-pyrevit-clones)
  * [Installing pyRevit](#installing-pyrevit)
  * [Installing Custom Clones](#installing-custom-clones)
  * [Maintaining Clones](#maintaining-clones)
    + [Managing Git Clones](#managing-git-clones)
    + [Updating Clones](#updating-clones)
- [Attaching pyRevit to Installed Revits](#attaching-pyrevit-to-installed-revits)
- [Managing pyRevit extensions](#managing-pyrevit-extensions)
  * [Finding Extensions](#finding-extensions)
  * [Installing Extensions](#installing-extensions)
  * [Managing Installed Extensions](#managing-installed-extensions)
    + [Updating Extensions](#updating-extensions)
  * [Managing Extensions Lookup Sources](#managing-extensions-lookup-sources)
- [Getting Environment Info](#getting-environment-info)
- [Configuring pyRevit](#configuring-pyrevit)
- [Extra Revit-Related Functionality](#extra-revit-related-functionality)
  * [Clear pyRevit Cache Files](#clear-pyrevit-cache-files)
- [Logging CLI messages](#logging-cli-messages)


## Getting Help
There is a lot of commands and options available in `pyrevit`. These functionalities are grouped by their function. This document will guide you in using these commands and options based on what you're trying to achieve. See the sections below. A full list can be obtained by running:

``` shell
pyrevit help
pyrevit (-h | --help)

$ pyrevit help          # will take you to this page

$ pyrevit --help        # OR
$ pyrevit -h            # will print help to console
```

### pyrevit CLI version

To determine the version of your installed `pyrevit` cli tool:

``` shell
pyrevit (-V | --version)

$ pyrevit -V            # OR
$ pyrevit --version
 pyrevit v0.1.5.0
```

### pyRevit Online Resources

To access a variety of online resource on pyRevit, use these commands

``` shell
pyrevit (blog | docs | source | youtube | support)

$ pyrevit blog      # pyRevit blog page
$ pyrevit docs      # pyRevit documentation
$ pyrevit source    # pyRevit source code
$ pyrevit youtube   # pyRevit YouTube channel
$ pyrevit support   # pyRevit suppport page for pyRevit patrons
```

## Managing pyRevit clones

### Installing pyRevit

``` shell
pyrevit clone <clone_name> <deployment_name> [--dest=<dest_path>] [--source=<archive_url>] [--branch=<branch_name>] [--log=<log_file>]
pyrevit clone <clone_name> [--dest=<dest_path>] [--source=<repo_url>] [--branch=<branch_name>] [--log=<log_file>]
```
`pyrevit` can maintain multiple clones of pyRevit on your system. In order to do so, it needs to assign a name to each clone (`<clone_name>`). You'll set this name when cloning pyRevit or when adding an existing clone to `pyrevit` registry. From then on you can always refer to that clone by its name.

Let's say I want one clone of pyRevit `master` branch as my master repo; one clone of pyRevit without the full git repository (much smaller download) as my main repo; and finally one clone of the `develop` branch of pyRevit as my development repo.

Let's create the master clone first. This will be a full git repo of the master branch.

``` shell
$ # master is <clone_name> that we're providing to pyrevit cli
$ # we're not providing any value for <dest_path>, therefore pyrevit cli will clone
$ # pyRevit into the default path (%APPDATA%/pyRevit)
$ pyrevit clone master
```

Now let's create the main clone. This one does not include the full repository. It'll be cloned from the ZIP archive provided by the Github repo:

``` shell
$ # we're providing the <dest_path> for this clone
$ # we're using the `base` deployment in this example which includes the base pyRevit tools
$ pyrevit clone main base --dest="C:\pyRevit\main"
```

- `<deployment_name>`: When provided the tool installs from the ZIP archive and NOT the complete git repo. This is the preferred method and is used with the native pyRevit installer. pyRevit has multiple deployments. These deployments are defined in the [pyRevitfile](https://github.com/eirannejad/pyRevit/blob/develop/pyRevitfile) inside the pyRevit repo. Each deployment, only deploys a subset of directories.

Now let's create the final development clone. This is a full git repo.

``` shell
$ pyrevit clone dev --dest="C:\pyRevit\dev" --branch=develop
```

- `--branch=`: Specify a specific branch to be cloned

#### Installing Custom Clones

You can also use the clone command to install your own pyRevit clones from any git url. This is done by providing `--source=<repo_url>` or `--source=<archive_url>` depending on if you're cloning from a git repo or an archive.

``` shell
$ pyrevit clone mypyrevit --source="https://www.github.com/my-pyrevit.git" --dest="C:\pyRevit\mypyrevit" --branch=develop
```

Or install from a ZIP archive using `<deployment_name>`:

``` shell
$ pyrevit clone mypyrevit base --source="\\network\my-pyrevit.ZIP" --dest="C:\pyRevit\mypyrevit"
```

### Maintaining Clones

You can see a list of registered clones using. Notice the full clones and the no-git clones are listed separately:

``` shell
$ pyrevit clones

==> Registered Clones (full git repos)
Name: "master" | Path: "%APPDATA%\pyRevit\pyRevit"
Name: "dev" | Path: "C:\pyRevit\dev"

==> Registered Clones (deployed from archive)
Name: "main" | Path: "C:\pyRevit\main"
```

Get info on a clone or open the path in file explorer:

``` shell
pyrevit clones (info | open) <clone_name>

$ pyrevit clones info master        # get info on master clone
$ pyrevit clones open dev           # open dev in file explorer
```

Get info on available engines and deployments in a clone:

``` shell
pyrevit clones deployments <clone_name>
pyrevit clones engines <clone_name>

$ pyrevit clones deployments dev    # print a list of deployments
$ pyrevit clones engines dev        # print a list of engines
```

Add existing clones (created without using `pyrevit`), remove, rename, and delete clones:

``` shell
pyrevit clones add <clone_name> <clone_path> [--log=<log_file>]
$ pyrevit clones add newclone "C:Some\Path"     # register existing clone

pyrevit clones forget (--all | <clone_name>) [--log=<log_file>]
$ pyrevit clones forget newclone     # forget a clone (does not delete)
$ pyrevit clones forget --all        # for get all clones

pyrevit clones rename <clone_name> <clone_new_name> [--log=<log_file>]
$ pyrevit clones rename main base   # rename clone `main` to `base`

pyrevit clones delete [(--all | <clone_name>)] [--clearconfigs] [--log=<log_file>]
$ pyrevit clones delete base        # delete clone `base`
$ pyrevit clones delete --all       # delete all clones
$ pyrevit clones delete --all --clearconfigs    # delete all clones and clear configurations
```

#### Managing Git Clones

Get info about branch, version and current head commit:

``` shell
$ pyrevit clones branch dev     # get current branch of `dev` clone
$ pyrevit clones version dev    # get current version of `dev` clone
$ pyrevit clones commit dev     # get current head commit of `dev` clone
```

Setting current branch:

``` shell
pyrevit clones branch <clone_name> [<branch_name>] [--log=<log_file>]

$ pyrevit clones branch dev master  # changing current branch to master for `dev` clone
```

Setting current version:

``` shell
pyrevit clones version <clone_name> [<tag_name>] [--log=<log_file>]

$ pyrevit clones version dev v4.6  # changing current version to v4.6 for `dev` clone
```

Setting current head commit:

``` shell
pyrevit clones commit <clone_name> [<commit_hash>] [--log=<log_file>]

$ pyrevit clones commit dev b06ec244ce81f521115926924e7322b22b161b54  # changing current commit for `dev` clone
```

#### Updating Clones

The update command automatically updates full git clones using git pull and no-git clones by downloading and replacing all contents:

``` shell
pyrevit clones update (--all | <clone_name>) [--log=<log_file>]

$ pyrevit clones update --all       # update all clones
$ pyrevit clones update dev         # update `dev` clone only
```

### Attaching pyRevit to Installed Revits

`pyrevit` can detect the exact installed Revit versions on your machine. You can use the commands below to attach any pyRevit clone to any or all installed Revits. Make sure to specify the clone to be attached and the desired engine version:

``` shell
pyrevit attach <clone_name> (latest | dynamosafe | <engine_version>) (<revit_year> | --all | --attached) [--allusers] [--log=<log_file>]

$ pyrevit attach dev latest --all       # attach `dev` clone to all installed Revits using latest engine
$ pyrevit attach dev 277 --all       # attach `dev` clone to all installed Revits using 277 engine
$ pyrevit attach dev dynamosafe 2018       # attach `dev` clone to Revit 2018 using an engine that has no conflicts with Dynamo BIM
```

- `--alusers`: Use this switch to attach for all users. It creates the manifest files inside the `%PROGRAMDATA%/Autodesk/Revit/Addons` instead of `%APPDATA%/Autodesk/Revit/Addons`
- `--attached`: This options is helpful when updating existing attachments e.g. specifying a new engine but keeping the same attachments as before

List all the attachments:

``` shell
$ pyrevit attached

==> Attachments
Autodesk Revit 2016 Service Pack 2 | Clone: "dev"
Autodesk Revit 2018.3 | Clone: "dev"
Autodesk Revit 2019.1 | Clone: "dev"
```

Detaching pyRevit from a specific Revit version or all installed:

``` shell
pyrevit detach (--all | <revit_year>) [--log=<log_file>]

$ pyrevit detach --all
$ pyrevit detach 2019
```

## Managing pyRevit extensions

### Finding Extensions

``` shell
pyrevit extensions search <search_pattern>
pyrevit extensions (info | help | open) <extension_name>

$ pyrevit extensions search apex    # search for an extension with apex in name
$ pyrevit extensions info apex      # get info on extension with apex in name
```

- `<search_pattern>`: Regular Expression (REGEX) pattern to search for

### Installing Extensions

``` shell
pyrevit extend <extension_name> <dest_path> [--branch=<branch_name>] [--log=<log_file>]

$ pyrevit extend pyApex "C:\pyRevit\Extensions"     # install pyApex extension
```

- `<dest_path>`: The destination directory will be added to pyRevit extensions search paths automatically and will be loaded on the next pyRevit reload.
- `--branch`: Specific branch of the extension repo to be installed

To installing your own extensions, you'll need to specify what type if extension you're installing (ui or lib) and provide the url:

``` shell
pyrevit extend (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>] [--log=<log_file>]

$ pyrevit extend ui MyExtension "https://www.github.com/my-extension.git" "C:\pyRevit\Extensions" 
```

List all installed extensions:

``` shell
pyrevit extensions
```

### Managing Installed Extensions

Delete an extension completely using:

``` shell
pyrevit extensions delete <extension_name> [--log=<log_file>]

$ pyrevit extensions delete pyApex
```

Add, remove extension search paths for all your existing extensions:

``` shell
pyrevit extensions paths
pyrevit extensions paths forget --all [--log=<log_file>]
pyrevit extensions paths (add | forget) <extensions_path> [--log=<log_file>]

$ pyrevit extensions paths add "C:\pyRevit\MyExtensions"    # add a search path
$ pyrevit extensions paths forget --all         # forget all search paths
```

Enable or Disable an extension in pyRevit config file:

``` shell
pyrevit extensions (enable | disable) <extension_name> [--log=<log_file>]

$ pyrevit extensions enable pyApex
```

Getting info, opening help or installed directory:

``` shell
pyrevit extensions (info | help | open) <extension_name>

$ pyrevit extensions info apex      # get info on extension with apex in name
$ pyrevit extensions help apex      # open help page
$ pyrevit extensions open apex      # open path in file explorer
```

#### Updating Extensions

``` shell
pyrevit extensions update (--all | <extension_name>) [--log=<log_file>]

$ pyrevit extensions update --all       # update all installed extension
$ pyrevit extensions update pyApex      # update pyApex extension
```

### Managing Extensions Lookup Sources

`pyrevit` can lookup in other places when searching for extensions. This means that you can define a `json` file with all your private extensions and add the path to the `pyrevit` sources. Your extensions will show up in search results from then on and can be installed by their name:

``` shell
pyrevit extensions sources
pyrevit extensions sources forget --all [--log=<log_file>]
pyrevit extensions sources (add | forget) <source_json_or_url> [--log=<log_file>]

$ pyrevit extensions sources add "https://www.github.com/me/extensions.json" 
```

- `<source_json_or_url>`: Can be a `json` file path or web url

## Getting Environment Info

Use `env` command to get info about the current `pyrevit` environment:

``` shell
$ pyrevit env

==> Registered Clones (full git repos)
Name: "master" | Path: "%APPDATA%\pyRevit\pyRevit"
Name: "dev" | Path: "C:\pyRevit\dev"

==> Registered Clones (deployed from archive)
Name: "main" | Path: "C:\pyRevit\main"

==> Attachments
Autodesk Revit 2016 Service Pack 2 | Clone: "dev"
Autodesk Revit 2018.3 | Clone: "dev"
Autodesk Revit 2019.1 | Clone: "dev"

==> Installed UI Extensions

==> Installed Library Extensions

==> Extension Search Paths

==> Extension Sources - Default
https://github.com/eirannejad/pyRevit/raw/master/extensions/extensions.json

==> Extension Sources - Additional

==> Installed Revits
Autodesk Revit 2016 Service Pack 2 | Version: 16.0.490.0 | Language: 1033 | Path: "C:\Program Files\Autodesk\Revit 2016\"
Autodesk Revit 2018.3 | Version: 18.3.0.81 | Language: 1033 | Path: "C:\Program Files\Autodesk\Revit 2018\"
Autodesk Revit 2019.1 | Version: 19.1.0.112 | Language: 1033 | Path: "C:\Program Files\Autodesk\Revit 2019\"

==> Running Revit Instances
PID: 61532 | Autodesk Revit 2018.3 | Version: 18.3.0.81 | Language: 0 | Path: "C:\Program Files\Autodesk\Revit 2018"
```

## Configuring pyRevit

Use `config` command to configure pyRevit on your machine from an existing template configuration file:

``` shell
pyrevit config <template_config_path> [--log=<log_file>]

$ pyrevit config "C:\myPyRevitTemplateConfig.ini"
```

Configuring core configurations:

``` shell
pyrevit configs logs [(none | verbose | debug)] [--log=<log_file>]
$ pyrevit configs logs verbose      # set logging to verbose
```

``` shell
pyrevit configs allowremotedll [(enable | disable)] [--log=<log_file>]
$ pyrevit configs allowremotedll enable  # allow remote dll loads in dotnet
```
``` shell
pyrevit configs checkupdates [(enable | disable)] [--log=<log_file>]
$ pyrevit configs checkupdates enable  # enable check updates on startup
```
``` shell
pyrevit configs autoupdate [(enable | disable)] [--log=<log_file>]
$ pyrevit configs autoupdate enable  # enable auto-update on startup
```
``` shell
pyrevit configs rocketmode [(enable | disable)] [--log=<log_file>]
$ pyrevit configs rocketmode enable  # enable rocket mode
```
``` shell
pyrevit configs filelogging [(enable | disable)] [--log=<log_file>]
$ pyrevit configs filelogging enable  # enable file logging
```
``` shell
pyrevit configs loadbeta [(enable | disable)] [--log=<log_file>]
$ pyrevit configs loadbeta enable  # enable loading beta tools
```
``` shell
pyrevit configs usagelogging
pyrevit configs usagelogging enable (file | server) <dest_path> [--log=<log_file>]
pyrevit configs usagelogging disable [--log=<log_file>]
$ pyrevit configs usagelogging enable file "C:\logs" # enable usage logging to file
$ pyrevit configs usagelogging enable server "http://server" # enable usage logging to server
```
``` shell
pyrevit configs outputcss [<css_path>] [--log=<log_file>]
$ pyrevit configs outputcss "C:\myOutputStyle.css"  # setting custom output window styling
```

Use the `configs` command to configure your custom config options. Specify the option in `section:option` format:

``` shell
pyrevit configs <option_path> (enable | disable) [--log=<log_file>]
pyrevit configs <option_path> [<option_value>] [--log=<log_file>]

$ pyrevit configs mysection:myswitch enable      # set myswitch to True
$ pyrevit configs mysection:myvalue 12           # set myvalue to 12
```

Seed the configuration to `%PROGRAMDATA%/pyRevit` to be used as basis for pyRevit configurtions when configuring pyRevit using System account on a user machine:

``` shell
pyrevit configs seed [--lock] [--log=<log_file>]
$ pyrevit configs seed
```
- `--lock`: Locks the file for current user only. pyRevit will now allow changing the configurations if the seed config file is not writable.

## Extra Revit-Related Functionality

List installed or running Revits:

``` shell
pyrevit revits [--installed] [--log=<log_file>]

$ pyrevit revits --installed        # list installed revits
Autodesk Revit 2016 Service Pack 2 | Version: 16.0.490.0 | Language: 1033 | Path: "C:\Program Files\Autodesk\Revit 2016\"
Autodesk Revit 2018.3 | Version: 18.3.0.81 | Language: 1033 | Path: "C:\Program Files\Autodesk\Revit 2018\"
Autodesk Revit 2019.1 | Version: 19.1.0.112 | Language: 1033 | Path: "C:\Program Files\Autodesk\Revit 2019\"
```

Close all or specific running Revit:
``` shell
pyrevit revits killall [<revit_year>] [--log=<log_file>]

$ pyrevit revits killall 2018   # close all Revit 2018s
```

Generate a report from `RVT` or `RFA` file info:

``` shell
pyrevit revits fileinfo <file_or_dir_path> [--csv=<output_file>]

$ pyrevit revits fileinfo "C\model.rvt"
Created in: Autodesk Revit 2018.3.1 (20180423_1000(x64))
Workshared: Yes
Central Model Path: \\central\path\Project3.rvt
Last Saved Path: C:\Downloads\Project3.rvt
Document Id: bfb3db00-ca65-4c6a-aa64-329761e5d0ca
Open Workset Settings: LastViewed
Document Increment: 2
```

- `file_or_dir_path`: could be a file or directory
- `--csv=`: Write output to specified CSV file


#### Clear pyRevit Cache Files

Cache files are stored under `%APPDATA%/pyRevit/<revit-version>`. You can clear all or specific Revit version caches. Revit needs to be closed for this operaton since it keeps the files locked if it is open:

``` shell
pyrevit caches clear (--all | <revit_year>) [--log=<log_file>]

$ pyrevit caches clear 2018     # clear all caches for Revit 2018
```

## Logging CLI Debug Messages

With many commands you can log the complete log messages to a log file:

``` shell
$ pyrevit clone master --log="C:\logfile.txt"
```