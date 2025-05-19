using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.ViewModels
{
    public partial class SelectUserViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Nickname))]
        [NotifyPropertyChangedFor(nameof(ProfileMedia))]
        public partial UserResponseDto User { get; set; }

        public bool IsSelected
        {
            get => field;
            set
            {
                if (field == value) return;

                if (value) WeakReferenceMessenger.Default.Send(new UserSelectedMessage(User));
                else WeakReferenceMessenger.Default.Send(new UserUnselectedMessage(User));

                OnPropertyChanging(nameof(IsSelected));
                field = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public string Nickname => User.Nickname;

        public IMediaViewModel ProfileMedia => User.UsesAnimatedProfileMedia
        ? new VideoViewModel(Utils.GenerateMediaUri(User.ProfileMediaId))
        : new ImageViewModel(Utils.GenerateMediaUri(User.ProfileMediaId) ?? Constants.DefaultProfileImageFileName);

        public SelectUserViewModel(UserResponseDto user)
        {
            User = user;

            WeakReferenceMessenger.Default.Register<UserSelectedMessage>(this, OnUserSelectedMessageReceived);

            WeakReferenceMessenger.Default.Register<UserUnselectedMessage>(this,  OnUserUnselectedMessageReceived);
        }

        public void OnUserSelectedMessageReceived(object recipient, UserSelectedMessage message)
        {
            if (message.Value.UserId != User.UserId) return;

            IsSelected = true;
        }

        public void OnUserUnselectedMessageReceived(object recipient, UserUnselectedMessage message)
        {
            if (message.Value.UserId != User.UserId) return;

            IsSelected = false;
        }

        [RelayCommand]
        public void HandleTap() => IsSelected = !IsSelected;
    }
}
