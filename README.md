# RCNB.csharp

C# implementation of [RCNB](https://github.com/rcnbapp/RCNB.js).

## Usage
```C#
RcnbConvert.ToRcnbString(new byte[] { 114, 99, 110, 98 }); // "ɌcńƁȓČņÞ"
RcnbConvert.FromRcnbString("ɌcńƁȓČņÞ"); // byte[] { 114, 99, 110, 98 }
```