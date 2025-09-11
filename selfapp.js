'use strict';

const ENDPOINTS = {
  encode: async (input) => { return await WorkAsnc(input, PEncode_Invoke); },
  decode: async (input) => { return await WorkAsnc(input, PDecode_Invoke); },
  hash:   async (input) => { return await WorkAsnc(input, PEncodeHashed_Invoke); }
};

const el = {
  input:  document.getElementById('input'),
  output: document.getElementById('output'),
  status: document.getElementById('status'),
  btnEncode: document.getElementById('btn-encode'),
  btnDecode: document.getElementById('btn-decode'),
  btnHash:   document.getElementById('btn-hash'),
  btnCopy:   document.getElementById('btn-copy'),
  btnClear:  document.getElementById('btn-clear')
};

function setBusy(b) {
  el.btnEncode.disabled = b;
  el.btnDecode.disabled = b;
  el.btnHash.disabled   = b;
}

function setStatus(msg) {
  if (!msg) {
    el.status.hidden = true;
  } else { 
    el.status.textContent = msg;
    el.status.hidden = false;
  }
}

function clearStatus() {
  setStatus(undefined);
}

async function requestTransform(kind) {
  clearStatus();
  
  const inText = el.input.value;
  if (!inText) {
    el.output.value = '';
    setStatus('입력이 비어 있음');
    return;
  }

  const location = ENDPOINTS[kind];
  if (!location) { throw new Error("key not found"); }
  
  setBusy(true);

  try {
    const resp = await location(inText);
    const errno = resp.first;
    if (errno !== E_SUCCESS) {
      if (errno === E_TOO_LARGE) {
        setStatus('변환 오류. 입력 문자열이 너무 긺');
      }
      else if (errno === E_CANT_DECODE) {
        setStatus('변환 오류. 인식할 수 없는 토큰');
      }
      else if (errno !== E_EMPTY) {
        setStatus(`변환 오류. 오류 코드: ${errno}`);
      }
    }
    
    el.output.value = resp.second;
  } catch (e) {
    el.output.value = '';
    setStatus(`오류: ${(e && e.message) || '알 수 없음'}`);
  } finally {
    setBusy(false);
  }
}

async function safeReadText(resp) {
  try { return await resp.text(); }
  catch { return ''; }
}

function clearSelection() {
  var sel;
  if ( (sel = document.selection) && sel.empty ) {
    sel.empty();
  } else {
    if (window.getSelection) {
        window.getSelection().removeAllRanges();
    }
    var activeEl = document.activeElement;
    if (activeEl) {
      var tagName = activeEl.nodeName.toLowerCase();
      if ( tagName == "textarea" || (tagName == "input" && activeEl.type == "text") ) {
        activeEl.selectionStart = activeEl.selectionEnd;
      }
    }
  }
}

async function copyOutput() {
  clearStatus();
  
  const text = el.output.value || '';
  if (!text) { return; }
  
  if (navigator.clipboard && window.isSecureContext) {
    navigator.clipboard.writeText(text)
    .then(() => {
      alert('복사됨');
    })
    .catch(() => {
      setStatus('복사 실패');
    });
  } else {
    new Promise((resolve, reject) => {
      el.output.focus();
      el.output.select();
      
      const ok = document.execCommand && document.execCommand('copy');
      if (ok) { resolve(); }
      else { reject(); }
    })
    .then(() => {
      alert('복사됨');
    })
    .catch(() => {
      setStatus('복사 실패');
    })
    .finally(clearSelection);
  }
}

function clearAll() {
  el.input.value = '';
  el.output.value = '';
}

el.btnEncode.addEventListener('click', () => requestTransform('encode'));
el.btnDecode.addEventListener('click', () => requestTransform('decode'));
el.btnHash  .addEventListener('click', () => requestTransform('hash'));
el.btnCopy  .addEventListener('click', copyOutput);
el.btnClear .addEventListener('click', clearAll);
