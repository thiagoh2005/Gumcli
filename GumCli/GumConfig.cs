namespace GumCli
{
    public class GumConfig
    {
        public string GitConfigPath { get; set; } = "%userprofile%";
        public List<Item> ConfigList { get; set; } = new List<Item>();

        public (bool, string, string) ValidateConfig(Item newConfig)
        {
            //if (ConfigList.Any(config => config.Title == newConfig.Title)) return (true, nameof(Item.Title), newConfig.Title);
            //if (ConfigList.Any(config => config.Name == newConfig.Name)) return (true, nameof(Item.Name), newConfig.Name);
            //if (ConfigList.Any(config => config.Email == newConfig.Email)) return (true, nameof(Item.Email), newConfig.Email);
            foreach(var item in ConfigList)
            {
                if (item.Id == newConfig.Id) continue;
                if (item.Title == newConfig.Title) return (true, nameof(Item.Title), item.Title);
                if (item.Name == newConfig.Name) return (true, nameof(Item.Name), item.Name);
                if (item.Email == newConfig.Email) return (true, nameof(Item.Email), item.Email);
            }

            return (false, "", "");
        }
    }
}
