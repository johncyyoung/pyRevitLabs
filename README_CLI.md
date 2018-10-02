# pyRevit Command Line Tool Help

`pyrevit` is the command line tool, developed specifically to install and configure pyRevit in your production/development environment. Each section below showcases a specific set of functionality of the command line tool.

There is a lot of commands and options available in `pyrevit`. These functionalities are grouped by their function. This document will guide you in using these commands and options based on what you're trying to achieve. See the sections below. A full list can be obtained by running:

``` shell
$ pyrevit --help        # OR
$ pyrevit -h

$ pyrevit help          # will take you to this page
```

### pyrevit CLI version

To determine the version of your installed `pyrevit` cli tool:

``` shell
$ pyrevit -V            # OR
$ pyrevit --version
pyrevit v0.1.5.0
```

### pyRevit Online Resources

To access a variety of online resource on pyRevit, use these commands

``` shell
$ pyrevit blog      # pyRevit blog page
$ pyrevit docs      # pyRevit documentation
$ pyrevit source    # pyRevit source code
$ pyrevit youtube   # pyRevit YouTube channel
$ pyrevit support   # pyRevit suppport page for pyRevit patrons
```

## Managing pyRevit clones

### Installing pyRevit

Command and Options:

``` shell
$ pyrevit clone <clone_name> [<dest_path>] [--branch=<branch_name>] [--deploy=<deployment_name>] [--nogit] [--log=<log_file>]
$ pyrevit clone <clone_name> <repo_or_archive_url> <dest_path> [--branch=<branch_name>] [--deploy=<deployment_name>] [--nogit] [--log=<log_file>]
```
`pyrevit` can maintain multiple clones of pyRevit on your system. In order to do so, it needs to assign a name to each clone (`<clone_name>`). You'll set this name when cloning pyRevit or when adding an existing clone to `pyrevit` registry. From then on you can always refer to that clone by its name.

Let's say I want one clone of pyRevit `master` branch as my master repo; one clone of pyRevit without the full git repository (much smaller download) as my main repo; and finally one clone of the `develop` branch of pyRevit as my development repo.

Let's create the master clone first:

``` shell
$ # master is <clone_name> that we're providing to pyrevit cli
$ # we're not providing any value for <dest_path>, therefore pyrevit cli will clone
$ # pyRevit into the default path (%appdata%/pyRevit)
$ pyrevit clone master
```

Now let's create the main clone. This one does not include the full repository. It'll be cloned from the ZIP archive provided by the Github repo:

``` shell
$ # we're providing the <dest_path> for this clone
$ # we're using the `base` deployment in this example which includes the base pyRevit tools
$ pyrevit clone main "C:\pyRevit\main" --nogit --deploy=base
```

- `--nogit`: Install from the ZIP archive and NOT the complete git repo
- `--deploy=`: pyRevit can have multiple deployments. These deployments are defined in the [pyRevitfile](https://github.com/eirannejad/pyRevit/blob/develop/pyRevitfile) inside the pyRevit repo. Each deployment, only deploys a set of directories.

Now let's create the final development clone.

``` shell
$ pyrevit clone dev "C:\pyRevit\dev" --branch=develop
```

- `--branch=`: Specify a specific branch to be cloned

### Maintaining Clones

Command and Options:

``` shell
pyrevit clones
pyrevit clones (info | open) <clone_name>
pyrevit clones add <clone_name> <clone_path> [--log=<log_file>]
pyrevit clones forget (--all | <clone_name>) [--log=<log_file>]
pyrevit clones rename <clone_name> <clone_new_name> [--log=<log_file>]
pyrevit clones delete [(--all | <clone_name>)] [--clearconfigs] [--log=<log_file>]
pyrevit clones branch <clone_name> [<branch_name>] [--log=<log_file>]
pyrevit clones version <clone_name> [<tag_name>] [--log=<log_file>]
pyrevit clones commit <clone_name> [<commit_hash>] [--log=<log_file>]
pyrevit clones update (--all | <clone_name>) [--log=<log_file>]
pyrevit clones deployments <clone_name>
pyrevit clones engines <clone_name>
```

``` shell
pyrevit env
pyrevit attach <clone_name> (latest | dynamosafe | <engine_version>) (<revit_year> | --all | --attached) [--allusers] [--log=<log_file>]
pyrevit attached
pyrevit detach (--all | <revit_year>) [--log=<log_file>]
pyrevit extend <extension_name> <dest_path> [--branch=<branch_name>] [--log=<log_file>]
pyrevit extend (ui | lib) <extension_name> <repo_url> <dest_path> [--branch=<branch_name>] [--log=<log_file>]
pyrevit extensions
pyrevit extensions search <search_pattern>
pyrevit extensions (info | help | open) <extension_name>
pyrevit extensions delete <extension_name> [--log=<log_file>]
pyrevit extensions paths
pyrevit extensions paths forget --all [--log=<log_file>]
pyrevit extensions paths (add | forget) <extensions_path> [--log=<log_file>]
pyrevit extensions (enable | disable) <extension_name> [--log=<log_file>]
pyrevit extensions sources
pyrevit extensions sources forget --all [--log=<log_file>]
pyrevit extensions sources (add | forget) <source_json_or_url> [--log=<log_file>]
pyrevit extensions update (--all | <extension_name>) [--log=<log_file>]
pyrevit revits [--installed] [--log=<log_file>]
pyrevit revits killall [<revit_year>] [--log=<log_file>]
pyrevit revits fileinfo <file_or_dir_path> [--csv=<output_file>]
pyrevit revits addons
pyrevit revits addons install <addon_name> <dest_path> [--allusers]
pyrevit revits addons uninstall <addon_name>
pyrevit init (ui | lib) <extension_name>
pyrevit init (tab | panel | pull | split | push | smart | command) <bundle_name>
pyrevit init templates
pyrevit init templates (add | forget) <init_templates_path>
pyrevit caches clear (--all | <revit_year>) [--log=<log_file>]
pyrevit config <template_config_path> [--log=<log_file>]
pyrevit configs logs [(none | verbose | debug)] [--log=<log_file>]
pyrevit configs allowremotedll [(enable | disable)] [--log=<log_file>]
pyrevit configs checkupdates [(enable | disable)] [--log=<log_file>]
pyrevit configs autoupdate [(enable | disable)] [--log=<log_file>]
pyrevit configs rocketmode [(enable | disable)] [--log=<log_file>]
pyrevit configs filelogging [(enable | disable)] [--log=<log_file>]
pyrevit configs loadbeta [(enable | disable)] [--log=<log_file>]
pyrevit configs usagelogging
pyrevit configs usagelogging enable (file | server) <dest_path> [--log=<log_file>]
pyrevit configs usagelogging disable [--log=<log_file>]
pyrevit configs outputcss [<css_path>] [--log=<log_file>]
pyrevit configs seed [--lock] [--log=<log_file>]
pyrevit configs <option_path> (enable | disable) [--log=<log_file>]
pyrevit configs <option_path> [<option_value>] [--log=<log_file>]


Options:
-h --help                   Show this screen.
-V --version                Show version.
--verbose                   Print info messages.
--debug                     Print docopt options and logger debug messages.
--core                      Install original pyRevit core only (no defualt tools).
--all                       All applicable items.
--attached                  All Revits that are configured to load pyRevit.
--authgroup=<auth_groups>   User groups authorized to use the extension.
--branch=<branch_name>      Target git branch name.
--log=<log_file>            Output all log levels to specified file.
```