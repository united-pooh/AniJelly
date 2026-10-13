const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('jsdom');
const parts = path.join(__dirname, '..', 'parts');
const item = { Id: 'item-1', Name: '真实剧集', SeriesName: '真实番剧', IndexNumber: 2, RunTimeTicks: 1200000000, UserData: { PlaybackPositionTicks: 230000000 }, MediaStreams: [{ Type: 'Video', Codec: 'h264', Width: 1280, Height: 720, BitRate: 4000000, AverageFrameRate: 24 }] };
const source = { Id: 'item-1', Name: 'BD 1080p', Container: 'mp4', SupportsDirectPlay: true, RunTimeTicks: item.RunTimeTicks, DefaultAudioStreamIndex: 1, DefaultSubtitleStreamIndex: -1, MediaStreams: [{ Type: 'Audio', Index: 1, DisplayTitle: '日语 AAC' }, { Type: 'Subtitle', Index: 2, DisplayTitle: '简体中文 SRT', IsTextSubtitleStream: true }] };
const tick = () => new Promise(resolve => setTimeout(resolve, 0));
function harness(override) {
  const dom = new JSDOM(fs.readFileSync(path.join(parts, 'player.html'), 'utf8'), { url: 'http://localhost:18777/', runScripts: 'outside-only' });
  const { window } = dom;
  const requests = [];
  let sessionNumber = 0;
  window.matchMedia = () => ({ matches: false, addEventListener() {} });
  window.HTMLMediaElement.prototype.load = function () {};
  window.HTMLMediaElement.prototype.canPlayType = function () { return 'probably'; };
  window.HTMLMediaElement.prototype.pause = function () { this._paused = true; this.dispatchEvent(new window.Event('pause')); };
  window.HTMLMediaElement.prototype.play = function () { this._paused = false; this.dispatchEvent(new window.Event('playing')); return Promise.resolve(); };
  Object.defineProperty(window.HTMLMediaElement.prototype, 'paused', { get() { return this._paused !== false; } });
  Object.defineProperty(window.HTMLMediaElement.prototype, 'duration', { get() { return 120; } });
  window.NightApi = { user: { Id: 'user-1' }, deviceId: 'device-1', url: (route, query = {}) => '/api/' + route.replace(/^\//, '') + '?' + new URLSearchParams(query), request: async (route, options = {}) => {
    requests.push({ route, ...options });
    if (override) { const value = override(route, options); if (value !== undefined) return value; }
    if (route.includes('PlaybackInfo')) return { PlaySessionId: 'session-' + (++sessionNumber), MediaSources: [source, { ...source, Id: 'source-2', Name: '<img src=x onerror=alert(1)>' }] };
    if (route.startsWith('MediaSegments/')) return { Items: [{ Type: 'Intro', StartTicks: 100000000, EndTicks: 300000000 }] };
  } };
  window.eval(fs.readFileSync(path.join(parts, 'player.js'), 'utf8'));
  window.NightPlayer.init({ onBack() {}, onNext() {} });
  return { dom, window, requests, video: window.document.querySelector('video'), find: selector => window.document.querySelector(selector), async cleanup() { window.NightPlayer.dispose(); await tick(); dom.window.close(); } };
}
test('real video negotiates playback, resumes server position, and reports pause/stop once', async () => {
  const h = harness();
  try {
    assert.ok(h.video, 'player must decode media with a video element');
    await h.window.NightPlayer.show({ item });
    const negotiation = h.requests.find(x => x.route.includes('PlaybackInfo'));
    assert.equal(negotiation.body.UserId, 'user-1');
    assert.equal(negotiation.body.StartTimeTicks, 230000000);
    assert.ok(negotiation.body.DeviceProfile.DirectPlayProfiles.length);
    assert.match(h.video.src, /Videos\/item-1\/stream/);
    h.video.dispatchEvent(new h.window.Event('loadedmetadata'));
    await tick();
    assert.equal(h.video.currentTime, 23);
    assert.equal(h.requests.filter(x => x.route === 'Sessions/Playing').length, 1);
    h.video.currentTime = 38;
    h.video.pause();
    await tick();
    assert.equal(h.requests.findLast(x => x.route.endsWith('/Progress')).body.PositionTicks, 380000000);
    h.window.NightPlayer.dispose(); h.window.NightPlayer.dispose(); await tick();
    const stops = h.requests.filter(x => x.route.endsWith('/Stopped'));
    assert.equal(stops.length, 1);
    assert.equal(stops[0].body.PositionTicks, 380000000);
    assert.equal(stops[0].keepalive, true);
  } finally { await h.cleanup(); }
});
test('version switch negotiates selected server source at real playback position', async () => {
  const h = harness();
  try {
    await h.window.NightPlayer.show({ item });
    h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    h.video.currentTime = 41;
    const select = h.find('[data-player-source]');
    assert.equal(select.options.length, 2);
    assert.equal(select.querySelector('img'), null);
    select.value = 'source-2'; select.dispatchEvent(new h.window.Event('change')); await tick();
    const latest = h.requests.findLast(x => x.route.includes('PlaybackInfo'));
    assert.equal(latest.body.MediaSourceId, 'source-2');
    assert.equal(latest.body.StartTimeTicks, 410000000);
    assert.equal(h.requests.filter(x => x.route.endsWith('/Stopped')).length, 1);
  } finally { await h.cleanup(); }
});
test('disposed player ignores late playback negotiation', async () => {
  let resolve;
  const waiting = new Promise(done => { resolve = done; });
  const h = harness(route => route.includes('PlaybackInfo') ? waiting : undefined);
  try {
    const pending = h.window.NightPlayer.show({ item });
    h.window.NightPlayer.dispose();
    resolve({ PlaySessionId: 'late', MediaSources: [source] });
    await pending;
    assert.equal(h.find('#player-screen').hidden, true);
    assert.equal(h.video.getAttribute('src'), null);
    assert.equal(h.requests.filter(x => x.route === 'Sessions/Playing').length, 0);
  } finally { await h.cleanup(); }
});
test('server playback failure is visible and cannot advance a fake timer', async () => {
  const h = harness(route => route.includes('PlaybackInfo') ? { ErrorCode: 'NotAllowed', MediaSources: [] } : undefined);
  try {
    await h.window.NightPlayer.show({ item });
    assert.match(h.find('[data-player-status]').textContent, /播放|权限/);
    assert.equal(h.video.getAttribute('src'), null);
    assert.equal(h.find('#player-screen').dataset.playing, 'false');
  } finally { await h.cleanup(); }
});
test('subtitle selection requests a real track and intro skip uses server segments', async () => {
  const h = harness();
  try {
    await h.window.NightPlayer.show({ item });
    h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    const skip = h.find('[data-player-action="skip"]');
    assert.equal(skip.hidden, false);
    skip.click(); assert.equal(h.video.currentTime, 30);
    const subtitles = h.find('[data-player-subtitles]');
    assert.equal(subtitles.options.length, 2);
    subtitles.value = '2'; subtitles.dispatchEvent(new h.window.Event('change')); await tick();
    const latest = h.requests.findLast(x => x.route.includes('PlaybackInfo'));
    assert.equal(latest.body.MediaSourceId, 'item-1');
    assert.equal(latest.body.SubtitleStreamIndex, 2);
    assert.equal(latest.body.StartTimeTicks, 300000000);
    assert.match(h.video.querySelector('track').src, /Subtitles\/2\/Stream.vtt/);
  } finally { await h.cleanup(); }
});
test('HLS URL uses the browser HLS engine and decoder errors remain visible', async () => {
  const transcodeSource = { ...source, SupportsDirectPlay: false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8?PlaySessionId=hls-1' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'hls-1', MediaSources: [transcodeSource] } : undefined);
  try {
    await h.window.NightPlayer.show({ item });
    assert.match(h.video.src, /master.m3u8/);
    h.video.dispatchEvent(new h.window.Event('error'));
    assert.match(h.find('[data-player-status]').textContent, /播放失败/);
    assert.equal(h.find('#player-screen').dataset.playing, 'false');
  } finally { await h.cleanup(); }
});
test('filtered version negotiation keeps the other known versions selectable', async () => {
  const h = harness((route, options) => route.includes('PlaybackInfo') && options.body?.MediaSourceId ? { PlaySessionId: 'filtered', MediaSources: [{ ...source, Id: options.body.MediaSourceId }] } : undefined);
  try {
    await h.window.NightPlayer.show({ item });
    h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    const versions = h.find('[data-player-source]');
    versions.value = 'source-2'; versions.dispatchEvent(new h.window.Event('change')); await tick();
    assert.equal(versions.options.length, 2, 'server filters PlaybackInfo by MediaSourceId; keep known versions');
  } finally { await h.cleanup(); }
});
test('audio choice remains transcoded when subtitle choice changes', async () => {
  const multi = { ...source, MediaStreams: [...source.MediaStreams, { Type: 'Audio', Index: 3, DisplayTitle: '评论音轨' }] };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'multi', MediaSources: [multi] } : undefined);
  try {
    await h.window.NightPlayer.show({ item });
    h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    const audio = h.find('[data-player-audio]'); audio.value = '3'; audio.dispatchEvent(new h.window.Event('change')); await tick();
    const subtitle = h.find('[data-player-subtitles]'); subtitle.value = '2'; subtitle.dispatchEvent(new h.window.Event('change')); await tick();
    const last = h.requests.findLast(x => x.route.includes('PlaybackInfo'));
    assert.equal(last.body.AudioStreamIndex, 3);
    assert.equal(last.body.EnableDirectPlay, false, 'HTML5 cannot select alternate internal audio on the static direct stream');
  } finally { await h.cleanup(); }
});
test('Chromium HLS uses the bundled decoder and destroys it on stop', async () => {
  const transcodeSource = { ...source, SupportsDirectPlay: false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8?PlaySessionId=hls-js' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'hls-js', MediaSources: [transcodeSource] } : undefined);
  const engines = [];
  class Decoder {
    static Events = { ERROR: 'error' };
    static isSupported() { return true; }
    constructor(config) { this.config = config; engines.push(this); }
    on(event, callback) { this.error = callback; }
    loadSource(url) { this.url = url; }
    attachMedia(video) { this.video = video; }
    destroy() { this.destroyed = true; }
  }
  try {
    h.video.canPlayType = mime => mime.startsWith('application/') ? '' : 'probably';
    h.window.Hls = Decoder;
    await h.window.NightPlayer.show({ item });
    assert.equal(engines.length, 1); assert.equal(engines[0].config.startPosition, 23);
    assert.equal(engines[0].video, h.video); assert.match(engines[0].url, /master.m3u8/);
    const profile = h.requests.find(x => x.route.includes('PlaybackInfo')).body.DeviceProfile;
    assert.ok(profile.CodecProfiles.some(rule => rule.Type === 'VideoAudio' && rule.Conditions.some(condition => condition.Property === 'IsSecondaryAudio' && condition.Value === 'false')), 'static streams must not claim selectable embedded audio');
    engines[0].error('error', { fatal: true }); await tick();
    assert.equal(engines[0].destroyed, true);
    assert.equal(h.requests.findLast(x => x.route.endsWith('/Stopped')).body.Failed, true);
  } finally { await h.cleanup(); }
});
test('another cut cannot inherit the original item intro timestamps', async () => {
  const h = harness();
  try {
    await h.window.NightPlayer.show({ item });
    h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    assert.equal(h.find('[data-player-action="skip"]').hidden, false);
    const select = h.find('[data-player-source]');
    select.value = 'source-2'; select.dispatchEvent(new h.window.Event('change')); await tick();
    assert.equal(h.find('[data-player-action="skip"]').hidden, true, 'different cuts require their own segment metadata');
  } finally { await h.cleanup(); }
});
const hdrSource = { ...source, MediaStreams: [{ Type: 'Video', Codec: 'hevc', BitDepth: 10, Width: 3840, Height: 2160, BitRate: 20000000, AverageFrameRate: 24, VideoRange: 'HDR', VideoRangeType: 'HDR10' }, ...source.MediaStreams] };
function enableHevc(h, supported = true) {
  h.video.canPlayType = mime => mime.includes('hvc1') || mime.includes('hev1') ? 'probably' : mime.startsWith('application/') ? '' : 'probably';
  h.window.MediaSource = { isTypeSupported: mime => mime.includes('hvc1') };
  Object.defineProperty(h.window.navigator, 'mediaCapabilities', { value: { decodingInfo: async () => ({ supported, smooth: true }) }, configurable: true });
}
test('HEVC Main10 capability permits original MP4 and video-copy fMP4 candidates without disabling fallback', async () => {
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'hevc', MediaSources: [hdrSource] } : undefined);
  try {
    enableHevc(h);
    await h.window.NightPlayer.show({ item: { ...item, MediaSources: [hdrSource] } });
    const body = h.requests.find(x => x.route.includes('PlaybackInfo')).body;
    assert.ok(body.DeviceProfile.DirectPlayProfiles.some(profile => profile.VideoCodec.includes('hevc')));
    assert.ok(body.DeviceProfile.TranscodingProfiles.some(profile => profile.Container === 'mp4' && profile.VideoCodec.includes('hevc')));
    assert.ok(body.DeviceProfile.TranscodingProfiles.some(profile => profile.VideoCodec.includes('h264')));
    assert.equal(body.EnableTranscoding, true);
    assert.match(h.find('[data-player-method]').textContent, /原片直放/);
    assert.match(h.find('[data-player-method]').textContent, /HDR/);
    assert.match(h.find('[data-player-method]').textContent, /显示器|Windows/);
  } finally { await h.cleanup(); }
});
test('a negative decoder capability vetoes speculative HEVC support', async () => {
  const h = harness();
  try {
    enableHevc(h, false);
    await h.window.NightPlayer.show({ item: { ...item, MediaSources: [hdrSource] } });
    const profile = h.requests.find(x => x.route.includes('PlaybackInfo')).body.DeviceProfile;
    assert.ok(profile.DirectPlayProfiles.every(profile => !profile.VideoCodec.includes('hevc')));
    assert.ok(profile.TranscodingProfiles.every(profile => !profile.VideoCodec.includes('hevc')));
  } finally { await h.cleanup(); }
});
test('late asynchronous capability checks cannot start a disposed player', async () => {
  const h = harness(); let finish;
  try {
    enableHevc(h);
    Object.defineProperty(h.window.navigator, 'mediaCapabilities', { value: { decodingInfo: () => new Promise(resolve => { finish = resolve; }) }, configurable: true });
    const pending = h.window.NightPlayer.show({ item: { ...item, MediaSources: [hdrSource] } }); await tick();
    assert.equal(typeof finish, 'function');
    h.window.NightPlayer.dispose(); finish({ supported: true }); await pending;
    assert.equal(h.requests.filter(x => x.route.includes('PlaybackInfo')).length, 0);
    assert.equal(h.video.getAttribute('src'), null);
  } finally { await h.cleanup(); }
});
for (const [audioDirect, label] of [[true, '仅重封装'], [false, '仅音频转码']]) test(`HLS actual server copy flags distinguish ${label}`, async () => {
  const transcoded = { ...hdrSource, SupportsDirectPlay: false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'stream-copy', MediaSources: [transcoded] } : route === 'Sessions' ? [{ DeviceId: 'device-1', UserId: 'user-1', NowPlayingItem: { Id: item.Id }, PlayState: { MediaSourceId: source.Id }, TranscodingInfo: { IsVideoDirect: true, IsAudioDirect: audioDirect, VideoCodec: 'hevc' } }] : undefined);
  try {
    await h.window.NightPlayer.show({ item });
    assert.match(h.find('[data-player-method]').textContent, /确认/);
    h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick(); await tick();
    assert.match(h.find('[data-player-method]').textContent, new RegExp(label));
    h.video.pause(); await tick();
    assert.equal(h.requests.findLast(x => x.route.endsWith('/Progress')).body.PlayMethod, 'DirectStream');
  } finally { await h.cleanup(); }
});
test('unavailable session details keep HLS method unknown rather than claiming HDR pass-through', async () => {
  const transcoded = { ...hdrSource, SupportsDirectPlay: false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'unknown', MediaSources: [transcoded] } : route === 'Sessions' ? Promise.reject(Object.assign(new Error('forbidden'), { status: 403 })) : undefined);
  try {
    await h.window.NightPlayer.show({ item }); h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick(); await tick();
    assert.match(h.find('[data-player-method]').textContent, /未确认|无法确认/);
    assert.doesNotMatch(h.find('[data-player-method]').textContent, /仅重封装|原片直放|视频未转码/);
  } finally { await h.cleanup(); }
});
test('completed media offers a fresh playback session from zero', async () => {
  const h = harness();
  try {
    await h.window.NightPlayer.show({ item }); h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    h.video.currentTime = 120; h.video.dispatchEvent(new h.window.Event('ended')); await tick();
    const replay = h.find('.player-center');
    assert.equal(replay.hidden, false); assert.equal(replay.disabled, false);
    assert.match(replay.getAttribute('aria-label'), /重新/);
    replay.click(); await tick();
    const calls = h.requests.filter(x => x.route.includes('PlaybackInfo'));
    assert.equal(calls.length, 2); assert.equal(calls[1].body.StartTimeTicks, 0);
  } finally { await h.cleanup(); }
});
test('a real video transcode is never labelled HDR-preserving', async () => {
  const transcoded = { ...hdrSource, SupportsDirectPlay: false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'tone-map', MediaSources: [transcoded] } : route === 'Sessions' ? [{ DeviceId: 'device-1', UserId: 'user-1', NowPlayingItem: { Id: item.Id }, PlayState: { MediaSourceId: source.Id }, TranscodingInfo: { IsVideoDirect: false, IsAudioDirect: false, VideoCodec: 'h264' } }] : undefined);
  try {
    await h.window.NightPlayer.show({ item }); h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick(); await tick();
    assert.match(h.find('[data-player-method]').textContent, /视频转码/);
    assert.match(h.find('[data-player-method]').textContent, /HDR 不保留/);
    assert.doesNotMatch(h.find('[data-player-method]').textContent, /源视频未转码/);
  } finally { await h.cleanup(); }
});
test('HEVC decoder failure retries once with H264-compatible server transcoding', async () => {
  const h = harness((route, options) => {
    if (!route.includes('PlaybackInfo')) return;
    return { PlaySessionId: 'fallback', MediaSources: [{ ...hdrSource, SupportsDirectPlay: options.body.AllowVideoStreamCopy !== false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8' }] };
  });
  try {
    enableHevc(h); h.video.canPlayType = () => 'probably';
    await h.window.NightPlayer.show({ item: { ...item, MediaSources: [hdrSource] } });
    Object.defineProperty(h.video, 'error', { value: { code: 3 }, configurable: true });
    h.video.dispatchEvent(new h.window.Event('error')); await tick();
    const calls = h.requests.filter(x => x.route.includes('PlaybackInfo'));
    assert.equal(calls.length, 2);
    assert.equal(calls[1].body.EnableTranscoding, true);
    assert.equal(calls[1].body.EnableDirectPlay, false);
    assert.equal(calls[1].body.AllowVideoStreamCopy, false);
    assert.ok(calls[1].body.DeviceProfile.TranscodingProfiles.every(profile => !profile.VideoCodec.includes('hevc')));
    h.video.dispatchEvent(new h.window.Event('error')); await tick();
    assert.equal(h.requests.filter(x => x.route.includes('PlaybackInfo')).length, 2);
    assert.match(h.find('[data-player-status]').textContent, /播放失败/);
  } finally { await h.cleanup(); }
});
test('late processing diagnostics cannot overwrite a later session', async () => {
  let finish;
  const transcoded = { ...hdrSource, SupportsDirectPlay: false, SupportsTranscoding: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'stream', MediaSources: [transcoded] } : route === 'Sessions' ? new Promise(resolve => { finish = resolve; }) : undefined);
  try {
    await h.window.NightPlayer.show({ item }); h.video.dispatchEvent(new h.window.Event('loadedmetadata')); await tick();
    h.window.NightPlayer.dispose();
    finish([{ DeviceId: 'device-1', UserId: 'user-1', NowPlayingItem: { Id: item.Id }, PlayState: { MediaSourceId: source.Id }, TranscodingInfo: { IsVideoDirect: true, IsAudioDirect: true } }]); await tick();
    assert.doesNotMatch(h.find('[data-player-method]').textContent, /仅重封装/);
  } finally { await h.cleanup(); }
});
test('server-provided direct stream remains playable when video transcoding is disabled', async () => {
  const remux = { ...source, SupportsDirectPlay: false, SupportsTranscoding: false, SupportsDirectStream: true, TranscodingSubProtocol: 'hls', TranscodingUrl: '/Videos/item-1/master.m3u8' };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'remux-only', MediaSources: [remux] } : undefined);
  try {
    await h.window.NightPlayer.show({ item });
    assert.match(h.video.src, /master\.m3u8/);
    assert.doesNotMatch(h.find('[data-player-status]').textContent, /失败/);
  } finally { await h.cleanup(); }
});
test('missing item streams are fetched before HEVC capability checks use actual source dimensions', async () => {
  const actual = { ...hdrSource, MediaStreams: [{ ...hdrSource.MediaStreams[0], Width: 1280, Height: 720, BitRate: 4000000 }, ...source.MediaStreams] };
  const h = harness(route => route.includes('PlaybackInfo') ? { PlaySessionId: 'actual-streams', MediaSources: [actual] } : undefined);
  const probes = [];
  try {
    enableHevc(h);
    Object.defineProperty(h.window.navigator, 'mediaCapabilities', { value: { decodingInfo: async config => { probes.push(config); return { supported: config.video.width === 1280 }; } }, configurable: true });
    await h.window.NightPlayer.show({ item: { ...item, MediaStreams: undefined } });
    assert.ok(probes.length);
    assert.ok(probes.every(config => config.video.width === 1280 && config.video.height === 720 && config.video.bitrate === 4000000));
    const calls = h.requests.filter(request => request.route.includes('PlaybackInfo'));
    assert.equal(calls.length, 2);
    assert.ok(calls[1].body.DeviceProfile.DirectPlayProfiles.some(profile => profile.VideoCodec.includes('hevc')));
  } finally { await h.cleanup(); }
});
