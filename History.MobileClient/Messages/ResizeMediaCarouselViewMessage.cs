using CommunityToolkit.Mvvm.Messaging.Messages;
using History.MobileClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Messages;

public class ResizeMediaCarouselViewMessage(IMediaViewModel value) : ValueChangedMessage<IMediaViewModel>(value);
