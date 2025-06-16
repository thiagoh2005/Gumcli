using System;

namespace GumCli
{
    public class Item
    {
        public Guid Id { get; private set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }

        public Item(string title, string name, string email, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            Title = title;
            Name = name;
            Email = email;
        }

        public override string ToString()
        {
            return $"Title: {Title}; User: {Name}; Email: {Email};";

        }
    }
}
