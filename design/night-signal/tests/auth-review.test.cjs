const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { JSDOM } = require('jsdom');
const part = name => fs.readFileSync(path.join(__dirname, '../parts', name), 'utf8');
const response = data => new Response(JSON.stringify(data), { status: 200 });
const flush = () => new Promise(resolve => setImmediate(resolve));
function create(fetcher, withLogin = false) {
  const dom = new JSDOM(withLogin ? part('login.html') : '', { url: 'http://localhost:18777', runScripts: 'outside-only', pretendToBeVisual: true });
  const callbacks = {};
  dom.window.matchMedia = () => ({ matches: true, addEventListener(name, callback) { callbacks[name] = callback; } });
  dom.window.fetch = fetcher;
  dom.window.eval(part('api.js'));
  if (withLogin) dom.window.eval(part('login.js'));
  return { dom, window: dom.window, callbacks, api: dom.window.NightApi };
}
test('a late restore cannot attach an old user to a newer account token', async () => {
  let finishRestore;
  const old = { Id: 'old-account', Name: 'Old' }, newer = { Id: 'new-account', Name: 'New' };
  const app = create((url, options) => {
    if (url.endsWith('Users/Me')) return new Promise(resolve => { finishRestore = resolve; });
    const user = JSON.parse(options.body).Username === 'old' ? old : newer;
    return Promise.resolve(response({ User: user, AccessToken: user.Id + '-token' }));
  });
  try {
    await app.api.login('old', 'first');
    const restoring = app.api.restore();
    await app.api.login('new', 'second');
    finishRestore(response(old));
    await restoring.catch(() => {});
    assert.equal(app.api.user.Id, 'new-account');
    assert.match(app.api.url('Items'), /new-account-token/);
  } finally { app.dom.window.close(); }
});
test('disposing the login screen cancels session establishment, not only navigation', async () => {
  let finishLogin;
  const app = create(url => url.endsWith('System/Info/Public') ? Promise.resolve(response({ StartupWizardCompleted: true })) : new Promise(resolve => { finishLogin = resolve; }), true);
  try {
    app.window.NightLogin.init(); await flush();
    app.window.document.querySelector('#night-login-name').value = 'owner';
    app.window.document.querySelector('#night-login-password').value = 'password';
    app.window.document.querySelector('form').dispatchEvent(new app.window.Event('submit', { bubbles: true, cancelable: true }));
    app.window.NightLogin.dispose();
    finishLogin(response({ User: { Id: 'owner' }, AccessToken: 'late-token' })); await flush();
    assert.equal(app.api.isAuthenticated, false);
  } finally { app.dom.window.close(); }
});
test('changing reduced motion during authentication does not strand the form', async () => {
  let finishLogin, entered = 0;
  const app = create(url => url.endsWith('System/Info/Public') ? Promise.resolve(response({ StartupWizardCompleted: true })) : new Promise(resolve => { finishLogin = resolve; }), true);
  try {
    app.window.NightLogin.init({ onEnter: () => entered++ }); await flush();
    app.window.document.querySelector('#night-login-name').value = 'owner';
    app.window.document.querySelector('#night-login-password').value = 'password';
    app.window.document.querySelector('form').dispatchEvent(new app.window.Event('submit', { bubbles: true, cancelable: true }));
    app.callbacks.change({ matches: true });
    finishLogin(response({ User: { Id: 'owner' }, AccessToken: 'token' })); await flush();
    assert.equal(entered, 1, 'pending auth should still complete or return an enabled retry form');
  } finally { app.dom.window.close(); }
});
