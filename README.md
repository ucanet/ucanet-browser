<p align="center">
  <img src="logo.png" width="200" alt="ucanet logo" />
</p>
<h1 align="center">ucanet browser</h1>
</p>

Access the ucanet with no DNS changes required in a standalone web browser.

`ucanet browser` is a Windows Forms-based web browser designed for exploring websites on the [ucanet (ucanet.net)](https://ucanet.net), an alt-root DNS server built for retro computers and legacy operating systems. This browser supports Windows 98 through Windows 11, and only requires .NET Framework 2.0.

---

## Features

- Access ucanet domains without system-wide DNS configuration
- Compatibility with Windows 98 to Windows 11
- Fallback for systems that don't support DNS changes

---

## Requirements

- Windows 98, ME, 2000, XP, Vista, 7, 8, 10, or 11
- .NET Framework 2.0 installed

---

## Getting Started

1. Download the latest build from the [Releases](../../releases) page.
2. Launch `ucanet_browser.exe`.
3. If prompted, allow the browser to configure your DNS or proxy.
4. Start browsing `http://ucanet.net` and other ucanet sites.

---

## Build Instructions

This project is written in C# and targets .NET Framework 2.0. It was originally built using Visual Studio 2005.

### To build:

1. Clone the repository:
   ```bash
   git clone https://github.com/YOUR_USERNAME/ucanet_browser.git
   cd ucanet_browser
   ```
   
2. Open the `.sln` file in Visual Studio 2005 (or later, with .NET 2.0 compatibility).

3. Ensure the following libraries are available:
   - `IEProxy`
   - `ucanetProxy.cs`
   - `Heijden DNS`

4. Build the solution and run the executable.
