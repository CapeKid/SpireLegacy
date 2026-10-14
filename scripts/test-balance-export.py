"""Evidence integrity checks: corrupted receipts must not become public results."""
import contextlib
import copy
import importlib.util
import io
import json
from pathlib import Path
import tempfile
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
        with contextlib.redirect_stdout(io.StringIO()):
            module.export(self.source, self.root/'output')

    def test_valid_receipts(self):
        self.run_export()
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()), 13)

    def test_running_worker_rejected(self):
        self.campaign['status'] = 'running'
        with self.assertRaises(AssertionError): self.run_export()

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


if __name__ == '__main__': unittest.main()
