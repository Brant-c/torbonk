"""Reuse the verified wardrobe conversion with helmet palette support."""
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
code=(ROOT/'scripts/convert_empire_wardrobe.py').read_text()
code=code.replace("materials=materials.replace(\"prop='rotationMap'\"", "materials=materials.replace(\"['chest','leg','hand','boot','waist']\",\"['chest','leg','hand','boot','waist','helmet']\")\nmaterials=materials.replace(\"prop='rotationMap'\"")
exec(compile(code,'collectible-heads','exec'))
