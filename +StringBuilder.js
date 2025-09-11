// c# like string builder
class StringBuilder {
  constructor(initialCapacity=1024) {
    this.buf = new Uint16Array(initialCapacity);
    this.pos = 0;
  }
  
  reserve(n) {
    const req = this.pos + n;
    if (!Number.isSafeInteger(req)) { throw new Error("length overflowed"); }
    
    if (req > this.buf.length) {
      let next = this.buf.length * 2;
      while (next < req) { next = next * 2; }
      
      const narr = new Uint16Array(next);
      narr.set(this.buf, 0);
      this.buf = narr;
    }
  }
  
  /**
   * 
   * @param {Number} c single UTF-16 character
   * @returns {StringBuilder} self
   */
  append(c) {
    this.reserve(1);
    this.buf[this.pos++] = c & 0xFFFF;
    return this;
  }
  
  /**
   * 
   * @param {Uint16Array} src 
   * @param {Number} start
   * @param {Number} count
   */
  appendRange(src, start, count) {
    const view = src.subarray(start, count);
    this.reserve(view.length);
    
    this.buf.set(view, this.pos);
    this.pos += view.length;
  }
  
  /**
   * 
   * @returns {String} 
   */
  toString() { return String.fromCharCode.apply(null, this.buf.subarray(0, this.pos)); }
}