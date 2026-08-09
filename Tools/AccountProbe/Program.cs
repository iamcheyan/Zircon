// 直读 Users.db 中账号字段: 密码 hash / WrongPasswordCount / Banned / BanExpiry
// 用法: dotnet run --project Tools/AccountProbe -- <dbroot> [email-filter]
using System;
using System.Linq;
using Library.SystemModels;
using MirDB;
using Library.MirDB;
using Server.DBModels;
using Server.Envir;

string root = args.Length > 0 ? args[0] : "Debug/Server/Database/";
root = Path.GetFullPath(root);
if (!root.EndsWith(Path.DirectorySeparatorChar)) root += Path.DirectorySeparatorChar;
string filter = args.Length > 1 ? args[1] : "test";
bool unban = args.Any(x => x.Equals("--unban", StringComparison.OrdinalIgnoreCase));

var session = new Session(SessionMode.Users, root);
session.Initialize(
    typeof(ItemInfo).Assembly,
    typeof(AccountInfo).Assembly
);

var colls = (System.Collections.Generic.Dictionary<Type, ADBCollection>)typeof(Session)
    .GetField("Collections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
    .GetValue(session);
foreach (var kv in colls.OrderBy(k => k.Key.Name))
    Console.WriteLine($"COLL {kv.Key.Name}: {kv.Value.Count}");
var accounts = session.GetCollection<AccountInfo>();
Console.WriteLine($"总账号数: {accounts.Count}");
if (unban)
{
    var account = accounts.Binding.FirstOrDefault(a =>
        a.EMailAddress.Equals(filter, StringComparison.OrdinalIgnoreCase));
    if (account == null)
    {
        Console.Error.WriteLine($"找不到账号: {filter}");
        Environment.ExitCode = 2;
    }
    else
    {
        Console.WriteLine($"解禁账号: {account.EMailAddress} (Banned={account.Banned}, WrongPasswordCount={account.WrongPasswordCount}, BanExpiry={account.BanExpiry:O})");
        account.Banned = false;
        account.BanReason = string.Empty;
        account.BanExpiry = DateTime.MinValue;
        account.WrongPasswordCount = 0;
        account.Password = SEnvir.CreateHash("test123");
        session.Save(true);
        Console.WriteLine("解禁完成，密码已重置为 test123，并已保存 Users.db");
    }
}
var rows = new List<string>();
for (int i = 0; i < accounts.Count; i++)
{
    var a = accounts[i];
    if (filter != "*" && !a.EMailAddress.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
    var props = a.GetType().GetProperties()
        .Where(p => p.DeclaringType == typeof(AccountInfo) && p.GetIndexParameters().Length == 0)
        .Select(p => $"{p.Name}={Format(p.GetValue(a))}");
    rows.Add($"--- {a.EMailAddress}\n    " + string.Join(" | ", props));
}
foreach (var r in rows.OrderBy(x => x)) Console.WriteLine(r);

static string Format(object v)
{
    if (v == null) return "null";
    if (v is byte[] b) return "hash:" + Convert.ToHexString(b);
    return v.ToString();
}
