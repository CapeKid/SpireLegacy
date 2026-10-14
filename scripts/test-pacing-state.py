import copy
import unittest
from pacing_state import completed,assert_live,durable_snapshot,completed_prefix

class Continuity(unittest.TestCase):
    def setUp(self):
        self.report=dict(cls='knight',status='finished',purchases=[],evidence=[dict(request=dict(requestId='a'),response=dict(requestId='a',ok=True))],
                         runs=[dict(run=n,status='finished',settledId=str(n),generation=n+3,crownsBefore=(n-1)*10,crownsAfter=n*10,earned=10) for n in range(1,13)])
    def test_valid_continuity(self):
        expected=completed(self.report,'knight')
        profile=dict(crowns=120,manor={},active=None,settledId='12',generation=15)
        assert_live(expected,dict(profile=profile,persisted=copy.deepcopy(profile)))
    def test_unfinished_report(self):
        self.report['status']='error'
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_wrong_class(self):
        with self.assertRaises(ValueError):completed(self.report,'mage')
    def test_failed_command(self):
        self.report['evidence'][0]['response']['ok']=False
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_duplicate_command(self):
        self.report['evidence']*=2
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_crown_drift(self):
        self.report['runs'][3]['earned']=99
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_duplicate_settlement(self):
        self.report['runs'][4]['settledId']='1'
        with self.assertRaises(ValueError):completed(self.report,'knight')
    def test_live_drift(self):
        expected=completed(self.report,'knight')
        profile=dict(crowns=999,manor={},active=None,settledId='12',generation=15)
        with self.assertRaises(ValueError):assert_live(expected,dict(profile=profile,persisted=copy.deepcopy(profile)))
    def test_durable_fork(self):
        profile=dict(crowns=120,manor={},active=None,settledId='12',generation=15)
        record=self.report['evidence'][0];record['request']['command']='lifecycle-state'
        record['response']['result']=dict(profile=profile,persisted=copy.deepcopy(profile))
        self.assertEqual(durable_snapshot(self.report,'knight'),profile)
    def test_fork_rejects_missing_snapshot(self):
        with self.assertRaises(ValueError):durable_snapshot(self.report,'knight')
    def interrupted(self):
        self.test_durable_fork()
        self.report['status']='running'
        self.report['runs'].append(dict(run=13,status='running'))
        self.report['evidence'].append(dict(request=dict(requestId='b',command='start'),response=dict(requestId='b',ok=True)))
    def test_interruption_excludes_unsettled_run_and_mutation_tail(self):
        self.interrupted();original=copy.deepcopy(self.report)
        result=completed_prefix(self.report,'knight')
        self.assertEqual(len(result['runs']),12)
        self.assertEqual(len(result['evidence']),1)
        self.assertEqual(result['checkpointExtraction']['excludedRuns'],[13])
        self.assertEqual(durable_snapshot(result,'knight')['crowns'],120)
        self.assertEqual(self.report,original)
    def test_interruption_requires_matching_durable_settlement(self):
        self.interrupted()
        self.report['evidence'][0]['response']['result']['persisted']['crowns']=999
        with self.assertRaises(ValueError):completed_prefix(self.report,'knight')
    def paid(self, level=1):
        self.report['purchases']=[dict(run=8,upgrade='vitality',level=level,purchased=True,crownsBefore=80,crownsAfter=0)]
        for run in self.report['runs'][8:]:
            run['crownsBefore']-=80;run['crownsAfter']-=80
    def test_paid_bank_preserved(self):
        self.paid();expected=completed(self.report,'knight')
        self.assertEqual(expected['crowns'],40)
        self.assertEqual(expected['manor'],dict(vitality=1))
    def test_skipped_upgrade_level_rejected(self):
        self.paid(level=2)
        with self.assertRaises(ValueError):completed(self.report,'knight')

if __name__=='__main__':unittest.main()
