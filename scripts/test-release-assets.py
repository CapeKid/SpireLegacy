"""Provenance checks: declarations must not masquerade as independent ownership proof."""
import hashlib,tempfile,unittest
from pathlib import Path
from release_assets import inventory,TABLES,GUIDES

class InventoryChecks(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix='release-assets-',dir=Path(__file__).resolve().parents[1]/'private');self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        for name in set(GUIDES.values()):
            p=self.root/name;p.parent.mkdir(parents=True,exist_ok=True);p.write_text('Original authoring record')
        self.path=self.root/'src/main/resources/heir/test.svg';self.path.parent.mkdir(parents=True);self.path.write_bytes(b'fixture-only')
        self.tables={key:[] for key in TABLES};self.tables['assets']=[dict(output='ExtractedGameSprite.png')]
        self.row=dict(id='test',path='heir/test.svg',sha256=hashlib.sha256(self.path.read_bytes()).hexdigest(),author='CapeKid',license='MIT',provenance='Original vector geometry')
        self.tables['icon_art']=[self.row]
    def test_inventory_matches_bytes_and_limits(self):
        r=inventory(self.root,self.tables);self.assertEqual(r['assetCount'],1);self.assertEqual(r['assets'][0]['sha256'],self.row['sha256']);self.assertTrue(any('not independent' in x for x in r['limits']))
    def test_unknown_license_rejected(self):
        self.row['license']='unreviewed'
        with self.assertRaisesRegex(ValueError,'license'):inventory(self.root,self.tables)
    def test_missing_record_rejected(self):
        self.row.pop('provenance')
        with self.assertRaisesRegex(ValueError,'provenance'):inventory(self.root,self.tables)
    def test_changed_bytes_rejected(self):
        self.path.write_bytes(b'changed')
        with self.assertRaisesRegex(ValueError,'hash'):inventory(self.root,self.tables)
    def test_legacy_asset_name_rejected(self):
        self.row['path']='heir/ExtractedGameSprite.png'
        with self.assertRaisesRegex(ValueError,'extracted'):inventory(self.root,self.tables)
    def test_style_references_disclosed(self):
        self.row['provenance']='Generated with Rogue Legacy style reference'
        self.assertTrue(inventory(self.root,self.tables)['assets'][0]['gameStyleReferenceDeclared'])
    def test_nested_death_and_outline_covered(self):
        for key in ('death','outline'):
            p=self.path.with_name(key+'.svg');p.write_bytes(key.encode())
            if key=='death':self.row[key]=dict(path='heir/'+p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest())
            else:self.row.update(outline='heir/'+p.name,outlineSha256=hashlib.sha256(p.read_bytes()).hexdigest())
        self.assertEqual(inventory(self.root,self.tables)['assetCount'],3)

if __name__=='__main__':unittest.main()
