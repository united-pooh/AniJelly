using System;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StartupDtos;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The startup wizard controller.
/// </summary>
[Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
public class StartupController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _config;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupController" /> class.
    /// </summary>
    /// <param name="config">The server configuration manager.</param>
    /// <param name="userManager">The user manager.</param>
    public StartupController(IServerConfigurationManager config, IUserManager userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    /// <summary>
    /// Completes the startup wizard.
    /// </summary>
    /// <response code="204">Startup wizard completed.</response>
    /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
    [HttpPost("Complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult CompleteWizard()
    {
        _config.Configuration.IsStartupWizardCompleted = true;
        _config.SaveConfiguration();
        return NoContent();
    }

    /// <summary>
    /// Sets the user name and password.
    /// </summary>
    /// <param name="startupUserDto">The DTO containing username and password.</param>
    /// <response code="204">Updated user name and password.</response>
    /// <returns>
    /// A <see cref="Task" /> that represents the asynchronous update operation.
    /// The task result contains a <see cref="NoContentResult"/> indicating success.
    /// </returns>
    [HttpPost("User")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UpdateStartupUser([FromBody] StartupUserDto startupUserDto)
    {
        await _userManager.InitializeAsync().ConfigureAwait(false);
        var user = _userManager.GetFirstUser();
        if (user is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(user.Password))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(startupUserDto.Password))
        {
            return BadRequest("Password must not be empty");
        }

        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

#pragma warning disable CA1309 // Use ordinal string comparison
        if (startupUserDto.Name is not null && !startupUserDto.Name.Equals(user.Username, StringComparison.InvariantCultureIgnoreCase))
        {
            await _userManager.RenameUser(user.Id, user.Username, startupUserDto.Name).ConfigureAwait(false);
        }
#pragma warning restore CA1309 // Use ordinal string comparison

        await _userManager.ChangePassword(user.Id, startupUserDto.Password).ConfigureAwait(false);

        return NoContent();
    }
}
