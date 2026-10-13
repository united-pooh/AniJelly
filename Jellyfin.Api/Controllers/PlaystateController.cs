using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Playstate controller.
/// </summary>
[Route("")]
[Authorize]
[Tags("Session")]
public class PlaystateController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataRepository;
    private readonly ILibraryManager _libraryManager;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<PlaystateController> _logger;
    private readonly ITranscodeManager _transcodeManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaystateController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="userDataRepository">Instance of the <see cref="IUserDataManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    public PlaystateController(
        IUserManager userManager,
        IUserDataManager userDataRepository,
        ILibraryManager libraryManager,
        ISessionManager sessionManager,
        ILoggerFactory loggerFactory,
        ITranscodeManager transcodeManager)
    {
        _userManager = userManager;
        _userDataRepository = userDataRepository;
        _libraryManager = libraryManager;
        _sessionManager = sessionManager;
        _logger = loggerFactory.CreateLogger<PlaystateController>();

        _transcodeManager = transcodeManager;
    }

    /// <summary>
    /// Marks an item as played for user.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="itemId">Item id.</param>
    /// <param name="datePlayed">Optional. The date the item was played.</param>
    /// <response code="200">Item marked as played.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>, or a <see cref="NotFoundResult"/> if item was not found.</returns>
    [HttpPost("UserPlayedItems/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("UserData")]
    public async Task<ActionResult<UserItemDataDto?>> MarkPlayedItem(
        [FromQuery] Guid? userId,
        [FromRoute, Required] Guid itemId,
        [FromQuery, ModelBinder(typeof(LegacyDateTimeModelBinder))] DateTime? datePlayed)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value);
        if (user is null)
        {
            return NotFound();
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext, userId).ConfigureAwait(false);

        var dto = UpdatePlayedStatus(user, item, true, datePlayed);
        foreach (var additionalUserInfo in session.AdditionalUsers)
        {
            var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);
            if (additionalUser is null)
            {
                return NotFound();
            }

            UpdatePlayedStatus(additionalUser, item, true, datePlayed);
        }

        return dto;
    }

    /// <summary>
    /// Marks an item as unplayed for user.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="itemId">Item id.</param>
    /// <response code="200">Item marked as unplayed.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>, or a <see cref="NotFoundResult"/> if item was not found.</returns>
    [HttpDelete("UserPlayedItems/{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("UserData")]
    public async Task<ActionResult<UserItemDataDto?>> MarkUnplayedItem(
        [FromQuery] Guid? userId,
        [FromRoute, Required] Guid itemId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value);
        if (user is null)
        {
            return NotFound();
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return NotFound();
        }

        var session = await RequestHelpers.GetSession(_sessionManager, _userManager, HttpContext, userId).ConfigureAwait(false);

        var dto = UpdatePlayedStatus(user, item, false, null);
        foreach (var additionalUserInfo in session.AdditionalUsers)
        {
            var additionalUser = _userManager.GetUserById(additionalUserInfo.UserId);
            if (additionalUser is null)
            {
                return NotFound();
            }

            UpdatePlayedStatus(additionalUser, item, false, null);
        }

        return dto;
    }

    /// <summary>
    /// Reports playback has started within a session.
    /// </summary>
    /// <param name="playbackStartInfo">The playback start info.</param>
    /// <response code="204">Playback start recorded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Playing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ReportPlaybackStart([FromBody] PlaybackStartInfo playbackStartInfo)
    {
        playbackStartInfo.PlayMethod = ValidatePlayMethod(playbackStartInfo.PlayMethod, playbackStartInfo.PlaySessionId);
        playbackStartInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        await _sessionManager.OnPlaybackStart(playbackStartInfo).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Reports playback progress within a session.
    /// </summary>
    /// <param name="playbackProgressInfo">The playback progress info.</param>
    /// <response code="204">Playback progress recorded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Playing/Progress")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ReportPlaybackProgress([FromBody] PlaybackProgressInfo playbackProgressInfo)
    {
        playbackProgressInfo.PlayMethod = ValidatePlayMethod(playbackProgressInfo.PlayMethod, playbackProgressInfo.PlaySessionId);
        playbackProgressInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        await _sessionManager.OnPlaybackProgress(playbackProgressInfo).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Pings a playback session.
    /// </summary>
    /// <param name="playSessionId">Playback session id.</param>
    /// <response code="204">Playback session pinged.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Playing/Ping")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult PingPlaybackSession([FromQuery, Required] string playSessionId)
    {
        _transcodeManager.PingTranscodingJob(playSessionId, null);
        return NoContent();
    }

    /// <summary>
    /// Reports playback has stopped within a session.
    /// </summary>
    /// <param name="playbackStopInfo">The playback stop info.</param>
    /// <response code="204">Playback stop recorded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Sessions/Playing/Stopped")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> ReportPlaybackStopped([FromBody] PlaybackStopInfo playbackStopInfo)
    {
        _logger.LogDebug("ReportPlaybackStopped PlaySessionId: {0}", playbackStopInfo.PlaySessionId ?? string.Empty);
        if (!string.IsNullOrWhiteSpace(playbackStopInfo.PlaySessionId))
        {
            await _transcodeManager.KillTranscodingJobs(User.GetDeviceId()!, playbackStopInfo.PlaySessionId, s => true).ConfigureAwait(false);
        }

        playbackStopInfo.SessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, HttpContext).ConfigureAwait(false);
        await _sessionManager.OnPlaybackStopped(playbackStopInfo).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Updates the played status.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="item">The item.</param>
    /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
    /// <param name="datePlayed">The date played.</param>
    /// <returns>Task.</returns>
    private UserItemDataDto? UpdatePlayedStatus(User user, BaseItem item, bool wasPlayed, DateTime? datePlayed)
    {
        if (wasPlayed)
        {
            item.MarkPlayed(user, datePlayed, true);
        }
        else
        {
            item.MarkUnplayed(user);
        }

        return _userDataRepository.GetUserDataDto(item, user);
    }

    private PlayMethod ValidatePlayMethod(PlayMethod method, string? playSessionId)
    {
        if (method == PlayMethod.Transcode)
        {
            var job = string.IsNullOrWhiteSpace(playSessionId) ? null : _transcodeManager.GetTranscodingJob(playSessionId);
            if (job is null)
            {
                return PlayMethod.DirectPlay;
            }
        }

        return method;
    }
}
