using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Library;
using Library.SystemModels;
using ZirconClient.Formats;
using ZirconClient.Scripts;

namespace ZirconClient.Controls;

/// <summary>原版 NPCCompanionStorageDialog（Interface 147）。</summary>
public sealed partial class NPCCompanionStorageDialog : DXWindow
{
    private readonly DXLabel _name, _level, _experience, _hunger, _index;
    private readonly DXControl _experienceBar, _hungerBar;
    private readonly List<ClientUserCompanion> _companions = new();
    private readonly DXButton _left, _right, _store, _retrieve, _release;
    private int _selected = -1;
    private ObjectRenderer _preview;

    public NPCCompanionStorageDialog()
    {
        HasTitle = false; HasFooter = false; Movable = true;
        var background = new DXImageControl { LibraryFile = LibraryFile.Interface, Index = 147, MouseFilter = MouseFilterEnum.Ignore };
        AddControl(background);
        Size = (Vector2I)background.Size;
        if (Size.X < 280 || Size.Y < 230) Size = new Vector2I(300, 230);
        var close = new DXButton { LibraryFile = LibraryFile.Interface, Index = 15, Location = new Vector2I((int)Size.X - 30, 3) };
        close.MouseClick += (s, e) => WindowManager.Close(this); AddControl(close);
        AddControl(new DXLabel { Text = "伙伴仓库", FontSize = 11, TextColour = new Color(1f, .85f, .3f), DrawOutline = true,
            Align = HorizontalAlignment.Center, Size = new Vector2I((int)Size.X, 25), IsControl = false });

        AddControl(new DXLabel { Text = "名称", FontSize = 9, Location = new Vector2I(50, 52), IsControl = false });
        AddControl(new DXLabel { Text = "等级", FontSize = 9, Location = new Vector2I(50, 74), IsControl = false });
        AddControl(new DXLabel { Text = "经验", FontSize = 9, Location = new Vector2I(50, 96), IsControl = false });
        AddControl(new DXLabel { Text = "饱食度", FontSize = 9, Location = new Vector2I(50, 118), IsControl = false });
        _name = ValueLabel(52); _level = ValueLabel(74); _experience = ValueLabel(96); _hunger = ValueLabel(118);
        _experienceBar = AddBar(196, 98, 4310);
        _hungerBar = AddBar(196, 120, 4311);
        _index = new DXLabel { FontSize = 9, Align = HorizontalAlignment.Center, Size = new Vector2I(100, 20), Location = new Vector2I(135, 156), IsControl = false };
        AddControl(_index);

        _left = new DXButton { LibraryFile = LibraryFile.GameInter, Index = 4211, Location = new Vector2I(105, 157) };
        _right = new DXButton { LibraryFile = LibraryFile.GameInter, Index = 4216, Location = new Vector2I(245, 157) };
        _left.MouseClick += (s, e) => Select(_selected - 1);
        _right.MouseClick += (s, e) => Select(_selected + 1);
        AddControl(_left); AddControl(_right);

        _store = Button("收起", 30, (int)Size.Y - 43);
        _retrieve = Button("召回", 30, (int)Size.Y - 43);
        _release = Button("释放", 145, (int)Size.Y - 43);
        _store.MouseClick += (s, e) => GameScene.Game?.SendCompanionStore();
        _retrieve.MouseClick += (s, e) => GameScene.Game?.SendCompanionRetrieve(SelectedCompanionIndex);
        _release.MouseClick += (s, e) =>
        {
            int index = SelectedCompanionIndex;
            if (index < 0) return;
            var confirm = new ConfirmDialog("确定要释放当前伙伴吗？此操作不可撤销。", "确认释放伙伴", () => GameScene.Game?.SendCompanionRelease(index));
            WindowManager.Open(confirm, GameScene.Game?.UILayer ?? GetParent());
        };
    }

    private DXLabel ValueLabel(int y)
    {
        var label = new DXLabel { FontSize = 9, TextColour = Colors.White, Align = HorizontalAlignment.Center, Location = new Vector2I(190, y), Size = new Vector2I(160, 20), IsControl = false };
        AddControl(label);
        return label;
    }

    private DXControl AddBar(int x, int y, int index)
    {
        var size = MirSkin.GetSize(LibraryFile.GameInter, index);
        var bar = new DXControl { Location = new Vector2I(x, y), Size = size, Clip = true, IsControl = false };
        bar.AddControl(new DXImageControl { LibraryFile = LibraryFile.GameInter, Index = index, FixedSize = true, Size = size, IsControl = false });
        AddControl(bar);
        return bar;
    }

    private DXButton Button(string text, int x, int y)
    {
        var button = new DXButton { Text = text, FontSize = 9, LibraryFile = LibraryFile.Interface, Index = -1, Location = new Vector2I(x, y), Size = new Vector2I(80, 24) };
        AddControl(button);
        return button;
    }

    public void SetCompanions(IEnumerable<ClientUserCompanion> companions)
    {
        _companions.Clear();
        if (companions != null) _companions.AddRange(companions);
        int active = GameScene.Game?.Companion == null ? -1 : _companions.FindIndex(x => x?.Index == GameScene.Game.Companion.Index);
        Select(active >= 0 ? active : (_companions.Count > 0 ? 0 : -1));
    }

    public void AddCompanion(ClientUserCompanion companion)
    {
        if (companion == null) return;
        if (!_companions.Exists(x => x?.Index == companion.Index)) _companions.Add(companion);
        Select(_companions.FindIndex(x => x?.Index == companion.Index));
    }

    public void RemoveCompanion(int index)
    {
        _companions.RemoveAll(x => x?.Index == index);
        Select(Math.Min(Math.Max(0, _selected), _companions.Count - 1));
    }

    public void Refresh()
    {
        int active = GameScene.Game?.Companion == null ? -1 : _companions.FindIndex(x => x?.Index == GameScene.Game.Companion.Index);
        Select(active >= 0 ? active : Math.Clamp(_selected, 0, Math.Max(0, _companions.Count - 1)));
    }

    public int SelectedCompanionIndex => _selected >= 0 && _selected < _companions.Count
        ? _companions[_selected]?.Index ?? -1
        : -1;

    private void Select(int index)
    {
        if (_preview != null)
        {
            RemoveChild(_preview);
            _preview.QueueFree();
            _preview = null;
        }
        if (_companions.Count == 0 || index < 0 || index >= _companions.Count)
        {
            _selected = -1; _name.Text = "暂无伙伴"; _level.Text = string.Empty; _experience.Text = string.Empty; _hunger.Text = string.Empty; _index.Text = string.Empty; SetBar(_experienceBar, 0); SetBar(_hungerBar, 0);
            _left.Enabled = _right.Enabled = _store.Enabled = _retrieve.Enabled = _release.Enabled = false;
            _store.Visible = false; _retrieve.Visible = true;
            return;
        }
        _selected = index;
        var companion = _companions[index];
        var companionInfo = companion?.CompanionInfo;
        if (companionInfo?.MonsterInfo != null && MonsterLookup.Map.TryGetValue(companionInfo.MonsterInfo.Image, out var lookup))
        {
            _preview = new ObjectRenderer
            {
                Type = ObjectRenderer.Kind.Monster,
                MonsterImage = companionInfo.MonsterInfo.Image,
                MonsterInfo = companionInfo.MonsterInfo,
                BodyLibrary = LibraryCache.Get(lookup.File),
                BodyShape = lookup.Shape,
                BodyOffSet = 1000,
                DisplayName = string.Empty,
                NameColour = Colors.Transparent,
                DrawColour = Colors.White,
                Stats = companionInfo.MonsterInfo.Stats,
                Level = companionInfo.MonsterInfo.Level,
                Position = new Vector2(55, 90),
            };
            _preview.SetAnimation(MirAnimation.Standing);
            AddChild(_preview);
        }
        _name.Text = companion?.Name ?? companion?.CompanionInfo?.MonsterInfo?.MonsterName ?? "伙伴";
        _level.Text = $"Lv. {companion?.Level ?? 0}";
        var levelInfo = Globals.CompanionLevelInfoList?.Binding?.FirstOrDefault(x => x.Level == companion?.Level);
        int maxExperience = levelInfo?.MaxExperience ?? 1;
        int maxHunger = levelInfo?.MaxHunger ?? 100;
        _experience.Text = maxExperience > 0 ? $"{(companion?.Experience ?? 0) / (decimal)maxExperience:p2}" : "100%";
        _hunger.Text = $"{companion?.Hunger ?? 0} / {maxHunger}";
        _index.Text = $"{index + 1} / {_companions.Count}";
        SetBar(_experienceBar, Mathf.Clamp((companion?.Experience ?? 0) / (float)Math.Max(1, maxExperience), 0, 1));
        SetBar(_hungerBar, Mathf.Clamp((companion?.Hunger ?? 0) / (float)Math.Max(1, maxHunger), 0, 1));
        bool activeCompanion = GameScene.Game?.Companion?.Index == companion?.Index;
        bool assignedElsewhere = !string.IsNullOrWhiteSpace(companion?.CharacterName);
        _left.Enabled = _selected > 0;
        _right.Enabled = _selected + 1 < _companions.Count;
        _store.Visible = activeCompanion;
        _retrieve.Visible = !activeCompanion;
        _retrieve.Enabled = !assignedElsewhere;
        _release.Enabled = !assignedElsewhere;
    }

    private static void SetBar(DXControl bar, float percent)
    {
        bar.Size = new Vector2(Mathf.Max(1, Mathf.RoundToInt(70 * percent)), bar.Size.Y);
    }
}
