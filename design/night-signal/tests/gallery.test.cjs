const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('jsdom');

const parts = path.join(__dirname, '../parts');
const flush = () => new Promise(resolve => setTimeout(resolve, 15));
function fixture({ authenticated = true, hash = '#home', request, restoreError, dispose } = {}) {
  const dom = new JSDOM(fs.readFileSync(path.join(parts, 'gallery.html'), 'utf8') + '<section id="login-screen" class="screen" hidden></section><section id="player-screen" class="screen" hidden><h1>播放器</h1></section>', { url: `http://localhost/${hash}`, runScripts: 'outside-only', pretendToBeVisual: true });
  const w = dom.window;
  const calls = [], plays = [];
  let loggedIn = authenticated, loginOptions;
  w.scrollTo = () => {};
  w.matchMedia = () => ({ matches: false });
  w.HTMLElement.prototype.scrollIntoView = () => {};
  w.HTMLDialogElement.prototype.showModal = function () { this.open = true; };
  w.HTMLDialogElement.prototype.close = function () { this.open = false; this.dispatchEvent(new w.Event('close')); };
  w.NIGHT_ART = { hero: 'data:image/png;base64,AA==', atlas: 'data:image/png;base64,AA==' };
  w.NightApi = {
    get user() { return loggedIn ? { Id: 'user-id', Name: '真实用户' } : null; },
    get isAuthenticated() { return loggedIn; },
    async restore() { if (restoreError) throw restoreError; return this.user; },
    async logout() { calls.push(['logout']); loggedIn = false; },
    async request(route, options = {}) { calls.push([route, options]); if (request) { const value = await request(route, options); if (value !== undefined) return value; } return route === '/Items/Latest' ? [] : { Items: [], TotalRecordCount: 0 }; },
    url(route) { return `/api${route}?ApiKey=test`; }
  };
  w.NightLogin = { init(options) { loginOptions = options; }, show() { w.document.getElementById('login-screen').hidden = false; }, dispose() { w.document.getElementById('login-screen').hidden = true; } };
  w.NightPlayer = { init() {}, async show(options) { plays.push(options); w.document.getElementById('player-screen').hidden = false; }, async dispose() { if (dispose) await dispose(calls); w.document.getElementById('player-screen').hidden = true; } };
  w.eval(fs.readFileSync(path.join(parts, 'gallery.js'), 'utf8'));
  return { w, calls, plays, close: () => w.close(), expire: () => { loggedIn = false; w.dispatchEvent(new w.Event('night-auth-expired')); }, enter: () => { loggedIn = true; return loginOptions.onEnter(); } };
}

test('unauthenticated deep links return to login without fetching media', async () => {
  for (const hash of ['#home', '#detail?id=real-id', '#player?id=real-id']) {
    const s = fixture({ authenticated: false, hash });
    await flush();
    assert.equal(s.w.document.body.dataset.screen, 'login');
    assert.equal(s.w.document.getElementById('gallery-shell').hidden, true);
    assert.equal(s.calls.length, 0);
    s.close();
  }
});

test('an authenticated empty library shows no fictional cards or play actions', async () => {
  const s = fixture();
  await flush();
  assert.equal(s.w.document.body.dataset.screen, 'home');
  assert.match(s.w.document.getElementById('home-state').textContent, /没有.*媒体|暂无.*媒体/);
  assert.equal(s.w.document.querySelectorAll('.poster-card,.continue-card').length, 0);
  assert.doesNotMatch(s.w.document.body.textContent, /蓝色时差|薄荷字幕组|COUR 01|虚构作品/);
  assert(s.calls.some(([route]) => route === '/UserViews'));
  s.close();
});

test('restore network failure is shown with retry, without treating it as empty media', async () => {
  const s = fixture({ restoreError: new Error('offline') });
  await flush();
  const state = s.w.document.getElementById('connection-state');
  assert.equal(state.hidden, false);
  assert.match(state.textContent, /连接|重试/);
  assert.equal(s.calls.length, 0);
  s.close();
});

const movie = { Id: 'movie-abc-001', Type: 'Movie', Name: '真实影片 <img src=x onerror=alert(1)>', ProductionYear: 2024, RunTimeTicks: 900000000, UserData: { IsFavorite: false, PlaybackPositionTicks: 120000000 } };

test('real string IDs, metadata escaping and neutral missing images survive home to detail', async () => {
  const s = fixture({ request: route => {
    if (route === '/Items') return { Items: [movie], TotalRecordCount: 1 };
    if (route === '/Items/' + movie.Id) return movie;
  } });
  await flush();
  const card = s.w.document.querySelector('#library-posters [data-detail]');
  assert.equal(card.dataset.detail, movie.Id);
  assert.equal(card.querySelectorAll('img').length, 0);
  assert.equal(card.querySelectorAll('[onerror]').length, 0);
  card.click(); await flush();
  assert.equal(s.w.document.getElementById('detail-title').textContent, movie.Name);
  assert.equal(s.w.document.querySelectorAll('#gallery-shell [onerror]').length, 0);
  assert.equal(s.w.document.getElementById('people-section').hidden, true);
  assert.equal(s.w.document.getElementById('season-tabs').childElementCount, 0);
  s.close();
});

test('favorites change only after the server confirms and failed writes preserve state', async () => {
  let fail = true;
  const s = fixture({ hash: '#detail?id=' + movie.Id, request: route => {
    if (route === '/Items/' + movie.Id) return { ...movie, UserData: { IsFavorite: false } };
    if (route === '/UserFavoriteItems/' + movie.Id) { if (fail) throw new Error('failed'); return { IsFavorite: true }; }
  } });
  await flush();
  const button = s.w.document.getElementById('favorite-toggle');
  button.click(); await flush();
  assert.equal(button.getAttribute('aria-pressed'), 'false');
  fail = false; button.click(); await flush();
  assert.equal(button.getAttribute('aria-pressed'), 'true');
  assert.equal(s.calls.find(([route]) => route.startsWith('/UserFavoriteItems/'))[1].method, 'POST');
  s.close();
});

test('library pagination and selected library reach the server without invented items', async () => {
  const s = fixture({ request: (route, options) => {
    if (route === '/UserViews') return { Items: [{ Id: 'library-one', Name: '电影库', CollectionType: 'movies' }] };
    if (route === '/Items' && !options.query.isFavorite) return { Items: options.query.startIndex ? [{ ...movie, Id: 'movie-second', Name: '第二部' }] : [movie], TotalRecordCount: 2 };
  } });
  await flush();
  s.w.document.getElementById('more-library').click(); await flush();
  assert.equal(s.w.document.querySelectorAll('#library-posters .poster-card').length, 2);
  assert(s.calls.some(([route, options]) => route === '/Items' && options.query.startIndex === 1));
  const choice = s.w.document.getElementById('library-choice'); choice.value = 'library-one'; choice.dispatchEvent(new s.w.Event('change', { bubbles: true }));
  await flush();
  assert(s.calls.some(([route, options]) => route === '/Items' && options.query.parentId === 'library-one'));
  s.close();
});

test('search uses server paging and ignores an obsolete delayed response', async () => {
  let resolveOld;
  const s = fixture({ request: (route, options) => {
    if (options.query?.searchTerm === 'old') return new Promise(resolve => { resolveOld = resolve; });
    if (options.query?.searchTerm === 'new') return { Items: [{ ...movie, Id: 'new-id', Name: '新结果' }], TotalRecordCount: options.query.startIndex ? 2 : 2 };
  } });
  await flush(); s.w.document.querySelector('[data-panel="search"]').click();
  const input = s.w.document.getElementById('anime-search');
  input.value = 'old'; input.dispatchEvent(new s.w.Event('input', { bubbles: true })); await new Promise(resolve => setTimeout(resolve, 270));
  input.value = 'new'; input.dispatchEvent(new s.w.Event('input', { bubbles: true })); await new Promise(resolve => setTimeout(resolve, 270));
  resolveOld({ Items: [{ ...movie, Name: '旧结果' }], TotalRecordCount: 1 }); await flush();
  assert.match(s.w.document.getElementById('search-results').textContent, /新结果/);
  assert.doesNotMatch(s.w.document.getElementById('search-results').textContent, /旧结果/);
  s.w.document.getElementById('more-search').click(); await flush();
  assert(s.calls.some(([, options]) => options?.query?.searchTerm === 'new' && options.query.startIndex === 1));
  s.close();
});

test('series episodes use real seasons and pass exact item ticks and next ID to player', async () => {
  const episode = { Id: 'ep-one', Type: 'Episode', Name: '第一集', SeriesId: 'series-a', SeasonId: 'season-a', IndexNumber: 1, UserData: { PlaybackPositionTicks: 123456789 } };
  const next = { ...episode, Id: 'ep-two', IndexNumber: 2, Name: '第二集' };
  const s = fixture({ hash: '#detail?id=series-a', request: (route, options) => {
    if (route === '/Items/series-a') return { Id: 'series-a', Type: 'Series', Name: '真实剧集' };
    if (route === '/Shows/series-a/Seasons') return { Items: [{ Id: 'season-a', Name: '特别篇', IndexNumber: 0 }] };
    if (route === '/Shows/series-a/Episodes') return { Items: [episode, next], TotalRecordCount: 2 };
    if (route === '/Items/ep-one') return episode;
  } });
  await flush();
  assert.equal(s.w.document.querySelector('[data-season]').dataset.season, 'season-a');
  s.w.document.querySelector('[data-play="ep-one"]').click(); await flush();
  assert.equal(s.plays.length, 1);
  assert.equal(s.plays[0].item.Id, 'ep-one');
  assert.equal(s.plays[0].startPositionTicks, 123456789);
  assert.equal(s.plays[0].nextItemId, 'ep-two');
  assert(s.calls.some(([route, options]) => route === '/Shows/series-a/Episodes' && options.query.seasonId === 'season-a'));
  s.close();
});

test('logout awaits player stop, clears private markup and prevents delayed media from returning', async () => {
  let resolveStop;
  const s = fixture({ hash: '#player?id=' + movie.Id, request: route => route === '/Items/' + movie.Id ? movie : undefined, dispose: calls => new Promise(resolve => { calls.push(['stopping']); resolveStop = resolve; }) });
  await flush();
  s.w.document.querySelector('[data-action="logout"]').click(); await flush();
  assert.equal(s.calls.some(([route]) => route === 'logout'), false);
  resolveStop(); await flush();
  assert(s.calls.some(([route]) => route === 'logout'));
  assert.equal(s.w.document.body.dataset.screen, 'login');
  assert.doesNotMatch(s.w.document.getElementById('gallery-shell').textContent, /真实影片/);
  s.close();
});

test('expired authentication cancels late results and clears protected content', async () => {
  let resolveItems;
  const s = fixture({ request: (route, options) => route === '/Items' && !options.query.isFavorite ? new Promise(resolve => { resolveItems = resolve; }) : undefined });
  await flush(); s.expire(); await flush();
  resolveItems({ Items: [movie], TotalRecordCount: 1 }); await flush();
  assert.equal(s.w.document.body.dataset.screen, 'login');
  assert.equal(s.w.document.querySelectorAll('.poster-card').length, 0);
  s.close();
});

test('resuming an alternate version finds the next episode after the server-normalized primary', async () => {
  const alternate = { Id: 'alternate-one', Type: 'Episode', Name: '第一集备用版本', SeriesId: 'series-a', SeasonId: 'season-a', UserData: { PlaybackPositionTicks: 40000000 } };
  const s = fixture({ hash: '#player?id=alternate-one', request: route => {
    if (route === '/Items/alternate-one') return alternate;
    if (route === '/Shows/series-a/Episodes') return { Items: [{ ...alternate, Id: 'primary-one' }, { ...alternate, Id: 'primary-two', Name: '下一集' }], TotalRecordCount: 2 };
  } });
  await flush();
  assert.equal(s.plays[0].item.Id, 'alternate-one');
  assert.equal(s.plays[0].nextItemId, 'primary-two');
  s.close();
});

test('a pending favorite from a previous login cannot change the new user state or busy flag', async () => {
  let newUser = false;
  const pending = [];
  const s = fixture({ hash: '#detail?id=shared-item', request: route => {
    if (route === '/Items/shared-item') return { Id: 'shared-item', Type: 'Movie', Name: '共享影片', UserData: { IsFavorite: newUser } };
    if (route === '/UserFavoriteItems/shared-item') return new Promise(resolve => pending.push(resolve));
  } });
  try {
    await flush();
    const button = s.w.document.getElementById('favorite-toggle');
    button.click(); await flush();
    s.w.document.querySelector('[data-action="logout"]').click(); await flush();
    newUser = true; await s.enter();
    s.w.location.hash = '#detail?id=shared-item'; await flush();
    assert.equal(button.getAttribute('aria-pressed'), 'true');
    assert.equal(button.disabled, false);
    button.click(); await flush();
    assert.equal(pending.length, 2);
    pending[0]({ IsFavorite: true }); await flush();
    assert.equal(button.getAttribute('aria-pressed'), 'true');
    assert.equal(button.disabled, true);
    pending[1]({ IsFavorite: false }); await flush();
    assert.equal(button.getAttribute('aria-pressed'), 'false');
    assert.equal(button.disabled, false);
  } finally { s.close(); }
});

test('navigating away from a pending favorite releases the next detail page', async () => {
  let finishFavorite;
  const s = fixture({ hash: '#detail?id=first-item', request: route => {
    if (route.startsWith('/Items/') && !route.endsWith('/Similar')) return { ...movie, Id: route.split('/')[2] };
    if (route === '/UserFavoriteItems/first-item') return new Promise(resolve => { finishFavorite = resolve; });
  } });
  try {
    await flush(); s.w.document.getElementById('favorite-toggle').click(); await flush();
    s.w.location.hash = '#detail?id=next-item'; await flush();
    assert.equal(s.w.document.getElementById('favorite-toggle').disabled, false);
    finishFavorite({ IsFavorite: true }); await flush();
    assert.equal(s.w.document.getElementById('favorite-toggle').getAttribute('aria-pressed'), 'false');
  } finally { s.close(); }
});
