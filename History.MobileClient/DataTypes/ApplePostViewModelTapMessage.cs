using CommunityToolkit.Mvvm.Messaging.Messages;
using History.MobileClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.DataTypes;

public class ApplePostViewModelTapMessage(PostViewModel viewModel) : ValueChangedMessage<PostViewModel>(viewModel);
