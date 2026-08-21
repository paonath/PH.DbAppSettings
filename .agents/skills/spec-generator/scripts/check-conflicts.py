#!/usr/bin/env python3
"""Check for conflicts, duplicates, and overlaps in existing specifications.

Usage:
    python check-conflicts.py <spec-purpose> [--type TYPE]

Output:
    JSON with conflict analysis, related specs, recommendations
"""

import json
import sys
import os
import re
from pathlib import Path


def find_existing_specs(repo_root=None):
    """Find all existing specification files."""
    if repo_root is None:
        repo_root = Path.cwd()
    
    specs_dir = Path(repo_root) / "specs"
    implemented_dir = Path(repo_root) / "specs" / "implemented"
    
    specs = {
        "active": [],
        "implemented": []
    }
    
    # Find active specs
    if specs_dir.exists():
        for spec_file in specs_dir.glob("spec-*.md"):
            specs["active"].append({
                "file": spec_file.name,
                "path": str(spec_file),
                "type": extract_spec_type(spec_file.name),
                "description": extract_spec_description(spec_file.name),
                "content_summary": summarize_spec(spec_file)
            })
    
    # Find implemented specs
    if implemented_dir.exists():
        for spec_file in implemented_dir.rglob("*.md"):
            if not spec_file.name.startswith("spec-"):
                specs["implemented"].append({
                    "file": spec_file.name,
                    "path": str(spec_file),
                    "type": spec_file.parent.name,
                    "content_summary": summarize_spec(spec_file)
                })
    
    return specs


def extract_spec_type(filename):
    """Extract spec type from filename."""
    match = re.match(r'spec-(\w+)-', filename)
    return match.group(1) if match else "unknown"


def extract_spec_description(filename):
    """Extract description from filename."""
    match = re.match(r'spec-\w+-(.+)\.md', filename)
    return match.group(1) if match else ""


def summarize_spec(spec_file):
    """Extract key info from spec file."""
    try:
        content = Path(spec_file).read_text(encoding='utf-8')
        
        # Extract title
        title_match = re.search(r'title:\s*(.+?)[\n\r]', content)
        title = title_match.group(1).strip() if title_match else ""
        
        # Extract first few requirements
        req_matches = re.findall(r'- \*\*REQ-\d+\*\*:\s*(.+?)(?:\n|$)', content)
        requirements = req_matches[:3]
        
        return {
            "title": title,
            "sample_requirements": requirements
        }
    except Exception:
        return {"title": "", "sample_requirements": []}


def analyze_conflicts(spec_purpose, existing_specs):
    """Analyze potential conflicts with existing specs."""
    analysis = {
        "status": "ok",
        "conflicts": [],
        "related": [],
        "recommendations": []
    }
    
    purpose_lower = spec_purpose.lower()
    keywords = extract_keywords(spec_purpose)
    
    # Check each existing spec for conflicts
    for spec_list_name in ["active", "implemented"]:
        for spec in existing_specs[spec_list_name]:
            conflict_score = calculate_conflict_score(
                purpose_lower,
                spec,
                keywords
            )
            
            if conflict_score > 0.8:
                analysis["status"] = "conflict"
                analysis["conflicts"].append({
                    "status": spec_list_name,
                    "file": spec["file"],
                    "type": spec["type"],
                    "reason": f"High overlap (score: {conflict_score:.2f})",
                    "suggestion": "Review existing spec before creating duplicate"
                })
            elif conflict_score > 0.5:
                analysis["related"].append({
                    "status": spec_list_name,
                    "file": spec["file"],
                    "type": spec["type"],
                    "reason": f"Related topic (score: {conflict_score:.2f})"
                })
    
    # Generate recommendations
    if analysis["conflicts"]:
        analysis["recommendations"].append("Cancel creation - duplicates exist")
        analysis["recommendations"].append("Review conflicting specs and consolidate")
    elif len(analysis["related"]) > 0:
        analysis["recommendations"].append("Link related specs in frontmatter")
        analysis["recommendations"].append("Document relationship in 'Conflict Analysis' section")
    else:
        analysis["recommendations"].append("No conflicts detected - safe to create")
    
    return analysis


def extract_keywords(text):
    """Extract key terms from purpose."""
    # Simple keyword extraction
    stop_words = {'the', 'a', 'an', 'and', 'or', 'for', 'with', 'to', 'in', 'on', 'is', 'be'}
    words = re.findall(r'\b\w+\b', text.lower())
    return [w for w in words if len(w) > 3 and w not in stop_words]


def calculate_conflict_score(purpose, spec, keywords):
    """Calculate how much spec conflicts with purpose (0-1 scale)."""
    score = 0
    # Build a safe string representation of the spec content summary
    content_summary = spec.get('content_summary', {}) or {}
    title = content_summary.get('title', '')
    sample_requirements = content_summary.get('sample_requirements', [])
    if isinstance(sample_requirements, list):
        sample_requirements_str = ' '.join([str(x) for x in sample_requirements])
    else:
        sample_requirements_str = str(sample_requirements)

    spec_content = f"{spec.get('type','')} {spec.get('description','')} {title} {sample_requirements_str}"

    # Check keyword overlap
    spec_lower = spec_content.lower()
    keyword_hits = sum(1 for kw in keywords if kw in spec_lower)
    score += (keyword_hits / max(len(keywords), 1)) * 0.6

    # Check type match against sample requirements safely
    if any(str(req).lower() in purpose for req in (sample_requirements or [])):
        score += 0.3

    # Check filename similarity
    if any(kw in spec.get('file', '').lower() for kw in keywords):
        score += 0.1
    
    return min(score, 1.0)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print(json.dumps({
            "error": "Usage: python check-conflicts.py '<spec-purpose>' [--type TYPE]",
            "example": "python check-conflicts.py 'JWT authentication with refresh tokens' --type architecture"
        }))
        sys.exit(1)
    
    purpose = sys.argv[1]
    spec_type = None
    
    if "--type" in sys.argv:
        type_idx = sys.argv.index("--type")
        if type_idx + 1 < len(sys.argv):
            spec_type = sys.argv[type_idx + 1]
    
    # Find and analyze existing specs
    existing_specs = find_existing_specs()
    analysis = analyze_conflicts(purpose, existing_specs)
    
    result = {
        "purpose": purpose,
        "proposed_type": spec_type,
        "analysis": analysis,
        "existing_specs": {
            "active_count": len(existing_specs["active"]),
            "implemented_count": len(existing_specs["implemented"]),
            "conflicts": analysis["conflicts"],
            "related": analysis["related"]
        }
    }
    
    print(json.dumps(result, indent=2))
    sys.exit(0 if analysis["status"] == "ok" else 1)
