"""Check discovery precedence and manifest-free Deck installations."""
import tempfile, unittest, io
from contextlib import redirect_stdout
from pathlib import Path
from unittest.mock import patch
import load_rl

class Discovery(unittest.TestCase):
    def setUp(self):
        build = Path(__file__).resolve().parents[1] / 'build'
        build.mkdir(exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(dir=build)
        self.addCleanup(self.temp.cleanup)
        self.home = Path(self.temp.name) / 'home'
        self.deck = self.home / '.local/share/Steam/steamapps/common/Rogue Legacy 2'
        self.content(self.deck)
        self.stack = patch('load_rl.Path.home', return_value=self.home)
        self.stack.start(); self.addCleanup(self.stack.stop)
        self.fallback = patch('load_rl.deck_install_candidates', return_value=[self.deck])
        self.fallback.start(); self.addCleanup(self.fallback.stop)
        self.roots = patch.dict(load_rl.os.environ, {'PROGRAMFILES(X86)': str(self.home / 'absent')})
        self.roots.start(); self.addCleanup(self.roots.stop)
        self.registry = patch.dict('sys.modules', {'winreg': None})
        self.registry.start(); self.addCleanup(self.registry.stop)

    def content(self, root):
        (root / 'Rogue Legacy 2_Data').mkdir(parents=True)
        (root / 'Rogue Legacy 2_Data/resources.assets').touch()

    def test_no_manifest_or_game_launch_needed(self):
        self.assertEqual(load_rl.find_game(), self.deck)

    def test_explicit_override_wins(self):
        root = self.home / 'custom copy'; self.content(root)
        self.assertEqual(load_rl.find_game(root), root)

    def test_invalid_override_is_not_silently_ignored(self):
        with self.assertRaisesRegex(RuntimeError, 'supplied'):
            load_rl.find_game(self.home / 'missing')

    def test_manifest_install_wins(self):
        steam = self.home / '.local/share/Steam'
        root = steam / 'steamapps/common/Other copy'; self.content(root)
        (steam / 'steamapps/appmanifest_1253920.acf').write_text('"installdir" "Other copy"')
        self.assertEqual(load_rl.find_game(), root)

    def test_fallback_requires_installed_content(self):
        (self.deck / 'Rogue Legacy 2_Data/resources.assets').unlink()
        with self.assertRaisesRegex(RuntimeError, 'Rogue Legacy 2 is required'):
            load_rl.find_game()

    def test_platform_specific_data_folder(self):
        source = self.deck / 'Rogue Legacy 2_Data'
        renamed = self.deck / 'Rogue Legacy 2 Linux_Data'
        source.rename(renamed)
        self.assertEqual(load_rl.find_game(), self.deck)
        self.assertEqual(load_rl.content_directory(self.deck), renamed)

    def test_ambiguous_content_is_rejected(self):
        original = self.deck / 'Rogue Legacy 2_Data'
        original.rename(self.deck / 'First_Data')
        extra = self.deck / 'Second_Data'; extra.mkdir()
        (extra / 'resources.assets').touch()
        with self.assertRaisesRegex(RuntimeError, 'Multiple Unity content folders'):
            load_rl.find_game()

    def test_missing_content_diagnostic(self):
        (self.deck / 'Rogue Legacy 2_Data/resources.assets').unlink()
        output = io.StringIO()
        with redirect_stdout(output):
            with self.assertRaises(RuntimeError): load_rl.find_game()
        self.assertIn(str(self.deck), output.getvalue())
        self.assertIn('Folder exists', output.getvalue())

class DeckPaths(unittest.TestCase):
    def test_native_path(self):
        with patch('load_rl.os.name', 'posix'), patch('load_rl.Path') as path:
            load_rl.deck_install_candidates()
            path.assert_called_once_with('/home/deck/.local/share/Steam/steamapps/common/Rogue Legacy 2')

    def test_proton_path(self):
        with patch('load_rl.os.name', 'nt'), patch('load_rl.Path') as path:
            load_rl.deck_install_candidates()
            path.assert_called_once_with('Z:\\home\\deck\\.local\\share\\Steam\\steamapps\\common\\Rogue Legacy 2')

if __name__ == '__main__': unittest.main()
