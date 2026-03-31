# AppConfig Spec

## Scope
- `Engine/AppConfig.cs`

## Contracts

### 1) Missing sections/keys
- Accessing properties with missing INI sections/keys MUST NOT throw.
- Defaults defined in code MUST be used.

### 2) Integer parsing
- Valid integer strings MUST parse as expected.
- Invalid or empty integer strings MUST fall back to default values.

### 3) Boolean parsing
- Accepted true tokens: `true`, `1`, `on` (case-insensitive).
- Accepted false tokens: `false`, `0`, `off` (case-insensitive).
- Any other token, null, or empty MUST return provided default.

### 4) Key parsing
- Valid enum names (case-insensitive) MUST map to corresponding `Key` value.
- Invalid or empty values MUST use default key.

### 5) Graphics backend parsing
- Supported values: `opengl`, `gl` (case-insensitive, surrounding spaces ignored).
- Unsupported values MUST throw `InvalidOperationException` with clear message.

### 6) Property caching
- Property values MAY be cached after first read.
- Repeated reads MUST remain stable for unchanged source data.
