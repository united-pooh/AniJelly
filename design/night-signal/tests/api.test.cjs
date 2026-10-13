const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('jsdom');
const source = () => fs.readFileSync(path.join(__dirname, '../parts/api.js'), 'utf8');
const user = { Id: '11111111111111111111111111111111', Name: '测试用户', Policy: { IsAdministrator: true } };
function app(respond) {
  const dom = new JSDOM('', { url: 'http://localhost:18777', runScripts: 'outside-only' });
  const calls = [];
  dom.window.fetch = async (url, options) => { calls.push({ url, ...options }); return respond(url, options); };
  dom.window.eval(source());
  return { dom, api: dom.window.NightApi, calls };
}
const json = (data, status = 200) => new Response(JSON.stringify(data), { status, headers: { 'Content-Type': 'application/json' } });
test('authentication sends exact password, keeps token in tab session, and never persists password', async () => {
  const {dom, api, calls} = app(() => json({ User: user, AccessToken: 'test-token', ServerId: 'test-server' }));
  await api.login(' 测试用户 ', ' space password ');
  assert.deepEqual(JSON.parse(calls[0].body), { Username: '测试用户', Pw: ' space password ' });
  assert.match(calls[0].headers.Authorization, /Client="Night Signal"/);
  assert.equal(api.user.Id, user.Id);
  assert.equal(api.isAuthenticated, true);
  assert.equal(dom.window.localStorage.getItem('night-signal-session-v1'), null);
  assert.equal(dom.window.sessionStorage.getItem('night-signal-session-v1').includes('password'), false);
  const media = new URL(api.url('/Videos/item/stream', { Static: true }));
  assert.equal(media.pathname, '/api/Videos/item/stream');
  assert.equal(media.searchParams.get('ApiKey'), 'test-token');
  assert.throws(() => api.url('https://untrusted.example/video'), /地址/);
  dom.window.close();
});
test('wrong password never creates session; 401 on authenticated request clears session once', async () => {
  let authenticated = false;
  const {dom, api} = app((url) => url.includes('AuthenticateByName') && authenticated ? json({User:user,AccessToken:'token'}) : json({},401));
  await assert.rejects(api.login('name','wrong'), e => e.status === 401);
  assert.equal(api.isAuthenticated,false);
  authenticated = true;
  await api.login('name','correct');
  let expired = 0;
  dom.window.addEventListener('night-auth-expired',() => expired++);
  await assert.rejects(api.request('/Items'),e=>e.status===401);
  assert.equal(api.isAuthenticated,false);
  assert.equal(expired,1);
  dom.window.close();
});
test('restore verifies user with server and logout invalidates server session', async () => {
  const {dom, api, calls} = app((url) => url.includes('AuthenticateByName') ? json({User:user,AccessToken:'token'}) : url.endsWith('Users/Me') ? json(user) : new Response(null,{status:204}));
  await api.login('name','correct');
  assert.equal((await api.restore()).Id,user.Id);
  await api.logout();
  assert.equal(api.isAuthenticated,false);
  assert.equal(dom.window.sessionStorage.getItem('night-signal-session-v1'),null);
  const logout = calls.find(c=>c.url.endsWith('Sessions/Logout'));
  assert.match(logout.headers.Authorization,/Token="token"/);
  assert.equal(logout.method,'POST');
  dom.window.close();
});
test('startup calls user creation, authenticates, then completes with token', async () => {
  const {dom,api,calls} = app(url=>url.endsWith('System/Info/Public') ? json({StartupWizardCompleted:false}) : url.includes('AuthenticateByName') ? json({User:user,AccessToken:'token'}) : new Response(null,{status:204}));
  await api.setup('owner','a real password');
  assert.deepEqual(calls.map(c=>new URL(c.url).pathname),['/api/System/Info/Public','/api/Startup/User','/api/Users/AuthenticateByName','/api/Startup/Complete']);
  assert.match(calls[3].headers.Authorization,/Token="token"/);
  dom.window.close();
});
test('request uses auth headers without a token query, supports empty responses, and rejects after logout', async () => {
  const {dom,api,calls} = app(url=>url.includes('AuthenticateByName')?json({User:user,AccessToken:'token'}):new Response(null,{status:204}));
  await api.login('name','password');
  assert.equal(await api.request('UserFavoriteItems/id',{method:'POST'}),undefined);
  assert.equal(new URL(calls[1].url).searchParams.has('ApiKey'),false);
  await api.logout();
  await assert.rejects(api.request('/Items'),e=>e.status===401);
  dom.window.close();
});
