using History.Commons.DataTypes.ResponseDtos;
using History.MobileClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Messages;

public class SelectUserSelectionMessage(SelectUserViewModel user) : ValueDeletedMessage<SelectUserViewModel>(user);
