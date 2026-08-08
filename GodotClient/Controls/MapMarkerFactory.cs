using Godot;
using Library;
using Library.SystemModels;

namespace ZirconClient.Controls;

/// <summary>原版 GameScene.GetNPCControl 的地图标记部分。</summary>
public static class MapMarkerFactory
{
    public static DXControl CreateNpcMarker(NPCInfo npc)
    {
        int icon = 0;
        Color colour = Colors.White;
        string iconText = string.Empty;

        if (npc?.CurrentQuest != null)
        {
            icon = npc.CurrentQuest.Type switch
            {
                QuestType.General or QuestType.Repeatable => 16,
                QuestType.Daily or QuestType.Weekly => 76,
                QuestType.Story => 56,
                QuestType.Account => 36,
                _ => 0,
            };
            colour = npc.CurrentQuest.Type switch
            {
                QuestType.General or QuestType.Repeatable => Colors.Yellow,
                QuestType.Daily or QuestType.Weekly => Colors.Blue,
                QuestType.Story => Colors.LimeGreen,
                QuestType.Account => new Color(0.58f, 0.44f, 0.86f),
                _ => Colors.White,
            };

            switch (npc.CurrentQuest.Icon)
            {
                case QuestIcon.Incomplete:
                    icon = 2;
                    colour = Colors.White;
                    iconText = "?";
                    break;
                case QuestIcon.New:
                    iconText = "!";
                    break;
                case QuestIcon.Complete:
                    icon += 2;
                    iconText = "?";
                    break;
            }
        }

        if (!string.IsNullOrEmpty(iconText))
        {
            return new DXLabel
            {
                Text = iconText,
                TextColour = colour,
                FontSize = 10,
                DrawOutline = true,
                OutlineColour = Colors.Black,
                Align = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Center,
                AutoSize = false,
                Size = new Vector2I(18, 20),
                Tag = npc.CurrentQuest,
            };
        }

        if (icon > 0)
        {
            return new DXImageControl
            {
                LibraryFile = LibraryFile.QuestIcon,
                Index = icon,
                ForeColour = colour,
                Tag = npc.CurrentQuest,
            };
        }

        if (npc?.MapIcon != MapIcon.None)
        {
            var image = new DXImageControl
            {
                LibraryFile = LibraryFile.MiniMapIcon,
            };
            MiniMapDialog.UpdateMapIcon(image, npc.MapIcon);
            return image;
        }

        return new DXMapInfoControl
        {
            BackColour = Colors.Yellow,
            Size = new Vector2I(3, 3),
        };
    }
}
