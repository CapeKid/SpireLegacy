"""Evidence integrity checks: corrupted receipts must not become public results."""
import contextlib
import copy
import hashlib
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
        if hasattr(self,'checkpoint_report'):
            (self.source/'pacing-campaign-balance146-checkpoint.json').write_text(json.dumps(self.checkpoint_report),encoding='utf-8')
        with contextlib.redirect_stdout(io.StringIO()):
            module.export(self.source, self.root/'output',getattr(self,'stopped_reports',[]))

    def test_valid_receipts(self):
        self.run_export()
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()), 13)

    def test_native_task_id_spellings_and_fatal_crash_counted(self):
        (self.root/'errors.log').write_text('ERROR: Invalid Task ID\nERROR: InvalidTaskId\nFatal error. 0xC0000005\n',encoding='utf-8')
        self.run_export()
        row=json.loads((self.root/'output/manifest.json').read_text())[0]
        self.assertEqual(row['logMatches']['errors.log']['InvalidTaskId'],2)
        self.assertEqual(row['logMatches']['errors.log']['Fatal error'],1)

    def with_start(self):
        start=copy.deepcopy(self.campaign['evidence'][0])
        start['request']=dict(requestId='e'*32,command='start',**{'class':'knight','traits':[], 'manorTier':0,'ascension':0,'seed':'fixture'})
        start['response']['requestId']='e'*32
        start['response']['metadata']['requestId']='e'*32
        start['response']['result']=dict(heir=dict(classId='knight',traits=[],name='PrivateName'), hp=60,maxHp=70,relics=[dict(id='RELIC.FIXTURE',title='PrivateTitle')])
        self.campaign['evidence'].insert(0,start)
        return start

    def test_start_conditions_export_ids_without_names_or_paths(self):
        self.with_start();self.run_export()
        output=(self.root/'output/initial-conditions.json').read_text()
        rows=json.loads(output)
        self.assertEqual(len(rows),1)
        self.assertTrue(rows[0]['heirVerified'])
        self.assertEqual(rows[0]['relics'],['RELIC.FIXTURE'])
        self.assertEqual((rows[0]['hp'],rows[0]['maxHp']),(60,70))
        for private in ('PrivateName','PrivateTitle',str(self.root)):self.assertNotIn(private,output)

    def test_loaded_traits_mismatch_rejected(self):
        self.with_start()['response']['result']['heir']['traits']=['antique']
        with self.assertRaises(AssertionError):self.run_export()

    def test_changed_start_seed_rejected(self):
        self.with_start()['request']['seed']='different'
        with self.assertRaises(AssertionError):self.run_export()

    def test_multiple_starts_rejected(self):
        start=self.with_start()
        self.campaign['evidence'].insert(0,copy.deepcopy(start))
        with self.assertRaises(AssertionError):self.run_export()

    def test_native_control_does_not_claim_unused_heir_verification(self):
        start=self.with_start()
        self.campaign['cls']='ironclad';start['request']['class']='ironclad'
        self.run_export()
        row=json.loads((self.root/'output/initial-conditions.json').read_text())[0]
        self.assertEqual(row['cls'],'ironclad')
        self.assertFalse(row['heirVerified'])

    def test_standalone_campaign_study_has_no_invented_trajectories(self):
        (self.source/'balance-campaign-balance146-fixture.json').write_text(json.dumps(self.campaign), encoding='utf-8')
        module.export(self.source, self.root/'output')
        self.assertEqual(len((self.root/'output/campaigns.csv').read_text().splitlines()), 2)
        self.assertEqual((self.root/'output/trajectories.csv').read_text().splitlines(),
            ['trial,cls,run,seed,floor,earned,bankBefore,bankAfter,upgrade,cost,runsSincePurchase,checkpointRecovered'])

    def test_failed_only_study_retains_manifest_without_results(self):
        self.campaign['status']='error'
        self.campaign['error']='fixture failure'
        (self.source/'balance-campaign-balance146-fixture.json').write_text(json.dumps(self.campaign), encoding='utf-8')
        module.export(self.source, self.root/'output')
        manifest=json.loads((self.root/'output/manifest.json').read_text())
        self.assertFalse(manifest[0]['included'])
        for name in ('campaigns','trajectories','cards'):
            self.assertEqual(len((self.root/f'output/{name}.csv').read_text().splitlines()), 1)
        self.assertEqual((self.root/'output/decisions.jsonl').read_text(), '')

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

    def checkpoint(self):
        from pacing_state import completed_prefix
        for run in self.pacing['runs']:run['generation']=run['run']+3
        snapshot=copy.deepcopy(self.pacing['evidence'][0])
        snapshot['request'].update(requestId='d'*32,command='lifecycle-state')
        snapshot['response']['requestId']='d'*32
        snapshot['response']['metadata']['requestId']='d'*32
        profile=dict(crowns=120,manor={},active=None,settledId='12',generation=15)
        snapshot['response']['result']=dict(profile=profile,persisted=copy.deepcopy(profile))
        self.pacing['evidence'].append(snapshot)
        self.pacing['status']='running'
        self.pacing['runs'].append(dict(run=13,status='running'))
        self.extension=completed_prefix(self.pacing,'knight')
        self.extension['checkpointExtraction'].update(priorReport='pacing-campaign-balance146-fixture.json',receiptSha256=hashlib.sha256(json.dumps(self.pacing).encode()).hexdigest())
        self.stopped_reports=['pacing-campaign-balance146-fixture.json']

    def test_verified_checkpoint_counts_only_settled_prefix(self):
        self.checkpoint();self.run_export()
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()),13)

    def test_changed_checkpoint_metadata_rejected(self):
        self.checkpoint();self.extension['checkpointExtraction']['excludedRuns']=[]
        with self.assertRaises(AssertionError):self.run_export()

    def test_continued_checkpoint_preserves_prefix_and_counts_once(self):
        self.checkpoint();self.checkpoint_report=copy.deepcopy(self.extension)
        self.extension['targetReturns']=24
        self.extension['batches']=[dict(priorReport='pacing-campaign-balance146-checkpoint.json',afterRun=12,endRun=24)]
        for n in range(13,25):
            self.extension['runs'].append(dict(run=n,seed=str(n),status='finished',settledId=str(n),generation=n+3,
                crownsBefore=(n-1)*10,crownsAfter=n*10,earned=10,lastState=dict(floor=2)))
        self.run_export()
        self.assertEqual(len((self.root/'output/trajectories.csv').read_text().splitlines()),25)

    def speed_receipt(self):
        self.campaign['animationSpeed']=1
        record=self.campaign['evidence'][0]
        record['request'].update(command='balance-campaign-step',animationSpeed=1)
        record['response']['result']['animationSpeed']=1

    def test_normal_speed_preserved(self):
        self.speed_receipt();self.run_export()
        manifest=json.loads((self.root/'output/manifest.json').read_text())
        row=next(r for r in manifest if r['trial']=='balance-campaign-balance146-fixture')
        self.assertEqual(row['animationSpeed'],1)

    def test_changed_speed_request_rejected(self):
        self.speed_receipt();self.campaign['evidence'][0]['request']['animationSpeed']=6
        with self.assertRaises(AssertionError):self.run_export()

    def test_changed_native_speed_rejected(self):
        self.speed_receipt();self.campaign['evidence'][0]['response']['result']['animationSpeed']=6
        with self.assertRaises(AssertionError):self.run_export()

    def test_invalid_speed_rejected(self):
        self.speed_receipt();self.campaign['animationSpeed']=0
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
