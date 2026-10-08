"""Exercise real launcher discovery using isolated Steam-library fixtures."""
import tempfile,unittest
from pathlib import Path
from launch_linux import plan

class Discovery(unittest.TestCase):
    def setUp(self):
        fixture_root=Path(__file__).resolve().parents[1]/'build';fixture_root.mkdir(exist_ok=True)
        self.temp=tempfile.TemporaryDirectory(dir=fixture_root);self.addCleanup(self.temp.cleanup)
        self.home=Path(self.temp.name)/'home';self.steam=self.home/'.local/share/Steam';self.sd=Path(self.temp.name)/'SD card/Steam Library'
        (self.steam/'steamapps').mkdir(parents=True);(self.sd/'steamapps/common').mkdir(parents=True)
        (self.steam/'steamapps/libraryfolders.vdf').write_text('"libraryfolders" { "1" { "path" "'+self.sd.as_posix()+'" } }')
        self.host=self.sd/'steamapps/common/SlayTheSpire';self.game=self.sd/'steamapps/common/Rogue Legacy 2';self.package=self.host/'HeirOfTheSpire'
        self.package.mkdir(parents=True);(self.host/'jre/bin').mkdir(parents=True);(self.host/'mods').mkdir()
        (self.game/'Rogue Legacy 2_Data').mkdir(parents=True)
        for id,name in [(646570,'SlayTheSpire'),(1253920,'Rogue Legacy 2')]:
            (self.sd/'steamapps'/f'appmanifest_{id}.acf').write_text('"AppState" { "installdir" "'+name+'" }')
        (self.host/'desktop-1.0.jar').touch();(self.host/'jre/bin/java').write_bytes(b'\x7fELF fixture')
        (self.game/'Rogue Legacy 2_Data/resources.assets').touch()
        for file in [self.package/'ModTheSpire.jar',self.host/'mods/HeirOfTheSpire.jar',self.host/'mods/BaseMod.jar']:file.touch()
    def test_sd_library_and_spaces(self):
        value=plan(self.package,self.home,{})
        self.assertEqual(value['host'],str(self.host));self.assertEqual(value['game'],str(self.game));self.assertIn(str(self.game),value['reader']);self.assertEqual(value['env']['HEIR_DATA_DIR'],str(self.home/'.local/share/HeirOfTheSpire'))
    def test_data_override(self):
        self.assertEqual(plan(self.package,self.home,{'HEIR_DATA_DIR':'/custom family'})['data'],str(Path('/custom family')))
        self.assertEqual(plan(self.package,self.home,{'XDG_DATA_HOME':'/xdg'})['data'],str(Path('/xdg/HeirOfTheSpire')))
    def test_explicit_games_without_steam(self):
        self.assertEqual(plan(self.package,Path('/missing'),{},self.host,self.game)['game'],str(self.game))
    def test_windows_runtime_rejected(self):
        (self.host/'jre/bin/java').write_bytes(b'MZ windows')
        with self.assertRaisesRegex(RuntimeError,'Windows runtime'):plan(self.package,self.home,{})
    def test_missing_mod_rejected(self):
        (self.host/'mods/BaseMod.jar').unlink()
        with self.assertRaisesRegex(RuntimeError,'Required mod'):plan(self.package,self.home,{})
    def test_missing_rl2_rejected(self):
        (self.game/'Rogue Legacy 2_Data/resources.assets').unlink()
        with self.assertRaisesRegex(RuntimeError,'Rogue Legacy 2'):plan(self.package,self.home,{})
    def test_no_library_rejected(self):
        with self.assertRaisesRegex(RuntimeError,'Steam libraries'):plan(self.package,Path('/missing'),{})

if __name__=='__main__':unittest.main()
