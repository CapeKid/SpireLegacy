"""Evidence integrity checks: corrupted receipts must not become public results."""
import contextlib
import copy
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import subprocess
import sys
import unittest

spec = importlib.util.spec_from_file_location('balance_export', Path(__file__).with_name('export-balance-pacing.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class EvidenceChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root/'input'; self.source.mkdir()
        rid = 'a'*32
        record = dict(request=dict(requestId=rid), response=dict(requestId=rid, ok=True,
            metadata=dict(requestId=rid, dllSha256='b'*64, sourceSha='c'*40,
                          game='fixture', mod='fixture', profile=str(self.root),
                          mods=[dict(id='BaseLib', version='fixture'), dict(id='SpireLegacy', version='fixture')]),
            result=dict(action='card-reward', detail=dict(offers=[dict(id='test')], picked='test'), state=dict(act=1, floor=1))))
        self.campaign = dict(cls='knight', tier=0, policy='planned', rewardPolicy='defensive', ascension=0, seed='fixture', traits=[], status='finished',
                             interventions=0, evidence=[record], lastState=dict(act=1, floor=2, hp=0, wins=0, crowns=10, lastEarned=10))
        self.pacing = dict(cls='knight', status='finished', evidence=[copy.deepcopy(record)], purchases=[],
                           runs=[dict(run=i, seed=str(i), status='finished', settledId=str(i),
                                      crownsBefore=(i-1)*10, crownsAfter=i*10, earned=10,
                                      lastState=dict(floor=2)) for i in range(1,13)])

    def run_export(self):
        for name, report in [('balance-campaign-balance146-fixture.json', self.campaign),
                             ('pacing-campaign-balance146-fixture.json', self.pacing)]:
            (self.source/name).write_text(json.dumps(report), encoding='utf-8')
        if hasattr(self,'extension'):
            (self.source/'pacing-campaign-balance146-extended.json').write_text(json.dumps(self.extension),encoding='utf-8')
        with contextlib.redirect_stdout(io.StringIO()):
            module.export(self.source, self.root/'output',getattr(self,'stopped_reports',[]))

    def test_valid_receipts(self):
        self.run_export()
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()), 13)

    def test_running_worker_rejected(self):
        self.campaign['status'] = 'running'
        with self.assertRaises(AssertionError): self.run_export()

    def test_explicitly_stopped_receipt_excluded_and_labelled(self):
        self.extension=copy.deepcopy(self.pacing)
        self.pacing['status']='running'
        self.stopped_reports=['pacing-campaign-balance146-fixture.json']
        self.run_export()
        rows=json.loads((self.root/'output/manifest.json').read_text())
        stopped=next(r for r in rows if r['trial']=='pacing-campaign-balance146-fixture')
        self.assertEqual(stopped['status'],'interrupted')
        self.assertFalse(stopped['included'])
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()),13)

    def test_unknown_stopped_receipt_rejected(self):
        self.stopped_reports=['missing.json']
        with self.assertRaises(AssertionError):self.run_export()

    def test_duplicate_command_rejected(self):
        self.campaign['evidence'].append(copy.deepcopy(self.campaign['evidence'][0]))
        with self.assertRaises(AssertionError): self.run_export()

    def test_mismatched_reply_rejected(self):
        self.campaign['evidence'][0]['response']['requestId'] = 'd'*32
        with self.assertRaises(AssertionError): self.run_export()

    def test_duplicate_settlement_rejected(self):
        self.pacing['runs'][1]['settledId'] = '1'
        with self.assertRaises(AssertionError): self.run_export()

    def test_crown_creation_rejected(self):
        self.pacing['runs'][1]['crownsAfter'] += 1
        with self.assertRaises(AssertionError): self.run_export()

    def extend(self):
        self.extension=copy.deepcopy(self.pacing)
        self.extension['targetReturns']=24
        self.extension['batches']=[dict(priorReport='pacing-campaign-balance146-fixture.json',afterRun=12,endRun=24)]
        for n in range(13,25):
            self.extension['runs'].append(dict(run=n,seed=str(n),status='finished',settledId=str(n),
                crownsBefore=(n-1)*10,crownsAfter=n*10,earned=10,lastState=dict(floor=2)))

    def test_extended_prefix_counted_once(self):
        self.extend();self.run_export()
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()),25)
        manifest=json.loads((self.root/'output/manifest.json').read_text())
        old=next(r for r in manifest if r['trial']=='pacing-campaign-balance146-fixture')
        self.assertFalse(old['included'])

    def test_changed_completed_prefix_rejected(self):
        self.extend();self.extension['runs'][0]['earned']=99
        with self.assertRaises(AssertionError):self.run_export()

    def test_optimized_cli_rejected(self):
        result=subprocess.run([sys.executable,'-O',str(Path(__file__).with_name('export-balance-pacing.py')),
                               str(self.source),str(self.root/'output')],capture_output=True,text=True)
        self.assertNotEqual(result.returncode,0)
        self.assertIn('require assertions',result.stderr)
        self.assertFalse((self.root/'output').exists())


if __name__ == '__main__': unittest.main()
