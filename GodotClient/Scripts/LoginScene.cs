using System;
using System.Collections.Generic;
using Godot;
using Library;
using ZirconClient.Controls;

namespace ZirconClient.Scripts;

public partial class LoginScene : Control
{
    private Network.NetworkManager _net;
    private LineEdit _emailEdit;
    private LineEdit _passwordEdit;
    private Button _loginBtn;
    private Button _registerBtn;
    private Label _statusLabel;
    private LineEdit _keyEdit;
    private LineEdit _newPasswordEdit;
    private List<SelectInfo> _pendingCharacters;
    private DXTextInput _skinEmail, _skinPassword;
    private DXButton _skinLogin, _skinRegister, _skinChange, _skinRanking, _skinOptions, _skinExit, _skinActivation;
    private DXLabel _skinForgot;
    private DXCheckBox _skinRemember;
    private DXLabel _skinStatus;
    private RankingDialog _loginRanking;
    private ConfigDialog _loginConfig;
    private LegacyLoginDialog _accountDialog, _changeDialog, _requestResetDialog, _resetDialog, _activationDialog, _requestActivationDialog;

    public override void _Ready()
    {
        ClientSettings.Load();
        ClientSettings.ApplyDisplaySettings();
        SoundPlayback.Play(this, SoundIndex.LoginScene);
        _net = GetNode<Network.NetworkManager>("/root/NetworkManager");

        _emailEdit = GetNode<LineEdit>("VBox/EmailRow/EmailEdit");
        _passwordEdit = GetNode<LineEdit>("VBox/PasswordRow/PasswordEdit");
        _loginBtn = GetNode<Button>("VBox/LoginBtn");
        _registerBtn = GetNode<Button>("VBox/RegisterBtn");
        _statusLabel = GetNode<Label>("VBox/StatusLabel");
        var vbox = GetNode<VBoxContainer>("VBox");
        _keyEdit = new LineEdit { PlaceholderText = "激活码/重置码" };
        _newPasswordEdit = new LineEdit { PlaceholderText = "新密码", Secret = true };
        vbox.AddChild(_keyEdit);
        vbox.AddChild(_newPasswordEdit);
        AddAccountButton(vbox, "修改密码", () => _net.Connection?.SendChangePassword(_emailEdit.Text, _passwordEdit.Text, _newPasswordEdit.Text));
        AddAccountButton(vbox, "申请密码重置", () => _net.Connection?.SendRequestPasswordReset(_emailEdit.Text));
        AddAccountButton(vbox, "重置密码", () => _net.Connection?.SendResetPassword(_keyEdit.Text, _newPasswordEdit.Text));
        AddAccountButton(vbox, "激活账号", () => _net.Connection?.SendActivation(_keyEdit.Text));
        AddAccountButton(vbox, "申请激活码", () => _net.Connection?.SendRequestActivationKey(_emailEdit.Text));

        _loginBtn.Pressed += OnLoginPressed;
        _registerBtn.Pressed += OnRegisterPressed;
        BuildLegacyLoginUi();

        // 连接服务端
        _net.Log += OnNetLog;
        // 自动登录: --auto-login 固定测试账号, 或 --user/--pass 指定账号
        bool autoLogin = AutoLoginArgs.AutoLogin;
        string host = ClientSettings.UseNetworkConfig ? ClientSettings.IPAddress : "127.0.0.1";
        int port = ClientSettings.UseNetworkConfig ? ClientSettings.Port : 7000;
        if (!_net.Connect(host, port))
        {
            SetStatus("无法连接服务端");
            return;
        }

        // 订阅网络事件
        _net.Connection.ConnectedEvent += OnConnected;
        _net.Connection.VersionOK += (v, k) =>
        {
            GD.Print($"[Login] 版本校验通过, version={v}, dbKey={k}");
            CallDeferred(nameof(ShowVersionOK), v);
            if (autoLogin)
            {
                GD.Print($"[Login] 自动登录: {AutoLoginArgs.User}");
                _net.Connection.SendLogin(AutoLoginArgs.User, AutoLoginArgs.Password);
            }
        };
        _net.Connection.LoginResultEvent += OnLoginResult;
        _net.Connection.ChangePasswordResultEvent += r => SetStatus($"修改密码结果: {r}");
        _net.Connection.RequestPasswordResetResultEvent += r => SetStatus($"申请重置结果: {r}");
        _net.Connection.ResetPasswordResultEvent += r => SetStatus($"重置密码结果: {r}");
        _net.Connection.ActivationResultEvent += r => SetStatus($"激活结果: {r}");
        _net.Connection.RequestActivationKeyResultEvent += r => SetStatus($"申请激活码结果: {r}");
        _net.Connection.NewAccountResultEvent += OnNewAccountResult;
        _net.Connection.RankingsEvent += p => _loginRanking?.ApplyRankings(p);
        _net.Connection.DisconnectedEvent += OnDisconnected;
    }

    private void AddAccountButton(VBoxContainer parent, string text, Action action)
    {
        var button = new Button { Text = text };
        button.Pressed += () => action();
        parent.AddChild(button);
    }

    private void OnNetLog(string msg) => GD.Print(msg);

    private void SetStatus(string text)
    {
        if (_statusLabel != null) _statusLabel.Text = text;
        if (_skinStatus != null) _skinStatus.Text = text;
    }

    private void OnConnected()
    {
        GD.Print("[Login] 服务端确认连接");
    }

    private void ShowVersionOK(string version)
    {
        SetStatus($"已连接服务端 (版本: {version})\n请登录或注册");
        _loginBtn.Disabled = false;
        _registerBtn.Disabled = false;
        if (_skinLogin != null) _skinLogin.Enabled = true;
        if (_skinRegister != null) _skinRegister.Enabled = true;
    }

    private void OnLoginResult(LoginResult result, string message, List<SelectInfo> characters, string address)
    {
        _pendingCharacters = characters ?? new List<SelectInfo>();
        _pendingLoginResult = result;
        _pendingLoginMessage = message;
        _net.BuyAddress = address;
        CallDeferred(nameof(ShowLoginResult));
    }
    private LoginResult _pendingLoginResult;
    private string _pendingLoginMessage;
    private void ShowLoginResult()
    {
        if (_pendingLoginResult == LoginResult.Success)
        {
            SoundPlayback.Stop(SoundIndex.LoginScene);
            SetStatus($"登录成功! 角色数: {_pendingCharacters.Count}");
            GD.Print($"[Login] 登录成功, 角色数 {_pendingCharacters.Count}");
            var selectScene = ResourceLoader.Load<PackedScene>("res://Scenes/SelectScene.tscn");
            var selectScript = selectScene.Instantiate<SelectScene>();
            selectScript.SetCharacters(_pendingCharacters);
            GetTree().Root.AddChild(selectScript);
            QueueFree();
        }
        else
        {
            SetStatus($"登录失败: {_pendingLoginResult}\n{_pendingLoginMessage}");
            _loginBtn.Disabled = false;
        }
    }

    private void OnNewAccountResult(NewAccountResult result)
    {
        CallDeferred(nameof(ShowNewAccountResult), (int)result);
    }
    private void ShowNewAccountResult(int resultInt)
    {
        var result = (NewAccountResult)resultInt;
        if (result == NewAccountResult.Success || result == NewAccountResult.AlreadyExists)
            SetStatus($"注册成功 ({result}), 请登录");
        else
            SetStatus($"注册失败: {result}");
        _registerBtn.Disabled = false;
    }

    private void OnDisconnected()
    {
        CallDeferred(nameof(ShowDisconnected));
    }
    private void ShowDisconnected()
    {
        SetStatus("连接已断开");
        _loginBtn.Disabled = true;
        _registerBtn.Disabled = true;
    }

    private void OnLoginPressed()
    {
        _loginBtn.Disabled = true;
        SetStatus("登录中...");
        string email = _skinEmail?.Text ?? _emailEdit.Text;
        string password = _skinPassword?.Text ?? _passwordEdit.Text;
        if (_skinRemember?.Checked == true)
        {
            ClientSettings.RememberDetails = true;
            ClientSettings.RememberedEMail = email;
            ClientSettings.RememberedPassword = password;
        }
        else
        {
            ClientSettings.RememberDetails = false;
            ClientSettings.RememberedEMail = string.Empty;
            ClientSettings.RememberedPassword = string.Empty;
        }
        ClientSettings.Save();
        _net.Connection?.SendLogin(email, password);
    }

    private void OnRegisterPressed()
    {
        _registerBtn.Disabled = true;
        SetStatus("注册中...");
        _net.Connection?.SendNewAccount(_skinEmail?.Text ?? _emailEdit.Text, _skinPassword?.Text ?? _passwordEdit.Text);
    }

    private void ToggleLoginConfig()
    {
        if (_loginConfig == null) return;
        WindowManager.Toggle(_loginConfig, this);
    }

    private void ToggleLoginRanking()
    {
        if (_loginRanking == null) return;
        WindowManager.Toggle(_loginRanking, this);
        if (_loginRanking.Visible) _net.Connection?.SendRankings();
    }

    private void OpenAccountDialog()
    {
        _accountDialog ??= CreateAccountDialog();
        WindowManager.Open(_accountDialog, this);
    }

    private void OpenChangeDialog()
    {
        _changeDialog ??= CreateChangeDialog();
        WindowManager.Open(_changeDialog, this);
    }

    private void OpenRequestResetDialog()
    {
        _requestResetDialog ??= CreateRequestResetDialog();
        WindowManager.Open(_requestResetDialog, this);
    }

    private LegacyLoginDialog CreateAccountDialog()
    {
        var dialog = new LegacyLoginDialog("注册新账号", new Vector2I(300, 255),
            new[] { "邮箱", "密码", "确认密码", "真实姓名", "出生日期", "推荐人" },
            new[] { false, true, true, false, false, false });
        dialog.Submitted += values =>
        {
            if (values[0].Length < 3 || values[1].Length < 1 || values[1] != values[2]) { SetStatus("注册信息不完整或两次密码不一致"); return; }
            DateTime.TryParse(values[4], out var birth);
            if (birth == default) birth = new DateTime(1990, 1, 1);
            _net.Connection?.SendNewAccount(values[0], values[1], string.IsNullOrWhiteSpace(values[3]) ? "Player" : values[3], birth, values[5]);
            WindowManager.Close(dialog);
        };
        return dialog;
    }

    private LegacyLoginDialog CreateChangeDialog()
    {
        var dialog = new LegacyLoginDialog("修改密码", new Vector2I(330, 205),
            new[] { "邮箱", "当前密码", "新密码", "确认新密码" }, new[] { false, true, true, true });
        dialog.Submitted += values =>
        {
            if (values[0].Length < 3 || values[2] != values[3]) { SetStatus("修改密码信息不完整"); return; }
            _net.Connection?.SendChangePassword(values[0], values[1], values[2]);
            WindowManager.Close(dialog);
        };
        return dialog;
    }

    private LegacyLoginDialog CreateRequestResetDialog()
    {
        var dialog = new LegacyLoginDialog("申请密码重置", new Vector2I(330, 150), new[] { "邮箱" }, secondary: "已有重置码？");
        dialog.Submitted += values => { if (!string.IsNullOrWhiteSpace(values[0])) _net.Connection?.SendRequestPasswordReset(values[0]); };
        dialog.SecondaryClicked += () => { _resetDialog ??= CreateResetDialog(); WindowManager.Close(dialog); WindowManager.Open(_resetDialog, this); };
        return dialog;
    }

    private LegacyLoginDialog CreateResetDialog()
    {
        var dialog = new LegacyLoginDialog("重置密码", new Vector2I(330, 180), new[] { "重置码", "新密码", "确认密码" }, new[] { false, true, true });
        dialog.Submitted += values =>
        {
            if (values[1] != values[2]) { SetStatus("两次密码不一致"); return; }
            _net.Connection?.SendResetPassword(values[0], values[1]);
            WindowManager.Close(dialog);
        };
        return dialog;
    }

    private LegacyLoginDialog CreateActivationDialog()
    {
        var dialog = new LegacyLoginDialog("激活账号", new Vector2I(330, 155), new[] { "激活码" }, secondary: "重新申请激活码");
        dialog.Submitted += values => { if (!string.IsNullOrWhiteSpace(values[0])) _net.Connection?.SendActivation(values[0]); };
        dialog.SecondaryClicked += () => { _requestActivationDialog ??= CreateRequestActivationDialog(); WindowManager.Close(dialog); WindowManager.Open(_requestActivationDialog, this); };
        return dialog;
    }

    private LegacyLoginDialog CreateRequestActivationDialog()
    {
        var dialog = new LegacyLoginDialog("申请激活码", new Vector2I(330, 150), new[] { "邮箱" });
        dialog.Submitted += values => { if (!string.IsNullOrWhiteSpace(values[0])) _net.Connection?.SendRequestActivationKey(values[0]); };
        return dialog;
    }

    private void BuildLegacyLoginUi()
    {
        var viewport = GetViewport().GetVisibleRect().Size;
        var background = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface1c,
            Index = 20,
            FixedSize = true,
            Size = new Vector2I(1024, 768),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Position = (viewport - new Vector2(1024, 768)) / 2f,
        };
        AddChild(background);

        AddLoginAnimation(background, 2200, 100, 10, true, true, false);
        AddLoginAnimation(background, 2400, 30, 5, true, true, false);
        AddLoginAnimation(background, 2300, 30, 10, true, false, true);
        AddLoginAnimation(background, 2500, 30, 8, true, true, false);

        var logoBackground = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface1c,
            Index = 23,
            Position = new Vector2((viewport.X - 564) / 2f, 25),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        AddChild(logoBackground);
        var logo = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface1c,
            Index = 22,
            FixedSize = true,
            Size = new Vector2I(564, 300),
            Position = new Vector2(-35, -35),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        logoBackground.AddControl(logo);

        var dialog = new DXImageControl
        {
            LibraryFile = LibraryFile.Interface,
            Index = 151,
            Position = new Vector2((viewport.X - 780) / 2f, viewport.Y - 135),
        };
        AddChild(dialog);
        dialog.AddControl(new DXLabel
        {
            Text = "请输入邮箱和密码",
            TextColour = new Color(214f / 255f, 190f / 255f, 148f / 255f),
            Location = new Vector2I(280, 38),
            Size = new Vector2I(220, 18),
            IsControl = false,
        });
        _skinEmail = new DXTextInput { Location = new Vector2I(70, 65), Size = new Vector2I(170, 14), Text = ClientSettings.RememberDetails ? ClientSettings.RememberedEMail : _emailEdit.Text, Border = false, FontSize = 8 };
        _skinPassword = new DXTextInput { Location = new Vector2I(357, 65), Size = new Vector2I(170, 14), Text = ClientSettings.RememberDetails ? ClientSettings.RememberedPassword : _passwordEdit.Text, Border = false, Secret = true, FontSize = 8 };
        dialog.AddControl(_skinEmail); dialog.AddControl(_skinPassword);
        _skinEmail.TextChanged += value => _emailEdit.Text = value;
        _skinPassword.TextChanged += value => _passwordEdit.Text = value;

        int defaultButtonHeight = MirSkin.GetSize(LibraryFile.Interface, 16).Y;
        if (defaultButtonHeight <= 0) defaultButtonHeight = 21;
        _skinLogin = new DXButton { Text = "登录", FontSize = 10, TextColour = new Color(1f, .88f, .55f), LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(550, 60), Size = new Vector2I(100, defaultButtonHeight), Enabled = false };
        _skinRegister = new DXButton { Text = "注册新账号", FontSize = 10, TextColour = new Color(1f, .88f, .55f), LibraryFile = LibraryFile.Interface, Index = 152, Location = new Vector2I(485, 0), Size = new Vector2I(136, 32), Enabled = false };
        _skinChange = new DXButton { Text = "修改密码", FontSize = 10, TextColour = new Color(1f, .88f, .55f), LibraryFile = LibraryFile.Interface, Index = 152, Location = new Vector2I(625, 0), Size = new Vector2I(136, 32) };
        _skinRanking = new DXButton { Text = "排行榜", FontSize = 9, TextColour = new Color(1f, .88f, .55f), LibraryFile = LibraryFile.Interface, Index = 153, Location = new Vector2I(20, 0), Size = new Vector2I(68, 32) };
        _skinOptions = new DXButton { Text = "选项", FontSize = 9, TextColour = new Color(1f, .88f, .55f), LibraryFile = LibraryFile.Interface, Index = 153, Location = new Vector2I(93, 0), Size = new Vector2I(68, 32) };
        _skinLogin.MouseClick += (o, e) => OnLoginPressed();
        _skinRegister.MouseClick += (o, e) => OpenAccountDialog();
        _skinChange.MouseClick += (o, e) => OpenChangeDialog();
        _skinRanking.MouseClick += (o, e) => ToggleLoginRanking();
        _skinOptions.MouseClick += (o, e) => ToggleLoginConfig();
        dialog.AddControl(_skinLogin); dialog.AddControl(_skinRegister); dialog.AddControl(_skinChange); dialog.AddControl(_skinRanking); dialog.AddControl(_skinOptions);
        _skinExit = new DXButton { Text = "退出", FontSize = 10, TextColour = new Color(1f, .88f, .55f), LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(660, 60), Size = new Vector2I(100, defaultButtonHeight) };
        _skinExit.MouseClick += (o, e) => GetTree().Quit();
        dialog.AddControl(_skinExit);
        _skinForgot = new DXLabel { Text = "忘记密码", FontSize = 9, TextColour = new Color(1f, .75f, .25f), Location = new Vector2I(640, 38), Size = new Vector2I(100, 16), IsControl = true };
        _skinForgot.MouseEnter += (o, e) => _skinForgot.TextColour = Colors.White;
        _skinForgot.MouseLeave += (o, e) => _skinForgot.TextColour = new Color(1f, .75f, .25f);
        _skinForgot.MouseClick += (o, e) => OpenRequestResetDialog();
        dialog.AddControl(_skinForgot);
        _skinRemember = new DXCheckBox { Location = new Vector2I(490, 38), LabelBoxPadding = 4, Checked = ClientSettings.RememberDetails };
        _skinRemember.Label.Text = "记住账号";
        _skinRemember.Label.FontSize = 9;
        _skinRemember.Label.TextColour = new Color(1f, .75f, .25f);
        dialog.AddControl(_skinRemember);
        _skinActivation = new DXButton { Text = "激活账号", FontSize = 9, TextColour = new Color(1f, .75f, .25f), LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(20, 36), Size = new Vector2I(72, 20) };
        _skinActivation.MouseClick += (o, e) => { _activationDialog ??= CreateActivationDialog(); WindowManager.Open(_activationDialog, this); };
        dialog.AddControl(_skinActivation);
        _skinStatus = new DXLabel { Text = "正在连接服务端...", FontSize = 9, TextColour = new Color(1f, .85f, .45f), DrawOutline = true, Size = new Vector2I(500, 36), Location = new Vector2I(20, 100) };
        dialog.AddControl(_skinStatus);
        _loginRanking = new RankingDialog(false) { Position = new Vector2((viewport.X - 330) / 2f, (viewport.Y - 456) / 2f) };
        _loginConfig = new ConfigDialog { Position = new Vector2((viewport.X - 380) / 2f, (viewport.Y - 430) / 2f) };
        GetNode<Control>("VBox").Visible = false;
        // 原版 LoginBox 的位置基于 Interface[151] 实际尺寸，而不是固定高度。
        dialog.Position = new Vector2((viewport.X - dialog.Size.X) / 2f, viewport.Y - dialog.Size.Y - 20f);
    }

    private static void AddLoginAnimation(DXControl parent, int baseIndex, int frameCount, int seconds, bool loop, bool offset, bool blend)
    {
        parent.AddControl(new DXAnimatedControl
        {
            LibraryFile = LibraryFile.Interface1c,
            BaseIndex = baseIndex,
            FrameCount = frameCount,
            AnimationDelay = TimeSpan.FromSeconds(seconds),
            Animated = true,
            Loop = loop,
            UseOffSet = offset,
            Blend = blend,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
    }
}
