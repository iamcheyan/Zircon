using System;
using System.Reflection;
using MirDB;
using Library;
using Library.SystemModels;
using Server.Envir;
using Server.DBModels;

class Program
{
    static void Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : @".\Database\";
        Console.WriteLine($"Root={System.IO.Path.GetFullPath(root)}");
        Console.WriteLine($"System.db exists={System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetFullPath(root), "System.db"))}");

        var session = new Session(SessionMode.Users, root)
        {
            BackUpDelay = 60,
        };
        session.Initialize(
            Assembly.GetAssembly(typeof(ItemInfo)),
            Assembly.GetAssembly(typeof(AccountInfo))
        );

        var currencies = session.GetCollection<CurrencyInfo>();
        Console.WriteLine($"CurrencyInfo Binding.Count={currencies.Binding.Count}");
        foreach (var c in currencies.Binding)
            Console.WriteLine($"  #{c.Index} Name={c.Name} Type={c.Type} DropItem={c.DropItem?.ItemName}");
    }
}
