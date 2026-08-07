using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.MobileClient.DataTypes;
using History.MobileClient.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels
{
    public partial class SelectUserViewModel(UserResponseDto user, bool isSelected) : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Nickname))]
        [NotifyPropertyChangedFor(nameof(ProfileMedia))]
        public partial UserResponseDto User { get; set; } = user;

        public bool IsSelected
        {
            get => field;
            set
            {
                if (field == value) return;

                field = value;
                OnPropertyChanged();

                WeakReferenceMessenger.Default.Send(new SelectUserSelectionMessage(this));
            }
        } = isSelected;

        public string Nickname => User.Nickname;

        public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia
        ? new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName) { IsAnimated = true }
        : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

        public bool IsModerator => User.Rank == Rank.Moderator;
        public bool IsAdmin => User.Rank == Rank.Admin;

        [RelayCommand]
        public void HandleTap() => IsSelected = !IsSelected;
    }
}
