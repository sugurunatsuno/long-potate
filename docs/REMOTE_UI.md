# Remote UI over SSH

The repository includes an Avalonia Browser host for checking the same shared UI from a machine that is only reachable over SSH.

This is intended for development-time visual inspection on a headless Linux host. The browser host uses the same `App`, `MainView`, and ViewModel as the desktop host; it is not a separate HTML reimplementation.

## One-time setup on the SSH host

Install the .NET 10 SDK and the WebAssembly workload:

```bash
dotnet workload install wasm-tools
```

Clone or update the repository, then start the remote UI from the repository root:

```bash
./scripts/run-remote-ui.sh
```

The development server listens only on:

```text
127.0.0.1:5180
```

It is intentionally not exposed on the host's public network interfaces.

## SSH tunnel from the client machine

Create a local port forward:

```bash
ssh -L 5180:127.0.0.1:5180 <user>@<ssh-host>
```

If you already have an SSH session open, add the equivalent local-forward setting to your SSH config or start a second tunnel-only session:

```bash
ssh -N -L 5180:127.0.0.1:5180 <user>@<ssh-host>
```

Then open this URL on the client machine:

```text
http://127.0.0.1:5180
```

## What this verifies

The Browser host is useful for checking:

- Avalonia layout and styling
- shared `MainView` rendering
- bindings and ViewModel state
- compatibility/print preview UI as it is implemented
- general interaction flow

It does not prove desktop-native integration such as OS file pickers, native printing, drag-and-drop edge cases, or platform-specific rendering behavior. Those should still be verified with the Desktop host before release.

## Desktop host

The normal desktop entry point is now:

```bash
dotnet run --project src/GlassToKey.PrintStudio.Desktop/GlassToKey.PrintStudio.Desktop.csproj
```

The shared UI project is `src/GlassToKey.PrintStudio.App`; it no longer owns the desktop executable entry point.

## CI policy

The regular GitHub Actions CI remains Ubuntu-only and does not build the Browser host. This avoids installing the relatively large WebAssembly workload on every push. Browser/WASM is checked manually during remote UI development.
