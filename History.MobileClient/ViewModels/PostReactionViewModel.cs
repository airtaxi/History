using CommunityToolkit.Mvvm.ComponentModel;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UraniumUI.Icons.MaterialSymbols;

namespace History.MobileClient.ViewModels;

public partial class PostReactionViewModel(PostReactionDto reaction) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(User))]
    [NotifyPropertyChangedFor(nameof(Type))]
    [NotifyPropertyChangedFor(nameof(CreatedAt))]
    [NotifyPropertyChangedFor(nameof(TypeText))]
    public partial PostReactionDto Reaction { get; set; } = reaction;

    public UserResponseDto User => Reaction.User;
    public PostReactionType Type => Reaction.Type;
    public DateTime CreatedAt => Reaction.CreatedAt;

    public string Glyph
    {
        get
        {
            return Reaction.Type switch
            {
                PostReactionType.Like => MaterialSharp.Favorite,
                PostReactionType.Awesome => MaterialSharp.Star,
                PostReactionType.Happy => MaterialSharp.Sentiment_satisfied,
                PostReactionType.Sad => MaterialSharp.Water_drop,
                PostReactionType.Support => MaterialSharp.Bolt,
                _ => throw new ArgumentOutOfRangeException(nameof(Reaction.Type), Reaction.Type, null)
            };
        }
    }

    public Color Color
    {
        get
        {
            return Reaction.Type switch
            {
                PostReactionType.Like => Color.FromRgb(0xeb, 0x55, 0x27),
                PostReactionType.Awesome => Color.FromRgb(0xbb, 0xcc, 0x29),
                PostReactionType.Happy => Color.FromRgb(0xbb, 0xcc, 0x29),
                PostReactionType.Sad => Color.FromRgb(0xf5, 0xbe, 0x06),
                PostReactionType.Support => Color.FromRgb(0xa0, 0x61, 0xb1),
                _ => throw new ArgumentOutOfRangeException(nameof(Reaction.Type), Reaction.Type, null)
            };
        }
    }

    public string TypeText => Reaction.Type.ToDisplayString();
}
