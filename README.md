# GumCli - Git User Manager Command Line Interface

# Usage
Gumcli is an easy way to manage multiple git users in your global config, when you have multiple users for git, they might be for your job, open source contribution or personal repositories. You can save configs to use later, and manage them.

#  Commands:
  - **read**    Read configs from your list of config. 
  - **save**    Save current git user from your global git config. (Params: --title: string)
  - **new**     Save new config from command. (Params: --title: string; --name: string; --email: string)
  - **edit**    Edit configs from your list of config. (Params: --title: string; --new-title: string; --name: string; --email: string)
  - **delete**  Delete configs from your list of config. (Params: --title: string)
  - **set**     Set your global git user from one of your saved config. (Params: --title: string)
