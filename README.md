# RCNB.csharp

[![#](https://img.shields.io/nuget/v/RCNB.csharp.svg)](https://www.nuget.org/packages/RCNB.csharp/)

C# implementation of [RCNB](https://github.com/rcnbapp/RCNB.js).

## Usage
Add dependency.
```
dotnet add package RCNB.csharp
```

Write code.
```C#
using RCNB;

RcnbConvert.ToRcnbString(new byte[] { 114, 99, 110, 98 }); // "ɌcńƁȓČņÞ"
RcnbConvert.FromRcnbString("ɌcńƁȓČņÞ"); // byte[] { 114, 99, 110, 98 }
```

## Example
https://rcnb.b11p.com

Blazor Webassembly App for RCNB encoding and decoding online.