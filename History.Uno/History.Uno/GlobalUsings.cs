global using System.Collections.Immutable;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using History.Uno.Models;
#if MAUI_EMBEDDING
global using History.Uno.MauiControls;
#endif
global using ApplicationExecutionState = Windows.ApplicationModel.Activation.ApplicationExecutionState;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using CommunityToolkit.Mvvm.Messaging;
global using CommunityToolkit.Mvvm.Messaging.Messages;
global using History.Commons;
global using History.Commons.Enums;
global using History.Commons.DataTypes.ResponseDtos;
global using History.Commons.DataTypes.Contents;
global using History.Commons.Interfaces;
global using History.Uno.Enums;
global using History.Uno.DataTypes;