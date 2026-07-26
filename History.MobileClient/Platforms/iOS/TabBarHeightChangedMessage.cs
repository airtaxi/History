using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace History.MobileClient.DataTypes;

public class TabBarHeightChangedMessage(double tabBarHeight) : ValueChangedMessage<double>(tabBarHeight);
