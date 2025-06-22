using History.Commons.DataTypes.ResponseDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.DataTypes;

public class UserUnselectedMessage(UserResponseDto user) : ValueDeletedMessage<UserResponseDto>(user);
