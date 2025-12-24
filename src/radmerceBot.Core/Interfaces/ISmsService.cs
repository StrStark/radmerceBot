using System;
using System.Collections.Generic;
using System.Text;

namespace radmerceBot.Core.Interfaces;

public interface ISmsService
{
    Task SendOtp(string phone, string code, CancellationToken cancellationToken);
}
