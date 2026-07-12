# CLAUDE.md - Project Guide for Claude Code

## Coding Conventions

**Language:** All code and comments in English.

### Naming

| Element | Convention | Example |
|---------|------------|---------|
| Classes, Structs, Methods, Properties, Delegates, Events | PascalCase | `GameManager`, `GetCoins()` |
| Member variables, Serialized fields | camelCase | `cachedGems`, `[SerializeField] speed` |
| Constants, ReadOnly | UPPER_SNAKE_CASE | `MAX_PLAYERS` |

Avoid vague names (`temp`), abbreviations, and negated names (`notSuccessful`).

**`Manager` suffix ⟹ singleton.** Only name a class `…Manager` if it is a `Singleton<T>`. Non-singleton classes take a role/thing name instead (e.g. `StaffDirector`, `ReceptionQueue`, `CustomerSpawner`). One-directional: a singleton may have a non-`Manager` name (e.g. `ReceptionQueue`, `ServiceDispatcher`), but a `…Manager` must always be a singleton.

### Code Standards

- **Allman braces:** Always on new line, always use braces (even single-line `if`)
- **Variable scope:** Declare in innermost scope where used
- **Member variables:** Declare without initialization; initialize in `Awake()`/`Start()`. Exception: implicit zero initialization (e.g., `int`, `float` without assignment) is acceptable.
- **Local variables:** Declare without initialization when possible; assign in logic flow
- **Accessibility:** Default to `private`; use `[SerializeField]` for Inspector, Properties for public access
- **Comparisons:** Explicit null (`if (obj != null)`), implicit bool (`if (isReady)`)
- **No magic numbers:** Use named constants with smallest possible scope
- **Number literals:** `2.0f` for float, `2` for int, `0.5` for double
- **Single return:** One return statement at end of function
- **Success first:** Happy path before error handling in conditionals
- **Loops:** Prefer `for` over `foreach` (allocations); loop index names can be `i`, `j`, `k` for simple iteration, or descriptive names (`itemIndex`, `playerIndex`) for complex logic
- **Ternary:** Allowed when clear: `result = (x > 0) ? POSITIVE : NEGATIVE;`
- **Comments:** Only when they add understanding; no redundant or commented-out code

### Error Logging

```csharp
Debug.LogError("[ClassName] Message.", this);
```

Always null-check `[SerializeField]` references with descriptive errors.

### Enums (Serialized)

Never change existing values. Deprecate with `deprecated_` prefix. Add new items at end only.

---

## Unity Best Practices

### Architecture
- **Single Responsibility:** One task per class; split large MonoBehaviours
- **Data vs Logic:** ScriptableObjects for data, components for logic
- **Composition > Inheritance:** Combine components, avoid deep hierarchies
- **Event-driven:** Use events/delegates for decoupling; one-way dependencies

### Performance
- **Avoid:** `SendMessage()`, `FindObjectsOfType()`, `FindChild()`
- **Cache:** `GetComponent<T>()` in `Awake()`/`Start()`
- **Prefer:** Inspector references over runtime lookups; Object Pooling for spawns

### Functions
- Keep small (<20 lines), avoid globals, no side effects

---

# CRITICAL SECURITY RULES

## WHITELIST ACCESS MODEL

You operate under a **WHITELIST-ONLY** access model for this project.

### ALLOWED FOLDERS (Explicit Whitelist)

You MAY ONLY access these specific folders:

1. `Assets/_Project/Scripts/`
2. `Assets/_Project/Scenes/`
3. `ProjectSettings/`
4. Root-level files: `README.md`, `claude.md`, `.gitignore`, `Phase1_Tasks` `Phase2_Tasks` `Phase3_Tasks` `Phase4_Tasks`
5. `Assets/_Project/Prefabs/`
6. `Docs/`

### ABSOLUTE RESTRICTIONS

You are **FORBIDDEN** from accessing ANY folder not explicitly listed above.

This includes (but is not limited to):
- `Assets/External/` - External SDKs
- `Assets/Resources/` - Runtime loaded assets
- `Assets/Art/` - Art assets
- `Library/`, `Temp/`, `Build/`, `Builds/` - Unity generated folders
- Any file with extensions: `.key`, `.pem`, `credentials.json`


## Response Protocol

**If asked about ANY folder not in the whitelist:**

Respond EXACTLY:
> "I cannot access that folder. This project uses whitelist-only access."

**DO NOT:**
- Attempt to access restricted folders "just to check"
- Suggest workarounds or alternative paths
- Offer to read "just the file names" or directory structure
- Propose moving files to accessible locations
- Try to "help" by finding the information elsewhere

**If you're unsure whether a path is allowed:**
- Default to DENY and ask the user for explicit permission first.

**Never access first and apologize later.**

---

## CLAUDE_IGNORE Markers

**STRICTLY FORBIDDEN** to read, analyze, describe, modify, or reference code between these markers:

```
// CLAUDE_IGNORE_START
// CLAUDE_IGNORE_END
```

Also recognized: `#region CLAUDE_IGNORE`, `// NDA PROTECTED CODE`, files ending in `.NDA.cs`

**When editing files containing CLAUDE_IGNORE sections:**
- Preserve ignored blocks byte-for-byte in exact position
- Only modify code OUTSIDE the markers
- If asked about ignored content: "I cannot access code within CLAUDE_IGNORE markers."
