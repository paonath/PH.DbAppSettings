#!/usr/bin/env python3
"""
Validate AI Agent Skill compliance with Agent Skills standard.

Checks YAML frontmatter, naming conventions, file organization, and content structure.
For use with AI Agent terminal execution.

Usage: python scripts/validate_skill.py <skill-path>
Output: JSON report with validation results

Example:
    python scripts/validate_skill.py .agents/skills/skill-creator/
"""

import json
import sys
from pathlib import Path
from typing import Dict, List, Tuple
import re

try:
    import yaml
except ImportError:
    print(json.dumps({"error": "PyYAML not installed. Run: pip install pyyaml"}))
    sys.exit(1)


class SkillValidator:
    """Validates AI Agent Skill compliance."""
    
    MAX_NAME_LENGTH = 64
    MAX_DESCRIPTION_LENGTH = 1024
    MAX_BODY_LINES = 500
    
    def __init__(self, skill_path: str):
        """Initialize validator with skill directory path."""
        self.skill_path = Path(skill_path)
        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.checks: Dict[str, bool] = {}
    
    def validate(self) -> Dict:
        """Run all validation checks."""
        if not self.skill_path.exists():
            return {
                "status": "error",
                "error": f"Skill path not found: {self.skill_path}",
                "errors": [],
                "warnings": []
            }
        
        self._validate_skill_md_exists()
        self._validate_frontmatter()
        self._validate_file_organization()
        self._validate_content_quality()
        self._validate_resources()
        
        status = "pass" if not self.errors else "fail"
        
        return {
            "status": status,
            "skill": str(self.skill_path),
            "checks": self.checks,
            "errors": self.errors,
            "warnings": self.warnings,
            "summary": f"{len(self.errors)} errors, {len(self.warnings)} warnings"
        }
    
    def _validate_skill_md_exists(self) -> None:
        """Check if SKILL.md exists."""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            self.errors.append("SKILL.md not found in skill directory")
            self.checks["skill_md_exists"] = False
        else:
            self.checks["skill_md_exists"] = True
    
    def _validate_frontmatter(self) -> None:
        """Validate YAML frontmatter."""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        try:
            content = skill_md.read_text(encoding='utf-8')
            
            # Extract frontmatter
            if not content.startswith('---'):
                self.errors.append("SKILL.md must start with YAML frontmatter (---)")
                self.checks["frontmatter_format"] = False
                return
            
            end_marker = content.find('---', 3)
            if end_marker == -1:
                self.errors.append("SKILL.md frontmatter not properly closed")
                self.checks["frontmatter_format"] = False
                return
            
            frontmatter_str = content[3:end_marker].strip()
            
            # Parse YAML
            try:
                frontmatter = yaml.safe_load(frontmatter_str)
            except yaml.YAMLError as e:
                self.errors.append(f"Invalid YAML frontmatter: {e}")
                self.checks["frontmatter_format"] = False
                return
            
            self.checks["frontmatter_format"] = True
            
            # Validate required fields
            if not isinstance(frontmatter, dict):
                self.errors.append("Frontmatter must be a YAML dictionary")
                self.checks["frontmatter_required_fields"] = False
                return
            
            # Check for exactly name and description
            allowed_fields = {'name', 'description'}
            actual_fields = set(frontmatter.keys())
            
            if 'name' not in frontmatter:
                self.errors.append("Missing required field: 'name' in frontmatter")
                self.checks["frontmatter_required_fields"] = False
            elif 'description' not in frontmatter:
                self.errors.append("Missing required field: 'description' in frontmatter")
                self.checks["frontmatter_required_fields"] = False
            else:
                # Validate name
                name = frontmatter.get('name', '')
                if not re.match(r'^[a-z0-9]+(-[a-z0-9]+)*$', name):
                    self.errors.append(
                        f"Skill name '{name}' must be lowercase with hyphens only"
                    )
                    self.checks["skill_name_format"] = False
                elif len(name) > self.MAX_NAME_LENGTH:
                    self.errors.append(
                        f"Skill name exceeds {self.MAX_NAME_LENGTH} characters"
                    )
                    self.checks["skill_name_format"] = False
                else:
                    self.checks["skill_name_format"] = True
                
                # Validate description
                description = frontmatter.get('description', '')
                if len(description) > self.MAX_DESCRIPTION_LENGTH:
                    self.errors.append(
                        f"Description exceeds {self.MAX_DESCRIPTION_LENGTH} characters "
                        f"({len(description)} provided)"
                    )
                    self.checks["description_length"] = False
                elif len(description.split()) < 5:
                    self.warnings.append("Description is very short; consider adding use-case triggers")
                    self.checks["description_length"] = True
                else:
                    self.checks["description_length"] = True
                
                self.checks["frontmatter_required_fields"] = True
            
            # Check for extra fields
            extra_fields = actual_fields - allowed_fields
            if extra_fields:
                self.warnings.append(
                    f"Extra fields in frontmatter (allowed: name, description only): "
                    f"{', '.join(extra_fields)}"
                )
        
        except Exception as e:
            self.errors.append(f"Error validating frontmatter: {e}")
            self.checks["frontmatter_validation"] = False
    
    def _validate_file_organization(self) -> None:
        """Validate directory structure and file organization."""
        # Check for subdirectories
        subdirs_found = [d.name for d in self.skill_path.iterdir() if d.is_dir()]
        
        # Allowed subdirectories
        allowed_subdirs = {'scripts', 'examples', 'references'}
        invalid_subdirs = set(subdirs_found) - allowed_subdirs
        
        if invalid_subdirs:
            self.warnings.append(
                f"Unexpected subdirectories: {', '.join(invalid_subdirs)}. "
                f"Use only: scripts/, examples/, references/"
            )
        
        self.checks["file_organization"] = True
        
        # Validate scripts/
        scripts_dir = self.skill_path / "scripts"
        if scripts_dir.exists():
            scripts = list(scripts_dir.glob("*"))
            if not scripts:
                self.warnings.append("scripts/ directory exists but is empty")
            else:
                self.checks["scripts_present"] = True
        
        # Validate references/
        refs_dir = self.skill_path / "references"
        if refs_dir.exists():
            # Check for nested directories
            nested = [d.name for d in refs_dir.iterdir() if d.is_dir()]
            if nested:
                self.errors.append(
                    f"references/ must be flat (one level). Found nested: {', '.join(nested)}"
                )
                self.checks["references_flat"] = False
            else:
                self.checks["references_flat"] = True
            
            # Check for non-markdown files
            md_files = list(refs_dir.glob("*.md"))
            other_files = [f.name for f in refs_dir.glob("*") if f.is_file() and not f.name.endswith('.md')]
            if other_files:
                self.warnings.append(
                    f"references/ should contain only .md files: {', '.join(other_files)}"
                )
    
    def _validate_content_quality(self) -> None:
        """Validate SKILL.md content quality."""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        content = skill_md.read_text(encoding='utf-8')
        
        # Extract body (after frontmatter)
        end_marker = content.find('---', 3)
        if end_marker == -1:
            return
        
        body = content[end_marker + 3:].strip()
        body_lines = body.split('\n')
        
        # Check body length
        if len(body_lines) > self.MAX_BODY_LINES:
            self.warnings.append(
                f"SKILL.md body exceeds {self.MAX_BODY_LINES} lines "
                f"({len(body_lines)} lines). Consider splitting into references/"
            )
            self.checks["body_length"] = False
        else:
            self.checks["body_length"] = True
        
        # Check for imperative form usage
        imperative_keywords = ['use', 'generate', 'create', 'run', 'execute', 'validate', 'check']
        has_imperative = any(
            f"## {kw}" in body.lower() or f" {kw} " in body.lower()
            for kw in imperative_keywords
        )
        
        if not has_imperative:
            self.warnings.append(
                "SKILL.md body should use imperative form (Use, Generate, Run, Validate, etc.)"
            )
        
        # Check for inline examples
        if "```" not in body:
            self.warnings.append("SKILL.md body should include code examples (```...```)")
        
        self.checks["content_quality"] = True
    
    def _validate_resources(self) -> None:
        """Validate resource file references and structure."""
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            return
        
        content = skill_md.read_text(encoding='utf-8')
        
        # Find all markdown links
        link_pattern = r'\[([^\]]+)\]\(([^)]+)\)'
        links = re.findall(link_pattern, content)
        
        missing_files = []
        for link_text, link_path in links:
            if link_path.startswith(('http://', 'https://', '#')):
                continue  # External links or anchors
            
            # Resolve relative path
            target_path = self.skill_path / link_path
            if not target_path.exists():
                missing_files.append(link_path)
        
        if missing_files:
            self.errors.append(
                f"Broken links in SKILL.md (files not found): {', '.join(missing_files)}"
            )
            self.checks["resource_links"] = False
        else:
            self.checks["resource_links"] = True


def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        error_result = {
            "status": "error",
            "error": "Usage: python validate_skill.py <skill-path>",
            "example": "python validate_skill.py .agents/skills/skill-creator/"
        }
        print(json.dumps(error_result, indent=2))
        sys.exit(1)
    
    skill_path = sys.argv[1]
    validator = SkillValidator(skill_path)
    result = validator.validate()
    
    print(json.dumps(result, indent=2))
    sys.exit(0 if result["status"] == "pass" else 1)


if __name__ == "__main__":
    main()
