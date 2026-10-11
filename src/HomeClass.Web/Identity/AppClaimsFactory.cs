using System.Security.Claims;
using HomeClass.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HomeClass.Web.Identity;

/// <summary>Agrega el nombre completo a la sesión para mostrarlo en la barra sin consultar la base en cada página.</summary>
public class AppClaimsFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    public AppClaimsFactory(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("nombre", user.NombreCompleto));
        return identity;
    }
}
