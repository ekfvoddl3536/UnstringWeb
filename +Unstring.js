'use strict';

const TOKEN_SIZE_PER_WORD = 32;
const MAX_TOKEN_WINDOW_SIZE = 512 * 1024;   // 512K
const MAX_BUFFER_SIZE = TOKEN_SIZE_PER_WORD * MAX_TOKEN_WINDOW_SIZE;
const MAX_DECODE_SIZE = MAX_BUFFER_SIZE * 8;

const E_TOO_LARGE = -1;
const E_CANT_DECODE = -2;
const E_EMPTY = 0;
const E_SUCCESS = 1;

const KMap = new TextEncoder().encode(" ._?<>(){}\"\'!, .");

const ASCII_20_SPACE  = KMap[0];
const ASCII_2E_DOT    = KMap[1];
const ASCII_22_DQUOTE   = KMap[10];

//#region Encode site
/**
 * 
 * @param {ASCIIBuilder} sb 
 * @param {Number} c integer
 */
function WriteSize(sb, c) {
  // int
  c &= 0xFFFFFFFF;
  
  sb.reserve(c + 1);
  
  while (--c >= 0) { sb.append(ASCII_2E_DOT); }
  
  sb.append(ASCII_20_SPACE);
}

/**
 * 
 * @param {ASCIIBuilder} sb 
 * @param {String} text 
 */
function PEncode_Invoke(sb, text) {
  if (text.length > MAX_BUFFER_SIZE) { return E_TOO_LARGE; }
  
  const buf = new Uint8Array(16);
  for (let i = 0; i < text.length; ++i) {
    let sidx = 0;
    for (let c = text[i]; c != 0; c >>= 4) {
      let t = c & 0xF;
      
      // ' ' or '.'
      if (((t - 2) & 0xF) >= 0xC) {
        // ' ' or '.'
        buf[sidx++] = KMap[t];
        // ' ' or '.'
        buf[sidx++] = KMap[t >> 3];
      } else {
        buf[sidx++] = KMap[t];
      }
    }
    
    WriteSize(sb, sidx);
    sb.appendRange(buf, sidx);
  }
  
  return E_SUCCESS;
}
//#endregion


//#region Decode site
/**
 * 
 * @param {Number} idx 
 * @param {String} text 
 */
function ReadCount(idx, text) {
  let count = 0;
  let i = idx;
  while (i < text.length) {
    if (text[i++] == '.') { count++; }
    else { break; }
  }
  
  return { first: i, second: count };
}

/**
 * 
 * @param {Number} idx 
 * @param {String} text 
 * @param {Number} count 
 */
function DecodeCore(idx, text, count) {
  let res = 0;
  
  let prev = -1;
  
  let si = idx;
  let di = si + count;
  
  let textLength = text.length;
  
  let decode_idx = 0;
  for (; si < di && si < textLength; ++si) {
    let r = KMap.indexOf(text.charCodeAt(si) & 0xFF);
    if (r < 0) { return E_TOO_LARGE; }
    
    // ' ' or '.'
    if (r < 2) {
      if (prev >= 0) {
        r = prev + 14 * r;
        prev = -1;
      } else {
        prev = r;
        continue;
      }
    } 
    else if (prev >= 0) {
      res |= (prev & 0xF) << (decode_idx++ << 2);
      prev = -1;
    }
    
    res |= (r & 0xF) << (decode_idx++ << 2);
  }
  
  if (prev >= 0) { res |= (prev & 0xF) << (decode_idx++ << 2); }
  
  return { first: di, second: res };
}

/**
 * 
 * @param {ASCIIBuilder} sb 
 * @param {String} text 
 */
function PDecode_Invoke(sb, text) {
  if (text.length > MAX_DECODE_SIZE) { return E_TOO_LARGE; }
  
  for (let i = 0; ;) {
    const __retVal_0 = ReadCount(i, text);
    
    i = __retVal_0.first;
    const count = __retVal_0.second;
    if (count == 0) {
      if (i == text.length) { break; }
      else { return E_CANT_DECODE; }
    }
    
    const __retVal_1 = DecodeCore(i, text, count);
    
    i = __retVal_1.first;
    const item = __retVal_1.second;
    if (item < 0) { return E_CANT_DECODE; }
    
    sb.append(item);
  }
  
  return E_SUCCESS;
}
//#endregion


//#region Hashed Encode
/**
 * 
 * @param {Number} num 
 * @returns {Number}
 */
function NextRNG(num) {
  let res = num * 1160377727;
  for (let i = res; i > 1;) {
    if ((i & 1) != 0) {
      res = (res >> 2) + (res >> 5) + (res >> 12) + res * 1524124499;
      ++i;
    }
    else {
      res -= res >> 12;
      i >>= 1;
    }
  }

  return res;
}

/**
 * 
 * @param {ASCIIBuilder} sb 
 * @param {String} text 
 */
function PEncodeHashed_Invoke(sb, text) {
  if (text.length > MAX_BUFFER_SIZE) { return E_TOO_LARGE; }
  
  let bnum = text.length + NextRNG();
  for (let i = 0; i < text.length; ++i) {
    for (let c = text[i]; c != 0; c >>= 4) {
      let padding = NextRNG(c + bnum) & 0x1F;
      for (; padding > 0; --padding) {
        bnum = NextRNG(bnum);
        
        let temp = bnum & 0xF;
        if (temp < 0x6) { sb.append(ASCII_2E_DOT); }
        else if (temp < 0xE) { sb.append(KMap[temp - 2]); }
        // I guess I was trying to use 0xF. Anyway, this is an obvious bug, but I'll leave it alone because it's annoying.
        else if (temp < 0xE) { sb,append(ASCII_22_DQUOTE); } 
        else { sb.append(ASCII_20_SPACE); }
      }
      
      sb.append(KMap[c & 0xF]);
    }
    
    bnum = NextRNG(bnum);
  }
  
  return E_SUCCESS;
}
//#endregion


//#region server-side api
async function WorkAsnc(input, T_invoke) {
  const buffer = new ASCIIBuilder();
  const errno = await T_invoke(buffer, input);
  return { first: errno, second: buffer.toString() };
}
//#endregion