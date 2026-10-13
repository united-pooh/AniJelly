const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const {JSDOM} = require('jsdom');
const part = n=>fs.readFileSync(path.join(__dirname,'../parts',n),'utf8');
async function app(api = {}) {
  const dom = new JSDOM(part('login.html'),{url:'http://localhost:18777',runScripts:'outside-only',pretendToBeVisual:true});
  dom.window.matchMedia=()=>({matches:true,addEventListener(){},removeEventListener(){}});
  let calls=[],entered=0;
  dom.window.NightApi={publicInfo:async()=>({StartupWizardCompleted:true}),login:async(name,password)=>{calls.push({name,password});return{Id:'user'};},...api};
  dom.window.eval(part('login.js'));
  dom.window.NightLogin.init({onEnter:()=>entered++});
  await new Promise(r=>setImmediate(r));
  return{dom,calls,get entered(){return entered;},submit(name,password){dom.window.document.querySelector('#night-login-name').value=name;dom.window.document.querySelector('#night-login-password').value=password;dom.window.document.querySelector('form').dispatchEvent(new dom.window.Event('submit',{bubbles:true,cancelable:true}));},flush:()=>new Promise(r=>setImmediate(r))};
}
test('login authenticates before entering and preserves exact password',async()=>{
  const a=await app();a.submit('owner',' spaced ');await a.flush();
  assert.deepEqual(a.calls,[{name:'owner',password:' spaced '}]);assert.equal(a.entered,1);
  assert.equal(a.dom.window.document.querySelector('#night-login-password').value,'');a.dom.window.close();
});
test('authentication failure stays on readable form, focuses password and permits retry',async()=>{
  const a=await app({login:async()=>{throw Object.assign(new Error('bad'),{status:401});}});a.submit('owner','wrong');await a.flush();
  assert.equal(a.entered,0);assert.match(a.dom.window.document.querySelector('.login-error').textContent,/用户名|密码/);
  assert.equal(a.dom.window.document.querySelector('[data-login-announcement]').textContent,'');
  assert.equal(a.dom.window.document.activeElement.id,'night-login-password');assert.equal(a.dom.window.document.querySelector('.login-enter').disabled,false);a.dom.window.close();
});
test('pending authentication prevents duplicate submit and dispose prevents late route entry',async()=>{
  let resolve,count=0;const a=await app({login:()=>{count++;return new Promise(r=>resolve=r);}});
  a.submit('owner','pass');a.submit('owner','pass');assert.equal(count,1);
  a.dom.window.NightLogin.dispose();resolve({Id:'user'});await a.flush();assert.equal(a.entered,0);a.dom.window.close();
});
test('unconfigured server creates real administrator through setup callback',async()=>{
  let count=0;const a=await app({publicInfo:async()=>({StartupWizardCompleted:false}),setup:async()=>{count++;return{Id:'admin'};}});
  assert.match(a.dom.window.document.querySelector('h2').textContent,/设置/);a.submit('owner','new-password');await a.flush();assert.equal(count,1);assert.equal(a.entered,1);a.dom.window.close();
});
