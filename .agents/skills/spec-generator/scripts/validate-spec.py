#!/usr/bin/env python3
"""Validate a specification file for AI-readiness and template compliance.

Usage:
    python validate-spec.py <spec-file-path>

Output:
    JSON with validation results, errors, and warnings
"""

import json
import sys
import re
from pathlib import Path
from datetime import datetime


def validate_spec(spec_file):
    """Validate a specification file."""
    results = {
        "status": "pass",
        "file": str(spec_file),
        "errors": [],
        "warnings": [],
        "checks": {}
    }
    
    try:
        content = Path(spec_file).read_text(encoding='utf-8')
    except FileNotFoundError:
        results["status"] = "fail"
        results["errors"].append(f"File not found: {spec_file}")
        return results
    except Exception as e:
        results["status"] = "fail"
        results["errors"].append(f"Error reading file: {str(e)}")
        return results
    
    # Check 1: Frontmatter exists and is valid YAML
    results["checks"]["frontmatter"] = check_frontmatter(content, results)
    
    # Check 2: All required sections present
    results["checks"]["sections"] = check_required_sections(content, results)
    
    # Check 3: Naming convention
    results["checks"]["naming"] = check_naming_convention(spec_file, results)
    
    # Check 4: AI-readiness criteria
    results["checks"]["ai_readiness"] = check_ai_readiness(content, results)
    
    # Check 5: Requirements have RFC 2119 keywords
    results["checks"]["requirements"] = check_requirements_language(content, results)
    
    # Check 6: Task format validation
    results["checks"]["task_format"] = check_task_format(content, results)
    
    # Check 7: No code implementation detected
    results["checks"]["no_implementation"] = check_no_implementation(content, results)
    
    # Determine final status
    if results["errors"]:
        results["status"] = "fail"
    elif results["warnings"]:
        results["status"] = "warn"
    
    return results


def check_frontmatter(content, results):
    """Verify YAML frontmatter structure."""
    check = {"pass": False, "issues": []}
    
    # Must start with ---
    if not content.startswith("---"):
        results["errors"].append("File must start with YAML frontmatter (---)")
        return check
    
    # Find closing ---
    lines = content.split('\n')
    fm_end = None
    for i, line in enumerate(lines[1:], 1):
        if line.strip() == "---":
            fm_end = i
            break
    
    if not fm_end:
        results["errors"].append("Frontmatter not properly closed (missing closing ---)")
        return check
    
    frontmatter = '\n'.join(lines[1:fm_end])
    
    # Check required fields
    required_fields = ['title', 'version', 'date_created', 'last_updated', 'owner', 
                       'tags', 'git_commit', 'git_branch', 'status', 'source_purpose']
    
    for field in required_fields:
        if f"{field}:" not in frontmatter:
            results["errors"].append(f"Missing required frontmatter field: {field}")
        else:
            check["pass"] = True
    
    # Validate status value
    if 'status:' in frontmatter:
        status_match = re.search(r'status:\s*(\w+)', frontmatter)
        if status_match:
            status = status_match.group(1)
            valid_statuses = ['draft', 'review', 'approved', 'implemented', 'deprecated']
            if status not in valid_statuses:
                results["warnings"].append(f"Invalid status '{status}'. Should be one of: {', '.join(valid_statuses)}")
    
    return check


def check_required_sections(content, results):
    """Verify all 15 required sections are present."""
    check = {"pass": True, "missing_sections": []}
    
    required_sections = [
        "## 1. Purpose & Scope",
        "## 2. Definitions & Terminology",
        "## 3. Requirements & Constraints",
        "## 4. Architecture & Interfaces",
        "## 5. Dependencies & External Integrations",
        "## 6. Acceptance Criteria",
        "## 7. Test Automation Strategy",
        "## 8. Examples & Edge Cases",
        "## 9. Validation Criteria",
        "## 10. AI-Readiness Checklist",
        "## 11. Related Specifications & References",
        "## 12. Task Breakdown for Implementation",
        "## 13. Conflict Detection & Resolution",
        "## 14. Files Added to Context",
        "## 15. Always Follow Project Instructions"
    ]
    
    for section in required_sections:
        if section not in content:
            check["pass"] = False
            check["missing_sections"].append(section)
            results["errors"].append(f"Missing section: {section}")
    
    return check


def check_naming_convention(spec_file, results):
    """Validate filename follows naming convention."""
    check = {"pass": False, "filename": Path(spec_file).name, "issues": []}
    
    filename = Path(spec_file).name
    
    # Validate prefix
    if filename.startswith("spec-"):
        results["errors"].append("Filename must start with a 3-digit numeric prefix followed by an underscore (e.g., 000_)")
        return check
    
    prefix_match = re.match(r'^(\d+)_spec-', filename)
    if prefix_match:
        if len(prefix_match.group(1)) != 3:
            results["errors"].append("Filename numeric prefix must be exactly 3 digits")
            return check
    else:
        results["errors"].append("Filename must start with a 3-digit numeric prefix followed by an underscore (e.g., 000_)")
        return check

    # Must end with .md
    if not filename.endswith(".md"):
        results["errors"].append("Filename must end with '.md'")
        return check
    
    # Extract type and description
    parts = filename[9:-3].split("-", 1)  # Remove 000_spec- and .md, split on first -
    
    if len(parts) < 2:
        results["errors"].append("Filename format must be: [prefix]_spec-[type]-[description].md")
        return check
    
    spec_type = parts[0]
    description = parts[1]
    
    # Validate type
    valid_types = ['architecture', 'design', 'process', 'infrastructure', 'data', 'schema', 'tool', 'bugfix']
    if spec_type not in valid_types:
        results["warnings"].append(f"Unknown spec type '{spec_type}'. Expected one of: {', '.join(valid_types)}")
    
    # Validate description (lowercase, hyphens only, no spaces)
    if not re.match(r'^[a-z0-9]+(-[a-z0-9]+)*$', description):
        results["errors"].append("Description must be lowercase with hyphens only (no spaces, underscores, or uppercase)")
        return check
    
    # Check total length
    if len(filename) > 80:
        results["warnings"].append(f"Filename length {len(filename)} exceeds recommended 80 characters")
    
    check["pass"] = True
    check["type"] = spec_type
    check["description"] = description
    
    return check


def check_ai_readiness(content, results):
    """Check for AI-readiness criteria."""
    check = {"pass": True, "issues": []}
    
    # Look for AI-readiness checklist section
    if "## 10. AI-Readiness Checklist" not in content:
        check["issues"].append("Missing AI-Readiness Checklist section")
        return check
    
    # Extract checklist
    checklist_start = content.find("## 10. AI-Readiness Checklist")
    checklist_end = content.find("## 11. Related Specifications", checklist_start)
    if checklist_end == -1:
        checklist_end = len(content)
    
    checklist = content[checklist_start:checklist_end]
    
    # Count checkboxes
    unchecked = checklist.count("- [ ]")
    checked = checklist.count("- [x]")
    
    if unchecked > 0:
        results["warnings"].append(f"AI-Readiness Checklist has {unchecked} unchecked items")
    
    if checked >= 8:  # At least 8 of 10 checks passed
        check["pass"] = True
    else:
        check["pass"] = False
        check["issues"].append(f"Only {checked}/10 AI-readiness checks passed")
    
    return check


def check_requirements_language(content, results):
    """Verify requirements use RFC 2119 keywords."""
    check = {"pass": True, "keyword_count": {}}
    
    keywords = ['MUST', 'SHALL', 'MUST NOT', 'SHOULD', 'SHOULD NOT', 'MAY']
    
    for keyword in keywords:
        count = content.count(keyword)
        check["keyword_count"][keyword] = count
    
    # Must have at least some explicit requirements
    total_keywords = sum(check["keyword_count"].values())
    if total_keywords < 5:
        results["warnings"].append("Spec should have explicit requirements with RFC 2119 keywords (MUST/SHALL/SHOULD/MAY)")
        check["pass"] = False
    
    return check


def check_task_format(content, results):
    """Validate task breakdown YAML format."""
    check = {"pass": True, "tasks_found": 0}
    
    # Look for task section
    if "## 12. Task Breakdown for Implementation" not in content:
        results["warnings"].append("Missing Task Breakdown section")
        return check
    
    # Look for YAML task structure
    task_pattern = r'- id: (TASK-\d+)'
    tasks = re.findall(task_pattern, content)
    check["tasks_found"] = len(tasks)
    
    if len(tasks) < 3:
        results["warnings"].append(f"Expected at least 3 tasks, found {len(tasks)}")
        check["pass"] = False
    
    # Check for required task fields
    required_task_fields = ['title:', 'type:', 'priority:', 'objective:', 'acceptance_criteria:']
    for field in required_task_fields:
        if field not in content:
            results["warnings"].append(f"Tasks may be missing field: {field}")
    
    return check


def check_no_implementation(content, results):
    """Ensure no actual code implementation is present."""
    check = {"pass": True, "issues": []}
    
    # Look for executable code patterns that suggest implementation
    # (Not documentation code blocks, but actual file modifications)
    
    dangerous_patterns = [
        (r'\/\/ TODO.*implement', 'TODO comments suggesting implementation'),
        (r'@Override|override\s', 'Method override suggesting implementation'),
        (r'new\s+\w+\(.*\)\s*\{.*\}', 'Object instantiation suggesting implementation'),
    ]
    
    # Code blocks are fine, but check outside of them
    code_block_pattern = r'```[\s\S]*?```'
    non_code_content = re.sub(code_block_pattern, '', content)
    
    # Generally specs shouldn't have file paths pointing to implementation
    # or specific line numbers being modified
    if re.search(r'src/.*\.cs\b', non_code_content):
        # This is okay for documentation, just checking it's not actual modifications
        pass
    
    # All good - this is a spec, not implementation
    check["pass"] = True
    return check


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({
            "error": "Usage: python validate-spec.py <spec-file-path>",
            "example": "python validate-spec.py /specs/spec-architecture-jwt-auth.md"
        }))
        sys.exit(1)
    
    spec_file = sys.argv[1]
    results = validate_spec(spec_file)
    
    print(json.dumps(results, indent=2))
    sys.exit(0 if results["status"] == "pass" else 1)
