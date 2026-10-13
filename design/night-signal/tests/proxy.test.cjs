const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const { join } = require('node:path');
const test = require('node:test');

const directory = join(__dirname, '..');
const nginx = readFileSync(join(directory, 'nginx.conf'), 'utf8');
const compose = readFileSync(join(directory, 'compose.yaml'), 'utf8');
const api = nginx.match(/location\s+\^~\s+\/api\/\s*\{([^}]+)\}/)?.[1] || '';

test('same-origin API forwards to the existing server and strips only the API prefix', () => {
  assert.match(api, /proxy_pass\s+http:\/\/jellyfin:8096\/\s*;/);
  assert.match(api, /proxy_http_version\s+1\.1\s*;/);
  assert.match(api, /proxy_set_header\s+Host\s+\$http_host\s*;/);
});

test('API requests cannot put authentication URLs in proxy logs or referrers', () => {
  assert.match(api, /access_log\s+off\s*;/);
  assert.match(api, /error_log\s+\/dev\/null(?:\s+\w+)?\s*;/);
  assert.match(nginx, /add_header\s+Referrer-Policy\s+"no-referrer"\s+always\s*;/);
});

test('streaming responses are not cached or spooled into the small temporary filesystem', () => {
  assert.match(api, /proxy_buffering\s+off\s*;/);
  assert.match(api, /proxy_cache\s+off\s*;/);
  assert.match(api, /proxy_read_timeout\s+3600s\s*;/);
  assert.doesNotMatch(api, /proxy_set_header\s+(?:Range|If-Range|Authorization)\s+""/);
});

test('the frontend joins a selectable existing network without backend or data volumes', () => {
  assert.match(compose, /networks:\s*\n\s+-\s+jellyfin\s*\n/);
  assert.match(compose, /jellyfin:\s*\n\s+external:\s*true\s*\n\s+name:\s*\$\{JELLYFIN_NETWORK:-anijelly_default\}/);
  assert.match(compose, /127\.0\.0\.1:\$\{NIGHT_SIGNAL_PORT:-18777\}:8080/);
  assert.match(compose, /read_only:\s*true/);
  assert.doesNotMatch(compose, /^\s*volumes:/m);
});
