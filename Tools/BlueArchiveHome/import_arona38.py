"""Import the current user-provided Spine 3.8 Arona project export."""
from pathlib import Path
import runpy
runpy.run_path(str(Path(__file__).with_name('import_arona_project.py')), run_name='__main__')
