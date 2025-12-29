using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.DataTypes;

public class FullScreenPageNavigationMessage(Page page, bool disappear) : ValueChangedMessage<Page>(page)
{
    public bool Disappear { get; } = disappear;
}