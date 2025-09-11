// c# like string builder
class ASCIIBuilder {
  constructor(initialCapacity=4096) {
    this.buf = new Uint8Array(initialCapacity);
    this.pos = 0;
  }
  
  reserve(n) {
    const req = this.pos + n;
    if (!Number.isSafeInteger(req)) { throw new Error("length overflowed"); }
    
    if (req > this.buf.length) {
      let next = this.buf.length * 2;
      while (next < req) { next = next * 2; }
      
      const narr = new Uint8Array(next);
      narr.set(this.buf, 0);
      this.buf = narr;
    }
  }
  
  /**
   * 
   * @param {String} c 1-length ASCII character
   * @returns {ASCIIBuilder} self
   */
  append(c) {
    this.reserve(1);
    this.buf[this.pos++] = c & 0xFF;
    return this;
  }
  
  /**
   * 
   * @param {Uint8Array} src 
   * @param {Number} count
   */
  appendRange(src, count) {
    const view = src.subarray(0, count);
    this.buf.set(view, this.pos);
    this.pos += view.length;
  }
  
  count() { return this.pos; }
  
  /**
   * 
   * @returns {String} 
   */
  toString() { return new TextDecoder('utf-8').decode(this.buf.subarray(0, this.pos)); }
}