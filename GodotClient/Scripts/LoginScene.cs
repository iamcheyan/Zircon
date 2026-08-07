using System;
using System.Collections.Generic;
using Godot;
using Library;

namespace ZirconClient.Scripts;

public partial class LoginScene : Control
{
    private Network.NetworkManager _net;
    private LineEdit _emailEdit;
    private LineEdit _passwordEdit;
    private Button _loginBtn;
    private Button _registerBtn;
    private Label _statusLabel;
    private List<SelectInfo> _pendingCharacters;

    public override void _Ready()
    {
        _net = GetNode<Network.NetworkManager>("/root/NetworkManager");

        _emailEdit = GetNode<LineEdit>("VBox/EmailRow/EmailEdit");
        _passwordEdit = GetNode<LineEdit>("VBox/PasswordRow/PasswordEdit");
        _loginBtn = GetNode<Button>("VBox/LoginBtn");
        _registerBtn = GetNode<Button>("VBox/RegisterBtn");
        _statusLabel = GetNode<Label>("VBox/StatusLabel");

        _loginBtn.Pressed += OnLoginPressed;
        _registerBtn.Pressed += OnRegisterPressed;

        // 连接服务端
        _net.Log += OnNetLog;
        // 自动登录: --auto-login 固定测试账号, 或 --user/--pass 指定账号
        bool autoLogin = AutoLoginArgs.AutoLogin;
        if (!_net.Connect("127.0.0.1", 7000))
        {
            _statusLabel.Text = "无法连接服务端";
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
        _net.Connection.NewAccountResultEvent += OnNewAccountResult;
        _net.Connection.DisconnectedEvent += OnDisconnected;
    }

    private void OnNetLog(string msg) => GD.Print(msg);

    private void OnConnected()
    {
        GD.Print("[Login] 服务端确认连接");
    }

    private void ShowVersionOK(string version)
    {
        _statusLabel.Text = $"已连接服务端 (版本: {version})\n请登录或注册";
        _loginBtn.Disabled = false;
        _registerBtn.Disabled = false;
    }

    private void OnLoginResult(LoginResult result, string message, List<SelectInfo> characters)
    {
        _pendingCharacters = characters ?? new List<SelectInfo>();
        _pendingLoginResult = result;
        _pendingLoginMessage = message;
        CallDeferred(nameof(ShowLoginResult));
    }
    private LoginResult _pendingLoginResult;
    private string _pendingLoginMessage;
    private void ShowLoginResult()
    {
        if (_pendingLoginResult == LoginResult.Success)
        {
            _statusLabel.Text = $"登录成功! 角色数: {_pendingCharacters.Count}";
            GD.Print($"[Login] 登录成功, 角色数 {_pendingCharacters.Count}");
            var selectScene = ResourceLoader.Load<PackedScene>("res://Scenes/SelectScene.tscn");
            var selectScript = selectScene.Instantiate<SelectScene>();
            selectScript.SetCharacters(_pendingCharacters);
            GetTree().Root.AddChild(selectScript);
            QueueFree();
        }
        else
        {
            _statusLabel.Text = $"登录失败: {_pendingLoginResult}\n{_pendingLoginMessage}";
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
            _statusLabel.Text = $"注册成功 ({result}), 请登录";
        else
            _statusLabel.Text = $"注册失败: {result}";
        _registerBtn.Disabled = false;
    }

    private void OnDisconnected()
    {
        CallDeferred(nameof(ShowDisconnected));
    }
    private void ShowDisconnected()
    {
        _statusLabel.Text = "连接已断开";
        _loginBtn.Disabled = true;
        _registerBtn.Disabled = true;
    }

    private void OnLoginPressed()
    {
        _loginBtn.Disabled = true;
        _statusLabel.Text = "登录中...";
        _net.Connection?.SendLogin(_emailEdit.Text, _passwordEdit.Text);
    }

    private void OnRegisterPressed()
    {
        _registerBtn.Disabled = true;
        _statusLabel.Text = "注册中...";
        _net.Connection?.SendNewAccount(_emailEdit.Text, _passwordEdit.Text);
    }
}
