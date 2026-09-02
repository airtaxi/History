## Important: Do Not Run CSharpStyleGuard

- Do not run CSharpStyleGuard (`--fix`/`--check`) in this repository. The guard force-merges ternary operators (CSG0001) etc. into a single line, producing unreadable lines that exceed 320 characters. Use manual formatting only.
- Exception: In the `History.WindowsClient/` path, the Guard project (`--fix`) **must** be run.

## Important Code Style

- **Must** load the `/csharp-code-style` skill before starting any C# related work (.cs/.xaml/.csproj etc. — create, modify, review, refactor).