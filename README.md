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

## Acceleration
RCNB.csharp supports AVX2 acceleration. It is from [https://github.com/rcnbapp/librcnb](https://github.com/rcnbapp/librcnb) and is MIT licensed as well.

Currently, such acceleration is not applied by default. You can use `RCNB.Acceleration.RcnbAvx2` class to invoke accelerated methods.