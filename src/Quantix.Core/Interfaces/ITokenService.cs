using Quantix.Core.DTOs;
using Quantix.Core.Entities;

namespace Quantix.Core.Interfaces;

public interface ITokenService
{
    TokenDto CreateToken(AppUser user, IList<string> roles);
}
