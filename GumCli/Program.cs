using GumCli;
using System.CommandLine;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.CommandLine.Parsing;

namespace gum;

class Program
{
    static readonly string _gumFolderPath = ".\\GumList";
    static readonly string _gumListJsonPath = ".\\GumList\\gum-list.json";
    //TO-DO: HANDLE EXCEPTIONS INSTEAD OF USING CONSOLE.WRITELINE;
    //TO-DO: Learn something about credential manager for git? That might be how git authenticates users;
    static int Main(string[] args)
    {
        GumConfig gumConfig = new GumConfig();

        Console.OutputEncoding = Encoding.UTF8;

        var titleOption = new Option<string>(
            name: "--title",
            description: "Title parameter for config.",
            parseArgument: ValidateTitle)
        { IsRequired = true };

        var newTitleOption = new Option<string>(
            name: "--new-title",
            description: "Title parameter for config.",
            parseArgument: ValidateTitle);

        var nameOption = new Option<string>(
            name: "--name",
            description: "Name parameter for config.",
            parseArgument: ValidateName)
        { IsRequired = true };

        var newNameOption = nameOption;
        newNameOption.IsRequired = false;

        var emailOption = new Option<string>(
            name: "--email",
            description: "Email parameter for config.",
            parseArgument: ValidateEmail)
        { IsRequired = true };

        var newEmailOption = emailOption;
        newEmailOption.IsRequired = false;

        var rootCommand = new RootCommand("Git user manager (●'◡'●)");
        var readCommand = new Command("read", "Read configs from your list of config.");
        var saveCommand = new Command("save", "Save current git user from your global git config.") { titleOption };
        var newCommand = new Command("new", "Save new config from command.") { titleOption, nameOption, emailOption };
        var editCommand = new Command("edit", "Edit configs from your list of config.") { titleOption, newTitleOption, newNameOption, newEmailOption };
        var deleteCommand = new Command("delete", "Delete configs from your list of config.") { titleOption };
        var setGitConfigCommand = new Command("set", "Set your global git user from one of your saved config.") { titleOption };

        rootCommand.AddCommand(readCommand);
        rootCommand.AddCommand(saveCommand);
        rootCommand.AddCommand(newCommand);
        rootCommand.AddCommand(editCommand);
        rootCommand.AddCommand(deleteCommand);
        rootCommand.AddCommand(setGitConfigCommand);
        SetupGum();

        readCommand.SetHandler(DisplayGumConfig);

        saveCommand.SetHandler(SaveGitConfig, titleOption);

        newCommand.SetHandler(NewConfig, titleOption, nameOption, emailOption);

        editCommand.SetHandler(EditGumConfig, titleOption, newTitleOption, newNameOption, newEmailOption);
        
        deleteCommand.SetHandler(DeleteGumConfig, titleOption);
        setGitConfigCommand.SetHandler(SetGitGlobalUser, titleOption);

        return rootCommand.Invoke(args);
    }

    static List<Item> ReadGumJson()
    {
        using (StreamReader r = new StreamReader(_gumListJsonPath))
        {
            string json = r.ReadToEnd();
            List<Item>? items = JsonConvert.DeserializeObject<List<Item>>(json);

            if (items == null)
            {
                throw new Exception("Gum config list not found.");
            }

            return items;
        }
    }

    static void DisplayGumConfig()
    {
        GumConfig gumConfig = new GumConfig();
        gumConfig.ConfigList = ReadGumJson();

        if (gumConfig.ConfigList.Count == 0) Console.WriteLine("Gum config list is empty.");
        for (var i = 0; i < gumConfig.ConfigList.Count(); i++)
        {
            var item = gumConfig.ConfigList[i];
            Console.WriteLine($"{i+1})" + " " + item.ToString());
        }
    }

    static void NewConfig(string title, string name, string email)
    {
        GumConfig gumConfig = new GumConfig();
        gumConfig.ConfigList = ReadGumJson();

        Item newConfig = new Item(title, name, email);

        if (ValidateAddUpdate(gumConfig, newConfig)) return;

        gumConfig.ConfigList.Add(newConfig);

        SaveGumConfigJson(gumConfig);

        Console.WriteLine("CONFIG CREATED SUCCESSFULLY");
    }

    static void SaveGitConfig(string title)
    {
        GumConfig gumConfig = new GumConfig();
        gumConfig.ConfigList = ReadGumJson();

        string output = (string)RunProcess("git config --list", true);

        List<string> lines = output.Split('\n').Where(line => line !=  "").ToList();
        lines = lines.ToList().Where(line => line.Substring(0, 4) == "user").ToList();

        if (lines.Count == 0)
        {
            Console.WriteLine("User config not found.");
            return;
        }

        string name = lines.FirstOrDefault(line => line.Contains("name"));
        string email = lines.FirstOrDefault(line => line.Contains("email"));

        if (name == null || email == null)
        {
            Console.WriteLine("User name or email is missing.");
            return;
        }

        Item newConfig = new Item(title, name.Split("=")[1], email.Split("=")[1]);

        if (ValidateAddUpdate(gumConfig, newConfig)) return;

        gumConfig.ConfigList.Add(newConfig);

        SaveGumConfigJson(gumConfig);

        Console.WriteLine("CONFIG CREATED SUCCESSFULLY");
    }

    static void EditGumConfig(string title, string newTitle, string newName, string newEmail)
    {
        GumConfig gumConfig = new GumConfig();
        gumConfig.ConfigList = ReadGumJson();

        var configItemIndex = gumConfig.ConfigList.FindIndex(x => x.Title == title);

        if (configItemIndex == -1)
        {
            Console.WriteLine($"Config with title '{title}' not found.");
            return;
        }

        var configItem = gumConfig.ConfigList[configItemIndex];

        string[] validateStringList = {newTitle, newName, newEmail};

        if (!validateStringList.Any(x => x != "" && x != null))
        {
            Console.WriteLine("Must inform an parameter to edit.");
            return;
        }

        configItem.Title = newTitle != null && newTitle != "" ? newTitle : configItem.Title;
        configItem.Name = newName != null && newName != "" ? newName : configItem.Name;
        configItem.Email = newEmail != null && newEmail != "" ? newEmail : configItem.Email;

        if(ValidateAddUpdate(gumConfig, configItem)) return;

        gumConfig.ConfigList[configItemIndex] = configItem;

        SaveGumConfigJson(gumConfig);

        Console.WriteLine("CONFIG EDITED SUCCESSFULLY");
    }

    static void DeleteGumConfig(string title)
    {
        GumConfig gumConfig = new GumConfig();
        gumConfig.ConfigList = ReadGumJson();

        var configItemIndex = gumConfig.ConfigList.FindIndex(x => x.Title == title);

        if (configItemIndex == -1)
        {
            Console.WriteLine($"Config with title '{title}' not found.");
            return;
        }

        gumConfig.ConfigList.RemoveAt(configItemIndex);

        SaveGumConfigJson(gumConfig);

        Console.WriteLine("CONFIG REMOVED SUCCESSFULLY");
    }

    static void SetGitGlobalUser(string title)
    {
        GumConfig gumConfig = new GumConfig();
        gumConfig.ConfigList = ReadGumJson();

        var configItemIndex = gumConfig.ConfigList.FindIndex(x => x.Title == title);

        if (configItemIndex == -1)
        {
            Console.WriteLine($"Config with title '{title}' not found.");
            return;
        }

        var configItem = gumConfig.ConfigList[configItemIndex];

        RunProcess($"git config --global user.name \"{configItem.Name}\" ");
        RunProcess($"git config --global user.email \"{configItem.Email}\" ");

        Console.WriteLine("GLOBAL GIT CONFIG SET SUCCESSFULLY");
    }

    static void SaveGumConfigJson(GumConfig gumConfig)
    {
        string gumConfigJson = JsonConvert.SerializeObject(gumConfig.ConfigList, Formatting.Indented);
        File.WriteAllText(_gumListJsonPath, gumConfigJson);
    }

    static void SetupGum()
    {
        if (!Directory.Exists(_gumFolderPath))
        {
            Directory.CreateDirectory(_gumFolderPath);
        }

        if (!File.Exists(_gumListJsonPath))
        {
            File.Create(_gumListJsonPath).Dispose();
            using (StreamWriter listFile = new StreamWriter(_gumListJsonPath))
            {
                listFile.WriteLine("[]");
            }
        }
    }

    static bool ValidateAddUpdate(GumConfig gumConfig, Item configItem)
    {
        (bool, string, string) configValidation = gumConfig.ValidateConfig(configItem);

        if (configValidation.Item1)
        {
            Console.WriteLine($"The chosen {configValidation.Item2}: '{configValidation.Item3}' for this configuration already exists in your saved items.");
            return true;
        }

        return false;
    }

    static string ValidateTitle(ArgumentResult arg)
    {
        return ValidateStringLength(arg, nameof(Item.Title), 25);
    }

    static string ValidateName(ArgumentResult arg)
    {
        return ValidateStringLength(arg, nameof(Item.Name), 30);
    }

    static string ValidateEmail(ArgumentResult arg)
    {
       if (!IsValidEmail(arg))
        {
            arg.ErrorMessage = "Email is invalid.";
            return null;
        }
        return ValidateStringLength(arg, nameof(Item.Email), 254);
    }

    static string ValidateStringLength(ArgumentResult arg, string property, int lengthValue)
    {
        var titleValue = arg.Tokens.Single().Value;

        if (titleValue?.Length > lengthValue)
        {
            arg.ErrorMessage = $"{property} too long (maximum of {lengthValue} characters).";
            return null;
        }
        else
        {
            return titleValue;
        }
    }

    //TEMPORARY FUNCTION TO VALIDATE EMAIL UNTIL I FEEL LIKE FIGURING IT OUT MYSELF
    static bool IsValidEmail(ArgumentResult arg)
    {
        string email = arg.Tokens.Single().Value;
        var trimmedEmail = email.Trim();

        if (trimmedEmail.EndsWith("."))
        {
            return false;
        }
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == trimmedEmail;
        }
        catch
        {
            return false;
        }
    }

    static object? RunProcess(string command, bool hasOutput = false)
    {
        Process process = new Process();
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.Arguments = $"/c {command}";
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        process.WaitForExit();

        if (hasOutput)
        {
            return process.StandardOutput.ReadToEnd();
        }

        return null;
    }
}