# Redistributable runtime

Place the **.NET 10 Desktop Runtime** installer here when `BundleRuntime` is enabled in `wsl-squad.iss`.

Expected filename:

```text
windowsdesktop-runtime-10.0.x-win-x64.exe
```

Download:

```text
https://dotnet.microsoft.com/download/dotnet/10.0
```

The setup runs it with:

```text
/install /quiet /norestart
```

This file is not versioned: it is ignored by Git.

To avoid bundling the runtime, use a self-contained publication and leave `BundleRuntime` at `0`.
