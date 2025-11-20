## Running the script:
## py engine_version_manager.py

import os
import subprocess
import shutil
import json
from pathlib import Path
import time
import re
from enum import Enum

class BuildType(Enum):
    DEBUG = '_D'
    RELEASE = '_R'

class EngineVersionManager:
    def __init__(self):
        self.build_dirs = {
            BuildType.DEBUG: Path('build/engines/Debug'),
            BuildType.RELEASE: Path('build/engines/Release')
        }
        self.storage_dir = Path('stored_engines')
        self.engine_info_file = self.storage_dir / 'engine_info.json'
        self.stored_engines = self.load_stored_engine_info()
        
        # Create storage directory if it doesn't exist
        self.storage_dir.mkdir(parents=True, exist_ok=True)
    
    def load_stored_engine_info(self):
        """Load information about previously stored engines"""
        if self.engine_info_file.exists():
            with open(self.engine_info_file, 'r') as f:
                return json.load(f)
        return {}
    
    def save_stored_engine_info(self):
        """Save current engine information to file"""
        with open(self.engine_info_file, 'w') as f:
            json.dump(self.stored_engines, f, indent=2)
    
    def parse_engine_info(self, name_string):
        """Parse engine name and version from UCI id string"""
        # Expected format: "EngineName X.Y" where X.Y is version number
        # Regex: (.*?) matches 'smallest' string of chars up till the \s which is
        # white space. \s+ at least one white space. \d+ one or more digits,
        # \. match . character. $ means it must be at the end of the line
        match = re.match(r"(.*?)\s+(\d+\.\d+)$", name_string.strip())
        if match:
            engine_name = match.group(1)
            version = match.group(2)
            return engine_name, version
        return None, None

    def format_filename(self, engine_name, version, build_type):
        """Format the filename with build type suffix"""
        # Convert version from "0.2" to "0_2"
        version_str = version.replace('.', '_')
        return f"{engine_name}_v{version_str}{build_type.value}.exe"
    
    def get_engine_info(self, engine_path):
        """Get engine name and version using UCI protocol"""
        try:
            # Start the engine process
            process = subprocess.Popen(
                str(engine_path),
                stdin=subprocess.PIPE,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True
            )
            
            # Send UCI command
            process.stdin.write('uci\n')
            process.stdin.flush()
            
            engine_name = None
            engine_author = None
            
            # Read output until we get 'uciok'
            while True:
                line = process.stdout.readline().strip()
                if line.startswith('id name'):
                    name_string = line.replace('id name', '').strip()
                    engine_name, version = self.parse_engine_info(name_string)
                elif line.startswith('id author'):
                    engine_author = line.replace('id author', '').strip()
                elif line == 'uciok':
                    break
                    
            # Clean up
            process.stdin.write('quit\n')
            process.stdin.flush()
            process.terminate()
            process.wait(timeout=1)
            
            if engine_name and version:
                return {
                    'name': engine_name,
                    'version': version,
                    'author': engine_author
                }
            return None
            
        except Exception as e:
            print(f"Error getting engine info from {engine_path}: {e}")
            return None
    
    def store_new_engines(self):
        """Check build directories for new engines and store them if new"""
        print("Checking for new engines...")
        
        # Check each build type directory
        for build_type, build_dir in self.build_dirs.items():
            print(f"\nChecking {build_dir} directory...")
            
            if not build_dir.exists():
                print(f"Directory {build_dir} does not exist - skipping")
                continue
            
            # Check each executable in build directory
            for file in build_dir.glob('*.exe'):
                print(f"\nChecking {file.name}...")
                
                # Get engine info
                engine_info = self.get_engine_info(file)
                if not engine_info:
                    print(f"Skipping {file.name} - could not get engine info")
                    continue
                
                engine_name = engine_info['name']
                version = engine_info['version']
                
                # Create unique ID that includes build type
                full_id = f"{engine_name} {version} {build_type.value}"
                
                # Check if we've seen this exact version and build type before
                if full_id in self.stored_engines:
                    print(f"Engine {full_id} already stored")
                    continue
                
                # This is a new engine version - store it
                new_filename = self.format_filename(engine_name, version, build_type)
                new_path = self.storage_dir / new_filename
                
                print(f"Storing new engine: {new_filename}")
                shutil.copy2(file, new_path)
                
                # Update stored engine info
                self.stored_engines[full_id] = {
                    'filename': new_filename,
                    'author': engine_info['author'],
                    'build_type': build_type.value,
                    'stored_date': time.strftime("%Y-%m-%d %H:%M:%S")
                }
                
                self.save_stored_engine_info()
                print(f"Stored {engine_name} version {version} ({build_type.value})")
    
    def list_stored_engines(self):
        """Print information about all stored engines."""
        print("\nStored Engines:")
        print("=" * 50)
        
        # Group engines by name and version
        grouped_engines = {}
        for full_id, info in self.stored_engines.items():
            name_version = full_id.rsplit(' ', 1)[0]  # Remove build type
            if name_version not in grouped_engines:
                grouped_engines[name_version] = []
            grouped_engines[name_version].append((full_id, info))
        
        # Print grouped information
        for name_version, builds in grouped_engines.items():
            print(f"\n{name_version}:")
            for full_id, info in builds:
                print(f"  Build: {info['build_type']}")
                print(f"  File: {info['filename']}")
                print(f"  Author: {info['author']}")
                print(f"  Stored: {info['stored_date']}")

if __name__ == '__main__':
    manager = EngineVersionManager()
    manager.store_new_engines()
    manager.list_stored_engines()