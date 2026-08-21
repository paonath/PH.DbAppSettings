#!/usr/bin/env python3
"""
Package AI Agent Skill into distributable .skill file (zip archive).

Creates a .skill file (zip archive) with validated directory structure for distribution
via .agents/skills/, ~/.agents/skills/, or community repositories.

For use with AI coding assistants.

Usage: python scripts/package_skill.py <skill-path> [output-dir]
Output: skill-name.skill file (zip archive) + validation report

Example:
    python scripts/package_skill.py .agents/skills/skill-creator/
    python scripts/package_skill.py .agents/skills/skill-creator/ ./dist/
"""

import json
import sys
import zipfile
from pathlib import Path
from typing import Dict, Tuple, List
import subprocess


class SkillPackager:
    """Packages AI Agent Skill into distributable .skill file."""
    
    def __init__(self, skill_path: str, output_dir: str = None):
        """Initialize packager."""
        self.skill_path = Path(skill_path)
        self.output_dir = Path(output_dir) if output_dir else Path.cwd() / "dist"
        self.skill_name = self.skill_path.name
        self.output_file = self.output_dir / f"{self.skill_name}.skill"
        self.validation_result = {}
        self.errors: List[str] = []
        self.warnings: List[str] = []
    
    def package(self) -> Dict:
        """Package skill into .skill file."""
        # Step 1: Validate skill
        self._validate_skill()
        if self.errors:
            return {
                "status": "error",
                "phase": "validation",
                "errors": self.errors,
                "warnings": self.warnings
            }
        
        # Step 2: Prepare output directory
        self._prepare_output_dir()
        
        # Step 3: Create zip archive
        try:
            self._create_archive()
        except Exception as e:
            self.errors.append(f"Failed to create archive: {e}")
            return {
                "status": "error",
                "phase": "packaging",
                "errors": self.errors
            }
        
        # Step 4: Verify archive
        if not self._verify_archive():
            return {
                "status": "error",
                "phase": "verification",
                "errors": self.errors
            }
        
        return self._success_result()
    
    def _validate_skill(self) -> None:
        """Validate skill before packaging."""
        if not self.skill_path.exists():
            self.errors.append(f"Skill path not found: {self.skill_path}")
            return
        
        skill_md = self.skill_path / "SKILL.md"
        if not skill_md.exists():
            self.errors.append("SKILL.md not found")
            return
        
        # Run validation script
        try:
            import subprocess
            validate_script = self.skill_path / "scripts" / "validate_skill.py"
            if not validate_script.exists():
                # Try to find validate_skill in current directory
                validate_script = Path(__file__).parent / "validate_skill.py"
            
            if validate_script.exists():
                result = subprocess.run(
                    [sys.executable, str(validate_script), str(self.skill_path)],
                    capture_output=True,
                    text=True,
                    timeout=30
                )
                
                try:
                    self.validation_result = json.loads(result.stdout)
                    if self.validation_result.get("status") == "fail":
                        self.errors.extend(self.validation_result.get("errors", []))
                        self.warnings.extend(self.validation_result.get("warnings", []))
                except json.JSONDecodeError:
                    self.warnings.append("Could not parse validation output")
            else:
                # Perform basic validation
                self._basic_validate()
        
        except Exception as e:
            self.warnings.append(f"Validation check failed: {e}")
    
    def _basic_validate(self) -> None:
        """Perform basic validation without validation script."""
        skill_md = self.skill_path / "SKILL.md"
        content = skill_md.read_text(encoding='utf-8')
        
        if not content.startswith('---'):
            self.errors.append("SKILL.md must start with YAML frontmatter")
            return
        
        if '---' not in content[3:]:
            self.errors.append("SKILL.md frontmatter not properly closed")
            return
    
    def _prepare_output_dir(self) -> None:
        """Create output directory if needed."""
        self.output_dir.mkdir(parents=True, exist_ok=True)
    
    def _create_archive(self) -> None:
        """Create .skill zip archive."""
        if self.output_file.exists():
            self.output_file.unlink()
        
        with zipfile.ZipFile(self.output_file, 'w', zipfile.ZIP_DEFLATED) as zf:
            # Add all files from skill directory
            for file_path in self.skill_path.rglob('*'):
                if file_path.is_file():
                    # Calculate archive name (relative to skill directory)
                    arcname = file_path.relative_to(self.skill_path.parent)
                    zf.write(file_path, arcname)
    
    def _verify_archive(self) -> bool:
        """Verify archive integrity and content."""
        if not self.output_file.exists():
            self.errors.append(f"Archive file not created: {self.output_file}")
            return False
        
        try:
            with zipfile.ZipFile(self.output_file, 'r') as zf:
                # Check for SKILL.md
                skill_md_found = False
                files = zf.namelist()
                
                for file_name in files:
                    if file_name.endswith('SKILL.md'):
                        skill_md_found = True
                        break
                
                if not skill_md_found:
                    self.errors.append("SKILL.md not found in archive")
                    return False
                
                # Verify archive can be read
                zf.testzip()
        
        except Exception as e:
            self.errors.append(f"Archive verification failed: {e}")
            return False
        
        return True
    
    def _success_result(self) -> Dict:
        """Build success result."""
        archive_size = self.output_file.stat().st_size
        
        # Get file listing
        file_listing = {}
        with zipfile.ZipFile(self.output_file, 'r') as zf:
            for info in zf.filelist:
                if not info.filename.endswith('/'):
                    file_listing[info.filename] = info.file_size
        
        return {
            "status": "success",
            "skill_name": self.skill_name,
            "output_file": str(self.output_file),
            "archive_size": f"{archive_size / 1024:.1f} KB",
            "file_count": len([f for f in file_listing.keys() if not f.endswith('/')]),
            "files": file_listing,
            "validation": self.validation_result,
            "warnings": self.warnings,
            "next_steps": [
                f"Copy to project: .agents/skills/{self.skill_name}/",
                f"Or: ~/.agents/skills/{self.skill_name}/",
                f"Or: Submit to relevant community registries"
            ]
        }


def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        error_result = {
            "status": "error",
            "error": "Usage: python package_skill.py <skill-path> [output-dir]",
            "examples": [
                "python package_skill.py .agents/skills/skill-creator/",
                "python package_skill.py .agents/skills/skill-creator/ ./dist/"
            ]
        }
        print(json.dumps(error_result, indent=2))
        sys.exit(1)
    
    skill_path = sys.argv[1]
    output_dir = sys.argv[2] if len(sys.argv) > 2 else None
    
    packager = SkillPackager(skill_path, output_dir)
    result = packager.package()
    
    print(json.dumps(result, indent=2))
    sys.exit(0 if result["status"] == "success" else 1)


if __name__ == "__main__":
    main()
