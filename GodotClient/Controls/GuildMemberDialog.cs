using Godot;
using Library;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 GuildMemberDialog：成员名称、职务、权限和踢出操作。</summary>
public sealed partial class GuildMemberDialog : DXWindow
{
    private readonly DXLabel _member;
    private readonly DXTextInput _rank;
    private readonly DXButton _permission;
    private int _index;
    private GuildPermission _permissions;

    public GuildMemberDialog()
    {
        Text = "Guild Member";
        Size = new Vector2I(220, 250);
        _member = new DXLabel { Text = Lang.GuildMemberMemberLabel, FontSize = 10, Location = new Vector2I(18, 38), IsControl = false };
        AddControl(_member);
        AddControl(new DXLabel { Text = Lang.GuildMemberRankLabel, FontSize = 9, Location = new Vector2I(18, 70), IsControl = false });
        _rank = new DXTextInput { Location = new Vector2I(62, 66), Size = new Vector2I(135, 22) };
        AddControl(_rank);
        _permission = new DXButton { Text = Lang.GuildMemberPermissionLabel, FontSize = 9, Location = new Vector2I(18, 103), Size = new Vector2I(180, 25), Index = -1, LibraryFile = LibraryFile.Interface };
        _permission.MouseClick += (s, e) => CyclePermission();
        AddControl(_permission);
        AddControl(new DXLabel { Text = Lang.GuildMemberStorageLabel, FontSize = 8, Location = new Vector2I(18, 138), Size = new Vector2I(185, 32), IsControl = false });
        var confirm = new DXButton { Text = Lang.CommonControlConfirm, FontSize = 9, Location = new Vector2I(30, 195), Size = new Vector2I(70, 25), Index = -1, LibraryFile = LibraryFile.Interface };
        confirm.MouseClick += (s, e) =>
        {
            GameScene.Game?.SendGuildEditMember(_index, _rank.Text, _permissions);
            WindowManager.Close(this);
        };
        AddControl(confirm);
        var kick = new DXButton { Text = Lang.GuildMemberDialogKickButtonLabel, FontSize = 9, Location = new Vector2I(120, 195), Size = new Vector2I(70, 25), Index = -1, LibraryFile = LibraryFile.Interface };
        kick.MouseClick += (s, e) =>
        {
            GameScene.Game?.SendGuildKickMember(_index);
            WindowManager.Close(this);
        };
        AddControl(kick);
    }

    public void OpenMember(int index, string name, string rank, GuildPermission permission)
    {
        _index = index;
        _member.Text = $"成员：{name ?? "未知"}";
        _rank.Text = rank ?? string.Empty;
        _permissions = permission;
        _permission.Text = string.Format(Lang.GuildMemberPermissionLabel2, _permissions);
    }

    private void CyclePermission()
    {
        _permissions = _permissions == GuildPermission.None ? GuildPermission.AddMember :
            _permissions == GuildPermission.AddMember ? GuildPermission.Storage :
            _permissions == GuildPermission.Storage ? GuildPermission.FundsMerchant : GuildPermission.None;
        _permission.Text = string.Format(Lang.GuildMemberPermissionLabel2, _permissions);
    }
}
