using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.RequestDtos;

public class UpdateMemoRequestDto
{
    [Required]
    [MaxLength(CommonConstants.MaxMemoLength)]
    public string Memo { get; set; }
}
