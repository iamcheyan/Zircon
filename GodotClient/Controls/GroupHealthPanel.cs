using System.Collections.Generic;
using Godot;

namespace ZirconClient.Controls;

/// <summary>原版 GroupHealthDialog 的常驻透明成员血条层。</summary>
public sealed partial class GroupHealthPanel : DXControl
{
    private readonly Dictionary<uint, GroupHealthRow> _rows = new();

    public GroupHealthPanel()
    {
        Size = new Vector2I(150, 500);
        IsControl = false;
        PassThrough = true;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void AddMember(uint objectId, string name)
    {
        if (!_rows.TryGetValue(objectId, out var row))
        {
            row = new GroupHealthRow();
            _rows[objectId] = row;
            AddControl(row);
        }
        row.NameText = string.IsNullOrWhiteSpace(name) ? objectId.ToString() : name;
        LayoutRows();
    }

    public void RemoveMember(uint objectId)
    {
        if (!_rows.Remove(objectId, out var row)) return;
        RemoveControl(row);
        row.QueueFree();
        LayoutRows();
    }

    public void UpdateMember(uint objectId, int health, int maxHealth)
    {
        if (!_rows.TryGetValue(objectId, out var row)) return;
        row.Health = health;
        row.MaxHealth = maxHealth;
        row.HealthPercent = maxHealth > 0 ? Mathf.Clamp(health / (float)maxHealth, 0f, 1f) : 1f;
        row.QueueRedraw();
    }

    public void ClearMembers()
    {
        foreach (var row in _rows.Values)
        {
            RemoveControl(row);
            row.QueueFree();
        }
        _rows.Clear();
    }

    private void LayoutRows()
    {
        int i = 0;
        foreach (var row in _rows.Values)
            row.Location = new Vector2I(0, i++ * 40);
    }
}

public sealed partial class GroupHealthRow : DXControl
{
    public string NameText { get; set; } = "";
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public float HealthPercent { get; set; } = 1f;

    public GroupHealthRow()
    {
        Size = new Vector2I(150, 38);
        IsControl = false;
    }

    protected override void DrawControl()
    {
        string healthText = MaxHealth > 0 ? $"{Health}/{MaxHealth}" : string.Empty;
        DrawString(MirSkin.GetFont(), new Vector2(15, 13), NameText, HorizontalAlignment.Left, 82, 10, Colors.White);
        DrawString(MirSkin.GetFont(), new Vector2(98, 13), healthText, HorizontalAlignment.Right, 37, 9, Colors.White);
        var bar = new Rect2(15, 20, 120, 8);
        DrawRect(bar, new Color(.08f, .08f, .08f, .75f));
        DrawRect(new Rect2(bar.Position, new Vector2(bar.Size.X * Mathf.Clamp(HealthPercent, 0, 1), bar.Size.Y)), new Color(.18f, .78f, .22f, .9f));
        DrawRect(bar, new Color(.55f, .42f, .17f), false, 1);
    }
}
