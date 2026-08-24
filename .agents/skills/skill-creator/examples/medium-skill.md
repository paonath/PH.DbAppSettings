---
name: json-validator
description: |
  Validate and fix JSON files with AI Agent using deterministic scripts.
  Use when: (1) validating JSON structure and syntax, (2) fixing formatting issues,
  (3) converting JSON to/from other formats, (4) generating JSON from templates
---

# JSON Validator & Formatter

Help AI Agent work with JSON files reliably by providing deterministic validation and formatting scripts.

## Overview

AI Agent can execute:
- **Validation**: Check JSON syntax and schema compliance
- **Formatting**: Beautify and normalize JSON files
- **Conversion**: Transform JSON to/from YAML, CSV, or other formats
- **Generation**: Create JSON from templates or structured input

## Validation Workflow

### Step 1: Validate JSON Syntax

AI Agent runs:
```bash
python scripts/validate_json.py input.json
```

Output:
- ✓ Valid JSON with structure summary
- ✗ Errors with line/column numbers for debugging

### Step 2: Format JSON

AI Agent runs:
```bash
python scripts/format_json.py input.json output.json --indent 2
```

Result: Pretty-printed JSON with consistent indentation

### Step 3: Check Schema (Optional)

For complex JSON, validate against schema:
```bash
python scripts/validate_json.py input.json --schema schema.json
```

## Examples

### Validate API Response

Input: `response.json`
```json
{"status":"ok","data":{"users":[{"id":1,"name":"Alice"}]}}
```

Command:
```bash
python scripts/validate_json.py response.json
```

Output:
```json
{
  "status": "valid",
  "structure": {
    "type": "object",
    "keys": ["status", "data"],
    "data": {
      "type": "object",
      "keys": ["users"],
      "users": {
        "type": "array",
        "length": 1
      }
    }
  }
}
```

### Format with Custom Indentation

Command:
```bash
python scripts/format_json.py config.json output.json --indent 4 --sort
```

Features:
- `--indent`: Set indentation level (default 2)
- `--sort`: Sort keys alphabetically
- `--compact`: Single-line output

## Script Reference

See [json-validation-scripts.md](../references/json-validation-scripts.md) for detailed script documentation.

See [json-schema-examples.md](../references/json-schema-examples.md) for schema validation patterns.

## Common Issues

**Q: How do I validate against a schema?**
A: Provide schema.json and use: `python scripts/validate_json.py data.json --schema schema.json`

**Q: Can I convert JSON to YAML?**
A: Yes, use: `python scripts/json_to_yaml.py input.json output.yaml`

**Q: How do I validate large JSON files?**
A: Scripts handle up to 100MB files. For larger files, stream validation is recommended.

---

This skill demonstrates how AI Agent uses **deterministic scripts** for reliable, repeatable file operations.
